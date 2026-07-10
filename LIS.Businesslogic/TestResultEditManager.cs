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
        private readonly ModuleRepo<TestMappingMaster> mappingRepo;
        private readonly ModuleRepo<TestParameterMappingMaster> testParamMappingRepo;
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
            mappingRepo = new ModuleRepo<TestMappingMaster>(logger, identity, uow);
            testParamMappingRepo = new ModuleRepo<TestParameterMappingMaster>(logger, identity, uow);
        }

        public IList<TestResultEditSearchRow> Search(TestResultEditSearchOptions options)
        {
            if (options == null)
            {
                return new List<TestResultEditSearchRow>();
            }

            var hasSample = !string.IsNullOrWhiteSpace(options.SampleNo);
            var hasInvoice = !string.IsNullOrWhiteSpace(options.InvoiceNo);
            var hasPatient = !string.IsNullOrWhiteSpace(options.PatientName);
            var hasDate = options.FromDate.HasValue || options.ToDate.HasValue;

            IEnumerable<TestRequestDetail> query;
            if (hasDate && !hasSample && !hasInvoice && !hasPatient)
            {
                var from = options.FromDate?.Date ?? DateTime.MinValue;
                var to = options.ToDate.HasValue ? options.ToDate.Value.Date.AddDays(1) : DateTime.MaxValue;
                query = requestRepo.Get(r => r.SampleCollectionDate >= from && r.SampleCollectionDate < to).AsEnumerable();
            }
            else
            {
                query = requestRepo.Get().AsEnumerable();
            }
            var patients = new Dictionary<long, PatientDetail>();
            if (hasPatient)
            {
                var name = options.PatientName.Trim();
                foreach (var p in patientRepo.Get().AsEnumerable())
                {
                    if (p.Name != null && p.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        patients[p.Id] = p;
                    }
                }
            }

            // Advanced Search: every supplied criterion is combined with OR, so a row is
            // returned when it matches ANY of the entered fields (union), not all of them.
            var predicates = new List<Func<TestRequestDetail, bool>>();

            if (hasSample)
            {
                var sn = options.SampleNo.Trim();
                predicates.Add(r =>
                    (r.SampleNo != null && (
                        r.SampleNo.Equals(sn, StringComparison.OrdinalIgnoreCase) ||
                        r.SampleNo.IndexOf(sn, StringComparison.OrdinalIgnoreCase) >= 0)) ||
                    (r.HISRequestNo != null && r.HISRequestNo.IndexOf(sn, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            if (hasInvoice)
            {
                var inv = options.InvoiceNo.Trim();
                var invoiceNos = invoiceRepo.Get(i => i.IsActive && i.InvoiceNo != null)
                    .AsEnumerable()
                    .Where(i => i.InvoiceNo.IndexOf(inv, StringComparison.OrdinalIgnoreCase) >= 0)
                    .Select(i => i.InvoiceNo)
                    .ToList();

                predicates.Add(r =>
                    (r.HISRequestNo != null && r.HISRequestNo.IndexOf(inv, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (r.SampleNo != null && r.SampleNo.IndexOf(inv, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    invoiceNos.Any(no =>
                        (r.HISRequestNo != null && r.HISRequestNo.Equals(no, StringComparison.OrdinalIgnoreCase)) ||
                        (r.SampleNo != null && r.SampleNo.Equals(no, StringComparison.OrdinalIgnoreCase))));
            }

            if (hasPatient)
            {
                var name = options.PatientName.Trim();
                predicates.Add(r =>
                    patients.TryGetValue(r.PatientId, out var p) &&
                    p.Name != null &&
                    p.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            // From/To together form a single date-range term so the range still behaves
            // sensibly while participating as one component of the overall OR.
            if (hasDate && (hasSample || hasInvoice || hasPatient))
            {
                var from = options.FromDate?.Date ?? DateTime.MinValue;
                var to = options.ToDate.HasValue ? options.ToDate.Value.Date.AddDays(1) : DateTime.MaxValue;
                predicates.Add(r => r.SampleCollectionDate >= from && r.SampleCollectionDate < to);
            }

            if (predicates.Any())
            {
                query = query.Where(r => predicates.Any(match => match(r)));
            }

            var allParams = parameterRepo.Get().ToList();
            var filteredRequests = query.ToList();
            var patientIds = filteredRequests.Select(r => r.PatientId).Distinct().ToList();
            if (patientIds.Any())
            {
                foreach (var p in patientRepo.Get(p => patientIds.Contains(p.Id)))
                {
                    if (!patients.ContainsKey(p.Id))
                    {
                        patients[p.Id] = p;
                    }
                }
            }

            var persistedRequestIds = GetRequestIdsWithPersistedResults(
                filteredRequests.Select(r => r.Id).Distinct().ToList());
            var editableByTestCode = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            var rows = filteredRequests
                .GroupBy(r => r.SampleNo ?? r.HISRequestNo)
                .Select(g =>
                {
                    var first = g.OrderByDescending(x => x.Id).First();
                    patients.TryGetValue(first.PatientId, out var patient);
                    var hasResults = g.Any(req => persistedRequestIds.Contains(req.Id));
                    var canEnter = g.Any(req =>
                        CanEditStatus(req.ReportStatus, false) &&
                        HasEditableParametersCached(req.HISTestCode, allParams, editableByTestCode));
                    return new TestResultEditSearchRow
                    {
                        SampleNo = first.SampleNo ?? first.HISRequestNo,
                        InvoiceNo = first.HISRequestNo,
                        PatientName = patient?.Name,
                        CollectionDate = first.SampleCollectionDate,
                        ReportStatus = (int)first.ReportStatus,
                        ReportStatusLabel = FormatReportStatus(first.ReportStatus),
                        HasResults = hasResults,
                        CanEnter = canEnter
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
                    var canEdit = CanEditStatus(req.ReportStatus, isAdministrator);
                    if (!canEdit)
                    {
                        continue;
                    }

                    var scaffoldParams = BuildScaffoldParameters(req.HISTestCode, patient, allParams, allRanges, canEdit);
                    if (!scaffoldParams.Any())
                    {
                        continue;
                    }

                    tests.Add(new TestResultEditTestDto
                    {
                        TestRequestId = req.Id,
                        TestResultId = 0,
                        HisTestCode = req.HISTestCode,
                        HisTestName = req.HISTestName ?? req.HISTestCode,
                        EquipmentName = null,
                        ReportStatus = (int)req.ReportStatus,
                        ReportStatusLabel = FormatReportStatus(req.ReportStatus),
                        ResultDate = null,
                        CanEdit = canEdit,
                        Parameters = scaffoldParams
                    });
                    continue;
                }

                var canEditExisting = CanEditStatus(req.ReportStatus, isAdministrator);
                var details = detailRepo.Get(d => d.TestResultId == result.Id).ToList();

                if (!details.Any())
                {
                    if (!canEditExisting)
                    {
                        continue;
                    }

                    var scaffoldParams = BuildScaffoldParameters(req.HISTestCode, patient, allParams, allRanges, canEditExisting);
                    if (!scaffoldParams.Any())
                    {
                        continue;
                    }

                    equipments.TryGetValue(result.EquipmentId ?? 0, out var shellEqName);

                    tests.Add(new TestResultEditTestDto
                    {
                        TestRequestId = req.Id,
                        TestResultId = 0,
                        HisTestCode = req.HISTestCode,
                        HisTestName = req.HISTestName ?? req.HISTestCode,
                        EquipmentName = shellEqName,
                        ReportStatus = (int)req.ReportStatus,
                        ReportStatusLabel = FormatReportStatus(req.ReportStatus),
                        ResultDate = result.ResultDate,
                        CanEdit = canEditExisting,
                        Parameters = scaffoldParams
                    });
                    continue;
                }

                var testCode = !string.IsNullOrWhiteSpace(req.HISTestCode)
                    ? req.HISTestCode
                    : result.HISTestCode;
                var paramDtos = BuildParameters(details, testCode, patient, allParams, allRanges, canEditExisting);

                equipments.TryGetValue(result.EquipmentId ?? 0, out var eqName);

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
                    CanEdit = canEditExisting,
                    Parameters = paramDtos
                });
            }

            if (!tests.Any())
            {
                throw new InvalidOperationException(
                    "No editable tests found for this sample. Configure parameters for the test(s) on this sample, or check the approval status.");
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
            if (request == null)
            {
                throw new ArgumentException("Invalid save request.");
            }

            if (request.TestResultId <= 0)
            {
                return CreateManualResult(request, isAdministrator);
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
                    var oldValue = detail.ParamValue ?? string.Empty;

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

                    detail.ParamValue = newValue;
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
                var paramMaster = ResolveParametersForTest(testCode, allParams).FirstOrDefault(p =>
                    (p.LISParamCode != null && p.LISParamCode.Equals(detail.LISParamCode, StringComparison.OrdinalIgnoreCase)) ||
                     p.HISParamCode != null && p.HISParamCode.Equals(detail.LISParamCode, StringComparison.OrdinalIgnoreCase) ||
                     (detail.HISParamCode != null && p.HISParamCode != null && p.HISParamCode.Equals(detail.HISParamCode, StringComparison.OrdinalIgnoreCase)));

                TestResultRangeEvaluator.Apply(
                    detail.ParamValue,
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
                    ResultValue = detail.ParamValue,
                    Unit = !string.IsNullOrWhiteSpace(detail.ParamUnit) ? detail.ParamUnit : paramMaster?.HISParamUnit,
                    ReferenceRange = refRange,
                    Flag = flag,
                    IsAbnormal = isAbnormal,
                    Method = paramMaster?.HISParamMethod,
                    IsEditable = canEdit
                });
            }

            return list.OrderBy(p => p.ParameterName).ToList();
        }

        private IList<TestResultEditParameterDto> BuildScaffoldParameters(
            string testCode,
            PatientDetail patient,
            List<HISParameterMaster> allParams,
            List<HISParameterRangMaster> allRanges,
            bool canEdit)
        {
            if (string.IsNullOrWhiteSpace(testCode))
            {
                return new List<TestResultEditParameterDto>();
            }

            var masters = ResolveParametersForTest(testCode, allParams);

            var list = new List<TestResultEditParameterDto>();
            foreach (var paramMaster in masters)
            {
                TestResultRangeEvaluator.Apply(
                    string.Empty,
                    paramMaster,
                    patient,
                    allRanges,
                    out var refRange,
                    out var flag,
                    out var isAbnormal);

                list.Add(new TestResultEditParameterDto
                {
                    DetailId = 0,
                    ParameterCode = paramMaster.HISParamCode ?? paramMaster.LISParamCode,
                    ParameterName = paramMaster.HISParamDescription ?? paramMaster.HISParamCode,
                    ResultValue = string.Empty,
                    Unit = paramMaster.HISParamUnit,
                    ReferenceRange = refRange,
                    Flag = flag,
                    IsAbnormal = isAbnormal,
                    Method = paramMaster.HISParamMethod,
                    IsEditable = canEdit
                });
            }

            return list;
        }

        private TestResultEditSaveResult CreateManualResult(TestResultEditSaveRequest request, bool isAdministrator)
        {
            if (request.TestRequestId <= 0)
            {
                throw new ArgumentException("Test request is required for new result entry.");
            }

            var testRequest = requestRepo.Get(request.TestRequestId);
            if (testRequest == null)
            {
                throw new InvalidOperationException("Test request not found.");
            }

            if (!CanEditStatus(testRequest.ReportStatus, isAdministrator))
            {
                throw new InvalidOperationException("This result cannot be entered in the current approval status.");
            }

            var parameters = (request.Parameters ?? new List<TestResultEditParameterSaveDto>())
                .Where(p => !string.IsNullOrWhiteSpace(p.ResultValue))
                .ToList();
            if (!parameters.Any())
            {
                throw new ArgumentException("Enter at least one parameter value before saving.");
            }

            var existing = resultRepo.Get(res => res.TestRequestId == testRequest.Id).FirstOrDefault();

            var allParams = parameterRepo.Get().ToList();
            var equipment = equipmentRepo.Get(e => e.IsActive).FirstOrDefault();

            var testCode = testRequest.HISTestCode;
            var lisTestCode = ResolveListTestCode(testCode);
            var now = DateTime.Now;
            var editor = identity?.ActivityMember ?? "system";

            long resultId;
            if (existing != null)
            {
                var hasDetails = detailRepo.Get(d => d.TestResultId == existing.Id).Any();
                if (hasDetails)
                {
                    throw new InvalidOperationException("Results already exist for this test. Reload the sample and try again.");
                }

                resultId = existing.Id;
            }
            else
            {
                var testResult = new TestResult
                {
                    PatientId = testRequest.PatientId,
                    HISTestCode = testCode,
                    LISTestCode = lisTestCode,
                    SampleNo = testRequest.SampleNo,
                    SpecimenCode = testRequest.SpecimenCode,
                    SpecimenName = testRequest.SpecimenName,
                    SampleCollectionDate = testRequest.SampleCollectionDate,
                    SampleReceivedDate = testRequest.SampleReceivedDate,
                    TestRequestId = testRequest.Id,
                    EquipmentId = equipment?.Id,
                    ResultDate = now,
                    CreatedBy = editor,
                    CreatedOn = now
                };
                resultId = resultRepo.Add(testResult);
            }

            foreach (var change in parameters)
            {
                var paramMaster = FindParameterMaster(allParams, testCode, change.ParameterCode);
                var paramCode = paramMaster?.LISParamCode ?? paramMaster?.HISParamCode ?? change.ParameterCode;
                if (string.IsNullOrWhiteSpace(paramCode))
                {
                    throw new InvalidOperationException($"Unknown parameter code '{change.ParameterCode}'.");
                }

                detailRepo.Add(new TestResultDetails
                {
                    LISParamCode = paramCode,
                    HISParamCode = paramMaster?.HISParamCode ?? change.ParameterCode,
                    ParamValue = change.ResultValue.Trim(),
                    ParamUnit = paramMaster?.HISParamUnit,
                    TestResultId = resultId,
                    CreatedBy = editor,
                    CreatedOn = now
                });
            }

            if (testRequest.ReportStatus == ReportStatusType.New ||
                testRequest.ReportStatus == ReportStatusType.SentToEquipment)
            {
                testRequest.ReportStatus = ReportStatusType.ReportGenerated;
            }

            requestRepo.Update(testRequest);

            logger.LogInfo(
                $"TestResultEdit: Manual entry Sample={testRequest.SampleNo} TestRequestId={testRequest.Id} TestResultId={resultId} By={editor}");

            return new TestResultEditSaveResult
            {
                Success = true,
                Message = "Results entered successfully.",
                ReportStatus = (int)testRequest.ReportStatus,
                ReportStatusLabel = FormatReportStatus(testRequest.ReportStatus)
            };
        }

        private string ResolveListTestCode(string hisTestCode)
        {
            return hisTestCode;
        }

        private List<HISParameterMaster> ResolveParametersForTest(string testCode, List<HISParameterMaster> allParams)
        {
            if (string.IsNullOrWhiteSpace(testCode) || allParams == null || !allParams.Any())
            {
                return new List<HISParameterMaster>();
            }

            var test = testRepo.Get()
                .FirstOrDefault(t => t.HISTestCode != null &&
                    t.HISTestCode.Equals(testCode.Trim(), StringComparison.OrdinalIgnoreCase));
            if (test != null)
            {
                var mappedIds = testParamMappingRepo.Get(m => m.IsActive && m.HisTestId == test.Id)
                    .Select(m => m.HisParameterId)
                    .ToHashSet();
                if (mappedIds.Any())
                {
                    return allParams
                        .Where(p => mappedIds.Contains(p.Id))
                        .OrderBy(p => p.HISParamDescription ?? p.HISParamCode)
                        .ToList();
                }
            }

            return allParams
                .Where(p => p.HISTestCode != null &&
                    p.HISTestCode.Equals(testCode.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.HISParamDescription ?? p.HISParamCode)
                .ToList();
        }

        private HISParameterMaster FindParameterMaster(
            List<HISParameterMaster> allParams,
            string testCode,
            string parameterCode)
        {
            if (string.IsNullOrWhiteSpace(testCode) || string.IsNullOrWhiteSpace(parameterCode))
            {
                return null;
            }

            return ResolveParametersForTest(testCode, allParams).FirstOrDefault(p =>
                (p.HISParamCode != null && p.HISParamCode.Equals(parameterCode, StringComparison.OrdinalIgnoreCase)) ||
                (p.LISParamCode != null && p.LISParamCode.Equals(parameterCode, StringComparison.OrdinalIgnoreCase)));
        }

        private bool HasEditableParameters(string testCode, List<HISParameterMaster> allParams)
        {
            return ResolveParametersForTest(testCode, allParams).Any();
        }

        private bool HasEditableParametersCached(
            string testCode,
            List<HISParameterMaster> allParams,
            Dictionary<string, bool> cache)
        {
            if (string.IsNullOrWhiteSpace(testCode))
            {
                return false;
            }

            if (cache.TryGetValue(testCode, out var cached))
            {
                return cached;
            }

            var hasParams = HasEditableParameters(testCode, allParams);
            cache[testCode] = hasParams;
            return hasParams;
        }

        private HashSet<long> GetRequestIdsWithPersistedResults(IList<long> requestIds)
        {
            var persisted = new HashSet<long>();
            if (requestIds == null || requestIds.Count == 0)
            {
                return persisted;
            }

            var results = resultRepo.Get(r => requestIds.Contains(r.TestRequestId)).ToList();
            if (!results.Any())
            {
                return persisted;
            }

            var latestResults = results
                .GroupBy(r => r.TestRequestId)
                .Select(g => g.OrderByDescending(x => x.Id).First())
                .ToList();

            var resultIds = latestResults.Select(r => r.Id).ToList();
            var resultIdsWithDetails = detailRepo.Get(d => resultIds.Contains(d.TestResultId))
                .Select(d => d.TestResultId)
                .Distinct()
                .ToHashSet();

            foreach (var result in latestResults)
            {
                if (resultIdsWithDetails.Contains(result.Id))
                {
                    persisted.Add(result.TestRequestId);
                }
            }

            return persisted;
        }

        private bool HasPersistedResults(long testRequestId)
        {
            var result = resultRepo.Get(res => res.TestRequestId == testRequestId)
                .OrderByDescending(res => res.Id)
                .FirstOrDefault();
            if (result == null)
            {
                return false;
            }

            return detailRepo.Get(d => d.TestResultId == result.Id).Any();
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
