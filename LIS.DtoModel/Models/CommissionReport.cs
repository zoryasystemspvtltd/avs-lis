using System.Collections.Generic;

namespace LIS.DtoModel.Models
{
    public class CommissionSummaryRow
    {
        public CommissionRecipientType RecipientType { get; set; }

        public int RecipientId { get; set; }

        public string RecipientName { get; set; }

        public int InvoiceCount { get; set; }

        public decimal Amount { get; set; }
    }

    /// <summary>One format only. Summary and Detail are not both populated.</summary>
    public class CommissionReportPage
    {
        public string Format { get; set; }

        public int TotalRecord { get; set; }

        public decimal TotalAmount { get; set; }

        public List<CommissionSummaryRow> Summary { get; set; }

        public List<CommissionCalculationLine> Detail { get; set; }
    }
}
