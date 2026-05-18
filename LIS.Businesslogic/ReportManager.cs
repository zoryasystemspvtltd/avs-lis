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

        public ReportManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
        {
            invoiceRepo = new ModuleRepo<SaleInvoice>(logger, identity, uow);
            patientRepo = new ModuleRepo<PatientDetail>(logger, identity, uow);
            doctorRepo = new ModuleRepo<ReferralDoctorMaster>(logger, identity, uow);
            corporateRepo = new ModuleRepo<CorporateMaster>(logger, identity, uow);
            requestRepo = new ModuleRepo<TestRequestDetail>(logger, identity, uow);
            testRepo = new ModuleRepo<HisTestMaster>(logger, identity, uow);
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

            if (!string.IsNullOrWhiteSpace(options.InvoiceNo))
            {
                var invSearch = options.InvoiceNo.Trim();
                query = query.Where(i => i.InvoiceNo != null &&
                    i.InvoiceNo.IndexOf(invSearch, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            var rows = query.ToList().Select(i =>
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
