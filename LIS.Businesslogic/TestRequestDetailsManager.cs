using LIS.BusinessLogic;
using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.DtoModel.Models.Notification;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LIS.Businesslogic
{
    public class TestRequestDetailsManager : ITestRequestDetailsManager
    {
        private ILogger logger;

        private ModuleRepo<TestRequestDetail> testRequestDetailsRepo;
        private ModuleRepo<TestMappingMaster> mappingRepo;
        private ModuleRepo<EquipmentMaster> equipmentRepo;
        private ModuleRepo<PatientDetail> patientRepo;
        private ModuleRepo<PatientVisit> patientVisitRepo;
        private ModuleRepo<TestResult> resultRepo;
        private ModuleRepo<TestResultDetails> resultDetailsRepo;
        private ModuleRepo<TestParameter> parameterRepo;
        private ModuleRepo<HISParameterMaster> parameterMapRepo;
        private ModuleRepo<TestParameterMappingMaster> testParameterMappingRepo;
        private ModuleRepo<HisTestMaster> testRepo;
        private ModuleRepo<Departments> departmentRepo;
        private ModuleRepo<HISParameterRangMaster> parameteRangeRepo;
        private IExternalApiManager externalApiManager;
        private INotificationManager notificationManager;
        private IFileHandler file;
        private IModuleIdentity identity;
        private GenericUnitOfWork genericUnitOfWork;
        public TestRequestDetailsManager(ILogger Logger, IModuleIdentity identity, GenericUnitOfWork genericUnitOfWork, IFileHandler file, INotificationManager notificationManager = null)
        {
            this.identity = identity;
            logger = Logger;
            this.genericUnitOfWork = genericUnitOfWork;
            this.file = file;
            this.notificationManager = notificationManager;
            testRequestDetailsRepo = new ModuleRepo<TestRequestDetail>(logger, this.identity, this.genericUnitOfWork);
            mappingRepo = new ModuleRepo<TestMappingMaster>(logger, this.identity, this.genericUnitOfWork);
            equipmentRepo = new ModuleRepo<EquipmentMaster>(logger, this.identity, this.genericUnitOfWork);
            patientRepo = new ModuleRepo<PatientDetail>(logger, this.identity, this.genericUnitOfWork);
            patientVisitRepo = new ModuleRepo<PatientVisit>(logger, this.identity, this.genericUnitOfWork);
            resultRepo = new ModuleRepo<TestResult>(logger, this.identity, this.genericUnitOfWork);
            resultDetailsRepo = new ModuleRepo<TestResultDetails>(logger, this.identity, this.genericUnitOfWork);
            externalApiManager = new ExternalApiManager(logger, this.identity, this.genericUnitOfWork, this.file);
            parameterRepo = new ModuleRepo<TestParameter>(logger, this.identity, this.genericUnitOfWork);
            parameterMapRepo = new ModuleRepo<HISParameterMaster>(logger, this.identity, this.genericUnitOfWork);
            testParameterMappingRepo = new ModuleRepo<TestParameterMappingMaster>(logger, this.identity, this.genericUnitOfWork);
            parameteRangeRepo = new ModuleRepo<HISParameterRangMaster>(logger, this.identity, this.genericUnitOfWork);
            testRepo = new ModuleRepo<HisTestMaster>(logger, this.identity, this.genericUnitOfWork);
            departmentRepo = new ModuleRepo<Departments>(logger, this.identity, this.genericUnitOfWork);
        }

        public long Add(TestRequestDetail testRequestDetail)
        {
            return testRequestDetailsRepo.Add(testRequestDetail);
        }

        public void TechnicianReview(long Id, ReportStatusType reportStatusType, string note, long recentTestRequestId)
        {
            ReviewProcess(Id, reportStatusType, note, recentTestRequestId, isDoctorReview: false);
        }

        public void DoctorReview(long Id, ReportStatusType reportStatusType, string note, long recentTestRequestId)
        {
            var requestDetail = ReviewProcess(Id, reportStatusType, note, recentTestRequestId, isDoctorReview: true);

            if (requestDetail != null && requestDetail.ReportStatus == ReportStatusType.DoctorApproved)
            {
                TryCreateReportReleasedNotification(requestDetail);
            }

            /* //Submit Test Result to HIS is changed through SQL job
            if (requestDetail.ReportStatus == ReportStatusType.DoctorApproved)
            {
                var hisTestResult = externalApiManager.PrepareHISTestResult(requestDetail);

                Task.Run(async () =>
                {
                    await externalApiManager.SubmitHISTestResult(hisTestResult);
                });
            }
            */
        }

        private TestRequestDetail ReviewProcess(long Id, ReportStatusType reportStatusType, string note, long recentTestRequestId, bool isDoctorReview)
        {
            if (reportStatusType == ReportStatusType.New)
            {
                return CreateNewTestRequest(Id);

            }
            else
            {
                var testRequestDetail = testRequestDetailsRepo.Get(Id);
                if (testRequestDetail == null)
                {
                    throw new InvalidOperationException("Test request not found.");
                }

                var testRequestDetails = testRequestDetailsRepo.Get(p => p.SampleNo.Equals(testRequestDetail.SampleNo, StringComparison.OrdinalIgnoreCase)
                                        && p.HISTestCode.Equals(testRequestDetail.HISTestCode, StringComparison.OrdinalIgnoreCase)).ToList();


                if (testRequestDetails != null)
                {
                    foreach (var testRequest in testRequestDetails)
                    {
                        var testResultList = resultRepo.Get(p => p.TestRequestId == testRequest.Id
                                                            && p.HISTestCode.Equals(testRequest.HISTestCode, StringComparison.OrdinalIgnoreCase))
                                                            .ToList();

                        if (testRequest.Id == recentTestRequestId || recentTestRequestId == 0)
                        {
                            ValidateStatusTransition(testRequest.ReportStatus, reportStatusType, isDoctorReview);
                            testRequest.ReportStatus = reportStatusType;
                            UpdateTestResulDetails(testResultList, reportStatusType, note);

                            testRequestDetail = testRequest;
                            testRequestDetailsRepo.Update(testRequest);
                        }
                        else
                        {
                            if (testRequest.ReportStatus != ReportStatusType.TechnicianRejected
                                && testRequest.ReportStatus != ReportStatusType.DoctorRejected
                                && testRequest.ReportStatus != ReportStatusType.FinallyRejected)
                            {
                                UpdateTestResulDetails(testResultList, reportStatusType, note);
                                testRequest.ReportStatus = ReportStatusType.TechnicianRejected;
                                testRequestDetailsRepo.Update(testRequest);
                            }
                        }


                    }

                }

                return testRequestDetail;
            }
        }

        private void UpdateTestResulDetails(List<TestResult> testResultList, ReportStatusType reportStatusType, string note)
        {
            if (testResultList != null)
            {
                var reviewDate = DateTime.Now;
                foreach (var testResult in testResultList)
                {
                    if (reportStatusType == ReportStatusType.TechnicianApproved
                    || reportStatusType == ReportStatusType.TechnicianRejected)
                    {
                        testResult.ReviewedBy = identity.ActivityMember;
                        testResult.ReviewDate = reviewDate;
                        testResult.TechnicianNote = $"{testResult.TechnicianNote}[{DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss")}] {this.identity.ActivityMember} : {note}<br>";
                    }
                    else if (reportStatusType == ReportStatusType.DoctorApproved
                        || reportStatusType == ReportStatusType.DoctorRejected)
                    {
                        testResult.AuthorizedBy = identity.ActivityMember;
                        testResult.AuthorizationDate = reviewDate;
                        var doctorEntry = $"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] {identity.ActivityMember} : {note}<br>";
                        testResult.DoctorNote = (testResult.DoctorNote ?? string.Empty) + doctorEntry;
                    }
                    resultRepo.Update(testResult);

                }
            }
        }

        private static void ValidateStatusTransition(ReportStatusType current, ReportStatusType requested, bool isDoctorReview)
        {
            if (current == requested
                && requested != ReportStatusType.TechnicianApproved)
            {
                throw new InvalidOperationException($"Sample is already in status {FormatStatus(requested)}.");
            }

            switch (requested)
            {
                case ReportStatusType.TechnicianApproved:
                    if (current == ReportStatusType.TechnicianApproved)
                    {
                        if (!isDoctorReview)
                        {
                            throw new InvalidOperationException("Technician approval already recorded for this sample.");
                        }
                        return;
                    }
                    if (current != ReportStatusType.ReportGenerated && current != ReportStatusType.SentToEquipment)
                    {
                        throw new InvalidOperationException(
                            $"Technician approval requires Report Generated status. Current status: {FormatStatus(current)}.");
                    }
                    break;

                case ReportStatusType.TechnicianRejected:
                    if (current != ReportStatusType.ReportGenerated
                        && current != ReportStatusType.SentToEquipment
                        && current != ReportStatusType.TechnicianApproved)
                    {
                        throw new InvalidOperationException(
                            $"Technician rejection is not allowed from status {FormatStatus(current)}.");
                    }
                    break;

                case ReportStatusType.DoctorApproved:
                case ReportStatusType.DoctorRejected:
                    if (current != ReportStatusType.TechnicianApproved)
                    {
                        throw new InvalidOperationException(
                            $"Doctor review requires Technician Approved status. Current status: {FormatStatus(current)}.");
                    }
                    break;

                default:
                    break;
            }
        }

        private static string FormatStatus(ReportStatusType status)
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

        private TestRequestDetail CreateNewTestRequest(long TestRequestId)
        {
            var testOldReqDetail = testRequestDetailsRepo.Get(TestRequestId);
            testOldReqDetail.ReportStatus = ReportStatusType.FinallyRejected;
            testRequestDetailsRepo.Update(testOldReqDetail);

            var patientOldDetails = patientRepo.Get(p => p.Id == testOldReqDetail.PatientId).FirstOrDefault();
            var patientDetails = new PatientDetail
            {
                Name = patientOldDetails.Name,
                Age = patientOldDetails.Age,
                Gender = patientOldDetails.Gender,
                Phone = patientOldDetails.Phone,
                IsActive = patientOldDetails.IsActive,
                DateOfBirth = patientOldDetails.DateOfBirth,
                HisPatientId = patientOldDetails.HisPatientId
            };
            var patientId = patientRepo.Add(patientDetails);

            var testRequestDetail = new TestRequestDetail
            {
                SampleNo = testOldReqDetail.SampleNo,
                HISTestCode = testOldReqDetail.HISTestCode,
                HISTestName = testOldReqDetail.HISTestName,
                SampleCollectionDate = testOldReqDetail.SampleCollectionDate,
                SampleReceivedDate = testOldReqDetail.SampleReceivedDate,
                SpecimenCode = testOldReqDetail.SpecimenCode,
                SpecimenName = testOldReqDetail.SpecimenName,
                ReportStatus = ReportStatusType.New,
                PatientId = patientId,
                Department = testOldReqDetail.Department,
                DepartmentId = testOldReqDetail.DepartmentId,
                HISRequestId = testOldReqDetail.HISRequestId,
                HISRequestNo = testOldReqDetail.HISRequestNo,
                LISTestCode = testOldReqDetail.LISTestCode,
            };

            var testRequsDetailstId = testRequestDetailsRepo.Add(testRequestDetail);
            testRequestDetail.Id = testRequsDetailstId;

            var oldParameter = parameterRepo.Get(p => p.TestRequestDetailsId == testOldReqDetail.Id).ToList();
            foreach (var testOldParameter in oldParameter)
            {
                var testParameter = new TestParameter()
                {
                    HISParamCode = testOldParameter.HISParamCode,
                    HISParamName = testOldParameter.HISParamName,
                    HISTestCode = testOldParameter.HISTestCode,
                    TestRequestDetailsId = testRequsDetailstId,
                };

                parameterRepo.Add(testParameter);
            }

            return testRequestDetail;
        }

        public void Delete(TestRequestDetail testRequestDetail)
        {
            testRequestDetailsRepo.Delete(testRequestDetail);
        }

        public IEnumerable<TestRequestDetail> Get(long PatientId)
        {
            var requestDetails = testRequestDetailsRepo
                .Get(p => p.PatientId == PatientId)
                .ToList();
            return requestDetails;
        }

        private IEnumerable<TestRun> GetTestRunDetails(TestResult result)
        {
            var testRuns = resultRepo.Get(p => p.SampleNo.Equals(result.SampleNo, StringComparison.OrdinalIgnoreCase)
                                    && p.HISTestCode.Equals(result.HISTestCode, StringComparison.OrdinalIgnoreCase))
                                    .Join(resultDetailsRepo.Get(d => d.Id > 0),
                                        res => res.Id,
                                        det => det.TestResultId,
                                        (res, det) => new
                                        {
                                            res.TestRequestId,
                                            res.TestRequestDetail.ReportStatus,
                                            res.ReviewDate,
                                            res.ReviewedBy,
                                            det.LISParamCode,
                                            det.ParamValue,
                                            det.ParamUnit
                                        }
                                    )
                                    .GroupBy(t => new { t.TestRequestId, t.ReportStatus, t.ReviewDate, t.ReviewedBy })
                                    .OrderByDescending(p => new { p.Key.TestRequestId, p.Key.ReviewDate })
                                    .Select(group => new TestRun()
                                    {
                                        RunIndex = group.Key.TestRequestId,
                                        ReportStatus = group.Key.ReportStatus,
                                        ReviewDate = group.Key.ReviewDate,
                                        ReviewedBy = group.Key.ReviewedBy,
                                        TestValues = group.Select(v => new TestValues()
                                        {
                                            LISParamCode = v.LISParamCode,
                                            ParamValue = v.ParamValue,
                                            ParamUnit = v.ParamUnit,
                                        })
                                    }).ToList();

            var testMap = mappingRepo.Get(m => m.EquipmentId == result.EquipmentId
                && m.IsActive
               );

            var code = resultRepo.Get(r => r.Id.Equals(result.Id))
                .Join(equipmentRepo.Get(d => d.IsActive),
                    rsl => rsl.EquipmentId,
                    eq => eq.Id,
                     (rsl, eq) => new { eq.Model }).FirstOrDefault();

            //Get Equipment testname (optional JSON mapping per analyzer model)
            var availableTest = file.GetJsonMappings(code?.Model);
            if (availableTest == null)
            {
                availableTest = new List<TestNameItem>();
            }

            foreach (var run in testRuns)
            {
                foreach (var item in run.TestValues)
                {
                    var paramMap = testMap.Where(m => m.LISTestCode.Equals(result.LISTestCode, StringComparison.OrdinalIgnoreCase))
                        .Join(parameterMapRepo.Get(p => p.HISTestCode.Equals(result.HISTestCode, StringComparison.OrdinalIgnoreCase)
                        && p.LISParamCode.Equals(item.LISParamCode, StringComparison.OrdinalIgnoreCase)),
                          map => map.HISParamCode,        // Select the primary key (the first part of the "on" clause in an sql "join" statement)
                          param => param.HISTestCode,   // Select the foreign key (the second part of the "on" clause)
                          (map, para) => new { para.HISParamCode, para.HISParamDescription, para.Id, para.HISParamUnit }) // selection
                                                                                                                          //.Join(parameteRangeRepo.Get(p => p.Id > 0),
                                                                                                                          // para => para.Id,        // Select the primary key (the first part of the "on" clause in an sql "join" statement)
                                                                                                                          // range => range.HisParameterId,   // Select the foreign key (the second part of the "on" clause)
                                                                                                                          // (para, range) => new { para.HISParamCode, para.HISParamDescription, range.HISRangeValue }) // selection
                       .FirstOrDefault();


                    if (paramMap != null)
                    {
                        item.HISParamCode = paramMap.HISParamCode;
                        item.HISParamName = paramMap.HISParamDescription;

                        // Ranges belong to HISParameterMaster via HisParameterId (not HISRangeCode == HISParamCode).
                        item.HISRangeValues = BuildRangeValueDisplay(paramMap.Id);

                        if (string.IsNullOrEmpty(item.ParamUnit))
                        {
                            item.ParamUnit = paramMap.HISParamUnit;
                        }
                    }
                    else
                    {
                        // Fallback 1: legacy HISParameterMaster rows keyed by HISTestCode.
                        var hisParam = parameterMapRepo.Get(p =>
                                p.HISTestCode != null
                                && result.HISTestCode != null
                                && p.HISTestCode.Equals(result.HISTestCode, StringComparison.OrdinalIgnoreCase)
                                && (
                                    (p.LISParamCode != null && item.LISParamCode != null
                                        && p.LISParamCode.Equals(item.LISParamCode, StringComparison.OrdinalIgnoreCase))
                                    || (p.HISParamCode != null && item.LISParamCode != null
                                        && p.HISParamCode.Equals(item.LISParamCode, StringComparison.OrdinalIgnoreCase))
                                    || (p.HISParamCode != null && item.HISParamCode != null
                                        && p.HISParamCode.Equals(item.HISParamCode, StringComparison.OrdinalIgnoreCase))))
                            .FirstOrDefault();

                        // Fallback 2: Sale Invoice / Test Parameter Mapping — parameter may belong to another
                        // HISTestCode on HISParameterMaster while linked to this test via TestParameterMappingMaster.
                        if (hisParam == null)
                        {
                            hisParam = ResolveParameterViaTestMapping(
                                result.HISTestCode,
                                item.LISParamCode,
                                item.HISParamCode);
                        }

                        if (hisParam != null)
                        {
                            item.HISParamCode = hisParam.HISParamCode;
                            if (string.IsNullOrEmpty(item.HISParamName))
                            {
                                item.HISParamName = hisParam.HISParamDescription ?? hisParam.HISParamCode;
                            }
                            if (string.IsNullOrEmpty(item.ParamUnit))
                            {
                                item.ParamUnit = hisParam.HISParamUnit;
                            }
                            item.HISRangeValues = BuildRangeValueDisplay(hisParam.Id);
                        }
                        else if (string.IsNullOrEmpty(item.HISParamName))
                        {
                            var equipmentTest = availableTest.FirstOrDefault(t => t.Code != null && t.Code.Equals(item.LISParamCode, StringComparison.OrdinalIgnoreCase));
                            item.HISParamName = equipmentTest != null ? equipmentTest.Description : item.LISParamCode;
                        }
                    }
                }

                run.ReviewDate = run.ReviewDate == null ? result.ResultDate : run.ReviewDate;
            }

            // Display parameters in ascending Sequence from Test Parameter Mapping
            // (Technician/Doctor Approval, Approved and Rejected sample details).
            var sequenceByParamCode = BuildParameterSequenceLookup(result.HISTestCode);
            if (sequenceByParamCode.Count > 0)
            {
                foreach (var run in testRuns)
                {
                    run.TestValues = run.TestValues
                        .OrderBy(v => GetParameterSequence(sequenceByParamCode, v))
                        .ToList();
                }
            }

            return testRuns;
        }

        /// <summary>
        /// Maps HIS/LIS parameter codes of the test's active Test Parameter Mappings to their Sequence.
        /// </summary>
        private Dictionary<string, int> BuildParameterSequenceLookup(string hisTestCode)
        {
            var lookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(hisTestCode))
            {
                return lookup;
            }

            var hisTest = testRepo.Get(t =>
                    t.HISTestCode != null
                    && t.HISTestCode.Equals(hisTestCode.Trim(), StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
            if (hisTest == null)
            {
                return lookup;
            }

            var mappings = testParameterMappingRepo
                .Get(m => m.IsActive && m.HisTestId == hisTest.Id)
                .ToList();
            foreach (var map in mappings)
            {
                var param = parameterMapRepo.Get(map.HisParameterId);
                if (param == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(param.HISParamCode) && !lookup.ContainsKey(param.HISParamCode))
                {
                    lookup[param.HISParamCode] = map.Sequence;
                }
                if (!string.IsNullOrWhiteSpace(param.LISParamCode) && !lookup.ContainsKey(param.LISParamCode))
                {
                    lookup[param.LISParamCode] = map.Sequence;
                }
            }

            return lookup;
        }

        private static int GetParameterSequence(Dictionary<string, int> sequenceByParamCode, TestValues value)
        {
            if (value.HISParamCode != null && sequenceByParamCode.TryGetValue(value.HISParamCode, out var seq))
            {
                return seq;
            }
            if (value.LISParamCode != null && sequenceByParamCode.TryGetValue(value.LISParamCode, out seq))
            {
                return seq;
            }

            return int.MaxValue;
        }

        /// <summary>
        /// Resolves HISParameterMaster for a test via TestParameterMappingMaster when the parameter's
        /// own HISTestCode does not match the request test (Sale Invoice / mapping-master model).
        /// </summary>
        private HISParameterMaster ResolveParameterViaTestMapping(
            string hisTestCode,
            string lisParamCode,
            string hisParamCode)
        {
            if (string.IsNullOrWhiteSpace(hisTestCode))
            {
                return null;
            }

            var hisTest = testRepo.Get(t =>
                    t.HISTestCode != null
                    && t.HISTestCode.Equals(hisTestCode.Trim(), StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
            if (hisTest == null)
            {
                return null;
            }

            var mappings = testParameterMappingRepo
                .Get(m => m.IsActive && m.HisTestId == hisTest.Id)
                .ToList();
            if (mappings.Count == 0)
            {
                return null;
            }

            foreach (var map in mappings)
            {
                var param = parameterMapRepo.Get(map.HisParameterId);
                if (param == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(lisParamCode)
                    && (
                        (param.LISParamCode != null
                            && param.LISParamCode.Equals(lisParamCode, StringComparison.OrdinalIgnoreCase))
                        || (param.HISParamCode != null
                            && param.HISParamCode.Equals(lisParamCode, StringComparison.OrdinalIgnoreCase))))
                {
                    return param;
                }

                if (!string.IsNullOrWhiteSpace(hisParamCode)
                    && param.HISParamCode != null
                    && param.HISParamCode.Equals(hisParamCode, StringComparison.OrdinalIgnoreCase))
                {
                    return param;
                }
            }

            return null;
        }

        /// <summary>
        /// Builds reference-range lines from Parameter Range Master for approval / report screens.
        /// Uses HisParameterId linkage and surfaces HISRangeValue (Range Value).
        /// </summary>
        private string[] BuildRangeValueDisplay(int hisParameterId)
        {
            if (hisParameterId <= 0)
            {
                return Array.Empty<string>();
            }

            var paramRanges = parameteRangeRepo.Get(p => p.HisParameterId == hisParameterId).ToList();
            if (paramRanges.Count == 0)
            {
                return Array.Empty<string>();
            }

            var ranges = new List<string>();
            foreach (var range in paramRanges)
            {
                var rangeValue = !string.IsNullOrWhiteSpace(range.HISRangeValue)
                    ? range.HISRangeValue.Trim()
                    : FormatMinMaxRange(range.MinValue, range.MaxValue);

                if (string.IsNullOrWhiteSpace(rangeValue))
                {
                    continue;
                }

                ranges.Add($"{range.Gender} {range.AgeFrom} - {range.AgeTo} {range.AgeType} : ( {rangeValue} )");
            }

            return ranges.Distinct().ToArray();
        }

        private static string FormatMinMaxRange(decimal minValue, decimal maxValue)
        {
            if (minValue <= 0 && maxValue <= 0)
            {
                return null;
            }

            return $"{minValue} - {maxValue}";
        }

        public ReviewTest GetTestResultByRequestId(long RequestId)
        {
            var testResult = resultRepo.Get(p => p.TestRequestId == RequestId)
                .FirstOrDefault();

            if (testResult == null)
            {
                return null;
            }

            var departmentname = testRepo.Get(t => t.HISTestCode.Equals(testResult.HISTestCode, StringComparison.OrdinalIgnoreCase))
                .Join(departmentRepo.Get(d => d.Code != null),
                test => test.DepartmentCode,
                dept => dept.Code,
                (test, dept) => new { dept.Name, test.HISSpecimenName, test.HISTestCodeDescription }).FirstOrDefault();

            var reviewTest = new ReviewTest
            {
                Test = new Test
                {
                    MRNo = testResult.Patient.MRNo,
                    Age = testResult.Patient.Age,
                    Gender = testResult.Patient.Gender,
                    Department = departmentname != null ? departmentname.Name : string.Empty,
                    PatientId = testResult.Patient.Id,
                    PatientName = testResult.Patient.Name,
                    SampleNo = testResult.SampleNo,
                    HisPatientId = testResult.Patient.HisPatientId,
                    TestName = departmentname.HISTestCodeDescription,
                    SpecimenName = departmentname.HISSpecimenName,
                    SampleCollectionDate = testResult.SampleCollectionDate,
                    SampleReceivedDate = testResult.SampleReceivedDate,
                    ReportDate = testResult.ResultDate,
                    ApprovedBy = testResult.AuthorizedBy,
                    ApprovedOn = testResult.AuthorizationDate,
                    ReviewedBy = testResult.ReviewedBy,
                    ReviewDate = testResult.ReviewDate,
                    TechnicianNote = testResult.TechnicianNote,
                    DoctorNote = testResult.DoctorNote,
                },
                TestRuns = GetTestRunDetails(testResult)
            };

            return reviewTest;
        }

        public IEnumerable<TestResultDetails> GetTestResultDetailsByRequestId(long ResultId)
        {
            var results = resultDetailsRepo.Get(p => p.TestResultId == ResultId)
                .ToList();
            return results;
        }

        public IEnumerable<LISDto> GetBySampleNo(string sampleNo)
        {
            try
            {
                var requestDetails = new List<LISDto>();

                var testRequestDetails = testRequestDetailsRepo
                                            .Get(p => p.SampleNo.Equals(sampleNo
                                                            , StringComparison.OrdinalIgnoreCase))
                                            .ToList();

                var mappingInfo = mappingRepo.Get(p => p.IsActive == true
                                                        && p.Equipment.AccessKey.Equals(identity.AccessKey
                                                                    , StringComparison.OrdinalIgnoreCase))
                                            .ToList();

                var patients = patientRepo.Get(p => p.IsActive == true)
                                            .ToList();

                var testtest = (from pm in testParameterMappingRepo.Get(p => p.IsActive)
                                join test in testRepo.Get(t => t.IsActive)
                                    on pm.HisTestId equals test.Id
                                select new
                                {
                                    test.HISTestCode,
                                    pm.HisParameterId
                                }).ToList();

                var testParam = (from ts in testtest
                                 join pm in parameterMapRepo.Get()
                                     on ts.HisParameterId equals pm.Id
                                 select new
                                 {
                                     ts.HISTestCode,
                                     pm.HISParamCode
                                 }).ToList();

                requestDetails = (from m in mappingInfo
                                  join param in testParam on m.HISParamCode equals param.HISParamCode
                                  join p in testRequestDetails on param.HISTestCode equals p.HISTestCode
                                  join tq in patients on p.PatientId equals tq.Id
                                  select new
                                  {
                                      p.Id,
                                      p.PatientId,
                                      p.SpecimenName,
                                      m.LISTestCode,
                                      p.SampleNo,
                                      p.SampleCollectionDate,
                                      m.GroupName,
                                      tq
                                  }).AsEnumerable().Select(u => new LISDto
                                  {
                                      TestRequestId = u.Id,
                                      PatientId = u.PatientId,
                                      SampleNo = u.SampleNo,
                                      SampleCollectionDate = u.SampleCollectionDate,
                                      LISTestCode = u.LISTestCode,
                                      SpecimenName = u.SpecimenName,
                                      PatientName = u.tq.Name,
                                      DOB = u.tq.DateOfBirth,
                                      Gender = u.tq.Gender,
                                      GroupName = u.GroupName
                                  }).Distinct().ToList();

                return requestDetails;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error occurred while fetching test request details for SampleNo: {sampleNo}");
                logger.LogException(ex.Message, ex);
                throw;
            }
        }

        public IEnumerable<LISDto> GetDetailsBySampleNoWithAnlyser(string sampleNo, string analyserModel)
        {
            try
            {
                var requestDetails = new List<LISDto>();

                var testRequestDetails = testRequestDetailsRepo
                                            .Get(p => p.SampleNo.Equals(sampleNo
                                                            , StringComparison.OrdinalIgnoreCase))
                                            .ToList();

                var mappingInfo = mappingRepo.Get(p => p.IsActive == true
                                                        && p.Equipment.AccessKey.Equals(identity.AccessKey
                                                                    , StringComparison.OrdinalIgnoreCase))
                                            .ToList();

                var patients = patientRepo.Get(p => p.IsActive == true)
                                            .ToList();

                var testtest = (from pm in testParameterMappingRepo.Get(p => p.IsActive)
                                join test in testRepo.Get(t => t.IsActive)
                                    on pm.HisTestId equals test.Id
                                select new
                                {
                                    test.HISTestCode,
                                    pm.HisParameterId
                                }).ToList();

                var testParam = (from ts in testtest
                                 join pm in parameterMapRepo.Get()
                                     on ts.HisParameterId equals pm.Id
                                 select new
                                 {
                                     ts.HISTestCode,
                                     pm.HISParamCode
                                 }).ToList();

                requestDetails = (from m in mappingInfo
                                  join param in testParam on m.HISParamCode equals param.HISParamCode
                                  join p in testRequestDetails on param.HISTestCode equals p.HISTestCode
                                  join tq in patients on p.PatientId equals tq.Id
                                  select new
                                  {
                                      p.Id,
                                      p.PatientId,
                                      p.SpecimenName,                                      
                                      p.SampleNo,
                                      p.SampleCollectionDate,
                                      m.GroupName,
                                      tq
                                  }).AsEnumerable().Select(u => new LISDto
                                  {
                                      TestRequestId = u.Id,
                                      PatientId = u.PatientId,
                                      SampleNo = u.SampleNo,
                                      SampleCollectionDate = u.SampleCollectionDate,
                                      LISTestCode = "",
                                      SpecimenName = u.SpecimenName,
                                      PatientName = u.tq.Name,
                                      DOB = u.tq.DateOfBirth,
                                      Gender = u.tq.Gender,
                                      GroupName = u.GroupName
                                  }).GroupBy(x => x.GroupName).Select(g => g.First()).ToList();

                return requestDetails;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error occurred while fetching test request details for SampleNo: {sampleNo}");
                logger.LogException(ex.Message, ex);
                throw;
            }
        }

        public IEnumerable<BarCodeDto> GetBarCodeSamples(ReportStatusType status)
        {
            var requestDetails = new List<BarCodeDto>();

            var testRequestDetails = testRequestDetailsRepo
                                        .Get(p => p.ReportStatus == status);

            var patients = patientRepo.Get(p => p.IsActive == true);
            var visits = patientVisitRepo.Get(v => v.IsActive);

            requestDetails = (from p in testRequestDetails
                              join tq in patients on p.PatientId equals tq.Id
                              join pv in visits on p.PatientVisitId equals pv.PatientVisitId into pvJoin
                              from pv in pvJoin.DefaultIfEmpty()
                              select new
                              {
                                  p.PatientId,
                                  p.SampleCollectionDate,
                                  p.HISRequestNo,
                                  p.HISTestName,
                                  p.SampleNo,
                                  p.SpecimenName,
                                  tq.MRNo,
                                  VisitId = pv != null ? pv.VisitId : tq.VisitId,
                                  tq.Name
                              }).AsEnumerable().Distinct().Select(u => new BarCodeDto
                              {
                                  PatientId = u.PatientId,
                                  SampleCollectionDate = u.SampleCollectionDate,
                                  SampleNo = u.SampleNo,
                                  HISTestName = u.HISTestName,
                                  HISRequestNo = u.HISRequestNo,
                                  PatientName = u.Name,
                                  MRNo = u.MRNo,
                                  VisitId = u.VisitId,
                                  SpecimenName = u.SpecimenName
                              }).OrderByDescending(p => p.SampleCollectionDate).ToList();

            return requestDetails;
        }
        public IEnumerable<TestRequestDetail> GetAllNewSamples(ReportStatusType status)
        {
            var requestDetails = new List<TestRequestDetail>();

            var testRequestDetails = testRequestDetailsRepo
                                        .Get(p => p.ReportStatus == status);

            var patients = patientRepo.Get(p => p.IsActive == true);

            requestDetails = (from p in testRequestDetails
                              join tq in patients on p.PatientId equals tq.Id
                              select new
                              {
                                  p.PatientId,
                                  p.SampleCollectionDate,
                                  p.HISRequestNo,
                                  p.HISTestName,
                                  p.SampleNo,
                                  tq
                              }).AsEnumerable().Distinct().Select(u => new TestRequestDetail
                              {
                                  PatientId = u.PatientId,
                                  SampleCollectionDate = u.SampleCollectionDate,
                                  SampleNo = u.SampleNo,
                                  HISTestName = u.HISTestName,
                                  HISRequestNo = u.HISRequestNo,
                                  Patient = u.tq
                              }).OrderByDescending(p => p.SampleCollectionDate).ToList();

            return requestDetails;
        }

        public IEnumerable<TestRequestDetail> GetByHisRequestNo(string RequestNo, ReportStatusType status)
        {
            var requestDetails = new List<TestRequestDetail>();

            var testRequestDetails = testRequestDetailsRepo
                                        .Get(p => p.HISRequestNo.Equals(RequestNo
                                                        , StringComparison.OrdinalIgnoreCase)
                                                  && p.ReportStatus == status);

            var patients = patientRepo.Get(p => p.IsActive == true);

            requestDetails = (from p in testRequestDetails
                              join tq in patients on p.PatientId equals tq.Id
                              select new
                              {
                                  p.PatientId,
                                  p.SampleCollectionDate,
                                  p.HISRequestNo,
                                  p.HISTestName,
                                  p.SampleNo,
                                  tq
                              }).AsEnumerable().Select(u => new TestRequestDetail
                              {
                                  PatientId = u.PatientId,
                                  SampleCollectionDate = u.SampleCollectionDate,
                                  SampleNo = u.SampleNo,
                                  HISTestName = u.HISTestName,
                                  HISRequestNo = u.HISRequestNo,
                                  Patient = u.tq
                              }).ToList();

            return requestDetails;
        }

        public IEnumerable<BarCodeDto> GetBarCodeSamplesByRequestNo(string RequestNo, ReportStatusType status)
        {
            var requestDetails = new List<BarCodeDto>();

            var testRequestDetails = testRequestDetailsRepo
                                        .Get(p => p.HISRequestNo.Equals(RequestNo
                                                        , StringComparison.OrdinalIgnoreCase)
                                                  && p.ReportStatus == status);

            var patients = patientRepo.Get(p => p.IsActive == true);
            var visits = patientVisitRepo.Get(v => v.IsActive);

            requestDetails = (from p in testRequestDetails
                              join tq in patients on p.PatientId equals tq.Id
                              join pv in visits on p.PatientVisitId equals pv.PatientVisitId into pvJoin
                              from pv in pvJoin.DefaultIfEmpty()
                              select new
                              {
                                  p.PatientId,
                                  p.SampleCollectionDate,
                                  p.HISRequestNo,
                                  p.HISTestName,
                                  p.SampleNo,
                                  tq.MRNo,
                                  VisitId = pv != null ? pv.VisitId : tq.VisitId,
                                  tq.Name
                              }).AsEnumerable().Select(u => new BarCodeDto
                              {
                                  PatientId = u.PatientId,
                                  SampleCollectionDate = u.SampleCollectionDate,
                                  SampleNo = u.SampleNo,
                                  HISTestName = u.HISTestName,
                                  HISRequestNo = u.HISRequestNo,
                                  PatientName = u.Name,
                                  MRNo = u.MRNo,
                                  VisitId = u.VisitId,
                              }).ToList();

            return requestDetails;
        }
        public bool IsPanelTest(string SampleNo, string LisHostCode)
        {
            var requestDetails = new List<TestRequestDetail>();

            var testRequestDetails = testRequestDetailsRepo
                                        .Get(p => p.SampleNo.Equals(SampleNo
                                                        , StringComparison.OrdinalIgnoreCase));

            var mappingInfo = mappingRepo.Get(p => p.IsActive == true
                                                && p.Equipment.AccessKey.Equals(identity.AccessKey
                                                                    , StringComparison.OrdinalIgnoreCase));

            var result = mappingInfo.Where(p => p.LISTestCode.Equals(LisHostCode, StringComparison.OrdinalIgnoreCase))
                .Join(parameterMapRepo.Get(),
                map => map.HISParamCode,
                param => param.HISParamCode,
                (map, param) => param.HISTestCode)
                .Join(testRequestDetails,
                testCode => testCode,
                test => test.HISTestCode,
                (testCode, test) => testCode)
                .GroupBy(t => t)
                .Select(group => new { TestCode = group.Key, TestCount = group.Count() }).FirstOrDefault();

            if (result == null)
                return false;

            if (result.TestCount > 1)
                return true;
            else
                return false;
        }
        public List<TestRequestDetail> GetRequestDetails(string sampleNo, int? equipmentId)
        {
            var testCodes =
                from m in mappingRepo.Get(x => x.IsActive && x.EquipmentId == equipmentId)
                join p in parameterMapRepo.Get(x => x.HISTestCode != null)
                    on m.HISParamCode equals p.HISParamCode
                select p.HISTestCode;

            return testRequestDetailsRepo.Get(x =>
                    x.SampleNo.ToLower() == sampleNo.ToLower() &&
                    (x.ReportStatus == ReportStatusType.SentToEquipment ||
                     x.ReportStatus == ReportStatusType.ReportGenerated) &&
                    testCodes.Contains(x.HISTestCode))
                .ToList();
        }

        public void Update(TestRequestDetail testRequestDetail)
        {
            testRequestDetailsRepo.Update(testRequestDetail);
        }

        public bool Ping()
        {
            bool isValid = false;
            var equipment = equipmentRepo.Get(p => p.AccessKey.Equals(identity.AccessKey, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            isValid = (equipment != null);

            return isValid;
        }

        public bool UpdateStatus(long Id, ReportStatusType status)
        {
            var testRequestDetail = testRequestDetailsRepo
                                        .Get(p => p.Id == Id)
                                        .First();
            testRequestDetail.ReportStatus = status;
            Update(testRequestDetail);

            return true;
        }

        public IEnumerable<NameValue> DailySampleSummary()
        {
            var today = DateTime.Today;
            var sampleSummary = testRequestDetailsRepo
                                        .Get(p => p.CreatedOn >= today
                                                && (p.ReportStatus == ReportStatusType.New
                                                    || p.ReportStatus == ReportStatusType.SentToEquipment
                                                    || p.ReportStatus == ReportStatusType.ReportGenerated))
                                        .GroupBy(t => t.ReportStatus)
                                        .Select(group => new NameValue()
                                        {
                                            Name = group.Key.ToString(),
                                            Value = group.Count()
                                        })
                                        .OrderBy(x => x.Name)
                                        .ToList();
            if (sampleSummary.Count == 0)
            {
                sampleSummary.Add(new NameValue() { Name = "New", Value = 0 });
                sampleSummary.Add(new NameValue() { Name = "SentToEquipment", Value = 0 });
                sampleSummary.Add(new NameValue() { Name = "ReportGenerated", Value = 0 });
            }
            return sampleSummary;
        }

        public IEnumerable<NameValue> DailyTechnicianApprovalSummary()
        {
            var today = DateTime.Today;
            var sampleSummary = testRequestDetailsRepo
                                        .Get(p => p.CreatedOn >= today
                                                && (p.ReportStatus == ReportStatusType.ReportGenerated
                                                    || p.ReportStatus == ReportStatusType.TechnicianApproved
                                                    || p.ReportStatus == ReportStatusType.TechnicianRejected))
                                        .GroupBy(t => t.ReportStatus)
                                        .Select(group => new NameValue()
                                        {
                                            Name = group.Key.ToString(),
                                            Value = group.Count()
                                        })
                                        .OrderBy(x => x.Name)
                                        .ToList();
            if (sampleSummary.Count == 0)
            {
                sampleSummary.Add(new NameValue() { Name = "ReportGenerated", Value = 0 });
                sampleSummary.Add(new NameValue() { Name = "TechnicianApproved", Value = 0 });
                sampleSummary.Add(new NameValue() { Name = "TechnicianRejected", Value = 0 });
            }

            return sampleSummary;
        }

        public IEnumerable<NameValue> DailyDoctorApprovalSummary()
        {
            var today = DateTime.Today;
            var sampleSummary = testRequestDetailsRepo
                                        .Get(p => p.CreatedOn >= today
                                                && (p.ReportStatus == ReportStatusType.TechnicianApproved
                                                    || p.ReportStatus == ReportStatusType.DoctorApproved
                                                    || p.ReportStatus == ReportStatusType.DoctorRejected))
                                        .GroupBy(t => t.ReportStatus)
                                        .Select(group => new NameValue()
                                        {
                                            Name = group.Key.ToString(),
                                            Value = group.Count()
                                        })
                                        .OrderBy(x => x.Name)
                                        .ToList();
            if (sampleSummary.Count == 0)
            {
                sampleSummary.Add(new NameValue() { Name = "TechnicianApproved", Value = 0 });
                sampleSummary.Add(new NameValue() { Name = "DoctorApproved", Value = 0 });
                sampleSummary.Add(new NameValue() { Name = "DoctorRejected", Value = 0 });
            }

            return sampleSummary;
        }

        public TestRequestDetail GetTestRequestByRequestId(long RequestId)
        {
            var request = testRequestDetailsRepo.Get(p => p.Id == RequestId).FirstOrDefault();

            return request;
        }

        public IEnumerable<TestRequestDetail> GetTestRequestsBySampleNo(string SampleNo)
        {
            var request = testRequestDetailsRepo.Get(p => p.SampleNo.Equals(SampleNo)).ToList();
            return request;
        }

        public IEnumerable<TestParameter> GetTestParametersByRequestId(long RequestId)
        {
            var parameters = parameterRepo.Get(p => p.TestRequestDetailsId == RequestId)
               .ToList();
            if (parameters != null && parameters.Count > 0)
            {
                return parameters;
            }

            // Sale Invoice / modern intake may create TestRequestDetails without TestParameters rows.
            // Resolve from mapping masters and persist so detail screens and result entry work.
            var request = testRequestDetailsRepo.Get(p => p.Id == RequestId).FirstOrDefault();
            if (request == null)
            {
                return parameters ?? new List<TestParameter>();
            }

            return EnsureTestParameters(request);
        }

        /// <summary>
        /// Creates TestParameters for a request from TestParameterMappingMaster (preferred)
        /// or legacy HISParameterMaster.HISTestCode links. No-op when rows already exist.
        /// </summary>
        public IList<TestParameter> EnsureTestParameters(TestRequestDetail request)
        {
            var existing = parameterRepo.Get(p => p.TestRequestDetailsId == request.Id).ToList();
            if (existing != null && existing.Count > 0)
            {
                return existing;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.HISTestCode))
            {
                return new List<TestParameter>();
            }

            var created = new List<TestParameter>();
            var testCode = request.HISTestCode.Trim();
            var hisTest = testRepo.Get(t =>
                    t.HISTestCode != null
                    && t.HISTestCode.Equals(testCode, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (hisTest != null)
            {
                var mappings = testParameterMappingRepo
                    .Get(m => m.IsActive && m.HisTestId == hisTest.Id)
                    .ToList();

                foreach (var map in mappings)
                {
                    var param = parameterMapRepo.Get(map.HisParameterId);
                    if (param == null || string.IsNullOrWhiteSpace(param.HISParamCode))
                    {
                        continue;
                    }

                    created.Add(AddParameterRow(request.Id, testCode, param.HISParamCode, param.HISParamDescription));
                }
            }

            if (created.Count == 0)
            {
                var legacy = parameterMapRepo
                    .Get(p => p.HISTestCode != null
                        && p.HISTestCode.Equals(testCode, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var param in legacy)
                {
                    if (string.IsNullOrWhiteSpace(param.HISParamCode))
                    {
                        continue;
                    }

                    created.Add(AddParameterRow(request.Id, testCode, param.HISParamCode, param.HISParamDescription));
                }
            }

            return created;
        }

        private TestParameter AddParameterRow(long requestId, string testCode, string paramCode, string paramName)
        {
            var row = new TestParameter
            {
                HISParamCode = paramCode,
                HISParamName = paramName,
                HISTestCode = testCode,
                TestRequestDetailsId = requestId,
                CreatedBy = identity?.ActivityMember,
                CreatedOn = DateTime.UtcNow
            };
            row.Id = parameterRepo.Add(row);
            return row;
        }

        public long[] GetTestResultByRequestId(string SampleNumber)
        {
            var resultHistory = testRequestDetailsRepo.Get(p => p.SampleNo.Equals(SampleNumber, StringComparison.OrdinalIgnoreCase));
            var testsList = new List<long>();
            foreach (var result in resultHistory)
            {
                testsList.Add(result.Id);
            }

            return testsList.ToArray();
        }

        public string GenerateNextRequestNo()
        {
            var count = testRequestDetailsRepo.Get().Count() + 1;
            return $"REQ{count:D4}";
        }

        private void TryCreateReportReleasedNotification(TestRequestDetail requestDetail)
        {
            if (notificationManager == null || requestDetail == null)
            {
                return;
            }

            try
            {
                notificationManager.RaiseNotificationEvent(new ReportReleasedNotificationContext
                {
                    InvoiceNo = requestDetail.HISRequestNo,
                    PatientId = requestDetail.PatientId,
                    TestRequestId = requestDetail.Id
                });
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
            }
        }

    }

}
