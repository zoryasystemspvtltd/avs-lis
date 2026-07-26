using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    /// <summary>
    /// Read-only aggregation for the operational dashboard. Each method covers a
    /// single permission boundary so every widget can be authorized independently.
    /// </summary>
    public interface IDashboardManager
    {
        IEnumerable<DashboardMetric> GetRegistrationMetrics();

        IEnumerable<DashboardMetric> GetBillingMetrics();

        IEnumerable<DashboardMetric> GetCollectionMetrics();

        IEnumerable<DashboardMetric> GetReceivingMetrics();

        IEnumerable<DashboardMetric> GetLaboratoryMetrics();

        IEnumerable<DashboardMetric> GetDoctorApprovalMetrics();

        IEnumerable<DashboardMetric> GetRadiologyMetrics();

        DashboardQueue GetPendingCollectionQueue(int take);

        DashboardQueue GetPendingRadiologyQueue(int take);

        IEnumerable<DashboardAlert> GetOperationalAlerts();
    }
}
