using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models.Notification;
using LIS.Logger;
using System;
using System.Diagnostics;
using System.Linq;

namespace LIS.BusinessLogic.Notifications
{
    public class NotificationProcessor : INotificationProcessor
    {
        private readonly INotificationConfigurationManager configurationManager;
        private readonly INotificationProviderFactory providerFactory;
        private readonly ModuleRepo<NotificationAudit> auditRepo;
        private readonly ILogger logger;

        public NotificationProcessor(
            ILogger logger,
            IModuleIdentity identity,
            GenericUnitOfWork unitOfWork,
            INotificationConfigurationManager configurationManager,
            INotificationProviderFactory providerFactory)
        {
            this.logger = logger;
            this.configurationManager = configurationManager;
            this.providerFactory = providerFactory;
            auditRepo = new ModuleRepo<NotificationAudit>(logger, identity, unitOfWork);
        }

        public void ScheduleProcessing(long auditId)
        {
            NotificationBackgroundDispatcher.ScheduleProcessing(auditId);
        }

        public void ProcessPendingQueue()
        {
            if (!configurationManager.IsEnabled())
            {
                return;
            }

            var now = DateTime.Now;
            var pending = auditRepo.Get(a =>
                    (a.Status == (int)NotificationStatusType.Pending
                     || a.Status == (int)NotificationStatusType.Retrying)
                    && (!a.NextRetryOn.HasValue || a.NextRetryOn <= now))
                .OrderBy(a => a.Priority)
                .ThenBy(a => a.CreatedOn)
                .Take(50)
                .ToList();

            foreach (var audit in pending)
            {
                ProcessAuditInternal(audit);
            }
        }

        public void ProcessAudit(long auditId)
        {
            var audit = auditRepo.Get(auditId);
            if (audit == null)
            {
                return;
            }

            ProcessAuditInternal(audit);
        }

        private void ProcessAuditInternal(NotificationAudit audit)
        {
            if (audit == null)
            {
                return;
            }

            if (audit.Status == (int)NotificationStatusType.Sent
                || audit.Status == (int)NotificationStatusType.Skipped)
            {
                return;
            }

            var config = configurationManager.GetConfiguration();
            if (!config.IsEnabled)
            {
                return;
            }

            var provider = providerFactory.GetProvider((NotificationChannelType)audit.Channel);
            var maxAttempts = Math.Max(1, config.RetryCount + 1);
            var stopwatch = Stopwatch.StartNew();

            if (audit.RetryCount >= maxAttempts)
            {
                audit.Status = (int)NotificationStatusType.Failed;
                audit.ErrorMessage = audit.ErrorMessage ?? "Maximum retry count exceeded.";
                auditRepo.Update(audit);
                return;
            }

            audit.Status = audit.RetryCount > 0
                ? (int)NotificationStatusType.Retrying
                : (int)NotificationStatusType.Pending;
            auditRepo.Update(audit);

            var request = new NotificationProviderRequest
            {
                RecipientPhone = audit.RecipientPhone,
                MessageBody = audit.MessageBody,
                EventCode = audit.EventCode,
                InvoiceNo = audit.InvoiceNo,
                CorrelationId = audit.CorrelationId,
                NotificationRequest = new NotificationRequest
                {
                    Event = audit.EventCode,
                    PatientId = audit.PatientId,
                    InvoiceId = audit.InvoiceId,
                    InvoiceNo = audit.InvoiceNo,
                    Channel = (NotificationChannelType)audit.Channel,
                    TemplateId = audit.TemplateId,
                    TemplateVersion = audit.TemplateVersion,
                    RecipientPhone = audit.RecipientPhone,
                    MessageBody = audit.MessageBody,
                    Priority = audit.Priority,
                    CorrelationId = audit.CorrelationId,
                    CreatedDate = audit.CreatedOn
                }
            };

            var result = provider.Send(request);
            stopwatch.Stop();
            audit.ElapsedTimeMs = (int)stopwatch.ElapsedMilliseconds;
            audit.ProviderName = provider.ProviderName;

            if (result.Success)
            {
                audit.Status = (int)NotificationStatusType.Sent;
                audit.SentOn = DateTime.Now;
                audit.ProviderResponse = result.ProviderResponse;
                audit.ErrorMessage = null;
                audit.NextRetryOn = null;
                auditRepo.Update(audit);
                return;
            }

            audit.RetryCount = audit.RetryCount + 1;
            audit.ErrorMessage = result.ErrorMessage;
            audit.ProviderResponse = result.ProviderResponse;

            if (audit.RetryCount >= maxAttempts)
            {
                audit.Status = (int)NotificationStatusType.Failed;
                audit.NextRetryOn = null;
            }
            else
            {
                audit.Status = (int)NotificationStatusType.Retrying;
                audit.NextRetryOn = DateTime.Now.AddSeconds(Math.Max(1, config.RetryIntervalSeconds));
                auditRepo.Update(audit);
                ScheduleProcessing(audit.Id);
                return;
            }

            auditRepo.Update(audit);
            logger.LogInfo(string.Format(
                "Notification failed for audit {0}, invoice {1}, channel {2}: {3}",
                audit.Id,
                audit.InvoiceNo,
                audit.Channel,
                audit.ErrorMessage));
        }
    }
}
