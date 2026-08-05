using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models.Notification;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LIS.BusinessLogic.Notifications
{
    public class NotificationTemplateEngine
    {
        public static string ReplacePlaceholders(string template, NotificationPlaceholderContext context)
        {
            if (string.IsNullOrEmpty(template) || context == null)
            {
                return template ?? string.Empty;
            }

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "{PatientName}", context.PatientName ?? string.Empty },
                { "{PatientNo}", context.PatientNo ?? string.Empty },
                { "{MRNo}", context.MRNo ?? string.Empty },
                { "{VisitId}", context.VisitId ?? string.Empty },
                { "{InvoiceNo}", context.InvoiceNo ?? string.Empty },
                { "{OutstandingAmount}", context.OutstandingAmount ?? string.Empty },
                { "{ReportType}", context.ReportType ?? string.Empty },
                { "{OrganizationName}", context.OrganizationName ?? string.Empty },
                { "{ReportDownloadLink}", context.ReportDownloadLink ?? string.Empty },
                { "{CurrentDate}", context.CurrentDate.ToString("dd-MMM-yyyy") },
                { "{CurrentTime}", context.CurrentTime.ToString("hh:mm tt") }
            };

            var result = template;
            foreach (var pair in map)
            {
                result = result.Replace(pair.Key, pair.Value);
            }

            return result;
        }
    }

    public class NotificationTemplateManager : INotificationTemplateManager
    {
        private readonly ModuleRepo<NotificationTemplate> templateRepo;

        public NotificationTemplateManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork unitOfWork)
        {
            templateRepo = new ModuleRepo<NotificationTemplate>(logger, identity, unitOfWork);
            NotificationTemplateStore.SeedDefaultsIfMissing(templateRepo.Get().ToList());
        }

        public NotificationTemplate GetActiveTemplate(string eventCode, NotificationChannelType channel)
        {
            var now = DateTime.Now;
            var fromDb = templateRepo.Get(t =>
                    t.IsActive
                    && t.IsDefault
                    && t.EventCode == eventCode
                    && t.Channel == (int)channel
                    && t.EffectiveFrom <= now
                    && (!t.EffectiveTo.HasValue || t.EffectiveTo >= now))
                .OrderByDescending(t => t.Version)
                .FirstOrDefault();

            return fromDb ?? NotificationTemplateStore.GetDefaultTemplate(eventCode, channel);
        }

        public string ResolveTemplate(string templateBody, NotificationPlaceholderContext context)
        {
            return NotificationTemplateEngine.ReplacePlaceholders(templateBody, context);
        }

        public IEnumerable<NotificationTemplateDto> GetAllTemplates()
        {
            var fromDb = templateRepo.Get().ToList();
            if (fromDb.Any())
            {
                return fromDb.Select(NotificationTemplateStore.ToDto);
            }

            return NotificationTemplateStore.GetAllTemplates();
        }
    }

    public static class NotificationTemplateStore
    {
        private static readonly object Sync = new object();
        private static List<NotificationTemplate> _cache;

        public static NotificationTemplate GetDefaultTemplate(string eventCode, NotificationChannelType channel)
        {
            var templates = GetTemplates();
            var match = templates.Find(t =>
                t.IsActive
                && t.IsDefault
                && string.Equals(t.EventCode, eventCode, StringComparison.OrdinalIgnoreCase)
                && t.Channel == (int)channel);

            if (match != null)
            {
                return match;
            }

            return BuildFallbackTemplate(eventCode, channel);
        }

        public static IEnumerable<NotificationTemplateDto> GetAllTemplates()
        {
            return GetTemplates().ConvertAll(ToDto);
        }

        public static void SeedDefaultsIfMissing(IEnumerable<NotificationTemplate> existing)
        {
            var current = new List<NotificationTemplate>(existing ?? new NotificationTemplate[0]);
            var defaults = BuildDefaultTemplates();
            foreach (var template in defaults)
            {
                var found = current.Exists(t =>
                    string.Equals(t.EventCode, template.EventCode, StringComparison.OrdinalIgnoreCase)
                    && t.Channel == template.Channel
                    && t.IsDefault);
                if (!found)
                {
                    current.Add(template);
                }
            }

            lock (Sync)
            {
                _cache = current;
            }
        }

        private static List<NotificationTemplate> GetTemplates()
        {
            lock (Sync)
            {
                if (_cache == null)
                {
                    _cache = BuildDefaultTemplates();
                }

                return _cache;
            }
        }

        public static NotificationTemplateDto ToDto(NotificationTemplate template)
        {
            return new NotificationTemplateDto
            {
                Id = template.Id,
                EventCode = template.EventCode,
                Channel = template.Channel,
                Name = template.Name,
                Body = template.Body,
                IsDefault = template.IsDefault,
                IsActive = template.IsActive,
                Version = template.Version > 0 ? template.Version : 1,
                EffectiveFrom = template.EffectiveFrom == default(DateTime) ? DateTime.Now : template.EffectiveFrom,
                EffectiveTo = template.EffectiveTo
            };
        }

        private static NotificationTemplate BuildFallbackTemplate(string eventCode, NotificationChannelType channel)
        {
            var defaults = BuildDefaultTemplates();
            return defaults.Find(t =>
                string.Equals(t.EventCode, eventCode, StringComparison.OrdinalIgnoreCase)
                && t.Channel == (int)channel) ?? defaults[0];
        }

        public static List<NotificationTemplate> BuildDefaultTemplates()
        {
            var now = DateTime.Now;
            return new List<NotificationTemplate>
            {
                new NotificationTemplate
                {
                    Id = 1,
                    EventCode = NotificationEventCodes.ReportReadyPaid,
                    Channel = (int)NotificationChannelType.Sms,
                    Name = "Report Ready (Fully Paid) - SMS",
                    Body = BuildReportReadyPaidBody(),
                    IsDefault = true,
                    IsActive = true,
                    CreatedOn = now,
                    ModifiedOn = now
                },
                new NotificationTemplate
                {
                    Id = 2,
                    EventCode = NotificationEventCodes.ReportReadyPaid,
                    Channel = (int)NotificationChannelType.WhatsApp,
                    Name = "Report Ready (Fully Paid) - WhatsApp",
                    Body = BuildReportReadyPaidBody(),
                    IsDefault = true,
                    IsActive = true,
                    CreatedOn = now,
                    ModifiedOn = now
                },
                new NotificationTemplate
                {
                    Id = 3,
                    EventCode = NotificationEventCodes.ReportReadyPartialPayment,
                    Channel = (int)NotificationChannelType.Sms,
                    Name = "Outstanding Payment - SMS",
                    Body = BuildOutstandingPaymentBody(),
                    IsDefault = true,
                    IsActive = true,
                    CreatedOn = now,
                    ModifiedOn = now
                },
                new NotificationTemplate
                {
                    Id = 4,
                    EventCode = NotificationEventCodes.ReportReadyPartialPayment,
                    Channel = (int)NotificationChannelType.WhatsApp,
                    Name = "Outstanding Payment - WhatsApp",
                    Body = BuildOutstandingPaymentBody(),
                    IsDefault = true,
                    IsActive = true,
                    CreatedOn = now,
                    ModifiedOn = now
                }
            };
        }

        private static string BuildReportReadyPaidBody()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Dear {PatientName},");
            sb.AppendLine();
            sb.AppendLine("Your {ReportType} report is now available.");
            sb.AppendLine();
            sb.AppendLine("Download:");
            sb.AppendLine("{ReportDownloadLink}");
            sb.AppendLine();
            sb.AppendLine("Thank you.");
            sb.AppendLine("{OrganizationName}");
            return sb.ToString().TrimEnd();
        }

        private static string BuildOutstandingPaymentBody()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Dear {PatientName},");
            sb.AppendLine();
            sb.AppendLine("Your {ReportType} report is ready.");
            sb.AppendLine();
            sb.AppendLine("Outstanding Amount:");
            sb.AppendLine("₹ {OutstandingAmount}");
            sb.AppendLine();
            sb.AppendLine("Please complete payment to receive your report.");
            sb.AppendLine();
            sb.AppendLine("Thank you.");
            sb.AppendLine("{OrganizationName}");
            return sb.ToString().TrimEnd();
        }
    }
}
