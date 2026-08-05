namespace LIS.DtoModel.Models.Notification
{
    public class NotificationHttpRequest
    {
        public string Url { get; set; }
        public string Method { get; set; }
        public string Body { get; set; }
        public string ContentType { get; set; }
        public string AuthorizationHeader { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int TimeoutSeconds { get; set; }
    }

    public class NotificationHttpResponse
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string ResponseBody { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsTimeout { get; set; }
    }

    public class SecureReportDownloadResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string InvoiceNo { get; set; }
        public long? PatientId { get; set; }
        public object ReportData { get; set; }
    }
}
