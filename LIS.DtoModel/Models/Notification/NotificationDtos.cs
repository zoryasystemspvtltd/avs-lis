using System;
using System.Collections.Generic;

namespace LIS.DtoModel.Models.Notification
{
    public class NotificationConfigurationDto
    {
        public int Id { get; set; }
        public bool IsEnabled { get; set; }
        public bool SmsEnabled { get; set; }
        public bool WhatsAppEnabled { get; set; }
        public int ChannelMode { get; set; }
        public int RetryCount { get; set; }
        public int RetryIntervalSeconds { get; set; }
        public int DefaultChannel { get; set; }
    }

    public class NotificationTemplateDto
    {
        public int Id { get; set; }
        public string EventCode { get; set; }
        public int Channel { get; set; }
        public string Name { get; set; }
        public string Body { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
        public int Version { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
    }

    public class NotificationAuditDto
    {
        public long Id { get; set; }
        public long? PatientId { get; set; }
        public string PatientName { get; set; }
        public long? InvoiceId { get; set; }
        public string InvoiceNo { get; set; }
        public string EventCode { get; set; }
        public string Channel { get; set; }
        public string ProviderName { get; set; }
        public string TemplateName { get; set; }
        public int? TemplateVersion { get; set; }
        public string RecipientPhone { get; set; }
        public string Status { get; set; }
        public int RetryCount { get; set; }
        public int? ElapsedTimeMs { get; set; }
        public string CorrelationId { get; set; }
        public string ProviderResponse { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? SentOn { get; set; }
    }

    public class NotificationRequest
    {
        public string Event { get; set; }
        public long? PatientId { get; set; }
        public long? InvoiceId { get; set; }
        public string InvoiceNo { get; set; }
        public NotificationChannelType Channel { get; set; }
        public int? TemplateId { get; set; }
        public int? TemplateVersion { get; set; }
        public string RecipientPhone { get; set; }
        public string MessageBody { get; set; }
        public int Priority { get; set; }
        public string CorrelationId { get; set; }
        public DateTime CreatedDate { get; set; }
        public Dictionary<string, string> Payload { get; set; }
    }

    public class NotificationPlaceholderContext
    {
        public string PatientName { get; set; }
        public string PatientNo { get; set; }
        public string MRNo { get; set; }
        public string VisitId { get; set; }
        public string InvoiceNo { get; set; }
        public string OutstandingAmount { get; set; }
        public string ReportType { get; set; }
        public string OrganizationName { get; set; }
        public string ReportDownloadLink { get; set; }
        public DateTime CurrentDate { get; set; }
        public DateTime CurrentTime { get; set; }
    }

    public class NotificationProviderRequest
    {
        public NotificationRequest NotificationRequest { get; set; }
        public string RecipientPhone { get; set; }
        public string MessageBody { get; set; }
        public string EventCode { get; set; }
        public string InvoiceNo { get; set; }
        public string CorrelationId { get; set; }
    }

    public class NotificationProviderResult
    {
        public bool Success { get; set; }
        public string ProviderResponse { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ReportReleasedNotificationContext
    {
        public string InvoiceNo { get; set; }
        public long PatientId { get; set; }
        public long? TestRequestId { get; set; }
    }
}
