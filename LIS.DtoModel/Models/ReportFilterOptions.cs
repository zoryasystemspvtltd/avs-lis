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
    }
}
