using LIS.DataAccess.Repo;

using LIS.DtoModel;

using LIS.DtoModel.Interfaces;

using LIS.DtoModel.Models;

using LIS.DtoModel.Models.Notification;

using LIS.Logger;

using System;

using System.Collections.Generic;

using System.Linq;



namespace LIS.BusinessLogic.Notifications

{

    public class NotificationManager : INotificationManager

    {

        private readonly ModuleRepo<NotificationAudit> auditRepo;

        private readonly ModuleRepo<NotificationTemplate> templateRepo;

        private readonly ModuleRepo<SecureLinkToken> tokenRepo;

        private readonly ModuleRepo<SaleInvoice> invoiceRepo;

        private readonly ModuleRepo<TestRequestDetail> requestRepo;

        private readonly ModuleRepo<PatientDetail> patientRepo;

        private readonly INotificationConfigurationManager configurationManager;

        private readonly INotificationEventManager eventManager;

        private readonly INotificationTemplateManager templateManager;

        private readonly INotificationProviderFactory providerFactory;

        private readonly IModuleIdentity identity;

        private readonly ILogger logger;



        public NotificationManager(

            ILogger logger,

            IModuleIdentity identity,

            GenericUnitOfWork unitOfWork,

            INotificationConfigurationManager configurationManager,

            INotificationEventManager eventManager,

            INotificationTemplateManager templateManager,

            INotificationProviderFactory providerFactory)

        {

            auditRepo = new ModuleRepo<NotificationAudit>(logger, identity, unitOfWork);

            templateRepo = new ModuleRepo<NotificationTemplate>(logger, identity, unitOfWork);

            tokenRepo = new ModuleRepo<SecureLinkToken>(logger, identity, unitOfWork);

            invoiceRepo = new ModuleRepo<SaleInvoice>(logger, identity, unitOfWork);

            requestRepo = new ModuleRepo<TestRequestDetail>(logger, identity, unitOfWork);

            patientRepo = new ModuleRepo<PatientDetail>(logger, identity, unitOfWork);

            this.configurationManager = configurationManager;

            this.eventManager = eventManager;

            this.templateManager = templateManager;

            this.providerFactory = providerFactory;

            this.identity = identity;

            this.logger = logger;



            NotificationTemplateStore.SeedDefaultsIfMissing(templateRepo.Get().ToList());

        }



        public void RaiseNotificationEvent(ReportReleasedNotificationContext context)

        {

            if (context == null || string.IsNullOrWhiteSpace(context.InvoiceNo))

            {

                return;

            }



            if (!configurationManager.IsEnabled())

            {

                return;

            }



            if (!AreAllInvoiceTestsDoctorApproved(context.InvoiceNo))

            {

                return;

            }



            var invoice = invoiceRepo.Get(i => i.InvoiceNo == context.InvoiceNo).FirstOrDefault();

            if (invoice == null)

            {

                logger.LogInfo("Notification skipped: invoice not found for " + context.InvoiceNo);

                return;

            }



            var eventCode = eventManager.ResolveEventCode(invoice.PaymentStatus);

            if (!eventManager.IsKnownEvent(eventCode))

            {

                logger.LogInfo("Notification skipped: unknown event for invoice " + context.InvoiceNo);

                return;

            }



            var patient = patientRepo.Get(invoice.PatientId);

            if (patient == null)

            {

                logger.LogInfo("Notification skipped: patient not found for invoice " + context.InvoiceNo);

                return;

            }



            var config = configurationManager.GetConfiguration();

            var channels = providerFactory.GetProvidersForConfiguration(config).ToList();

            if (!channels.Any())

            {

                return;

            }



            var correlationId = Guid.NewGuid().ToString("N");

            var includeDownloadLink = eventCode == NotificationEventCodes.ReportReadyPaid;

            string secureToken = null;

            if (includeDownloadLink)

            {

                secureToken = CreateSecureLinkToken(context.InvoiceNo, patient.Id);

            }



            var placeholderContext = BuildPlaceholderContext(invoice, patient, secureToken, includeDownloadLink);



            foreach (var provider in channels)

            {

                if (HasSuccessfulNotification(context.InvoiceNo, eventCode, provider.Channel))

                {

                    continue;

                }



                var template = templateManager.GetActiveTemplate(eventCode, provider.Channel);

                var messageBody = templateManager.ResolveTemplate(template?.Body, placeholderContext);

                var audit = new NotificationAudit

                {

                    PatientId = patient.Id,

                    PatientName = patient.Name,

                    InvoiceId = invoice.Id,

                    InvoiceNo = context.InvoiceNo,

                    EventCode = eventCode,

                    Channel = (int)provider.Channel,

                    TemplateId = template?.Id,

                    TemplateVersion = template?.Version,

                    ProviderName = provider.ProviderName,

                    RecipientPhone = patient.Phone,

                    MessageBody = messageBody,

                    SecureLinkToken = secureToken,

                    CorrelationId = correlationId,

                    Status = (int)NotificationStatusType.Pending,

                    RetryCount = 0,

                    Priority = 0

                };



                if (string.IsNullOrWhiteSpace(patient.Phone))

                {

                    audit.Status = (int)NotificationStatusType.Skipped;

                    audit.ErrorMessage = "Patient phone number is missing.";

                    Enqueue(audit);

                    continue;

                }



                var auditId = Enqueue(audit);

                NotificationBackgroundDispatcher.ScheduleProcessing(auditId);

            }

        }



