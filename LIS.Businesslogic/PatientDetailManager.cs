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
        private ModuleRepo<TestResultDetails> testResultDetailsRepo;
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
            testResultDetailsRepo = new ModuleRepo<TestResultDetails>(logger, this.identity, this.genericUnitOfWork);
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

            var patientsById = patientRepo.Get().ToDictionary(p => p.Id, p => p);

            // Materialize in memory — avoids EF enum/navigation issues that returned empty lists in UI.
            IEnumerable<TestRequestDetail> query;
            if (option.ReceivedOnly)
            {
                query = testRequestRepo.Get()
                    .Where(p => !string.IsNullOrWhiteSpace(p.ReceivedBy)
                        && IsRecentSampleStatus(p.ReportStatus))
                    .ToList();
            }
            else
            {
                query = testRequestRepo.Get()
                    .Where(p => p.ReportStatus == option.Status)
                    .ToList();
            }

            // Technician approval queue: only tests with entered/received parameter values.
            if (!option.ReceivedOnly && option.Status == ReportStatusType.ReportGenerated)
            {
                query = FilterRequestsWithEnteredResults(query, requireTechnicianReview: false);
            }

            // Doctor approval queue: only tests already approved by technician (ReviewedBy set + results present).
            if (!option.ReceivedOnly && option.Status == ReportStatusType.TechnicianApproved)
            {
                query = FilterRequestsWithEnteredResults(query, requireTechnicianReview: true);
            }

            if (!string.IsNullOrWhiteSpace(option.SearchText))
            {
                var search = option.SearchText.Trim();
                DateTime searchDate;
                bool isValidSearchDate = DateTime.TryParseExact(search, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out searchDate);
                if (search.Contains("/") && isValidSearchDate)
                {
                    query = query.Where(p => p.SampleCollectionDate.Year == searchDate.Year
                                                && p.SampleCollectionDate.Month == searchDate.Month
                                                && p.SampleCollectionDate.Day == searchDate.Day);
                }
                else
                {
                    query = query.Where(p =>
                        (p.SampleNo != null && p.SampleNo.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                        || (p.HISTestName != null && p.HISTestName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                        || (p.HISRequestNo != null && p.HISRequestNo.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)                        
                        || (patientsById.TryGetValue(p.PatientId, out var pat) && pat.Name != null
                            && pat.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0));
                }
            }

            var queryList = query.ToList();
            result.TotalRecord = queryList.Count;

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

            result.Items = queryList
                    .OrderBy(option.SortColumnName, option.SortDirection)
                    .Skip(minRow)
                    .Take(totalRecordToBeSelected)
                    .ToList();

            //Fetch department name and Doctor openion status
            foreach (var item in result.Items)
            {
                if (item.Patient == null && patientsById.TryGetValue(item.PatientId, out var patient))
                {
                    item.Patient = patient;
                }

                var testCode = item.HISTestCode ?? string.Empty;
                var departmentname = testRepo.Get()
                    .Where(t => t.HISTestCode != null && t.HISTestCode.Equals(testCode, StringComparison.OrdinalIgnoreCase))
                    .Join(departmentRepo.Get().Where(d => d.Code != null),
                        test => test.DepartmentCode,
                        dept => dept.Code,
                        (test, dept) => new { dept.Name })
                    .FirstOrDefault();

                item.Department = departmentname != null ? departmentname.Name : (item.Department ?? string.Empty);

                var paramNames = parameterRepo.Get(p => p.TestRequestDetailsId == item.Id)
                    .ToList()
                    .Select(p => !string.IsNullOrWhiteSpace(p.HISParamName) ? p.HISParamName : p.HISParamCode)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .ToList();
                item.TestParameterNames = paramNames.Any() ? string.Join(", ", paramNames) : string.Empty;

                var sampleNo = item.SampleNo ?? string.Empty;
                var testResult = testResultRepo.Get()
                    .ToList()
                    .FirstOrDefault(t =>
                        t.SampleNo != null && t.SampleNo.Equals(sampleNo, StringComparison.OrdinalIgnoreCase) &&
                        t.HISTestCode != null && t.HISTestCode.Equals(testCode, StringComparison.OrdinalIgnoreCase));

                if (testResult != null && !string.IsNullOrEmpty(testResult.TechnicianNote))
                {
                    string[] delim = { "<br>" };
                    var note = testResult.TechnicianNote.Split(delim, StringSplitOptions.None);
                    item.RequireReOpenion = note.Count() > 2 ? true : false;
                }
            }
            return result;

        }

        public ItemList<PatientDetail> GetForBilling(ListOptions option)
        {
            if (option == null)
            {
                return null;
            }

            var result = new ItemList<PatientDetail>();
            var query = patientRepo.Get(p => p.IsActive).AsEnumerable();

            if (!string.IsNullOrEmpty(option.SearchText))
            {
                var search = option.SearchText.Trim();
                query = query.Where(p =>
                    (p.Name != null && p.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.Phone != null && p.Phone.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.HisPatientId != null && p.HisPatientId.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.MRNo != null && p.MRNo.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.VisitId != null && p.VisitId.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            var list = query.OrderBy(p => p.Name).ToList();
            result.TotalRecord = list.Count;

            var sortColumn = string.IsNullOrEmpty(option.SortColumnName) ? "Name" : option.SortColumnName;
            if (sortColumn != "Name" && sortColumn != "Phone" && sortColumn != "Id")
            {
                sortColumn = "Name";
            }

            int minRow = (option.CurrentPage - 1) * option.RecordPerPage;
            int pageSize = option.RecordPerPage == 0 ? result.TotalRecord : option.RecordPerPage;

            result.Items = list
                .OrderBy(sortColumn, option.SortDirection)
                .Skip(minRow)
                .Take(pageSize)
                .ToList();

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
            var hisParamRepo = new ModuleRepo<HISParameterMaster>(logger, identity, genericUnitOfWork);
            var paramCodes = hisParamRepo.Get(p => p.HISTestCode != null
                    && p.HISTestCode.Equals(hISTestCode, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.HISParamCode)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToList();

            var mappings = mappingRepo
                .Get(p => p.IsActive && p.HISParamCode != null && paramCodes.Contains(p.HISParamCode))
                .Select(q => q.GroupName)
                .Distinct()
                .ToList();
            return mappings;
        }

        private static bool IsRecentSampleStatus(ReportStatusType status)
        {
            return status == ReportStatusType.New || status == ReportStatusType.SentToEquipment;
        }

        /// <summary>
        /// Keeps only requests that have a TestResult with at least one non-blank parameter value.
        /// When requireTechnicianReview is true (doctor queue), also requires ReviewedBy to be set.
        /// </summary>
        private IEnumerable<TestRequestDetail> FilterRequestsWithEnteredResults(
            IEnumerable<TestRequestDetail> requests,
            bool requireTechnicianReview)
        {
            var list = requests?.ToList() ?? new List<TestRequestDetail>();
            if (list.Count == 0)
            {
                return list;
            }

            var requestIds = list.Select(r => r.Id).Distinct().ToList();
            var results = testResultRepo.Get(r => requestIds.Contains(r.TestRequestId)).ToList();
            if (results.Count == 0)
            {
                return Enumerable.Empty<TestRequestDetail>();
            }

            var latestByRequest = results
                .GroupBy(r => r.TestRequestId)
                .Select(g => g.OrderByDescending(x => x.Id).First())
                .ToList();

            var resultIds = latestByRequest.Select(r => r.Id).ToList();
            var resultIdsWithValues = new HashSet<long>(
                testResultDetailsRepo.Get(d => resultIds.Contains(d.TestResultId))
                    .AsEnumerable()
                    .Where(d => !string.IsNullOrWhiteSpace(d.ParamValue))
                    .Select(d => d.TestResultId)
                    .Distinct());

            var requestIdsWithValues = new HashSet<long>(
                latestByRequest
                    .Where(r => resultIdsWithValues.Contains(r.Id)
                        && (!requireTechnicianReview || !string.IsNullOrWhiteSpace(r.ReviewedBy)))
                    .Select(r => r.TestRequestId));

            return list.Where(r => requestIdsWithValues.Contains(r.Id));
        }
    }
}

