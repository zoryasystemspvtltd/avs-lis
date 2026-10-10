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

        /// <summary>
        /// Reports set this so a missing rate is listed and other invoices continue.
        /// Payout leaves it false and fails closed.
        /// </summary>
        public bool ReportMissingRatesAsExclusions { get; set; }
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

        /// <summary>Sum of the already-rounded line amounts. Excluded invoices are not included.</summary>
        public decimal TotalAmount { get; set; }

        public List<CommissionRateExclusion> Exclusions { get; set; }
    }

    /// <summary>
    /// An eligible invoice omitted from commission totals because no active rate covers its invoice date.
    /// This is not a zero-percent rate and has no commission amount.
    /// </summary>
    public class CommissionRateExclusion
    {
        public long SaleInvoiceId { get; set; }

        public string InvoiceNo { get; set; }

        public DateTime InvoiceDate { get; set; }

        public CommissionRecipientType RecipientType { get; set; }

        public int RecipientId { get; set; }

        public string RecipientName { get; set; }

        public string Reason { get; set; }
    }
}
