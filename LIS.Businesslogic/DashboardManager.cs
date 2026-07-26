using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LIS.BusinessLogic
{
    /// <summary>
    /// Read-only aggregation for the operational dashboard. Every metric is a SQL
    /// COUNT/SUM over the existing workflow tables; nothing here writes or mutates state.
    /// </summary>
    public class DashboardManager : IDashboardManager
    {
        private const int OverdueCollectionHours = 4;
        private const int OverdueDoctorApprovalHours = 24;
        private const string FormatNumber = "number";
        private const string FormatCurrency = "currency";

        private readonly ModuleRepo<PatientDetail> patientRepo;
        private readonly ModuleRepo<PatientVisit> visitRepo;
        private readonly ModuleRepo<SaleInvoice> invoiceRepo;
        private readonly ModuleRepo<TestRequestDetail> requestRepo;
        private readonly ModuleRepo<TestResult> resultRepo;
        private readonly ModuleRepo<RadiologyRequestDetail> radiologyRepo;

        public DashboardManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
        {
            patientRepo = new ModuleRepo<PatientDetail>(logger, identity, uow);
            visitRepo = new ModuleRepo<PatientVisit>(logger, identity, uow);
            invoiceRepo = new ModuleRepo<SaleInvoice>(logger, identity, uow);
            requestRepo = new ModuleRepo<TestRequestDetail>(logger, identity, uow);
            resultRepo = new ModuleRepo<TestResult>(logger, identity, uow);
            radiologyRepo = new ModuleRepo<RadiologyRequestDetail>(logger, identity, uow);
        }

        public IEnumerable<DashboardMetric> GetRegistrationMetrics()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var newPatients = patientRepo
                .Get(p => p.IsActive && p.CreatedOn >= today && p.CreatedOn < tomorrow)
                .Count();

            var visitsToday = visitRepo
                .Get(v => v.IsActive && v.VisitDateTime >= today && v.VisitDateTime < tomorrow)
                .Count();

            return new List<DashboardMetric>
            {
                Metric("newPatients", "New Patients", newPatients),
                Metric("visitsToday", "Visits Today", visitsToday)
            };
        }

        public IEnumerable<DashboardMetric> GetBillingMetrics()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var cancelled = (int)InvoiceStatusType.Cancelled;

            var invoices = invoiceRepo.Get(i => i.IsActive
                && i.InvoiceStatus != cancelled
                && i.InvoiceDate >= today
                && i.InvoiceDate < tomorrow);

            return new List<DashboardMetric>
            {
                Metric("invoicesToday", "Invoices", invoices.Count()),
                Metric("netAmount", "Net Amount", invoices.Sum(i => (decimal?)i.NetAmount) ?? 0m, FormatCurrency),
                Metric("collected", "Collected", invoices.Sum(i => (decimal?)i.PaidAmount) ?? 0m, FormatCurrency),
                Metric("outstanding", "Outstanding", invoices.Sum(i => (decimal?)i.DueAmount) ?? 0m, FormatCurrency)
            };
        }

        public IEnumerable<DashboardMetric> GetCollectionMetrics()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var pending = requestRepo
                .Get(r => r.ReportStatus == ReportStatusType.New
                    && (r.CollectedBy == null || r.CollectedBy == ""))
                .Count();

            var collectedToday = requestRepo
                .Get(r => r.CollectedBy != null && r.CollectedBy != ""
                    && r.SampleCollectionDate >= today && r.SampleCollectionDate < tomorrow)
                .Count();

            return new List<DashboardMetric>
            {
                Metric("pendingCollection", "Pending Collection", pending),
                Metric("collectedToday", "Collected Today", collectedToday)
            };
        }

        public IEnumerable<DashboardMetric> GetReceivingMetrics()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var pending = requestRepo
                .Get(r => r.CollectedBy != null && r.CollectedBy != ""
                    && (r.ReceivedBy == null || r.ReceivedBy == "")
                    && r.ReportStatus != ReportStatusType.FinallyRejected)
                .Count();

            var receivedToday = requestRepo
                .Get(r => r.ReceivedBy != null && r.ReceivedBy != ""
                    && r.SampleReceivedDate >= today && r.SampleReceivedDate < tomorrow)
                .Count();

            return new List<DashboardMetric>
            {
                Metric("pendingReceiving", "Pending Receiving", pending),
                Metric("receivedToday", "Received Today", receivedToday)
            };
        }

        public IEnumerable<DashboardMetric> GetLaboratoryMetrics()
        {
            var pendingResultEntry = requestRepo
                .Get(r => r.ReceivedBy != null && r.ReceivedBy != ""
                    && r.ReportStatus == ReportStatusType.New)
                .Count();

            var onAnalyzer = requestRepo
                .Get(r => r.ReportStatus == ReportStatusType.SentToEquipment)
                .Count();

            var pendingTechnicianApproval = requestRepo
                .Get(r => r.ReportStatus == ReportStatusType.ReportGenerated)
                .Count();

            return new List<DashboardMetric>
            {
                Metric("pendingResultEntry", "Pending Result Entry", pendingResultEntry),
                Metric("onAnalyzer", "On Analyzer", onAnalyzer),
                Metric("pendingTechnicianApproval", "Pending Tech Approval", pendingTechnicianApproval)
            };
        }

        public IEnumerable<DashboardMetric> GetDoctorApprovalMetrics()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var pending = requestRepo
                .Get(r => r.ReportStatus == ReportStatusType.TechnicianApproved)
                .Count();

            // AuthorizationDate is stamped when a doctor approves or rejects a result.
            var approvedToday = resultRepo
                .Get(r => r.AuthorizationDate >= today && r.AuthorizationDate < tomorrow
                    && r.TestRequestDetail.ReportStatus == ReportStatusType.DoctorApproved)
                .Select(r => r.TestRequestId)
                .Distinct()
                .Count();

            var rejectedToday = resultRepo
                .Get(r => r.AuthorizationDate >= today && r.AuthorizationDate < tomorrow
                    && r.TestRequestDetail.ReportStatus == ReportStatusType.DoctorRejected)
                .Select(r => r.TestRequestId)
                .Distinct()
                .Count();

            return new List<DashboardMetric>
            {
                Metric("pendingDoctorApproval", "Pending Approval", pending),
                Metric("approvedToday", "Approved Today", approvedToday),
                Metric("rejectedToday", "Rejected Today", rejectedToday)
            };
        }

        public IEnumerable<DashboardMetric> GetRadiologyMetrics()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var pending = radiologyRepo
                .Get(r => r.ReportStatus == RadiologyReportStatus.Pending
                    || r.ReportStatus == RadiologyReportStatus.Draft
                    || r.ReportStatus == RadiologyReportStatus.UnderReview)
                .Count();

            var authorizedToday = radiologyRepo
                .Get(r => (r.ReportStatus == RadiologyReportStatus.Authorized
                        || r.ReportStatus == RadiologyReportStatus.Released)
                    && r.ModifiedOn >= today && r.ModifiedOn < tomorrow)
                .Count();

            return new List<DashboardMetric>
            {
                Metric("pendingRadiology", "Pending Reports", pending),
                Metric("authorizedToday", "Authorized Today", authorizedToday)
            };
        }

        public DashboardQueue GetPendingCollectionQueue(int take)
        {
            var pending = requestRepo.Get(r => r.ReportStatus == ReportStatusType.New
                && (r.CollectedBy == null || r.CollectedBy == ""));

            // Oldest first: the longest-waiting sample is the one that needs action.
            var rows = pending
                .OrderBy(r => r.CreatedOn)
                .Take(take)
                .Select(r => new { r.PatientId, r.SampleNo, r.HISTestName, r.CreatedOn })
                .ToList();

            var names = ResolvePatientNames(rows.Select(r => r.PatientId));

            return new DashboardQueue
            {
                TotalRecord = pending.Count(),
                Items = rows.Select(r => new DashboardQueueItem
                {
                    Reference = r.SampleNo,
                    PatientName = LookupName(names, r.PatientId),
                    Description = r.HISTestName,
                    OrderedOn = r.CreatedOn
                }).ToList()
            };
        }

        public DashboardQueue GetPendingRadiologyQueue(int take)
        {
            var pending = radiologyRepo.Get(r => r.ReportStatus == RadiologyReportStatus.Pending
                || r.ReportStatus == RadiologyReportStatus.Draft
                || r.ReportStatus == RadiologyReportStatus.UnderReview);

            var rows = pending
                .OrderBy(r => r.CreatedOn)
                .Take(take)
                .Select(r => new { r.PatientId, r.AccessionNo, r.Modality, r.CreatedOn })
                .ToList();

            var names = ResolvePatientNames(rows.Select(r => r.PatientId));

            return new DashboardQueue
            {
                TotalRecord = pending.Count(),
                Items = rows.Select(r => new DashboardQueueItem
                {
                    Reference = r.AccessionNo,
                    PatientName = LookupName(names, r.PatientId),
                    Description = r.Modality,
                    OrderedOn = r.CreatedOn
                }).ToList()
            };
        }

        /// <summary>Loads only the patients referenced by the visible queue rows.</summary>
        private Dictionary<long, string> ResolvePatientNames(IEnumerable<long> patientIds)
        {
            var ids = patientIds.Distinct().ToList();
            if (!ids.Any())
            {
                return new Dictionary<long, string>();
            }

            return patientRepo
                .Get(p => ids.Contains(p.Id))
                .Select(p => new { p.Id, p.Name })
                .ToList()
                .GroupBy(p => p.Id)
                .ToDictionary(g => g.Key, g => g.First().Name);
        }

        private static string LookupName(Dictionary<long, string> names, long patientId)
        {
            string name;
            return names.TryGetValue(patientId, out name) ? name : null;
        }

        public IEnumerable<DashboardAlert> GetOperationalAlerts()
        {
            var now = DateTime.Now;
            var collectionCutoff = now.AddHours(-OverdueCollectionHours);
            var approvalCutoff = now.AddHours(-OverdueDoctorApprovalHours);

            var overdueCollection = requestRepo
                .Get(r => r.ReportStatus == ReportStatusType.New
                    && (r.CollectedBy == null || r.CollectedBy == "")
                    && r.CreatedOn < collectionCutoff)
                .Count();

            var overdueApproval = requestRepo
                .Get(r => r.ReportStatus == ReportStatusType.TechnicianApproved
                    && r.CreatedOn < approvalCutoff)
                .Count();

            var rejected = requestRepo
                .Get(r => r.ReportStatus == ReportStatusType.FinallyRejected
                    || r.ReportStatus == ReportStatusType.TechnicianRejected
                    || r.ReportStatus == ReportStatusType.DoctorRejected)
                .Count();

            var alerts = new List<DashboardAlert>();

            if (overdueCollection > 0)
            {
                alerts.Add(new DashboardAlert
                {
                    Key = "overdueCollection",
                    Severity = "critical",
                    Title = "Collection overdue",
                    Detail = string.Format("{0} sample(s) ordered more than {1} hours ago are still not collected.",
                        overdueCollection, OverdueCollectionHours),
                    Count = overdueCollection
                });
            }

            if (overdueApproval > 0)
            {
                alerts.Add(new DashboardAlert
                {
                    Key = "overdueDoctorApproval",
                    Severity = "warning",
                    Title = "Doctor approval overdue",
                    Detail = string.Format("{0} report(s) have been awaiting doctor approval for more than {1} hours.",
                        overdueApproval, OverdueDoctorApprovalHours),
                    Count = overdueApproval
                });
            }

            if (rejected > 0)
            {
                alerts.Add(new DashboardAlert
                {
                    Key = "rejectedSamples",
                    Severity = "info",
                    Title = "Rejected samples open",
                    Detail = string.Format("{0} sample(s) are currently in a rejected state and may need recollection.", rejected),
                    Count = rejected
                });
            }

            return alerts;
        }

        private static DashboardMetric Metric(string key, string label, decimal value, string format = FormatNumber)
        {
            return new DashboardMetric
            {
                Key = key,
                Label = label,
                Value = value,
                Format = format
            };
        }
    }
}
