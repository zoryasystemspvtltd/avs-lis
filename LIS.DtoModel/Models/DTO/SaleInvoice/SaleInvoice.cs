using Newtonsoft.Json;
using System;
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
        //Invoice Information
        [Required]
        [MaxLength(50)]
        public string InvoiceNo { get; set; }
        [Required]
        public DateTime InvoiceDate { get; set; }
        [ForeignKey("TestRequestDetail")]
        public long RequestDetailId { get; set; }
        [JsonIgnore]
        public virtual TestRequestDetail TestRequestDetail { get; set; }

        //Patient Information
        [ForeignKey("PatientDetail")]
        public long PatientId { get; set; }
        [JsonIgnore]
        public virtual PatientDetail PatientDetail { get; set; }

        //Billing Information
        public decimal GrossAmount { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;
        public decimal TaxAmount { get; set; } = 0;
        public decimal NetAmount { get; set; } = 0;
        public decimal PaidAmount { get; set; } = 0;
        public decimal DueAmount { get; set; } = 0;
        public string RefDoctorName { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsActive { get; set; }
    }
}
