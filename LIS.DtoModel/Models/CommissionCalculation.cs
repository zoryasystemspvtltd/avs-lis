using System;
using System.Collections.Generic;

namespace LIS.DtoModel.Models
{
    public enum CommissionRecipientType
    {
        ReferralDoctor = 1,
        SampleCollector = 2
    }

    /// <summary>
    /// Read-only calculation request. Reports and payout call this. Invoice save does not.
    /// </summary>
    public class CommissionCalculationRequest
    {
        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public CommissionRecipientType? RecipientType { get; set; }

        public int? RecipientId { get; set; }
    }

    public class CommissionCalculationLine
    {
        public long SaleInvoiceId { get; set; }

        public string InvoiceNo { get; set; }

        public DateTime InvoiceDate { get; set; }

        public CommissionRecipientType RecipientType { get; set; }

        public int RecipientId { get; set; }

        public string RecipientName { get; set; }

        public decimal CommissionBase { get; set; }

        public decimal CommissionPercent { get; set; }

        public decimal Amount { get; set; }
    }

    public class CommissionCalculationResult
    {
        public List<CommissionCalculationLine> Lines { get; set; }

        /// <summary>Sum of the already-rounded line amounts.</summary>
        public decimal TotalAmount { get; set; }
    }
}
