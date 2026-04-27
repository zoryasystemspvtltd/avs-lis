using LIS.BusinessLogic.Helper;
using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.DtoModel.Models.ExternalApi;
using LIS.Logger;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LIS.BusinessLogic
{
    public class ExternalApiManager : IExternalApiManager
    {
        private ILogger logger;
        private IResultManager resultManager;
        private IModuleIdentity identity;

        private ModuleRepo<TestMappingMaster> mappingRepo;
        private ModuleRepo<PatientDetail> patientRepo;
        private ModuleRepo<TestRequestDetail> testRepo;
        private ModuleRepo<DtoModel.Models.TestParameter> paramRepo;
        private ModuleRepo<HISParameterMaster> paramRepoHis;
        private IFileHandler file;
        private GenericUnitOfWork genericUnitOfWork;

        public ExternalApiManager(ILogger Logger
            , IModuleIdentity identity
            , GenericUnitOfWork genericUnitOfWork
            , IFileHandler file)
        {
            this.identity = identity;
            logger = Logger;
            this.file = file;
            this.genericUnitOfWork = genericUnitOfWork;
            mappingRepo = new ModuleRepo<TestMappingMaster>(logger, this.identity, this.genericUnitOfWork);
            resultManager = new ResultManager(logger, identity, this.genericUnitOfWork, this.file);
            patientRepo = new ModuleRepo<PatientDetail>(logger, this.identity, this.genericUnitOfWork);
            testRepo = new ModuleRepo<TestRequestDetail>(logger, this.identity, this.genericUnitOfWork);
            paramRepo = new ModuleRepo<DtoModel.Models.TestParameter>(logger, this.identity, this.genericUnitOfWork);
            paramRepoHis = new ModuleRepo<HISParameterMaster>(logger, this.identity, this.genericUnitOfWork);
        }

        public string LisServerUrl { private get; set; }
        public string ApiKey { private get; set; }
        public ResultDto PrepareHISTestResult(TestRequestDetail testRequestDetail)
        {
            var result = resultManager.Get(testRequestDetail.Id, testRequestDetail.SampleNo);

            if (result == null)
            {
                return null;
            }

            var testResult = new TestResultDto
            {
                EquipmentId = result.TestResult.EquipmentId,
                HISTestCode = result.TestResult.HISTestCode,
                DoctorNote = result.TestResult.DoctorNote,
                LISTestCode = result.TestResult.LISTestCode,
                PatientId = result.TestResult.PatientId,
                AuthorizationDate = result.TestResult.AuthorizationDate,
                AuthorizedBy = result.TestResult.AuthorizedBy,
                CreatedBy = result.TestResult.CreatedBy,
                CreatedOn = result.TestResult.CreatedOn,
                Id = result.TestResult.Id,
                ResultDate = result.TestResult.ResultDate,
                ReviewDate = result.TestResult.ReviewDate,
                ReviewedBy = result.TestResult.ReviewedBy,
                SampleCollectionDate = result.TestResult.SampleCollectionDate,
                SampleNo = result.TestResult.SampleNo,
                SampleReceivedDate = result.TestResult.SampleReceivedDate,
                SpecimenCode = result.TestResult.SpecimenCode,
                SpecimenName = result.TestResult.SpecimenName,
                TechnicianNote = result.TestResult.TechnicianNote,
                TestRequestId = result.TestResult.TestRequestId
            };

            var lstResultDetails = new List<TestResultDetailsDto>();
            foreach (var resultDetail in result.ResultDetails)
            {
                var paramCodeHis = paramRepoHis.Get(p => p.LISParamCode.Equals(resultDetail.LISParamCode, StringComparison.OrdinalIgnoreCase)
                                                    && p.HISTestCode.Equals(result.TestResult.HISTestCode, StringComparison.OrdinalIgnoreCase)
                                                    ).FirstOrDefault().HISParamCode;

                var testResultDetail = new TestResultDetailsDto
                {
                    CreatedBy = resultDetail.CreatedBy,
                    CreatedOn = resultDetail.CreatedOn,
                    Id = resultDetail.Id,
                    LISParamCode = paramCodeHis,
                    LISParamUnit = resultDetail.LISParamUnit,
                    LISParamValue = resultDetail.LISParamValue,
                    TestResultId = result.TestResult.Id
                };

                lstResultDetails.Add(testResultDetail);
            }

            var resultDto = new ResultDto
            {
                TestResult = testResult,
                TestResultDetails = lstResultDetails
            };

            return resultDto;
        }

        public void SaveHISTestDetails(IEnumerable<TestDetail> testDetails)
        {
            foreach (var patient in testDetails)
            {
                var lstTestAcknowledgements = new List<TestAcknowledgement>();

                DateTime dob = TryGetDob(patient.DOB);

                long patientId = 0;

                var patientDetail = patientRepo.Get(p => p.HisPatientId.Equals(patient.PatientId, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (patientDetail == null)
                {
                    try
                    {
                        patientId = patientRepo.Add(new PatientDetail()
                        {
                            HisPatientId = patient.PatientId,
                            Name = patient.PatientName,
                            DateOfBirth = dob,
                            Gender = patient.Gender,
                            Age = dob.Age(),
                            IsActive = true
                        });
                    }
                    catch (Exception e)
                    {
                        //logger.LogException(e);
                        logger.LogDebug("Exception in Adding Patient '{0}'", patient?.PatientId);
                    }
                }
                else
                {
                    patientId = patientDetail.Id;

                    patientDetail.HisPatientId = patient.PatientId;
                    patientDetail.Name = patient.PatientName;
                    patientDetail.DateOfBirth = dob;
                    patientDetail.Gender = patient.Gender;
                    patientDetail.Age = dob.Age();
                    patientDetail.IsActive = true;

                    patientRepo.Update(patientDetail);
                }

                foreach (var order in patient.Orders)
                {
                    var testMappings = mappingRepo
                        .Get(p => p.IsActive
                            && p.HISTestCode.Equals(order.TestCode, StringComparison.OrdinalIgnoreCase))
                            .Select(q => new { q.SpecimenCode, q.SpecimenName, q.GroupName })
                            .Distinct()
                            .ToList();

                    foreach (var testMapping in testMappings)
                    {
                        var specimenCode = string.Empty;
                        var specimenName = string.Empty;
                        var groupname = string.Empty;
                        if (testMapping != null)
                        {
                            specimenCode = testMapping.SpecimenCode;
                            specimenName = testMapping.SpecimenName;
                            groupname = testMapping.GroupName;


                            var specimenTag = Helper.Helper.GetGroupTag(groupname);
                            var sampleNo = $"{order.BarcodeNo}{specimenTag}";

                            var testCheck = testRepo.Get(p => p.HISTestCode.Equals(order.TestCode, StringComparison.OrdinalIgnoreCase)
                                && p.SampleNo.Equals(order.BarcodeNo, StringComparison.OrdinalIgnoreCase)
                                && p.ReportStatus == ReportStatusType.New).FirstOrDefault();

                            if (testCheck == null)
                            {
                                try
                                {
                                    var testId = testRepo.Add(new TestRequestDetail()
                                    {
                                        PatientId = patientId,
                                        HISTestCode = order.TestCode,
                                        HISTestName = order.TestName,
                                        SampleNo = sampleNo,
                                        SampleCollectionDate = DateTime.Now,
                                        SampleReceivedDate = DateTime.Now,
                                        SpecimenCode = specimenCode,
                                        SpecimenName = specimenName,
                                        BedNo = patient.BedNo,
                                        Department = patient.Department,
                                        DepartmentId = patient.DepartmentId,
                                        HISRequestId = patient.HISRequestId,
                                        HISRequestNo = patient.HISRequestNo,
                                        IPNo = patient.IPNo,
                                        MRNo = patient.MRNo
                                    });

                                    foreach (var param in order.TestParameter)
                                    {
                                        try
                                        {
                                            paramRepo.Add(new DtoModel.Models.TestParameter()
                                            {
                                                TestRequestDetailsId = testId,
                                                HISParamCode = param.ParameterCode,
                                                HISParamName = param.Parameter,
                                                HISTestCode = order.TestCode
                                            });
                                        }
                                        catch (Exception e)
                                        {
                                            //logger.LogException(e);
                                            logger.LogDebug("Error in Add Test Parameter '{0}'", testId);
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    //logger.LogException(e);
                                    logger.LogDebug("Error in Add Test '{0}'", sampleNo);
                                }
                            }

                            lstTestAcknowledgements.Add(new TestAcknowledgement()
                            {
                                RequesitionId = patient.RequisitionId,
                                RequesitionNumber = patient.RequisitionNumber,
                                TestId = order.TestCode
                            });

                        }
                    }
                }

                SendTestAcknowledgementsAsync(lstTestAcknowledgements);

            }

            logger.LogInfo("SaveHISTestDetails: Completed");

        }

        private DateTime TryGetDob(string dateString)
        {
            dateString = dateString.Length > 10 ? dateString.Substring(0, 10) : dateString;
            CultureInfo provider = CultureInfo.InvariantCulture;
            DateTime dob = DateTime.ParseExact(dateString, "dd/MM/yyyy", provider);
            return dob;
        }

        public async Task SubmitHISTestResult(ResultDto testResult)
        {
            try
            {
                string json = JsonConvert.SerializeObject(testResult, Formatting.Indented);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                using (var client = new ApiClient().GetHttpClient())
                {
                    var responsePost = await client.PostAsync(Config.GetConfigValue(Config.TestResultUri), httpContent);
                    if (responsePost.IsSuccessStatusCode)
                    {
                        logger.LogInfo($"Test result posted successfully for Bar code - {testResult.TestResult.SampleNo}");
                    }
                    else
                    {
                        logger.LogInfo($"Test result posting failed for Bar code - {testResult.TestResult.SampleNo} Status Code - {responsePost.StatusCode.ToString()}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
            }
        }

        private async void SendTestAcknowledgementsAsync(IEnumerable<TestAcknowledgement> testAcknowledgements)
        {
            await SendTestAcknowledgement(testAcknowledgements.ToArray());
        }
        private async Task SendTestAcknowledgement(DtoModel.Models.ExternalApi.TestAcknowledgement[] testAcknowledgement)
        {
            HttpResponseMessage responsePost = null;

            string json = JsonConvert.SerializeObject(testAcknowledgement, Formatting.Indented);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                using (var client = new ApiClient().GetHttpClient())
                {
                    responsePost = await client.PostAsync(Config.GetConfigValue(Config.TestAckUri), httpContent);
                    if (responsePost.IsSuccessStatusCode)
                    {
                        logger.LogInfo($"Test acknowledgment posted successfully for - {json}");
                    }
                    else
                    {
                        logger.LogInfo($"Test acknowledgment posting failed for - {json}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
            }
        }
    }
}
