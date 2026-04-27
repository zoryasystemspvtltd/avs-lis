using LIS.Businesslogic;
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
    public class ResultManager : IResultManager
    {
        private ILogger logger;
        private ModuleRepo<TestResult> testResultRepo;
        private ModuleRepo<TestResultDetails> resultDetailsRepo;
        private ModuleRepo<ControlResult> controlResultRepo;
        private ModuleRepo<ControlResultDetails> controlResultDetailsRepo;
        private ModuleRepo<EquipmentMaster> equpmentRepo;
        private IModuleIdentity identity;
        private GenericUnitOfWork genericUnitOfWork;
        private ModuleRepo<HISParameterMaster> parameterMapRepo;
        private ModuleRepo<TestMappingMaster> testMappingRepo;
        private IFileHandler file;
        public ResultManager(ILogger Logger, IModuleIdentity identity, GenericUnitOfWork genericUnitOfWork, IFileHandler file)
        {
            this.identity = identity;
            logger = Logger;
            this.genericUnitOfWork = genericUnitOfWork;
            parameterMapRepo = new ModuleRepo<HISParameterMaster>(logger, this.identity, this.genericUnitOfWork);
            testResultRepo = new ModuleRepo<TestResult>(logger, this.identity, this.genericUnitOfWork);
            resultDetailsRepo = new ModuleRepo<TestResultDetails>(logger, this.identity, this.genericUnitOfWork);
            controlResultRepo = new ModuleRepo<ControlResult>(logger, this.identity, this.genericUnitOfWork);
            controlResultDetailsRepo = new ModuleRepo<ControlResultDetails>(logger, this.identity, this.genericUnitOfWork);
            equpmentRepo = new ModuleRepo<EquipmentMaster>(logger, this.identity, this.genericUnitOfWork);
            testMappingRepo = new ModuleRepo<TestMappingMaster>(logger, this.identity, this.genericUnitOfWork);
            this.file = file;
        }
        public long Add(Result result)
        {
            long resultId;
            if (result.TestResult.LISTestCode == null)
            {
                resultId = SaveControlResult(result);
            }
            else
            {
                resultId = SaveTestResult(result);
            }
            return resultId;
        }

        public long GetParameterDetails(string SampleNo, string lisTestCode)
        {
            var resultId = testResultRepo.Get(p => p.SampleNo.Equals(SampleNo, StringComparison.OrdinalIgnoreCase))
                    .Join(parameterMapRepo.Get(p => p.LISParamCode.Equals(lisTestCode, StringComparison.OrdinalIgnoreCase)),
                    test => test.HISTestCode,
                    param => param.HISTestCode,
                     (test, param) => test.Id
                ).FirstOrDefault();

            return resultId;
        }

        private long SaveTestResult(Result result)
        {
            long resultId = 0;
            var equpment = equpmentRepo.Get(e => e.AccessKey.Equals(this.identity.AccessKey)).FirstOrDefault();
            var testRequestDetailManager = new TestRequestDetailsManager(logger, identity, this.genericUnitOfWork, this.file);
            if (equpment.Model == "MindRay")
            {
                var request = testRequestDetailManager.GetBySampleNo(result.TestResult.SampleNo, ReportStatusType.SentToEquipment).FirstOrDefault();
                if (request != null)
                {
                    result.TestResult.LISTestCode = request.LISTestCode;
                }
            }

            var testRequestList = testRequestDetailManager.GetRequestDetails(result.TestResult.SampleNo, result.TestResult.LISTestCode);
            if (testRequestList.Count == 0)
            {
                //Insert calculated/mapped parameter
                var existResultId = GetParameterDetails(result.TestResult.SampleNo, result.TestResult.LISTestCode);
                if (existResultId > 0)
                {
                    foreach (TestResultDetails resultDetail in result.ResultDetails)
                    {
                        resultDetail.TestResultId = existResultId;
                        resultDetailsRepo.Add(resultDetail);
                    }
                    return existResultId;
                }
                else
                {
                    // TODO Add Test 
                    logger.LogDebug($"Requested Test not exists {result?.TestResult?.SampleNo}");
                    resultId = 0;
                    return resultId;
                }
            }

            foreach (var testRequest in testRequestList)
            {
                var existResult = testResultRepo.Get(r => r.TestRequestId.Equals(testRequest.Id) && r.LISTestCode.Equals(result.TestResult.LISTestCode)).FirstOrDefault();

                if (existResult == null)
                {
                    var testResult = new TestResult
                    {
                        PatientId = testRequest.PatientId,
                        HISTestCode = testRequest.HISTestCode,
                        SampleCollectionDate = testRequest.SampleCollectionDate,
                        SampleReceivedDate = testRequest.SampleReceivedDate,
                        SpecimenCode = testRequest.SpecimenCode,
                        SpecimenName = testRequest.SpecimenName,
                        TestRequestId = testRequest.Id,
                        EquipmentId = equpment.Id,
                        ResultDate = result.TestResult.ResultDate,
                        SampleNo = result.TestResult.SampleNo,
                        LISTestCode = result.TestResult.LISTestCode
                    };

                    resultId = testResultRepo.Add(testResult);

                    foreach (TestResultDetails resultDetail in result.ResultDetails)
                    {
                        resultDetail.TestResultId = resultId;
                        resultDetailsRepo.Add(resultDetail);
                    }

                    testRequestDetailManager.UpdateStatus(testRequest.Id, ReportStatusType.ReportGenerated);

                    //DXH800 or Mindray or XN1000 machines for missing tests
                    if (equpment.Model == "DXH800" || equpment.Model == "MindRay" || equpment.Model == "XN1000")
                    {
                        var requestList = testRequestDetailManager.GetBySampleNo(result.TestResult.SampleNo, ReportStatusType.SentToEquipment).ToList();
                        foreach (var subRequest in requestList)
                        {
                            var specimenDetails = testMappingRepo.Get(p => p.HISTestCode.Equals(subRequest.HISTestCode, StringComparison.OrdinalIgnoreCase))
                                .Select(p => new { p.SpecimenCode, p.SpecimenName })
                                .FirstOrDefault();

                            var paramList = parameterMapRepo.Get(p => p.HISTestCode.Equals(subRequest.HISTestCode, StringComparison.OrdinalIgnoreCase))
                                .Select(p => new { p.LISParamCode })
                                .Distinct().ToList();

                            var list = paramList
                                .Join(result.ResultDetails,
                                param => param.LISParamCode,
                                rd => rd.LISParamCode,
                                (param, rd) => rd).ToList();

                            if (list.Count() > 0)
                            {
                                var subResult = new TestResult
                                {
                                    PatientId = subRequest.PatientId,
                                    HISTestCode = subRequest.HISTestCode,
                                    SampleCollectionDate = subRequest.SampleCollectionDate,
                                    SampleReceivedDate = subRequest.SampleReceivedDate,
                                    SpecimenCode = specimenDetails.SpecimenCode,
                                    SpecimenName = specimenDetails.SpecimenName,
                                    TestRequestId = subRequest.Id,
                                    EquipmentId = equpment.Id,
                                    ResultDate = result.TestResult.ResultDate,
                                    SampleNo = result.TestResult.SampleNo,
                                    LISTestCode = subRequest.LISTestCode
                                };

                                var subresultId = testResultRepo.Add(subResult);
                                foreach (TestResultDetails resultDetail in list)
                                {
                                    resultDetail.TestResultId = subresultId;
                                    resultDetailsRepo.Add(resultDetail);
                                }
                                testRequestDetailManager.UpdateStatus(subRequest.Id, ReportStatusType.ReportGenerated);
                            }
                        }
                    }
                }
                else
                {
                    resultId = 0;
                }
            }
            return resultId;
        }

        private long SaveControlResult(Result result)
        {
            long resultId;
            var equpment = equpmentRepo.Get(e => e.AccessKey.Equals(this.identity.AccessKey)).FirstOrDefault();

            var control = new ControlResult()
            {
                SampleNo = result.TestResult.SampleNo,
                ResultDate = DateTime.Now,
                EquipmentId = equpment.Id
            };

            resultId = controlResultRepo.Add(control);

            foreach (var resultDetail in result.ResultDetails)
            {
                var controlDetail = new ControlResultDetails()
                {
                    LISParamCode = resultDetail.LISParamCode,
                    LISParamValue = resultDetail.LISParamValue,
                    LISParamUnit = resultDetail.LISParamUnit,
                    ControlResultId = resultId
                };

                controlResultDetailsRepo.Add(controlDetail);
            }
            return resultId;
        }

        public void Delete(Result result)
        {
            foreach (var entityResultDeatil in result.ResultDetails)
            {
                resultDetailsRepo.Delete(entityResultDeatil); // TODO This may cause error 
            };

            testResultRepo.Delete(result.TestResult);
        }

        public Result Get(string ResultId)
        {
            var entityResultDetails = resultDetailsRepo.Get(p => p.Id.Equals(ResultId)).ToList();
            var entityTestResult = testResultRepo.Get(ResultId);

            var result = new Result
            {
                ResultDetails = entityResultDetails,
                TestResult = entityTestResult
            };

            return result;
        }

        public Result Get(long TestRequestId, string SampleNo)
        {
            var entityTestResult = testResultRepo.Get(p => p.TestRequestId == TestRequestId
                                    && p.SampleNo.Equals(SampleNo, StringComparison.OrdinalIgnoreCase)).ToList();

            var entityResultDetails = new List<TestResultDetails>();
            foreach (var item in entityTestResult)
            {
                var results = resultDetailsRepo.Get(p => p.TestResultId.Equals(item.Id)).ToList();
                entityResultDetails.AddRange(results);
            }


            var result = new Result
            {
                ResultDetails = entityResultDetails,
                TestResult = entityTestResult.FirstOrDefault()
            };

            return result;
        }

        public void Update(Result result)
        {
            var entityResultDetails = resultDetailsRepo.Get(p => p.Id.Equals(result.TestResult.Id)).ToList();
            foreach (var entityResultDeatil in entityResultDetails)
            {
                resultDetailsRepo.Delete(entityResultDeatil);
            };

            foreach (var entityResultDeatil in result.ResultDetails)
            {
                resultDetailsRepo.Add(entityResultDeatil);
            };

            testResultRepo.Update(result.TestResult);
        }

    }
}
