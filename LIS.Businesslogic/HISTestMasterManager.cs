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
    public class HISTestMasterManager : IHisTestMasterManager
    {
        private ILogger logger;
        private ModuleRepo<HISParameterRangMaster> rangeRepo;
        private ModuleRepo<HisTestMaster> testRepo;
        private ModuleRepo<HISParameterMaster> parameterRepo;
        private ModuleRepo<TestMappingMaster> repo;
        private ModuleRepo<Departments> departmentRepo;
        private IModuleIdentity identity;
        private GenericUnitOfWork genericUnitOfWork;
        public HISTestMasterManager(ILogger Logger, IModuleIdentity identity, GenericUnitOfWork genericUnitOfWork)
        {
            this.identity = identity;
            logger = Logger;
            this.genericUnitOfWork = genericUnitOfWork;
            repo = new ModuleRepo<TestMappingMaster>(logger, this.identity, this.genericUnitOfWork);
            rangeRepo = new ModuleRepo<HISParameterRangMaster>(logger, this.identity, this.genericUnitOfWork);
            testRepo = new ModuleRepo<HisTestMaster>(logger, this.identity, this.genericUnitOfWork);
            parameterRepo = new ModuleRepo<HISParameterMaster>(logger, this.identity, this.genericUnitOfWork);
            departmentRepo = new ModuleRepo<Departments>(logger, this.identity, this.genericUnitOfWork);
        }

        public IEnumerable<HISParameterMaster> GetParameterByTestId(int TestId)
        {
            var parameters = parameterRepo.Get(p => p.HisTestId == TestId);
            return parameters;
        }
        public ItemList<HisTestMaster> Get(ListOptions option)
        {
            if (option == null)
            {
                return null;
            }

            ItemList<HisTestMaster> result = new ItemList<HisTestMaster>();

            var departments = departmentRepo.Get(d => d.Code != null).ToList();
            var query = testRepo.Get().AsEnumerable().Select(test =>
            {
                var dept = departments.FirstOrDefault(d =>
                    !string.IsNullOrEmpty(test.DepartmentCode) &&
                    d.Code.Equals(test.DepartmentCode, StringComparison.OrdinalIgnoreCase));
                return new HisTestMaster
                {
                    Id = test.Id,
                    HISTestCode = test.HISTestCode,
                    HISTestCodeDescription = test.HISTestCodeDescription,
                    HISSpecimenCode = test.HISSpecimenCode,
                    HISSpecimenName = test.HISSpecimenName,
                    DepartmentCode = test.DepartmentCode,
                    DepartmentName = dept != null ? dept.Name : test.DepartmentCode,
                    IsActive = test.IsActive,
                    CreatedOn = test.CreatedOn,
                    CreatedBy = test.CreatedBy
                };
            }).AsQueryable();

            if (!string.IsNullOrEmpty(option.SearchText))
            {
                query = query.Where(p =>
                    p.HISTestCode.Contains(option.SearchText) ||
                    p.HISTestCodeDescription.Contains(option.SearchText) ||
                    p.HISSpecimenName.Contains(option.SearchText) ||
                    p.DepartmentCode.Contains(option.SearchText));
            }

            result.TotalRecord = query.Count();

            int minRow = (option.CurrentPage - 1) * option.RecordPerPage;

            option.SortColumnName = string.IsNullOrEmpty(option.SortColumnName)
                ? "HISTestCodeDescription"
                : option.SortColumnName;

            if (!option.SortColumnName.Equals("HISTestCode", StringComparison.OrdinalIgnoreCase)
                && !option.SortColumnName.Equals("HISTestCodeDescription", StringComparison.OrdinalIgnoreCase))
            {
                option.SortColumnName = "HISTestCodeDescription";
                option.SortDirection = false;
            }

            if (option.RecordPerPage == 0)
            {
                option.RecordPerPage = result.TotalRecord;
            }

            result.Items = query
                .OrderBy(option.SortColumnName, option.SortDirection)
                .Skip(minRow)
                .Take(option.RecordPerPage)
                .ToList();

            return result;
        }
        public IEnumerable<HisTestMaster> GetTests()
        {
            var tests = testRepo.Get(p => p.IsActive)
                .Join(repo.Get(r => r.IsActive),
                test => test.HISTestCode,
                mapp => mapp.HISTestCode,
                 (test, mapp) => test)
                .OrderBy(p => p.HISTestCodeDescription)
                .Distinct()
                .ToList();
            return tests;


        }

        public IEnumerable<HISParameterRangMaster> GetRangesByParameterId(int ParameterId)
        {
            var ranges = rangeRepo.Get(p => p.HisParameterId == ParameterId).ToList();
            return ranges;
        }

        public HisTestMaster GetTestById(long id)
        {
            var test = testRepo.Get(p => p.Id == id)
                .FirstOrDefault();
            if (test == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(test.DepartmentCode))
            {
                var dept = departmentRepo.Get(d => d.Code != null)
                    .FirstOrDefault(d => d.Code.Equals(test.DepartmentCode, StringComparison.OrdinalIgnoreCase));
                test.DepartmentName = dept != null ? dept.Name : test.DepartmentCode;
            }

            return test;
        }

        public long Add(HisTestMaster test)
        {
            return testRepo.Add(test);
        }

        public void Update(HisTestMaster test)
        {
            testRepo.Update(test);
        }

        public void Delete(HisTestMaster test)
        {
            testRepo.Delete(test);
        }
    }
}
