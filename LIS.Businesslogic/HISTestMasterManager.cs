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
                var search = option.SearchText.Trim();
                query = query.Where(p =>
                    (p.HISTestCode != null && p.HISTestCode.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.HISTestCodeDescription != null && p.HISTestCodeDescription.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.HISSpecimenName != null && p.HISSpecimenName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.DepartmentCode != null && p.DepartmentCode.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.DepartmentName != null && p.DepartmentName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            result.TotalRecord = query.Count();

            int minRow = (option.CurrentPage - 1) * option.RecordPerPage;

            option.SortColumnName = ResolveSortColumn(option.SortColumnName);

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
        private static string ResolveSortColumn(string sortColumnName)
        {
            if (string.IsNullOrWhiteSpace(sortColumnName))
            {
                return "HISTestCode";
            }

            if (sortColumnName.Equals("hisTestCode", StringComparison.OrdinalIgnoreCase)
                || sortColumnName.Equals("HISTestCode", StringComparison.OrdinalIgnoreCase))
            {
                return "HISTestCode";
            }

            if (sortColumnName.Equals("hisTestCodeDescription", StringComparison.OrdinalIgnoreCase)
                || sortColumnName.Equals("HISTestCodeDescription", StringComparison.OrdinalIgnoreCase))
            {
                return "HISTestCodeDescription";
            }

            if (sortColumnName.Equals("hisSpecimenName", StringComparison.OrdinalIgnoreCase)
                || sortColumnName.Equals("HISSpecimenName", StringComparison.OrdinalIgnoreCase))
            {
                return "HISSpecimenName";
            }

            if (sortColumnName.Equals("departmentName", StringComparison.OrdinalIgnoreCase)
                || sortColumnName.Equals("DepartmentName", StringComparison.OrdinalIgnoreCase))
            {
                return "DepartmentName";
            }

            return "HISTestCodeDescription";
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

        public string GenerateNextTestCode()
        {
            var codes = testRepo.Get().Select(t => t.HISTestCode).Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
            var max = 0;
            foreach (var code in codes)
            {
                var trimmed = code.Trim();
                if (trimmed.Length > 1 && trimmed[0] == 'T' && int.TryParse(trimmed.Substring(1), out var num))
                {
                    if (num > max)
                    {
                        max = num;
                    }
                }
            }

            return $"T{(max + 1).ToString("D7", CultureInfo.InvariantCulture)}";
        }

        public long Add(HisTestMaster test)
        {
            if (test == null)
            {
                throw new ArgumentNullException(nameof(test));
            }

            if (string.IsNullOrWhiteSpace(test.HISTestCode))
            {
                test.HISTestCode = GenerateNextTestCode();
            }

            test.HISTestCode = test.HISTestCode.Trim();
            ValidateUniqueTest(test, null);
            test.IsActive = true;
            if (test.CreatedOn == default(DateTime))
            {
                test.CreatedOn = DateTime.Now;
            }

            if (string.IsNullOrWhiteSpace(test.CreatedBy))
            {
                test.CreatedBy = identity?.ActivityMember;
            }

            return testRepo.Add(test);
        }

        public void Update(HisTestMaster test)
        {
            if (test == null)
            {
                throw new ArgumentNullException(nameof(test));
            }

            ValidateUniqueTest(test, test.Id);
            testRepo.Update(test);
        }

        private void ValidateUniqueTest(HisTestMaster test, long? excludeId)
        {
            var code = (test.HISTestCode ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("Test Code is required.");
            }

            var codeDuplicate = testRepo.Get(t =>
                    (!excludeId.HasValue || t.Id != excludeId.Value))
                .AsEnumerable()
                .Any(t => string.Equals((t.HISTestCode ?? string.Empty).Trim(), code, StringComparison.OrdinalIgnoreCase));

            if (codeDuplicate)
            {
                throw new InvalidOperationException("Test Code already exists.");
            }

            var name = (test.HISTestCodeDescription ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(name))
            {
                var nameDuplicate = testRepo.Get(t =>
                        (!excludeId.HasValue || t.Id != excludeId.Value))
                    .AsEnumerable()
                    .Any(t => string.Equals((t.HISTestCodeDescription ?? string.Empty).Trim(), name, StringComparison.OrdinalIgnoreCase));

                if (nameDuplicate)
                {
                    throw new InvalidOperationException("Test Name already exists.");
                }
            }
        }

        public void Delete(HisTestMaster test)
        {
            testRepo.Delete(test);
        }
    }
}