        public long Enqueue(NotificationAudit audit)

        {

            if (audit == null)

            {

                throw new ArgumentNullException(nameof(audit));

            }



            audit.CreatedOn = DateTime.Now;

            audit.CreatedBy = identity?.ActivityMember ?? "system";

            if (audit.Status != (int)NotificationStatusType.Skipped

                && audit.Status != (int)NotificationStatusType.Sent

                && audit.Status != (int)NotificationStatusType.Failed)

            {

                audit.Status = (int)NotificationStatusType.Pending;

            }



            return auditRepo.Add(audit);

        }



        public void UpdateAudit(NotificationAudit audit)

        {

            if (audit == null)

            {

                return;

            }



            auditRepo.Update(audit);

        }



        public bool HasSuccessfulNotification(string invoiceNo, string eventCode, NotificationChannelType channel)

        {

            if (string.IsNullOrWhiteSpace(invoiceNo) || string.IsNullOrWhiteSpace(eventCode))

            {

                return false;

            }



            return auditRepo.Get(a =>

                    a.InvoiceNo == invoiceNo

                    && a.EventCode == eventCode

                    && a.Channel == (int)channel

                    && a.Status == (int)NotificationStatusType.Sent)

                .Any();

        }



        public IEnumerable<NotificationAuditDto> GetAuditHistory(int take = 100)

        {

            var templates = templateRepo.Get().ToList();

            if (!templates.Any())

            {

                templates = NotificationTemplateStore.BuildDefaultTemplates();

            }



            return auditRepo.Get()

                .OrderByDescending(a => a.CreatedOn)

                .Take(take)

                .ToList()

                .Select(a => ToDto(a, templates));

        }



        public string CreateSecureLinkToken(string invoiceNo, long? patientId)

        {

            var tokenValue = Guid.NewGuid().ToString("N");

            var now = DateTime.Now;

            var token = new SecureLinkToken

            {

                Token = tokenValue,

                InvoiceNo = invoiceNo,

                PatientId = patientId,

                ExpiresOn = now.AddDays(7),

                IsUsed = false,

                CreatedOn = now,

                CreatedBy = identity?.ActivityMember ?? "system"

            };



            tokenRepo.Add(token);

            return tokenValue;

        }



        private bool AreAllInvoiceTestsDoctorApproved(string invoiceNo)

        {

            var requests = requestRepo.Get(r => r.HISRequestNo == invoiceNo).ToList();

            if (!requests.Any())

            {

                return false;

            }



            return requests.All(r => r.ReportStatus == ReportStatusType.DoctorApproved);

        }



        private NotificationPlaceholderContext BuildPlaceholderContext(

            SaleInvoice invoice,

            PatientDetail patient,

            string secureToken,

            bool includeDownloadLink)

        {

            var baseUrl = NotificationSettings.Get(NotificationSettings.SecureLinkBaseUrlKey);

            if (string.IsNullOrWhiteSpace(baseUrl))

            {

                baseUrl = "https://portal.zoryalms.local/api/report/download";

            }



            var organization = NotificationSettings.Get(NotificationSettings.OrganizationNameKey);

            if (string.IsNullOrWhiteSpace(organization))

            {

                organization = NotificationSettings.Get("Report:LabName");

            }



            var now = DateTime.Now;

            var downloadLink = string.Empty;

            if (includeDownloadLink && !string.IsNullOrWhiteSpace(secureToken))

            {

                downloadLink = string.Format("{0}/{1}", baseUrl.TrimEnd('/'), secureToken);

            }



            return new NotificationPlaceholderContext

            {

                PatientName = patient.Name,

                PatientNo = patient.HisPatientId,

                MRNo = patient.MRNo,

                VisitId = patient.VisitId,

                InvoiceNo = invoice.InvoiceNo,

                OutstandingAmount = invoice.DueAmount.ToString("0.00"),

                ReportType = "Laboratory",

                OrganizationName = organization,

                ReportDownloadLink = downloadLink,

                CurrentDate = now,

                CurrentTime = now

            };

        }



        private static NotificationAuditDto ToDto(NotificationAudit audit, List<NotificationTemplate> templates)

        {

            var template = templates.FirstOrDefault(t => t.Id == audit.TemplateId);

            return new NotificationAuditDto

            {

                Id = audit.Id,

                PatientId = audit.PatientId,

                PatientName = audit.PatientName,

                InvoiceId = audit.InvoiceId,

                InvoiceNo = audit.InvoiceNo,

                EventCode = audit.EventCode,

                Channel = ((NotificationChannelType)audit.Channel).ToString(),

                ProviderName = audit.ProviderName,

                TemplateName = template?.Name,

                TemplateVersion = audit.TemplateVersion,

                RecipientPhone = audit.RecipientPhone,

                Status = ((NotificationStatusType)audit.Status).ToString(),

                RetryCount = audit.RetryCount,

                ElapsedTimeMs = audit.ElapsedTimeMs,

                CorrelationId = audit.CorrelationId,

                ProviderResponse = audit.ProviderResponse,

                ErrorMessage = audit.ErrorMessage,

                CreatedOn = audit.CreatedOn,

                SentOn = audit.SentOn

            };

        }

    }

}


