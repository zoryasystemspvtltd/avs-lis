using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models
{
    [Table("SaleInvoice")]
    public class SaleInvoice
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string InvoiceNo { get; set; }

        [Required]
        public DateTime InvoiceDate { get; set; }

        public int InvoiceStatus { get; set; }

        public int PaymentStatus { get; set; }

        [ForeignKey("TestRequestDetail")]
        public long? RequestDetailId { get; set; }

        [JsonIgnore]
        public virtual TestRequestDetail TestRequestDetail { get; set; }

        [ForeignKey("PatientDetail")]
        public long PatientId { get; set; }

        [JsonIgnore]
        public virtual PatientDetail PatientDetail { get; set; }

        public decimal GrossAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal NetAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal DueAmount { get; set; }

        public string RefDoctorName { get; set; }

        public int? ReferralDoctorId { get; set; }

        public int? CorporateId { get; set; }

        public string Notes { get; set; }

        public DateTime CreatedOn { get; set; }

        public string CreatedBy { get; set; }

        public DateTime ModifiedOn { get; set; }

        public string ModifiedBy { get; set; }

        public bool IsActive { get; set; }

        [NotMapped]
        public string PatientName { get; set; }

        [NotMapped]
        public string PatientPhone { get; set; }

        [NotMapped]
        public virtual ICollection<SaleInvoiceDetail> Details { get; set; }
    }
}
