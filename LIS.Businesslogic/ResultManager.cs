using LIS.Businesslogic;
using LIS.BusinessLogic.Helper;
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
        public string GetParameter(string lisTestCode)
        {
            var param = parameterMapRepo.Get(p => p.LISParamCode.Equals(lisTestCode, StringComparison.OrdinalIgnoreCase))
                   .FirstOrDefault();

            return param.HISParamCode;
        }
        private long SaveTestResult(Result result)
        {
            try
            {

                if (result?.TestResult == null)
                    return 0;

                long resultId = 0;

                var testResultInfo = result.TestResult;

                var equipment = equpmentRepo.Get(e => e.AccessKey.Equals(identity.AccessKey)).FirstOrDefault();

                if (equipment == null)
                    return 0;

                var testRequestDetailManager =
                    new TestRequestDetailsManager(logger, identity, genericUnitOfWork, file);

                var testRequests = testRequestDetailManager.GetRequestDetails(testResultInfo.SampleNo, equipment.Id);

                if (!testRequests.Any())
                    return 0;

                // Load existing TestResult IDs once (avoids N+1 query)
                var requestIds = testRequests.Select(x => x.Id).ToList();

                var existingResultIds = testResultRepo
                    .Get(x => requestIds.Contains(x.TestRequestId))
                    .Select(x => x.TestRequestId)
                    .ToHashSet();

                // Load all parameter mappings once
                var lisParamCodes = result.ResultDetails
                    .Select(x => x.LISParamCode)
                    .Distinct()
                    .ToList();

                var parameterMap = testMappingRepo
                    .Get(x => lisParamCodes.Contains(x.LISTestCode))
                    .ToDictionary(x => x.LISTestCode, x => x.HISParamCode);

                foreach (var request in testRequests)
                {
                    if (existingResultIds.Contains(request.Id))
                        continue;

                    var testResult = new TestResult
                    {
                        PatientId = request.PatientId,
                        HISTestCode = request.HISTestCode,
                        SampleCollectionDate = request.SampleCollectionDate,
                        SampleReceivedDate = request.SampleReceivedDate,
                        SpecimenCode = request.SpecimenCode,
                        SpecimenName = request.SpecimenName,
                        TestRequestId = request.Id,
                        EquipmentId = equipment.Id,
                        ResultDate = testResultInfo.ResultDate,
                        SampleNo = testResultInfo.SampleNo,
                        LISTestCode = testResultInfo.LISTestCode,
                        CreatedBy = "LIS"
                    };

                    resultId = testResultRepo.Add(testResult);

                    var details = result.ResultDetails
                            .Select(detail => new TestResultDetails
                            {
                                TestResultId = resultId,
                                CreatedBy = "LIS",
                                LISParamCode = detail.LISParamCode,
                                ParamValue = detail.ParamValue,
                                ParamUnit = detail.ParamUnit,
                                HISParamCode = parameterMap.TryGetValue(detail.LISParamCode, out var code)
                                                ? code
                                                : null
                            }).Where(detail => !string.IsNullOrEmpty(detail.HISParamCode))
                            .ToList();

                    foreach (var detail in details)
                    {
                        resultDetailsRepo.Add(detail);
                    }

                    testRequestDetailManager.UpdateStatus(request.Id, ReportStatusType.ReportGenerated);
                }

                return resultId;

            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw;
            }
        }

        private long SaveControlResult(Result result)
        {
            long resultId;
            var equpment = equpmentRepo.Get(e => e.AccessKey.Equals(this.identity.AccessKey)).FirstOrDefault();

            var control = new ControlResult()
            {
                SampleNo = result.TestResult.SampleNo,
                ResultDate = OperationalDateTime.GetFacilityNow(),
                EquipmentId = equpment.Id
            };

            resultId = controlResultRepo.Add(control);

            foreach (var resultDetail in result.ResultDetails)
            {
                var controlDetail = new ControlResultDetails()
                {
                    LISParamCode = resultDetail.LISParamCode,
                    LISParamValue = resultDetail.ParamValue,
                    LISParamUnit = resultDetail.ParamUnit,
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
            }
            ;

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
            }
            ;

            foreach (var entityResultDeatil in result.ResultDetails)
            {
                resultDetailsRepo.Add(entityResultDeatil);
            }
            ;

            testResultRepo.Update(result.TestResult);
        }

    }
}
