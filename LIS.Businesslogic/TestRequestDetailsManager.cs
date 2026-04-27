using LIS.BusinessLogic;
using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
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
        private ModuleRepo<TestResult> resultRepo;
        private ModuleRepo<TestResultDetails> resultDetailsRepo;
        private ModuleRepo<TestParameter> parameterRepo;
        private ModuleRepo<HISParameterMaster> parameterMapRepo;
        private ModuleRepo<HisTestMaster> testRepo;
        private ModuleRepo<Departments> departmentRepo;
        private ModuleRepo<HISParameterRangMaster> parameteRangeRepo;
        private IExternalApiManager externalApiManager;
        private IFileHandler file;
        private IModuleIdentity identity;
        private GenericUnitOfWork genericUnitOfWork;
        public TestRequestDetailsManager(ILogger Logger, IModuleIdentity identity, GenericUnitOfWork genericUnitOfWork, IFileHandler file)
        {
            this.identity = identity;
            logger = Logger;
            this.genericUnitOfWork = genericUnitOfWork;
            this.file = file;
            testRequestDetailsRepo = new ModuleRepo<TestRequestDetail>(logger, this.identity, this.genericUnitOfWork);
            mappingRepo = new ModuleRepo<TestMappingMaster>(logger, this.identity, this.genericUnitOfWork);
            equipmentRepo = new ModuleRepo<EquipmentMaster>(logger, this.identity, this.genericUnitOfWork);
            patientRepo = new ModuleRepo<PatientDetail>(logger, this.identity, this.genericUnitOfWork);
            resultRepo = new ModuleRepo<TestResult>(logger, this.identity, this.genericUnitOfWork);
            resultDetailsRepo = new ModuleRepo<TestResultDetails>(logger, this.identity, this.genericUnitOfWork);
            externalApiManager = new ExternalApiManager(logger, this.identity, this.genericUnitOfWork, this.file);
            parameterRepo = new ModuleRepo<TestParameter>(logger, this.identity, this.genericUnitOfWork);
            parameterMapRepo = new ModuleRepo<HISParameterMaster>(logger, this.identity, this.genericUnitOfWork);
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
            ReviewProcess(Id, reportStatusType, note, recentTestRequestId);
        }

        public void DoctorReview(long Id, ReportStatusType reportStatusType, string note, long recentTestRequestId)
        {
            var requestDetail = ReviewProcess(Id, reportStatusType, note, recentTestRequestId);

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

        private TestRequestDetail ReviewProcess(long Id, ReportStatusType reportStatusType, string note, long recentTestRequestId)
        {
            if (reportStatusType == ReportStatusType.New)
            {
                return CreateNewTestRequest(Id);

            }
            else
            {
                var testRequestDetail = testRequestDetailsRepo.Get(Id);
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
                        testResult.TechnicianNote = $"{testResult.TechnicianNote}[{DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss")}] {this.identity.ActivityMember} : {note}<br>";
                    }
                    resultRepo.Update(testResult);

                }
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
                BedNo = testOldReqDetail.BedNo,
                Department = testOldReqDetail.Department,
                DepartmentId = testOldReqDetail.DepartmentId,
                HISRequestId = testOldReqDetail.HISRequestId,
                HISRequestNo = testOldReqDetail.HISRequestNo,
                IPNo = testOldReqDetail.IPNo,
                LISTestCode = testOldReqDetail.LISTestCode,
                MRNo = testOldReqDetail.MRNo

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
                                            det.LISParamValue,
                                            det.LISParamUnit
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
                                            ParamValue = v.LISParamValue,
                                            ParamUnit = v.LISParamUnit,
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

            //Get Equipment testname
            var availableTest = file.GetJsonMappings(code.Model);

            foreach (var run in testRuns)
            {
                foreach (var item in run.TestValues)
                {
                    var paramMap = testMap.Where(m => m.LISTestCode.Equals(result.LISTestCode, StringComparison.OrdinalIgnoreCase))
                        .Join(parameterMapRepo.Get(p => p.HISTestCode.Equals(result.HISTestCode, StringComparison.OrdinalIgnoreCase)
                        && p.LISParamCode.Equals(item.LISParamCode, StringComparison.OrdinalIgnoreCase)),
                          map => map.HISTestCode,        // Select the primary key (the first part of the "on" clause in an sql "join" statement)
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

                        var paramRanges = parameteRangeRepo
                            .Get(p => p.HISRangeCode.Equals(paramMap.HISParamCode, StringComparison.OrdinalIgnoreCase))
                            .ToList()
                            .Distinct();
                        var ranges = new List<string>();
                        foreach (var range in paramRanges)
                        {
                            ranges.Add($"{range.Gender} {range.AgeFrom} - {range.AgeTo} {range.AgeType} : ( {range.HISRangeValue} )");
                        }

                        item.HISRangeValues = ranges.Distinct().ToArray();

                        if (string.IsNullOrEmpty(item.ParamUnit))
                        {
                            item.ParamUnit = paramMap.HISParamUnit;
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(item.HISParamName))
                        {
                            var eqptest = availableTest.Where(t => t.Code.Equals(item.LISParamCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                            item.HISParamName = eqptest.Description;
                        }
                    }
                }

                run.ReviewDate = run.ReviewDate == null ? result.ResultDate : run.ReviewDate;
            }



            return testRuns;
        }
        public ReviewTest GetTestResultByRequestId(long RequestId)
        {
            var testResult = resultRepo.Get(p => p.TestRequestId == RequestId)
                .FirstOrDefault();

            if (testResult == null)
            {
                return null;
            }

            var testMap = mappingRepo.Get(p => p.HISTestCode.Equals(testResult.HISTestCode, StringComparison.OrdinalIgnoreCase)
                && p.EquipmentId == testResult.EquipmentId).FirstOrDefault();

            var departmentname = testRepo.Get(t => t.HISTestCode.Equals(testResult.HISTestCode, StringComparison.OrdinalIgnoreCase))
                .Join(departmentRepo.Get(d => d.Code != null),
                test => test.DepartmentCode,
                dept => dept.Code,
                (test, dept) => new { dept.Name }).FirstOrDefault();

            var reviewTest = new ReviewTest
            {
                Test = new Test
                {
                    Age = testResult.Patient.Age,
                    Gender = testResult.Patient.Gender,
                    Department = departmentname.Name,
                    PatientId = testResult.Patient.Id,
                    PatientName = testResult.Patient.Name,
                    SampleNo = testResult.SampleNo,
                    HisPatientId = testResult.Patient.HisPatientId,
                    TestName = testMap == null ? "" : testMap.HISTestCodeDescription,
                    SpecimenName = testMap == null ? "" : testMap.SpecimenName,
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

        public IEnumerable<TestRequestDetail> GetBySampleNo(string SampleNo, ReportStatusType status)
        {
            var requestDetails = new List<TestRequestDetail>();

            var testRequestDetails = testRequestDetailsRepo
                                        .Get(p => p.SampleNo.Equals(SampleNo
                                                        , StringComparison.OrdinalIgnoreCase)
                                                  && p.ReportStatus == status);

            var mappingInfo = mappingRepo.Get(p => p.IsActive == true
                                                && p.Equipment.AccessKey.Equals(identity.AccessKey
                                                                    , StringComparison.OrdinalIgnoreCase));
            var patients = patientRepo.Get(p => p.IsActive == true);

            requestDetails = (from m in mappingInfo
                              join p in testRequestDetails on m.HISTestCode equals p.HISTestCode
                              join tq in patients on p.PatientId equals tq.Id
                              select new
                              {
                                  p.Id,
                                  p.PatientId,
                                  p.SpecimenName,
                                  m.LISTestCode,
                                  p.HISTestCode,
                                  p.SampleCollectionDate,
                                  p.SampleReceivedDate,
                                  p.SampleNo,
                                  p.BedNo,
                                  m.EquipmentId,
                                  p.CreatedBy,
                                  p.CreatedOn,
                                  tq
                              }).AsEnumerable().Select(u => new TestRequestDetail
                              {
                                  Id = u.Id,
                                  PatientId = u.PatientId,
                                  HISTestCode = u.HISTestCode,
                                  SampleCollectionDate = u.SampleCollectionDate,
                                  SampleReceivedDate = u.SampleReceivedDate,
                                  SampleNo = u.SampleNo,
                                  BedNo = u.BedNo,
                                  LISTestCode = u.LISTestCode,
                                  SpecimenName = u.SpecimenName,
                                  CreatedBy = u.CreatedBy,
                                  CreatedOn = u.CreatedOn,
                                  Patient = u.tq
                              }).ToList();

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
                                  p.BedNo,
                                  p.IPNo,
                                  tq
                              }).AsEnumerable().Distinct().Select(u => new TestRequestDetail
                              {
                                  PatientId = u.PatientId,
                                  SampleCollectionDate = u.SampleCollectionDate,
                                  SampleNo = u.SampleNo,
                                  HISTestName = u.HISTestName,
                                  HISRequestNo = u.HISRequestNo,
                                  BedNo = u.BedNo,
                                  IPNo = u.IPNo,
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
                                  p.BedNo,
                                  p.IPNo,
                                  tq
                              }).AsEnumerable().Select(u => new TestRequestDetail
                              {
                                  PatientId = u.PatientId,
                                  SampleCollectionDate = u.SampleCollectionDate,
                                  SampleNo = u.SampleNo,
                                  HISTestName = u.HISTestName,
                                  HISRequestNo = u.HISRequestNo,
                                  BedNo = u.BedNo,
                                  IPNo = u.IPNo,
                                  Patient = u.tq
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
                .Join(testRequestDetails,
                map => map.HISTestCode,
                test => test.HISTestCode,
                (map, test) => new { TestCode = map.HISTestCode })
                .Join(mappingInfo,
                res => res.TestCode,
                newmap => newmap.HISTestCode,
                 (res, newmap) => new { TestCode = newmap.HISTestCode }).GroupBy(t => t.TestCode)
                .Select(group => new { TestCode = group.Key, TestCount = group.Count() }).FirstOrDefault();

            if (result == null)
                return false;

            if (result.TestCount > 1)
                return true;
            else
                return false;
        }
        public List<TestRequestDetail> GetRequestDetails(string SampleNo, string lisTestCode)
        {
            var requestDetails = new List<TestRequestDetail>();

            var testMappings = mappingRepo.Get(p => p.IsActive == true && p.LISTestCode.Equals(lisTestCode, StringComparison.OrdinalIgnoreCase))
                                        .Join(equipmentRepo.Get(e => e.AccessKey.Equals(identity.AccessKey, StringComparison.OrdinalIgnoreCase)),
                                        map => map.EquipmentId,
                                        eqp => eqp.Id,
                                        (map, eqp) => new
                                        {
                                            map.HISTestCode,
                                            map.LISTestCode
                                        }).Distinct();

            var testRequestDetails = testMappings
                                        .Join(testRequestDetailsRepo.Get(p => p.SampleNo.Equals(SampleNo, StringComparison.OrdinalIgnoreCase)
                                        && (p.ReportStatus == ReportStatusType.SentToEquipment || p.ReportStatus == ReportStatusType.ReportGenerated)),
                                         map => map.HISTestCode,
                                         req => req.HISTestCode,
                                         (map, req) => req).ToList();

            return testRequestDetails;
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
            return parameters;
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


    }

}
