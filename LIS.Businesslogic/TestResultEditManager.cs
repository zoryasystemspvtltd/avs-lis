using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.DtoModel.Models.TestResultEdit;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LIS.BusinessLogic
{
    public class TestResultEditManager : ITestResultEditManager
    {
        private readonly ModuleRepo<TestRequestDetail> requestRepo;
        private readonly ModuleRepo<TestResult> resultRepo;
        private readonly ModuleRepo<TestResultDetails> detailRepo;
        private readonly ModuleRepo<PatientDetail> patientRepo;
        private readonly ModuleRepo<HisTestMaster> testRepo;
        private readonly ModuleRepo<HISParameterMaster> parameterRepo;
        private readonly ModuleRepo<HISParameterRangMaster> rangeRepo;
        private readonly ModuleRepo<EquipmentMaster> equipmentRepo;
        private readonly ModuleRepo<SaleInvoice> invoiceRepo;
        private readonly ILogger logger;
        private readonly IModuleIdentity identity;

        public TestResultEditManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
        {
            this.logger = logger;
            this.identity = identity;
            requestRepo = new ModuleRepo<TestRequestDetail>(logger, identity, uow);
            resultRepo = new ModuleRepo<TestResult>(logger, identity, uow);
            detailRepo = new ModuleRepo<TestResultDetails>(logger, identity, uow);
            patientRepo = new ModuleRepo<PatientDetail>(logger, identity, uow);
            testRepo = new ModuleRepo<HisTestMaster>(logger, identity, uow);
            parameterRepo = new ModuleRepo<HISParameterMaster>(logger, identity, uow);
            rangeRepo = new ModuleRepo<HISParameterRangMaster>(logger, identity, uow);
            equipmentRepo = new ModuleRepo<EquipmentMaster>(logger, identity, uow);
            invoiceRepo = new ModuleRepo<SaleInvoice>(logger, identity, uow);
        }

        public IList<TestResultEditSearchRow> Search(TestResultEditSearchOptions options)
        {
            if (options == null)
            {
                return new List<TestResultEditSearchRow>();
            }

            var query = requestRepo.Get().AsEnumerable();

            if (!string.IsNullOrWhiteSpace(options.SampleNo))
            {
                var sn = options.SampleNo.Trim();
                query = query.Where(r =>
                    (r.SampleNo != null && (
                        r.SampleNo.Equals(sn, StringComparison.OrdinalIgnoreCase) ||
                        r.SampleNo.IndexOf(sn, StringComparison.OrdinalIgnoreCase) >= 0)) ||
                    (r.HISRequestNo != null && r.HISRequestNo.IndexOf(sn, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            if (!string.IsNullOrWhiteSpace(options.InvoiceNo))
            {
                var inv = options.InvoiceNo.Trim();
                var invoiceNos = invoiceRepo.Get(i => i.IsActive && i.InvoiceNo != null)
                    .AsEnumerable()
                    .Where(i => i.InvoiceNo.IndexOf(inv, StringComparison.OrdinalIgnoreCase) >= 0)
                    .Select(i => i.InvoiceNo)
                    .ToList();

                query = query.Where(r =>
                    (r.HISRequestNo != null && r.HISRequestNo.IndexOf(inv, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (r.SampleNo != null && r.SampleNo.IndexOf(inv, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    invoiceNos.Any(no =>
                        (r.HISRequestNo != null && r.HISRequestNo.Equals(no, StringComparison.OrdinalIgnoreCase)) ||
                        (r.SampleNo != null && r.SampleNo.Equals(no, StringComparison.OrdinalIgnoreCase))));
            }

            if (options.FromDate.HasValue)
            {
                var from = options.FromDate.Value.Date;
                query = query.Where(r => r.SampleCollectionDate >= from);
            }

            if (options.ToDate.HasValue)
            {
                var to = options.ToDate.Value.Date.AddDays(1);
                query = query.Where(r => r.SampleCollectionDate < to);
            }

            var patients = patientRepo.Get().ToDictionary(p => p.Id, p => p);
            if (!string.IsNullOrWhiteSpace(options.PatientName))
            {
                var name = options.PatientName.Trim();
                query = query.Where(r =>
                    patients.TryGetValue(r.PatientId, out var p) &&
                    p.Name != null &&
                    p.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            var resultRequestIds = new HashSet<long>(
                resultRepo.Get().Select(res => res.TestRequestId));

            var rows = query
                .GroupBy(r => r.SampleNo ?? r.HISRequestNo)
                .Select(g =>
                {
                    var first = g.OrderByDescending(x => x.Id).First();
                    patients.TryGetValue(first.PatientId, out var patient);
                    var hasResults = g.Any(req => resultRequestIds.Contains(req.Id));
                    return new TestResultEditSearchRow
                    {
                        SampleNo = first.SampleNo ?? first.HISRequestNo,
                        InvoiceNo = first.HISRequestNo,
                        PatientName = patient?.Name,
                        CollectionDate = first.SampleCollectionDate,
                        ReportStatus = (int)first.ReportStatus,
                        ReportStatusLabel = FormatReportStatus(first.ReportStatus),
                        HasResults = hasResults
                    };
                })
                .OrderByDescending(r =>
                    !string.IsNullOrWhiteSpace(options.SampleNo) &&
                    r.SampleNo != null &&
                    r.SampleNo.Equals(options.SampleNo.Trim(), StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(r => r.CollectionDate)
                .Take(100)
                .ToList();

            return rows;
        }

        public TestResultEditSampleDto GetBySampleNo(string sampleNo, bool isAdministrator)
        {
            if (string.IsNullOrWhiteSpace(sampleNo))
            {
                throw new ArgumentException("Sample No is required.");
            }

            var key = sampleNo.Trim();
            var requests = requestRepo.Get(r =>
                    (r.SampleNo != null && r.SampleNo.Equals(key, StringComparison.OrdinalIgnoreCase)) ||
                    (r.HISRequestNo != null && r.HISRequestNo.Equals(key, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(r => r.HISTestName ?? r.HISTestCode)
                .ToList();

            if (!requests.Any())
            {
                throw new InvalidOperationException("No test booking found for this sample / lab number.");
            }

            var firstReq = requests.First();
            var patient = patientRepo.Get(firstReq.PatientId);
            var allRanges = rangeRepo.Get().ToList();
            var allParams = parameterRepo.Get().ToList();
            var equipments = equipmentRepo.Get().ToDictionary(e => e.Id, e => e.Name);

            var tests = new List<TestResultEditTestDto>();
            foreach (var req in requests)
            {
                var result = resultRepo.Get(res => res.TestRequestId == req.Id)
                    .OrderByDescending(res => res.Id)
                    .FirstOrDefault();

                if (result == null)
                {
                    continue;
                }

                var canEdit = CanEditStatus(req.ReportStatus, isAdministrator);
                var details = detailRepo.Get(d => d.TestResultId == result.Id).ToList();
                var testCode = !string.IsNullOrWhiteSpace(req.HISTestCode)
                    ? req.HISTestCode
                    : result.HISTestCode;
                var paramDtos = BuildParameters(details, testCode, patient, allParams, allRanges, canEdit);

                equipments.TryGetValue(result.EquipmentId, out var eqName);

                tests.Add(new TestResultEditTestDto
                {
                    TestRequestId = req.Id,
                    TestResultId = result.Id,
                    HisTestCode = req.HISTestCode,
                    HisTestName = req.HISTestName ?? req.HISTestCode,
                    EquipmentName = eqName,
                    ReportStatus = (int)req.ReportStatus,
                    ReportStatusLabel = FormatReportStatus(req.ReportStatus),
                    ResultDate = result.ResultDate,
                    CanEdit = canEdit,
                    Parameters = paramDtos
                });
            }

            if (!tests.Any())
            {
                throw new InvalidOperationException("No test results found for this sample. Results may not have been received from the analyzer yet.");
            }

            return new TestResultEditSampleDto
            {
                SampleNo = firstReq.SampleNo ?? key,
                InvoiceNo = firstReq.HISRequestNo,
                PatientName = patient?.Name,
                PatientId = patient?.HisPatientId,
                Age = patient != null ? patient.Age.ToString() : string.Empty,
                Gender = patient?.Gender,
                CanEditAny = tests.Any(t => t.CanEdit),
                IsAdministrator = isAdministrator,
                Tests = tests
            };
        }

        public TestResultEditSaveResult Save(TestResultEditSaveRequest request, bool isAdministrator)
        {
            if (request == null || request.TestResultId <= 0)
            {
                throw new ArgumentException("Invalid save request.");
            }

            var result = resultRepo.Get(request.TestResultId);
            if (result == null)
            {
                throw new InvalidOperationException("Test result not found.");
            }

            var testRequest = requestRepo.Get(request.TestRequestId > 0 ? request.TestRequestId : result.TestRequestId);
            if (testRequest == null)
            {
                throw new InvalidOperationException("Test request not found.");
            }

            if (!CanEditStatus(testRequest.ReportStatus, isAdministrator))
            {
                throw new InvalidOperationException("This result cannot be edited in its current approval status.");
            }

            var patient = patientRepo.Get(result.PatientId);
            var existingDetails = detailRepo.Get(d => d.TestResultId == result.Id).ToList();
            var detailsById = existingDetails.ToDictionary(d => d.Id, d => d);
            var auditLog = new StringBuilder();
            var now = DateTime.Now;
            var editor = identity?.ActivityMember ?? "system";
            var changed = false;

            if (request.Parameters != null)
            {
                foreach (var change in request.Parameters)
                {
                    if (!detailsById.TryGetValue(change.DetailId, out var detail))
                    {
                        continue;
                    }

                    var newValue = (change.ResultValue ?? string.Empty).Trim();
                    var oldValue = detail.LISParamValue ?? string.Empty;

                    if (string.Equals(oldValue, newValue, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(newValue))
                    {
                        throw new InvalidOperationException($"Result value cannot be empty for parameter {detail.LISParamCode}.");
                    }

                    auditLog.AppendFormat("[{0:dd/MM/yyyy HH:mm}] {1} edited {2}: '{3}' → '{4}'<br>",
                        now, editor, detail.LISParamCode, oldValue, newValue);

                    detail.LISParamValue = newValue;
                    detailRepo.Update(detail);
                    changed = true;

                    logger.LogInfo($"TestResultEdit: Sample={result.SampleNo} TestResultId={result.Id} Param={detail.LISParamCode} Old={oldValue} New={newValue} By={editor}");
                }
            }

            if (!changed)
            {
                return new TestResultEditSaveResult
                {
                    Success = true,
                    Message = "No changes to save.",
                    ReportStatus = (int)testRequest.ReportStatus,
                    ReportStatusLabel = FormatReportStatus(testRequest.ReportStatus)
                };
            }

            result.TechnicianNote = (result.TechnicianNote ?? string.Empty) + auditLog;

            ApplyApprovalReset(testRequest);
            requestRepo.Update(testRequest);

            if (testRequest.ReportStatus != ReportStatusType.DoctorApproved)
            {
                result.AuthorizedBy = null;
                result.AuthorizationDate = null;
                result.ReviewedBy = null;
                result.ReviewDate = null;
            }

            resultRepo.Update(result);

            return new TestResultEditSaveResult
            {
                Success = true,
                Message = "Results updated successfully.",
                ReportStatus = (int)testRequest.ReportStatus,
                ReportStatusLabel = FormatReportStatus(testRequest.ReportStatus)
            };
        }

        private IList<TestResultEditParameterDto> BuildParameters(
            IList<TestResultDetails> details,
            string testCode,
            PatientDetail patient,
            List<HISParameterMaster> allParams,
            List<HISParameterRangMaster> allRanges,
            bool canEdit)
        {
            var list = new List<TestResultEditParameterDto>();
            foreach (var detail in details)
            {
                var paramMaster = allParams.FirstOrDefault(p =>
                    p.HISTestCode != null && p.HISTestCode.Equals(testCode, StringComparison.OrdinalIgnoreCase) &&
                    (p.LISParamCode != null && p.LISParamCode.Equals(detail.LISParamCode, StringComparison.OrdinalIgnoreCase) ||
                     p.HISParamCode != null && p.HISParamCode.Equals(detail.LISParamCode, StringComparison.OrdinalIgnoreCase)));

                TestResultRangeEvaluator.Apply(
                    detail.LISParamValue,
                    paramMaster,
                    patient,
                    allRanges,
                    out var refRange,
                    out var flag,
                    out var isAbnormal);

                list.Add(new TestResultEditParameterDto
                {
                    DetailId = detail.Id,
                    ParameterCode = paramMaster?.HISParamCode ?? detail.LISParamCode,
                    ParameterName = paramMaster?.HISParamDescription ?? detail.LISParamCode,
                    ResultValue = detail.LISParamValue,
                    Unit = !string.IsNullOrWhiteSpace(detail.LISParamUnit) ? detail.LISParamUnit : paramMaster?.HISParamUnit,
                    ReferenceRange = refRange,
                    Flag = flag,
                    IsAbnormal = isAbnormal,
                    Method = paramMaster?.HISParamMethod,
                    IsEditable = canEdit
                });
            }

            return list.OrderBy(p => p.ParameterName).ToList();
        }

        private static bool CanEditStatus(ReportStatusType status, bool isAdministrator)
        {
            switch (status)
            {
                case ReportStatusType.TechnicianRejected:
                case ReportStatusType.DoctorRejected:
                case ReportStatusType.FinallyRejected:
                    return false;
                case ReportStatusType.DoctorApproved:
                    return false;
                case ReportStatusType.ReportGenerated:
                case ReportStatusType.TechnicianApproved:
                case ReportStatusType.New:
                case ReportStatusType.SentToEquipment:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// After manual correction, technician must re-approve (same pattern as rejection re-run).
        /// </summary>
        private static void ApplyApprovalReset(TestRequestDetail request)
        {
            if (request.ReportStatus == ReportStatusType.TechnicianApproved)
            {
                request.ReportStatus = ReportStatusType.ReportGenerated;
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
