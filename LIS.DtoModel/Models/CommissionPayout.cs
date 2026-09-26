using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models
{
    [Table("CommissionPayout")]
    public class CommissionPayout
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string PayoutNo { get; set; }

        public int RecipientType { get; set; }

        public int RecipientId { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public decimal TotalAmount { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        [NotMapped]
        public string RecipientName { get; set; }
    }

    [Table("CommissionPayoutDetail")]
    public class CommissionPayoutDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long CommissionPayoutId { get; set; }

        public int RecipientType { get; set; }

        public int RecipientId { get; set; }

        public long SaleInvoiceId { get; set; }

        [MaxLength(50)]
        public string InvoiceNo { get; set; }

        public DateTime InvoiceDate { get; set; }

        public decimal CommissionBase { get; set; }

        public decimal CommissionPercent { get; set; }

        public decimal Amount { get; set; }
    }

    public class CommissionPayoutRequest
    {
        public int RecipientType { get; set; }

        public int RecipientId { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime ToDate { get; set; }
    }

    public class CommissionPayoutSchedule
    {
        public DateTime FromDate { get; set; }

        public bool IsFirstPayout { get; set; }

        public DateTime ImplementationMonth { get; set; }
    }

    public class CommissionPayoutPreview
    {
        public int RecipientType { get; set; }

        public int RecipientId { get; set; }

        public string RecipientName { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public bool IsFirstPayout { get; set; }

        public List<CommissionCalculationLine> Lines { get; set; }

        public decimal TotalAmount { get; set; }
    }

    public class CommissionPayoutMemo
    {
        public CommissionPayout Payout { get; set; }

        public List<CommissionPayoutDetail> Details { get; set; }

        public string LabName { get; set; }
    }
}
