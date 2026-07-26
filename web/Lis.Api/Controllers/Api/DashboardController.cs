using Lis.Api.Providers;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace Lis.Api.Controllers.Api
{
    /// <summary>
    /// Read-only KPI endpoints for the operational dashboard. Each action is scoped to the
    /// same module permission as the widget that renders it, so hiding a widget in the UI
    /// cannot be bypassed by calling the API directly.
    /// </summary>
    [RoutePrefix("api/Dashboard")]
    public class DashboardController : ApiController
    {
        private const int DefaultQueueSize = 5;
        private const int MaxQueueSize = 20;

        private readonly IDashboardManager dashboardManager;
        private readonly ILogger logger;

        public DashboardController(IDashboardManager dashboardManager, ILogger logger)
        {
            this.dashboardManager = dashboardManager;
            this.logger = logger;
        }

        [HttpGet]
        [Route("Registration")]
        [QAuthorize(ModuleName = "PatientDetails", AlternateModuleName = "Masters", ModulePermissionTypes = ModulePermissionType.CanView)]
        public IEnumerable<DashboardMetric> GetRegistration()
        {
            return Metrics(dashboardManager.GetRegistrationMetrics);
        }

        [HttpGet]
        [Route("Billing")]
        [QAuthorize(ModuleName = "SaleInvoices", ModulePermissionTypes = ModulePermissionType.CanView)]
        public IEnumerable<DashboardMetric> GetBilling()
        {
            return Metrics(dashboardManager.GetBillingMetrics);
        }

        [HttpGet]
        [Route("Collection")]
        [QAuthorize(ModuleName = "SampleCollection", AlternateModuleName = "Samples", ModulePermissionTypes = ModulePermissionType.CanView)]
        public IEnumerable<DashboardMetric> GetCollection()
        {
            return Metrics(dashboardManager.GetCollectionMetrics);
        }

        [HttpGet]
        [Route("Receiving")]
        [QAuthorize(ModuleName = "SampleReceiving", AlternateModuleName = "Samples", ModulePermissionTypes = ModulePermissionType.CanView)]
        public IEnumerable<DashboardMetric> GetReceiving()
        {
            return Metrics(dashboardManager.GetReceivingMetrics);
        }

        [HttpGet]
        [Route("Laboratory")]
        [QAuthorize(ModuleName = "Reports", AlternateModuleName = "Samples", ModulePermissionTypes = ModulePermissionType.CanView)]
        public IEnumerable<DashboardMetric> GetLaboratory()
        {
            return Metrics(dashboardManager.GetLaboratoryMetrics);
        }

        [HttpGet]
        [Route("DoctorApproval")]
        [QAuthorize(ModuleName = "DoctorsApprovals", ModulePermissionTypes = ModulePermissionType.CanView)]
        public IEnumerable<DashboardMetric> GetDoctorApproval()
        {
            return Metrics(dashboardManager.GetDoctorApprovalMetrics);
        }

        [HttpGet]
        [Route("Radiology")]
        [QAuthorize(ModuleName = "RadiologyReports", AlternateModuleName = "RadiologyDoctorApprovals", ModulePermissionTypes = ModulePermissionType.CanView)]
        public IEnumerable<DashboardMetric> GetRadiology()
        {
            return Metrics(dashboardManager.GetRadiologyMetrics);
        }

        [HttpGet]
        [Route("PendingCollectionQueue")]
        [QAuthorize(ModuleName = "SampleCollection", AlternateModuleName = "Samples,Reports", ModulePermissionTypes = ModulePermissionType.CanView)]
        public DashboardQueue GetPendingCollectionQueue(int take = DefaultQueueSize)
        {
            return Queue(() => dashboardManager.GetPendingCollectionQueue(NormalizeTake(take)));
        }

        [HttpGet]
        [Route("PendingRadiologyQueue")]
        [QAuthorize(ModuleName = "RadiologyReports", AlternateModuleName = "RadiologyDoctorApprovals", ModulePermissionTypes = ModulePermissionType.CanView)]
        public DashboardQueue GetPendingRadiologyQueue(int take = DefaultQueueSize)
        {
            return Queue(() => dashboardManager.GetPendingRadiologyQueue(NormalizeTake(take)));
        }

        [HttpGet]
        [Route("Alerts")]
        [QAuthorize(ModuleName = "Samples", AlternateModuleName = "Reports", ModulePermissionTypes = ModulePermissionType.CanView)]
        public IEnumerable<DashboardAlert> GetAlerts()
        {
            try
            {
                return dashboardManager.GetOperationalAlerts();
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                return new List<DashboardAlert>();
            }
        }

        /// <summary>A failing KPI group renders as an empty widget instead of breaking the dashboard.</summary>
        private IEnumerable<DashboardMetric> Metrics(Func<IEnumerable<DashboardMetric>> action)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                return new List<DashboardMetric>();
            }
        }

        private DashboardQueue Queue(Func<DashboardQueue> action)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                return new DashboardQueue { TotalRecord = 0, Items = new List<DashboardQueueItem>() };
            }
        }

        /// <summary>Caps the page size so the endpoint cannot be used to pull large result sets.</summary>
        private static int NormalizeTake(int take)
        {
            if (take <= 0)
            {
                return DefaultQueueSize;
            }
            return take > MaxQueueSize ? MaxQueueSize : take;
        }
    }
}
