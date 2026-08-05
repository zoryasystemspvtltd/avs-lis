namespace LIS.DtoModel.Models.Notification
{
    public enum NotificationChannelType
    {
        Sms = 1,
        WhatsApp = 2
    }

    public enum NotificationChannelMode
    {
        Sms = 1,
        WhatsApp = 2,
        Both = 3
    }

    public enum NotificationStatusType
    {
        Pending = 0,
        Sent = 1,
        Failed = 2,
        Retrying = 3,
        Skipped = 4
    }

    public static class NotificationEventCodes
    {
        public const string ReportReadyPaid = "ReportReadyPaid";
        public const string ReportReadyPartialPayment = "ReportReadyPartialPayment";
    }
}
