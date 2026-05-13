using LIS.DataAccess;
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
    public class PatientDetailManager : IPatientDetailsManager
    {
        private ILogger logger;
        private ModuleRepo<PatientDetail> patientRepo;
        private ModuleRepo<TestRequestDetail> testRequestRepo;
        private ModuleRepo<TestParameter> parameterRepo;
        private ModuleRepo<HISParameterMaster> parameterMapRepo;
        private IModuleIdentity identity;
        private GenericUnitOfWork genericUnitOfWork;
        private ModuleRepo<HisTestMaster> testRepo;
        private ModuleRepo<Departments> departmentRepo;
        private ModuleRepo<TestMappingMaster> mappingRepo;
        private ModuleRepo<TestResult> testResultRepo;
        public PatientDetailManager(ILogger Logger
            , IModuleIdentity identity
            , GenericUnitOfWork genericUnitOfWork
            , ApplicationDBContext dBContext)
        {
            this.identity = identity;
            logger = Logger;
            this.genericUnitOfWork = genericUnitOfWork;
            patientRepo = new ModuleRepo<PatientDetail>(logger, this.identity, this.genericUnitOfWork);
            testRequestRepo = new ModuleRepo<TestRequestDetail>(logger, this.identity, this.genericUnitOfWork);
            parameterMapRepo = new ModuleRepo<HISParameterMaster>(logger, this.identity, this.genericUnitOfWork);
            parameterRepo = new ModuleRepo<TestParameter>(logger, this.identity, this.genericUnitOfWork);
            testRepo = new ModuleRepo<HisTestMaster>(logger, this.identity, this.genericUnitOfWork);
            mappingRepo = new ModuleRepo<TestMappingMaster>(logger, this.identity, this.genericUnitOfWork);
            departmentRepo = new ModuleRepo<Departments>(logger, this.identity, this.genericUnitOfWork);
            testResultRepo = new ModuleRepo<TestResult>(logger, this.identity, this.genericUnitOfWork);
        }
        public long Add(PatientDetail patientDetail)
        {
            patientDetail.IsActive = true;
            return patientRepo.Add(patientDetail);
        }

        public void Delete(PatientDetail patientDetail)
        {
            patientRepo.Delete(patientDetail);
        }

        public IEnumerable<PatientDetail> Get()
        {
            return patientRepo.Get(); ;
        }

        public PatientDetail Get(long Id)
        {
            return patientRepo.Get(Id);
        }

        public PatientDetail Get(string Code)
        {
            return patientRepo.Get(Code); ;
        }

        public ItemList<TestRequestDetail> Get(ListOptions option)
        {
            if (option == null)
            {
                return null;
            }

            ItemList<TestRequestDetail> result = new ItemList<TestRequestDetail>();

            var query = testRequestRepo.Get(p => p.ReportStatus == option.Status);

            if (!string.IsNullOrEmpty(option.SearchText))
            {
                DateTime searchDate;
                bool isValidSearchDate = DateTime.TryParseExact(option.SearchText, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out searchDate);
                if (option.SearchText.Contains("/") && isValidSearchDate)
                {
                    query = query.Where(p => p.SampleCollectionDate.Year == searchDate.Year
                                                && p.SampleCollectionDate.Month == searchDate.Month
                                                && p.SampleCollectionDate.Day == searchDate.Day);
                }
                else
                {
                    query = query.Where(p => p.SampleNo.Contains(option.SearchText)
                                || p.HISTestName.Contains(option.SearchText)
                                || p.HISRequestNo.Contains(option.SearchText)
                                || p.IPNo.Contains(option.SearchText)
                                || p.Patient.Name.Contains(option.SearchText));
                }
            }

            result.TotalRecord = query.Count();

            int minRow = (option.CurrentPage - 1) * option.RecordPerPage;
            int maxRow = (option.CurrentPage) * option.RecordPerPage;

            int totalRecordToBeSelected = ((result.TotalRecord - minRow) > option.RecordPerPage)
                ? option.RecordPerPage : (result.TotalRecord - minRow);

            option.SortColumnName = string.IsNullOrEmpty(option.SortColumnName)
                ? "SampleCollectionDate" : option.SortColumnName;

            //TODO SQL View
            if (!option.SortColumnName.Equals("SampleNo", StringComparison.OrdinalIgnoreCase)
                && !option.SortColumnName.Equals("PatientStatus", StringComparison.OrdinalIgnoreCase)
                && !option.SortColumnName.Equals("HISTestCode", StringComparison.OrdinalIgnoreCase)
                && !option.SortColumnName.Equals("sampleCollectionDate", StringComparison.OrdinalIgnoreCase))
            {
                option.SortColumnName = "SampleCollectionDate";
                option.SortDirection = false;
            }

            if (option.RecordPerPage == 0)
            {
                totalRecordToBeSelected = result.TotalRecord;
            }

            result.Items = query
                    .OrderBy(option.SortColumnName, option.SortDirection)
                    .Skip(minRow)
                    .Take(totalRecordToBeSelected)
                    .ToList();

            //Fetch department name and Doctor openion status
            foreach (var item in result.Items)
            {
                var departmentname = testRepo.Get(t => t.HISTestCode.Equals(item.HISTestCode, StringComparison.OrdinalIgnoreCase))
              .Join(departmentRepo.Get(d => d.Code != null),
              test => test.DepartmentCode,
              dept => dept.Code,
              (test, dept) => new { dept.Name }).FirstOrDefault();

                item.Department = departmentname.Name;

                var testResult = testResultRepo.Get(t => t.SampleNo.Equals(item.SampleNo, StringComparison.OrdinalIgnoreCase)
                                                        && t.HISTestCode.Equals(item.HISTestCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                if (testResult != null && !string.IsNullOrEmpty(testResult.TechnicianNote))
                {
                    string[] delim = { "<br>" };
                    var note = testResult.TechnicianNote.Split(delim, StringSplitOptions.None);
                    item.RequireReOpenion = note.Count() > 2 ? true : false;
                }
            }
            return result;

        }

        public void Update(PatientDetail patientDetail)
        {
            patientRepo.Update(patientDetail);
        }

        public long CreateNewOrder(NewOrder newOrder)
        {
            long patientId = 0;

            var patientCheck = patientRepo.Get(p => p.HisPatientId.Equals(newOrder.PatientDetail.HisPatientId, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (patientCheck == null)
            {
                try
                {
                    var patientDetails = new PatientDetail()
                    {
                        Name = newOrder.PatientDetail.Name,
                        HisPatientId = newOrder.PatientDetail.HisPatientId,
                        DateOfBirth = newOrder.PatientDetail.DateOfBirth,
                        Gender = newOrder.PatientDetail.Gender,
                        Age = newOrder.PatientDetail.DateOfBirth.Age(),
                        IsActive = true
                    };
                    patientId = patientRepo.Add(patientDetails);
                }
                catch (Exception e)
                {
                    //logger.LogException(e);
                    logger.LogDebug("Error in Add Patient '{0}'", newOrder?.PatientDetail?.HisPatientId);
                }
            }
            else
            {
                patientId = patientCheck.Id;
            }

            foreach (var order in newOrder.TestRequestDetails)
            {
                var test = testRepo.Get(p => p.HISTestCode.Equals(order.HISTestCode, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                var specimenCode = string.Empty;
                var specimenName = string.Empty;
                var department = string.Empty;
                var departmentId = string.Empty;

                if (test != null)
                {
                    specimenCode = test.HISSpecimenCode;
                    specimenName = test.HISSpecimenName;
                    department = test.Departments.Name;
                    departmentId = test.Departments.Code;
                }

                var groups = GetTestGroupName(order.HISTestCode);
                foreach (var groupname in groups)
                {
                    var specimenTag = Helper.Helper.GetGroupTag(groupname);
                    var sampleNo = $"{order.HISRequestNo}{specimenTag}";

                    var testCheck = testRequestRepo.Get(p => p.HISTestCode.Equals(order.HISTestCode, StringComparison.OrdinalIgnoreCase)
                               && p.SampleNo.Equals(sampleNo, StringComparison.OrdinalIgnoreCase)
                               && p.ReportStatus == ReportStatusType.New).FirstOrDefault();

                    if (testCheck == null)
                    {
                        var testRequestDetail = new TestRequestDetail()
                        {
                            PatientId = patientId,
                            HISTestCode = order.HISTestCode,
                            HISTestName = order.HISTestName,
                            SampleNo = sampleNo,
                            SampleCollectionDate = order.SampleCollectionDate,
                            SampleReceivedDate = order.SampleCollectionDate,
                            SpecimenCode = order.SpecimenCode,
                            SpecimenName = order.SpecimenName,
                            HISRequestNo = order.HISRequestNo,
                            HISRequestId = $"R{order.HISRequestNo}",
                            BedNo = order.BedNo,
                            IPNo = order.IPNo,
                            MRNo = order.MRNo,
                            DepartmentId = departmentId,
                            Department = department
                        };

                        try
                        {
                            var testRequestId = testRequestRepo.Add(testRequestDetail);


                            var parameterlist = parameterMapRepo.Get(p => p.HISTestCode.Equals(order.HISTestCode, StringComparison.OrdinalIgnoreCase)).ToList();

                            foreach (var param in parameterlist)
                            {
                                try
                                {
                                    var testParameter = new TestParameter()
                                    {
                                        HISParamCode = param.HISParamCode,
                                        HISParamName = param.HISParamDescription,
                                        HISTestCode = param.HISTestCode,
                                        TestRequestDetailsId = testRequestId
                                    };

                                    parameterRepo.Add(testParameter);
                                }
                                catch (Exception e)
                                {
                                    //logger.LogException(e);
                                    logger.LogDebug("Error in Add Test Parameter '{0}'", param?.HISTestCode);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            //logger.LogException(e);
                            logger.LogDebug("Error in Add Test '{0}'", sampleNo);
                        }
                    }
                }
            }
            return patientId;
        }

        private List<string> GetTestGroupName(string hISTestCode)
        {
            var mappings = mappingRepo
                .Get(p => p.IsActive
                    && p.HISTestCode.Equals(hISTestCode, StringComparison.OrdinalIgnoreCase))
                .Select(q => q.GroupName)
                .Distinct()
                .ToList();
            return mappings;
        }
    }
}

