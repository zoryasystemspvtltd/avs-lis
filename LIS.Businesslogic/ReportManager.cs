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
    public class ReportManager : IReportManager
    {
        private readonly ModuleRepo<SaleInvoice> invoiceRepo;
        private readonly ModuleRepo<PatientDetail> patientRepo;
        private readonly ModuleRepo<ReferralDoctorMaster> doctorRepo;
        private readonly ModuleRepo<CorporateMaster> corporateRepo;
        private readonly ModuleRepo<TestRequestDetail> requestRepo;
        private readonly ModuleRepo<HisTestMaster> testRepo;
        private readonly ModuleRepo<RadiologyRequestDetail> radiologyRequestRepo;
        private readonly ModuleRepo<RadiologyResultDetail> radiologyResultRepo;

        public ReportManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
        {
            invoiceRepo = new ModuleRepo<SaleInvoice>(logger, identity, uow);
            patientRepo = new ModuleRepo<PatientDetail>(logger, identity, uow);
            doctorRepo = new ModuleRepo<ReferralDoctorMaster>(logger, identity, uow);
            corporateRepo = new ModuleRepo<CorporateMaster>(logger, identity, uow);
            requestRepo = new ModuleRepo<TestRequestDetail>(logger, identity, uow);
            testRepo = new ModuleRepo<HisTestMaster>(logger, identity, uow);
            radiologyRequestRepo = new ModuleRepo<RadiologyRequestDetail>(logger, identity, uow);
            radiologyResultRepo = new ModuleRepo<RadiologyResultDetail>(logger, identity, uow);
        }

        public ItemList<SaleInvoiceRegisterRow> GetSaleInvoiceRegister(ReportFilterOptions options)
        {
            ValidateDateRange(options);
            var from = options.FromDate.Value.Date;
            var to = options.ToDate.Value.Date.AddDays(1).AddTicks(-1);

            var patients = patientRepo.Get().ToDictionary(p => p.Id, p => p);
            var doctors = doctorRepo.Get().ToDictionary(d => d.Id, d => d);
            var corporates = corporateRepo.Get().ToDictionary(c => c.Id, c => c);

            var query = invoiceRepo.Get(i => i.InvoiceDate >= from && i.InvoiceDate <= to);

            if (options.PatientId.HasValue && options.PatientId.Value > 0)
            {
                query = query.Where(i => i.PatientId == options.PatientId.Value);
            }

            if (options.ReferralDoctorId.HasValue && options.ReferralDoctorId.Value > 0)
            {
                query = query.Where(i => i.ReferralDoctorId == options.ReferralDoctorId.Value);
            }

            var invoiceList = query.ToList();
            if (!string.IsNullOrWhiteSpace(options.InvoiceNo))
            {
                var invSearch = options.InvoiceNo.Trim();
                invoiceList = invoiceList.Where(i => i.InvoiceNo != null &&
                    i.InvoiceNo.IndexOf(invSearch, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            var rows = invoiceList.Select(i =>
            {
                patients.TryGetValue(i.PatientId, out var patient);
                string doctorName = i.RefDoctorName;
                if (string.IsNullOrWhiteSpace(doctorName) && i.ReferralDoctorId.HasValue)
                {
                    doctors.TryGetValue(i.ReferralDoctorId.Value, out var doc);
                    doctorName = doc?.Name;
                }

                string corpName = null;
                if (i.CorporateId.HasValue)
                {
                    corporates.TryGetValue(i.CorporateId.Value, out var corp);
                    corpName = corp?.Name;
                }

                return new SaleInvoiceRegisterRow
                {
                    Id = i.Id,
                    InvoiceDate = i.InvoiceDate,
                    InvoiceNo = i.InvoiceNo,
                    PatientId = patient?.HisPatientId,
                    PatientName = patient?.Name,
                    ReferralDoctor = doctorName,
                    Corporate = corpName,
                    GrossAmount = i.GrossAmount,
                    DiscountAmount = i.DiscountAmount,
                    TaxAmount = i.TaxAmount,
                    NetAmount = i.NetAmount,
                    InvoiceStatus = i.InvoiceStatus,
                    PaymentStatus = i.PaymentStatus,
                    InvoiceStatusName = FormatInvoiceStatus(i.InvoiceStatus),
                    PaymentStatusName = FormatPaymentStatus(i.PaymentStatus),
                    CreatedBy = i.CreatedBy,
                    IsActive = i.IsActive
                };
            }).ToList();

            return Paginate(rows, options, "InvoiceDate");
        }

        public ItemList<TestBookingRegisterRow> GetTestBookingRegister(ReportFilterOptions options)
        {
            ValidateDateRange(options);
            var from = options.FromDate.Value.Date;
            var to = options.ToDate.Value.Date.AddDays(1).AddTicks(-1);

            var patients = patientRepo.Get().ToDictionary(p => p.Id, p => p);
            var tests = testRepo.Get().GroupBy(t => t.HISTestCode).ToDictionary(g => g.Key, g => g.First());
            var invoices = invoiceRepo.Get()
                .Where(i => i.InvoiceNo != null)
                .GroupBy(i => i.InvoiceNo)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First());

            var requests = requestRepo.Get(r => r.SampleCollectionDate >= from && r.SampleCollectionDate <= to).ToList();

            if (options.PatientId.HasValue && options.PatientId.Value > 0)
            {
                requests = requests.Where(r => r.PatientId == options.PatientId.Value).ToList();
            }

            if (options.ReferralDoctorId.HasValue && options.ReferralDoctorId.Value > 0)
            {
                var doctorId = options.ReferralDoctorId.Value;
                requests = requests.Where(r =>
                {
                    if (string.IsNullOrEmpty(r.HISRequestNo))
                    {
                        return false;
                    }

                    return invoices.TryGetValue(r.HISRequestNo, out var inv) && inv.ReferralDoctorId == doctorId;
                }).ToList();
            }

            var rows = requests.Select(r =>
            {
                patients.TryGetValue(r.PatientId, out var patient);
                tests.TryGetValue(r.HISTestCode ?? string.Empty, out var test);

                string invoiceNo = null;
                string doctorName = null;
                if (!string.IsNullOrEmpty(r.HISRequestNo) && invoices.TryGetValue(r.HISRequestNo, out var inv))
                {
                    invoiceNo = inv.InvoiceNo;
                    doctorName = inv.RefDoctorName;
                    if (string.IsNullOrWhiteSpace(doctorName) && inv.ReferralDoctorId.HasValue)
                    {
                        var doc = doctorRepo.Get(inv.ReferralDoctorId.Value);
                        doctorName = doc?.Name;
                    }
                }

                var department = !string.IsNullOrWhiteSpace(r.Department)
                    ? r.Department
                    : test?.DepartmentCode;

                return new TestBookingRegisterRow
                {
                    Id = r.Id,
                    BookingDate = r.SampleCollectionDate,
                    RequestNumber = r.HISRequestNo,
                    InvoiceNumber = invoiceNo,
                    PatientId = patient?.HisPatientId,
                    PatientName = patient?.Name,
                    TestName = !string.IsNullOrWhiteSpace(r.HISTestName) ? r.HISTestName : test?.HISTestCodeDescription,
                    Department = department,
                    Specimen = !string.IsNullOrWhiteSpace(r.SpecimenName) ? r.SpecimenName : r.SpecimenCode,
                    ReferralDoctor = doctorName,
                    Status = FormatReportStatus(r.ReportStatus),
                    CreatedBy = r.CreatedBy,
                    SampleNo = r.SampleNo
                };
            }).ToList();

            return Paginate(rows, options, "BookingDate");
        }

        public ItemList<CollectionSummaryRow> GetCollectionSummary(ReportFilterOptions options)
        {
            ValidateDateRange(options);
            var from = options.FromDate.Value.Date;
            var to = options.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            var patients = patientRepo.Get().ToDictionary(p => p.Id, p => p);

            var rows = requestRepo.Get(r =>
                !string.IsNullOrEmpty(r.CollectedBy) &&
                r.SampleCollectionDate >= from &&
                r.SampleCollectionDate <= to).ToList()
                .Select(r =>
                {
                    patients.TryGetValue(r.PatientId, out var patient);
                    return new CollectionSummaryRow
                    {
                        Id = r.Id,
                        CollectionDate = r.SampleCollectionDate,
                        SampleNo = r.SampleNo,
                        OrderNumber = r.HISRequestNo,
                        PatientId = patient?.HisPatientId,
                        PatientName = patient?.Name,
                        TestName = r.HISTestName,
                        CollectedBy = r.CollectedBy,
                        Status = FormatCollectionStatus(r)
                    };
                }).ToList();

            if (!string.IsNullOrWhiteSpace(options.CollectorName))
            {
                var collector = options.CollectorName.Trim();
                rows = rows.Where(r => r.CollectedBy != null &&
                    r.CollectedBy.IndexOf(collector, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            return Paginate(rows, options, "CollectionDate");
        }

        public ItemList<CollectorWiseRow> GetCollectorWiseReport(ReportFilterOptions options)
        {
            ValidateDateRange(options);
            var from = options.FromDate.Value.Date;
            var to = options.ToDate.Value.Date.AddDays(1).AddTicks(-1);

            var rows = requestRepo.Get(r => r.SampleCollectionDate >= from && r.SampleCollectionDate <= to).ToList()
                .GroupBy(r => string.IsNullOrWhiteSpace(r.CollectedBy) ? "Unassigned" : r.CollectedBy)
                .Select(g => new CollectorWiseRow
                {
                    CollectorName = g.Key,
                    TotalCollected = g.Count(x => !string.IsNullOrWhiteSpace(x.CollectedBy)),
                    TotalRejected = g.Count(x => x.ReportStatus == ReportStatusType.FinallyRejected),
                    TotalRecollection = g.Count(x => x.CollectedRemarks != null && x.CollectedRemarks.IndexOf("Recollection", StringComparison.OrdinalIgnoreCase) >= 0)
                }).ToList();

            return Paginate(rows, options, "CollectorName");
        }

        public ItemList<PendingCollectionRow> GetPendingCollectionReport(ReportFilterOptions options)
        {
            var patients = patientRepo.Get().ToDictionary(p => p.Id, p => p);
            var rows = requestRepo.Get(r =>
                r.ReportStatus == ReportStatusType.New &&
                (r.CollectedBy == null || r.CollectedBy == "")).ToList()
                .Select(r =>
                {
                    patients.TryGetValue(r.PatientId, out var patient);
                    return new PendingCollectionRow
                    {
                        Id = r.Id,
                        SampleNo = r.SampleNo,
                        OrderNumber = r.HISRequestNo,
                        PatientId = patient?.HisPatientId,
                        PatientName = patient?.Name,
                        TestName = r.HISTestName,
                        OrderDate = r.CreatedOn
                    };
                }).ToList();

            return Paginate(rows, options, "OrderDate");
        }

        public ItemList<RecollectionRow> GetRecollectionReport(ReportFilterOptions options)
        {
            ValidateDateRange(options);
            var from = options.FromDate.Value.Date;
            var to = options.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            var patients = patientRepo.Get().ToDictionary(p => p.Id, p => p);

            var rows = requestRepo.Get(r =>
                r.SampleCollectionDate >= from &&
                r.SampleCollectionDate <= to &&
                r.CollectedRemarks != null &&
                r.CollectedRemarks.IndexOf("Recollection", StringComparison.OrdinalIgnoreCase) >= 0).ToList()
                .Select(r =>
                {
                    patients.TryGetValue(r.PatientId, out var patient);
                    return new RecollectionRow
                    {
                        Id = r.Id,
                        SampleNo = r.SampleNo,
                        OrderNumber = r.HISRequestNo,
                        PatientName = patient?.Name,
                        TestName = r.HISTestName,
                        CollectedBy = r.CollectedBy,
                        CollectionDate = r.SampleCollectionDate,
                        Remarks = r.CollectedRemarks
                    };
                }).ToList();

            return Paginate(rows, options, "CollectionDate");
        }

        public ItemList<ReceivedSampleRow> GetReceivedSamplesReport(ReportFilterOptions options)
        {
            ValidateDateRange(options);
            var from = options.FromDate.Value.Date;
            var to = options.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            var patients = patientRepo.Get().ToDictionary(p => p.Id, p => p);

            var rows = requestRepo.Get(r =>
                !string.IsNullOrEmpty(r.ReceivedBy) &&
                r.SampleReceivedDate >= from &&
                r.SampleReceivedDate <= to).ToList()
                .Select(r =>
                {
                    patients.TryGetValue(r.PatientId, out var patient);
                    var tat = (int)Math.Max(0, (r.SampleReceivedDate - r.SampleCollectionDate).TotalMinutes);
                    return new ReceivedSampleRow
                    {
                        Id = r.Id,
                        SampleNo = r.SampleNo,
                        PatientName = patient?.Name,
                        TestName = r.HISTestName,
                        CollectionDate = r.SampleCollectionDate,
                        ReceivedDate = r.SampleReceivedDate,
                        ReceivedBy = r.ReceivedBy,
                        TurnaroundMinutes = tat
                    };
                }).ToList();

            return Paginate(rows, options, "ReceivedDate");
        }

        public ItemList<RejectedSampleRow> GetRejectedSamplesReport(ReportFilterOptions options)
        {
            ValidateDateRange(options);
            var from = options.FromDate.Value.Date;
            var to = options.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            var patients = patientRepo.Get().ToDictionary(p => p.Id, p => p);

            var rows = requestRepo.Get(r =>
                r.ReportStatus == ReportStatusType.FinallyRejected &&
                r.CreatedOn >= from &&
                r.CreatedOn <= to).ToList()
                .Select(r =>
                {
                    patients.TryGetValue(r.PatientId, out var patient);
                    var stage = !string.IsNullOrWhiteSpace(r.ReceivedRemarks) ? "Receiving" : "Collection";
                    var reason = !string.IsNullOrWhiteSpace(r.ReceivedRemarks) ? r.ReceivedRemarks : r.CollectedRemarks;
                    return new RejectedSampleRow
                    {
                        Id = r.Id,
                        SampleNo = r.SampleNo,
                        PatientName = patient?.Name,
                        TestName = r.HISTestName,
                        RejectionReason = reason,
                        RejectedBy = !string.IsNullOrWhiteSpace(r.ReceivedBy) ? r.ReceivedBy : r.CollectedBy,
                        RejectedOn = !string.IsNullOrWhiteSpace(r.ReceivedBy) ? r.SampleReceivedDate : r.SampleCollectionDate,
                        Stage = stage
                    };
                }).ToList();

            return Paginate(rows, options, "RejectedOn");
        }

        public ItemList<SampleTurnaroundRow> GetSampleTurnaroundReport(ReportFilterOptions options)
        {
            var received = GetReceivedSamplesReport(options);
            var rows = received.Items.Select(r => new SampleTurnaroundRow
            {
                SampleNo = r.SampleNo,
                PatientName = r.PatientName,
                TestName = r.TestName,
                CollectionDate = r.CollectionDate,
                ReceivedDate = r.ReceivedDate,
                TurnaroundMinutes = r.TurnaroundMinutes
            }).ToList();

            return Paginate(rows, options, "ReceivedDate");
        }

        public ItemList<PendingRadiologyRow> GetPendingRadiologyReport(ReportFilterOptions options)
        {
            ValidateDateRange(options);
            var from = options.FromDate.Value.Date;
            var to = options.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            var patients = patientRepo.Get().ToDictionary(p => p.Id, p => p);
            var radRepo = radiologyRequestRepo;

            var rows = radRepo.Get(r =>
                r.CreatedOn >= from &&
                r.CreatedOn <= to &&
                (r.ReportStatus == RadiologyReportStatus.Pending ||
                 r.ReportStatus == RadiologyReportStatus.Draft ||
                 r.ReportStatus == RadiologyReportStatus.UnderReview)).ToList()
                .Select(r =>
                {
                    patients.TryGetValue(r.PatientId, out var patient);
                    return new PendingRadiologyRow
                    {
                        Id = r.Id,
                        AccessionNo = r.AccessionNo,
                        PatientName = patient?.Name,
                        TestName = r.HISTestName,
                        Modality = r.Modality,
                        Status = FormatRadiologyStatus(r.ReportStatus),
                        CreatedOn = r.CreatedOn
                    };
                }).ToList();

            return Paginate(rows, options, "CreatedOn");
        }

        public ItemList<AuthorizedRadiologyRow> GetAuthorizedRadiologyReport(ReportFilterOptions options)
        {
            ValidateDateRange(options);
            var from = options.FromDate.Value.Date;
            var to = options.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            var patients = patientRepo.Get().ToDictionary(p => p.Id, p => p);
            var radRepo = radiologyRequestRepo;
            var resultRadRepo = radiologyResultRepo;

            var authorized = resultRadRepo.Get(r =>
                r.AuthorizedOn.HasValue &&
                r.AuthorizedOn.Value >= from &&
                r.AuthorizedOn.Value <= to).ToList();

            var rows = authorized.Select(result =>
            {
                var request = radRepo.Get(result.RadiologyRequestId);
                patients.TryGetValue(request?.PatientId ?? 0, out var patient);
                return new AuthorizedRadiologyRow
                {
                    Id = request?.Id ?? result.RadiologyRequestId,
                    AccessionNo = request?.AccessionNo,
                    PatientName = patient?.Name,
                    TestName = request?.HISTestName,
                    Modality = request?.Modality,
                    AuthorizedBy = result.AuthorizedBy,
                    AuthorizedOn = result.AuthorizedOn,
                    Status = request != null ? FormatRadiologyStatus(request.ReportStatus) : "Authorized"
                };
            }).ToList();

            return Paginate(rows, options, "AuthorizedOn");
        }

        public ItemList<ModalityStatisticsRow> GetModalityStatisticsReport(ReportFilterOptions options)
        {
            ValidateDateRange(options);
            var from = options.FromDate.Value.Date;
            var to = options.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            var radRepo = radiologyRequestRepo;

            var rows = radRepo.Get(r => r.CreatedOn >= from && r.CreatedOn <= to).ToList()
                .GroupBy(r => string.IsNullOrWhiteSpace(r.Modality) ? "Unknown" : r.Modality)
                .Select(g => new ModalityStatisticsRow
                {
                    Modality = g.Key,
                    TotalCases = g.Count(),
                    AuthorizedCases = g.Count(x =>
                        x.ReportStatus == RadiologyReportStatus.Authorized ||
                        x.ReportStatus == RadiologyReportStatus.Released),
                    PendingCases = g.Count(x =>
                        x.ReportStatus == RadiologyReportStatus.Pending ||
                        x.ReportStatus == RadiologyReportStatus.Draft ||
                        x.ReportStatus == RadiologyReportStatus.UnderReview)
                }).ToList();

            return Paginate(rows, options, "Modality");
        }

        public ItemList<RadiologistProductivityRow> GetRadiologistProductivityReport(ReportFilterOptions options)
        {
            ValidateDateRange(options);
            var from = options.FromDate.Value.Date;
            var to = options.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            var radRepo = radiologyRequestRepo;
            var resultRadRepo = radiologyResultRepo;

            var results = resultRadRepo.Get(r =>
                r.AuthorizedOn.HasValue &&
                r.AuthorizedOn.Value >= from &&
                r.AuthorizedOn.Value <= to).ToList();

            var requestMap = radRepo.Get().ToDictionary(r => r.Id, r => r);
            var rows = results.GroupBy(r => string.IsNullOrWhiteSpace(r.AuthorizedBy) ? "Unknown" : r.AuthorizedBy)
                .Select(g => new RadiologistProductivityRow
                {
                    RadiologistName = g.Key,
                    AuthorizedCount = g.Count(),
                    ReleasedCount = g.Count(x =>
                        requestMap.TryGetValue(x.RadiologyRequestId, out var req) &&
                        req.ReportStatus == RadiologyReportStatus.Released)
                }).ToList();

            return Paginate(rows, options, "RadiologistName");
        }

        private static string FormatCollectionStatus(TestRequestDetail request)
        {
            if (request.ReportStatus == ReportStatusType.FinallyRejected)
            {
                return "Rejected";
            }

            if (!string.IsNullOrWhiteSpace(request.ReceivedBy))
            {
                return "Received";
            }

            return !string.IsNullOrWhiteSpace(request.CollectedBy) ? "Collected" : "Pending";
        }

        private static string FormatRadiologyStatus(RadiologyReportStatus status)
        {
            switch (status)
            {
                case RadiologyReportStatus.Pending: return "Pending";
                case RadiologyReportStatus.Draft: return "Draft";
                case RadiologyReportStatus.UnderReview: return "Under Review";
                case RadiologyReportStatus.Authorized: return "Authorized";
                case RadiologyReportStatus.Released: return "Released";
                default: return status.ToString();
            }
        }

        private static void ValidateDateRange(ReportFilterOptions options)
        {
            if (options == null)
            {
                throw new ArgumentException("Report options are required");
            }

            if (!options.FromDate.HasValue || !options.ToDate.HasValue)
            {
                throw new ArgumentException("From Date and To Date are required");
            }

            if (options.FromDate.Value.Date > options.ToDate.Value.Date)
            {
                throw new ArgumentException("From Date cannot be after To Date");
            }
        }

        private static ItemList<T> Paginate<T>(List<T> rows, ReportFilterOptions options, string defaultSort) where T : class
        {
            var result = new ItemList<T>();
            var sortColumn = string.IsNullOrWhiteSpace(options.SortColumnName) ? defaultSort : options.SortColumnName;
            var pageSize = options.RecordPerPage <= 0 ? 25 : options.RecordPerPage;
            var page = options.CurrentPage <= 0 ? 1 : options.CurrentPage;
            var exportAll = options.RecordPerPage <= 0;

            IEnumerable<T> sorted;
            try
            {
                sorted = rows.OrderBy(sortColumn, options.SortDirection);
            }
            catch
            {
                sorted = rows.OrderBy(defaultSort, false);
            }

            var list = sorted.ToList();
            result.TotalRecord = list.Count;
            if (exportAll)
            {
                result.Items = list;
                return result;
            }

            var minRow = (page - 1) * pageSize;
            result.Items = list.Skip(minRow).Take(pageSize).ToList();
            return result;
        }

        private static string FormatInvoiceStatus(int status)
        {
            switch (status)
            {
                case 0: return "Draft";
                case 1: return "Confirmed";
                case 2: return "Paid";
                case 3: return "Cancelled";
                default: return status.ToString();
            }
        }

        private static string FormatPaymentStatus(int status)
        {
            switch (status)
            {
                case 0: return "Unpaid";
                case 1: return "Partial";
                case 2: return "Paid";
                default: return status.ToString();
            }
        }

        private static string FormatReportStatus(ReportStatusType status)
        {
            switch (status)
            {
                case ReportStatusType.New: return "New";
                case ReportStatusType.SentToEquipment: return "Sent To Equipment";
                case ReportStatusType.ReportGenerated: return "Report Generated";
                case ReportStatusType.TechnicianApproved: return "Technician Approved";
                case ReportStatusType.TechnicianRejected: return "Technician Rejected";
                case ReportStatusType.DoctorApproved: return "Doctor Approved";
                case ReportStatusType.DoctorRejected: return "Doctor Rejected";
                case ReportStatusType.FinallyRejected: return "Finally Rejected";
                default: return status.ToString();
            }
        }
    }
}
