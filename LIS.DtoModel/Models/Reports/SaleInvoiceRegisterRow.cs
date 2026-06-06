using System;

namespace LIS.DtoModel.Models
{
    public class SaleInvoiceRegisterRow
    {
        public long Id { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string InvoiceNo { get; set; }
        public string PatientId { get; set; }
        public string PatientName { get; set; }
        public string ReferralDoctor { get; set; }
        public string Corporate { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }
        public int InvoiceStatus { get; set; }
        public int PaymentStatus { get; set; }
        public string InvoiceStatusName { get; set; }
        public string PaymentStatusName { get; set; }
        public string CreatedBy { get; set; }
        public bool IsActive { get; set; }
    }
}
