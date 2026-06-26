using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models
{
    [Table("SaleInvoiceDetail")]
    public class SaleInvoiceDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        [Required]
        [ForeignKey("SaleInvoice")]
        public long SaleInvoiceId { get; set; }
        [JsonIgnore]
        public virtual SaleInvoice SaleInvoice { get; set; }

        [Required]
        [ForeignKey("HisTestMaster")]
        public int TestId { get; set; }
        [JsonIgnore]
        public virtual HisTestMaster HisTestMaster { get; set; }

        public int? TestProfileId { get; set; }

        [NotMapped]
        public string TestProfileName { get; set; }

        [NotMapped]
        public string TestName { get; set; }

        public decimal Rate { get; set; } = 0;
        public int Quantity { get; set; } = 1;
        public decimal Amount { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;
        public decimal TaxAmount { get; set; } = 0;
        public decimal NetAmount { get; set; } = 0;
        
        [ForeignKey("TestRequestDetail")]
        public long? RequestDetailId { get; set; }
        [JsonIgnore]
        public virtual TestRequestDetail TestRequestDetail { get; set; }

        /// <summary>Legacy UI field — not used for routing.</summary>
        [NotMapped]
        public string LineType { get; set; }

        [NotMapped]
        public string DepartmentCode { get; set; }

        [MaxLength(30)]
        public string SampleNo { get; set; }

        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool IsActive { get; set; }
    }
}
