using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LIS.BusinessLogic
{
    public class TestReportManager : ITestReportManager
    {
        private readonly ModuleRepo<SaleInvoice> invoiceRepo;
        private readonly ModuleRepo<SaleInvoiceDetail> detailRepo;
        private readonly ModuleRepo<TestRequestDetail> requestRepo;
        private readonly ModuleRepo<TestResult> resultRepo;
        private readonly ModuleRepo<TestResultDetails> resultDetailsRepo;
        private readonly ModuleRepo<PatientDetail> patientRepo;
        private readonly ModuleRepo<ReferralDoctorMaster> doctorRepo;
        private readonly ModuleRepo<CorporateMaster> corporateRepo;
        private readonly ModuleRepo<HISParameterMaster> parameterRepo;
        private readonly ModuleRepo<HISParameterRangMaster> rangeRepo;
        private readonly ModuleRepo<HisTestMaster> testRepo;
        private readonly ModuleRepo<Departments> departmentRepo;
        private readonly ITestRequestDetailsManager testRequestManager;

        public TestReportManager(
            ILogger logger,
            IModuleIdentity identity,
            GenericUnitOfWork unitOfWork,
            ITestRequestDetailsManager testRequestDetailsManager)
        {
            invoiceRepo = new ModuleRepo<SaleInvoice>(logger, identity, unitOfWork);
            detailRepo = new ModuleRepo<SaleInvoiceDetail>(logger, identity, unitOfWork);
            requestRepo = new ModuleRepo<TestRequestDetail>(logger, identity, unitOfWork);
            resultRepo = new ModuleRepo<TestResult>(logger, identity, unitOfWork);
            resultDetailsRepo = new ModuleRepo<TestResultDetails>(logger, identity, unitOfWork);
            patientRepo = new ModuleRepo<PatientDetail>(logger, identity, unitOfWork);
            doctorRepo = new ModuleRepo<ReferralDoctorMaster>(logger, identity, unitOfWork);
            corporateRepo = new ModuleRepo<CorporateMaster>(logger, identity, unitOfWork);
            parameterRepo = new ModuleRepo<HISParameterMaster>(logger, identity, unitOfWork);
            rangeRepo = new ModuleRepo<HISParameterRangMaster>(logger, identity, unitOfWork);
            testRepo = new ModuleRepo<HisTestMaster>(logger, identity, unitOfWork);
            departmentRepo = new ModuleRepo<Departments>(logger, identity, unitOfWork);
            testRequestManager = testRequestDetailsManager;
        }

        public DiagnosticTestReportDto GetDiagnosticTestReport(string labNo, string invoiceNo)
        {
            if (string.IsNullOrWhiteSpace(labNo) && string.IsNullOrWhiteSpace(invoiceNo))
            {
                throw new TestReportValidationException("Lab No or Sale Invoice No is required.");
            }

            var invoice = ResolveInvoice(labNo, invoiceNo);
            if (invoice == null)
            {
                throw new TestReportValidationException("Invalid Lab No or Invoice No. No matching invoice was found.");
            }

            if (invoice.InvoiceStatus == (int)InvoiceStatusType.Cancelled)
            {
                throw new TestReportValidationException("Invoice is cancelled. Test report cannot be printed.");
            }

            if (invoice.PaymentStatus != (int)PaymentStatusType.Paid)
            {
                throw new TestReportValidationException("Payment pending. Test report can only be printed after full payment is completed.");
            }

            var patient = patientRepo.Get(invoice.PatientId);
            if (patient == null)
            {
                throw new TestReportValidationException("Patient record not found for this invoice.");
            }

            var requests = ResolveTestRequests(invoice);
            if (!requests.Any())
            {
                throw new TestReportValidationException("No test bookings found for this invoice.");
            }

            ValidateWorkflow(requests);

            var header = BuildHeader(invoice, patient, requests);
            var sections = new List<DiagnosticTestReportSection>();

            foreach (var request in requests.OrderBy(r => r.HISTestName ?? r.HISTestCode))
            {
                var section = BuildSection(request, patient);
                if (section != null && section.Parameters != null && section.Parameters.Any())
                {
                    sections.Add(section);
                }
            }

            if (!sections.Any())
            {
                throw new TestReportValidationException("Test results are not available for printing.");
            }

            return new DiagnosticTestReportDto
            {
                Header = header,
                Sections = sections
            };
        }

        private SaleInvoice ResolveInvoice(string labNo, string invoiceNo)
        {
            if (!string.IsNullOrWhiteSpace(invoiceNo))
            {
                return FindInvoiceByNumber(invoiceNo.Trim());
            }

            return FindInvoiceByNumber(labNo.Trim());
        }

        private SaleInvoice FindInvoiceByNumber(string number)
        {
            var invoice = invoiceRepo.Get(i => i.IsActive && i.InvoiceNo.Equals(number, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(i => i.Id)
                .FirstOrDefault();

            if (invoice != null)
            {
                return invoice;
            }

            var request = requestRepo.Get(r => r.HISRequestNo.Equals(number, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => r.Id)
                .FirstOrDefault();

            if (request == null || string.IsNullOrWhiteSpace(request.HISRequestNo))
            {
                return null;
            }

            return invoiceRepo.Get(i => i.IsActive && i.InvoiceNo.Equals(request.HISRequestNo, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(i => i.Id)
                .FirstOrDefault();
        }

        private List<TestRequestDetail> ResolveTestRequests(SaleInvoice invoice)
        {
            var fromLines = detailRepo.Get(d => d.SaleInvoiceId == invoice.Id && d.IsActive)
                .Where(d => d.RequestDetailId > 0)
                .Select(d => d.RequestDetailId)
                .Distinct()
                .Select(id => requestRepo.Get(id))
                .Where(r => r != null)
                .ToList();

            if (fromLines.Any())
            {
                return fromLines;
            }

            return requestRepo.Get(r => r.HISRequestNo.Equals(invoice.InvoiceNo, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private void ValidateWorkflow(List<TestRequestDetail> requests)
        {
            var notApproved = requests.Where(r => r.ReportStatus != ReportStatusType.DoctorApproved).ToList();
            if (notApproved.Any())
            {
                var names = string.Join(", ", notApproved.Select(r => r.HISTestName ?? r.HISTestCode ?? "Test"));
                throw new TestReportValidationException($"Report not approved for: {names}. Doctor approval is required before printing.");
            }

            foreach (var request in requests)
            {
                var result = resultRepo.Get(r => r.TestRequestId == request.Id).FirstOrDefault();
                if (result == null)
                {
                    throw new TestReportValidationException($"Results not ready for test: {request.HISTestName ?? request.HISTestCode}.");
                }

                var hasValues = resultDetailsRepo.Get(d => d.TestResultId == result.Id).Any();
                if (!hasValues)
                {
                    throw new TestReportValidationException($"Results not ready for test: {request.HISTestName ?? request.HISTestCode}.");
                }
            }
        }

        private DiagnosticTestReportHeader BuildHeader(SaleInvoice invoice, PatientDetail patient, List<TestRequestDetail> requests)
        {
            string doctorName = invoice.RefDoctorName;
            if (string.IsNullOrWhiteSpace(doctorName) && invoice.ReferralDoctorId.HasValue)
            {
                var doc = doctorRepo.Get(invoice.ReferralDoctorId.Value);
                doctorName = doc?.Name;
            }

            string corporateName = null;
            if (invoice.CorporateId.HasValue)
            {
                var corp = corporateRepo.Get(invoice.CorporateId.Value);
                corporateName = corp?.Name;
            }

            var firstRequest = requests.OrderBy(r => r.SampleCollectionDate).FirstOrDefault();
            var latestResult = requests
                .Select(r => resultRepo.Get(res => res.TestRequestId == r.Id).FirstOrDefault())
                .Where(r => r != null)
                .OrderByDescending(r => r.AuthorizationDate ?? r.ResultDate)
                .FirstOrDefault();

            return new DiagnosticTestReportHeader
            {
                LabNo = firstRequest?.HISRequestNo ?? invoice.InvoiceNo,
                InvoiceNo = invoice.InvoiceNo,
                PatientName = patient.Name,
                PatientId = patient.HisPatientId,
                Age = patient.Age,
                Gender = patient.Gender,
                ReferralDoctor = doctorName,
                Corporate = corporateName,
                CollectionDate = firstRequest?.SampleCollectionDate,
                ReportDate = latestResult?.AuthorizationDate ?? latestResult?.ResultDate ?? DateTime.Now,
                ApprovedBy = latestResult?.AuthorizedBy
            };
        }

        private DiagnosticTestReportSection BuildSection(TestRequestDetail request, PatientDetail patient)
        {
            var review = testRequestManager.GetTestResultByRequestId(request.Id);
            if (review?.Test == null)
            {
                return null;
            }

            var approvedRun = review.TestRuns?
                .Where(r => r.ReportStatus == ReportStatusType.DoctorApproved)
                .OrderByDescending(r => r.ReviewDate)
                .FirstOrDefault();

            var values = approvedRun?.TestValues?.ToList() ?? new List<TestValues>();
            if (!values.Any())
            {
                return null;
            }

            var testEntity = testRepo.Get(t => t.HISTestCode.Equals(request.HISTestCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            var department = request.Department;
            if (string.IsNullOrWhiteSpace(department) && testEntity != null)
            {
                var dept = departmentRepo.Get(d => d.Code == testEntity.DepartmentCode).FirstOrDefault();
                department = dept?.Name;
            }

            var parameters = values.Select(v => MapParameter(v, request.HISTestCode, patient)).Where(p => p != null).ToList();

            return new DiagnosticTestReportSection
            {
                TestCode = request.HISTestCode,
                TestName = review.Test.TestName ?? request.HISTestName,
                Specimen = review.Test.SpecimenName ?? request.SpecimenName,
                SampleNo = request.SampleNo,
                Department = department,
                Parameters = parameters
            };
        }

        private DiagnosticTestReportParameter MapParameter(TestValues value, string testCode, PatientDetail patient)
        {
            if (value == null)
            {
                return null;
            }

            var row = new DiagnosticTestReportParameter
            {
                ParameterCode = value.HISParamCode ?? value.LISParamCode,
                ParameterName = value.HISParamName ?? value.HISParamCode ?? value.LISParamCode,
                ResultValue = value.ParamValue,
                Unit = value.ParamUnit
            };

            var paramMaster = parameterRepo.Get(p =>
                    p.HISTestCode.Equals(testCode, StringComparison.OrdinalIgnoreCase) &&
                    (p.HISParamCode.Equals(row.ParameterCode, StringComparison.OrdinalIgnoreCase) ||
                     p.LISParamCode.Equals(value.LISParamCode, StringComparison.OrdinalIgnoreCase)))
                .FirstOrDefault();

            ApplyReferenceRange(row, paramMaster, patient, value);

            return row;
        }

        private void ApplyReferenceRange(DiagnosticTestReportParameter row, HISParameterMaster paramMaster, PatientDetail patient, TestValues source)
        {
            HISParameterRangMaster matchedRange = null;

            if (paramMaster != null)
            {
                var ranges = rangeRepo.Get(r => r.HisParameterId == paramMaster.Id).ToList();
                matchedRange = ranges
                    .Where(r => MatchesPatientRange(r, patient))
                    .OrderByDescending(r => r.MinValue > 0 || r.MaxValue > 0)
                    .ThenBy(r => r.Id)
                    .FirstOrDefault();
            }

            if (matchedRange == null && source.HISRangeValues != null && source.HISRangeValues.Length > 0)
            {
                row.ReferenceRange = string.Join("; ", source.HISRangeValues.Where(v => !string.IsNullOrWhiteSpace(v)));
            }
            else if (matchedRange != null)
            {
                if (matchedRange.MinValue > 0 || matchedRange.MaxValue > 0)
                {
                    row.ReferenceRange = $"{FormatDecimal(matchedRange.MinValue)} - {FormatDecimal(matchedRange.MaxValue)}";
                }
                else if (!string.IsNullOrWhiteSpace(matchedRange.HISRangeValue))
                {
                    row.ReferenceRange = matchedRange.HISRangeValue;
                }
            }

            if (matchedRange != null && TryParseResult(row.ResultValue, out var numeric))
            {
                if (matchedRange.MinValue > 0 && numeric < matchedRange.MinValue)
                {
                    row.Flag = "L";
                    row.IsAbnormal = true;
                }
                else if (matchedRange.MaxValue > 0 && numeric > matchedRange.MaxValue)
                {
                    row.Flag = "H";
                    row.IsAbnormal = true;
                }
            }
        }

        private static bool MatchesPatientRange(HISParameterRangMaster range, PatientDetail patient)
        {
            if (range == null || patient == null)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(range.Gender) &&
                !string.IsNullOrWhiteSpace(patient.Gender) &&
                !GenderMatches(range.Gender, patient.Gender))
            {
                return false;
            }

            if (range.AgeFrom > 0 || range.AgeTo > 0)
            {
                var age = patient.Age;
                if (age < range.AgeFrom || (range.AgeTo > 0 && age > range.AgeTo))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool GenderMatches(string rangeGender, string patientGender)
        {
            var rg = rangeGender.Trim().ToUpperInvariant();
            var pg = patientGender.Trim().ToUpperInvariant();
            if (rg.StartsWith("M") && pg.StartsWith("M")) return true;
            if (rg.StartsWith("F") && pg.StartsWith("F")) return true;
            return rg == pg;
        }

        private static bool TryParseResult(string value, out decimal numeric)
        {
            numeric = 0;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var cleaned = value.Trim().Replace(",", "");
            return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out numeric);
        }

        private static string FormatDecimal(decimal value)
        {
            return value % 1 == 0 ? value.ToString("0", CultureInfo.InvariantCulture) : value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        public IEnumerable<TestReportLabNoOption> GetPrintableLabNumbers()
        {
            var approvedRequests = requestRepo
                .Get(r => r.ReportStatus == ReportStatusType.DoctorApproved && r.HISRequestNo != null && r.HISRequestNo != "")
                .OrderByDescending(r => r.CreatedOn)
                .ToList();

            var options = new Dictionary<string, TestReportLabNoOption>(StringComparer.OrdinalIgnoreCase);

            foreach (var request in approvedRequests)
            {
                var labNo = request.HISRequestNo.Trim();
                if (options.ContainsKey(labNo))
                {
                    continue;
                }

                var invoice = FindInvoiceByNumber(labNo);
                if (invoice == null || invoice.PaymentStatus != (int)PaymentStatusType.Paid)
                {
                    continue;
                }

                var result = resultRepo.Get(r => r.TestRequestId == request.Id).FirstOrDefault();
                if (result == null || !resultDetailsRepo.Get(d => d.TestResultId == result.Id).Any())
                {
                    continue;
                }

                var patient = patientRepo.Get(invoice.PatientId);
                var patientName = patient?.Name ?? string.Empty;
                options[labNo] = new TestReportLabNoOption
                {
                    LabNo = labNo,
                    InvoiceNo = invoice.InvoiceNo,
                    PatientName = patientName,
                    DisplayLabel = string.IsNullOrWhiteSpace(patientName) ? labNo : $"{labNo} — {patientName}"
                };
            }

            return options.Values.OrderByDescending(o => o.LabNo).ToList();
        }
    }
}
