using System;
using System.Collections.Generic;

namespace LIS.DtoModel.Models
{
    /// <summary>
    /// Single KPI value rendered by the dashboard KPI widget. Kept intentionally
    /// generic so one Angular widget can render every KPI group.
    /// </summary>
    public class DashboardMetric
    {
        public string Key { get; set; }

        public string Label { get; set; }

        public decimal Value { get; set; }

        /// <summary>Rendering hint for the UI: "number" or "currency".</summary>
        public string Format { get; set; }
    }

    /// <summary>One pending-work row rendered by the dashboard queue widget.</summary>
    public class DashboardQueueItem
    {
        /// <summary>Sample number or accession number.</summary>
        public string Reference { get; set; }

        public string PatientName { get; set; }

        /// <summary>Test name or modality.</summary>
        public string Description { get; set; }

        public DateTime OrderedOn { get; set; }
    }

    /// <summary>Head of a pending-work queue plus the full pending count.</summary>
    public class DashboardQueue
    {
        public int TotalRecord { get; set; }

        public IEnumerable<DashboardQueueItem> Items { get; set; }
    }

    /// <summary>Operational alert rendered by the dashboard alerts widget.</summary>
    public class DashboardAlert
    {
        public string Key { get; set; }

        /// <summary>"critical", "warning" or "info".</summary>
        public string Severity { get; set; }

        public string Title { get; set; }

        public string Detail { get; set; }

        public int Count { get; set; }
    }
}
