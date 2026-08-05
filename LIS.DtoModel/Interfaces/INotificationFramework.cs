using LIS.DtoModel.Models.Notification;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface INotificationHttpTransport
    {
        NotificationHttpResponse Send(NotificationHttpRequest request);
    }

    public interface INotificationProcessor
    {
        void ProcessPendingQueue();
        void ProcessAudit(long auditId);
        void ScheduleProcessing(long auditId);
    }

    public interface INotificationSecureDownloadManager
    {
        SecureReportDownloadResult ValidateAndGetReport(string token);
        void MarkTokenUsed(string token);
    }

    public interface INotificationProvider
    {
        NotificationChannelType Channel { get; }
        string ProviderName { get; }
        NotificationProviderResult Send(NotificationProviderRequest request);
    }

    public interface INotificationProviderFactory
    {
        INotificationProvider GetProvider(NotificationChannelType channel);
        IEnumerable<INotificationProvider> GetProvidersForConfiguration(NotificationConfiguration config);
    }

    public interface INotificationConfigurationManager
    {
        NotificationConfiguration GetConfiguration();
        NotificationConfigurationDto GetConfigurationDto();
        void SaveConfiguration(NotificationConfigurationDto dto);
        bool IsEnabled();
    }

    public interface INotificationTemplateManager
    {
        NotificationTemplate GetActiveTemplate(string eventCode, NotificationChannelType channel);
        string ResolveTemplate(string templateBody, NotificationPlaceholderContext context);
        IEnumerable<NotificationTemplateDto> GetAllTemplates();
    }

    public interface INotificationEventManager
    {
        string ResolveEventCode(int paymentStatus);
        bool IsKnownEvent(string eventCode);
    }

    public interface INotificationManager
    {
        void RaiseNotificationEvent(ReportReleasedNotificationContext context);
        long Enqueue(NotificationAudit audit);
        void UpdateAudit(NotificationAudit audit);
        bool HasSuccessfulNotification(string invoiceNo, string eventCode, NotificationChannelType channel);
        IEnumerable<NotificationAuditDto> GetAuditHistory(int take = 100);
        string CreateSecureLinkToken(string invoiceNo, long? patientId);
    }

    public interface INotificationService
    {
        void EnqueueReportReleasedNotification(ReportReleasedNotificationContext context);
    }
}
