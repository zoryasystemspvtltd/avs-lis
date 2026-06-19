using System;

namespace LIS.DtoModel.Models
{
    public class ReportFilterOptions : ListOptions
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public long? PatientId { get; set; }
        public int? ReferralDoctorId { get; set; }
        public string InvoiceNo { get; set; }
        public string BarcodeNumber { get; set; }
        public string OrderNumber { get; set; }
        public string PatientName { get; set; }
        public string CollectorName { get; set; }
        public string Modality { get; set; }
        public string QueueType { get; set; }
    }
}
