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
    public class HISParameterRangeMasterManager : IHisMasterManager
    {
        private ILogger logger;
        private ModuleRepo<HISParameterRangMaster> rangeRepo;
        private ModuleRepo<HisTestMaster> testRepo;
        private ModuleRepo<HISParameterMaster> parameterRepo;
        private ModuleRepo<TestMappingMaster> repo;

        private IModuleIdentity identity;
        private GenericUnitOfWork genericUnitOfWork;
        public HISParameterRangeMasterManager(ILogger Logger, IModuleIdentity identity, GenericUnitOfWork genericUnitOfWork)
        {
            this.identity = identity;
            logger = Logger;
            this.genericUnitOfWork = genericUnitOfWork;
            repo = new ModuleRepo<TestMappingMaster>(logger, this.identity, this.genericUnitOfWork);
            rangeRepo = new ModuleRepo<HISParameterRangMaster>(logger, this.identity, this.genericUnitOfWork);
            testRepo = new ModuleRepo<HisTestMaster>(logger, this.identity, this.genericUnitOfWork);
            parameterRepo = new ModuleRepo<HISParameterMaster>(logger, this.identity, this.genericUnitOfWork);
        }

        public IEnumerable<HISParameterMaster> GetParameterByTestId(int TestId)
        {
            var parameters = parameterRepo.Get(p => p.HisTestId == TestId);
            return parameters;
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

        public HisTestMaster GetTestById(string hisTestCode)
        {
            var test = testRepo.Get(p => p.HISTestCode.Equals(hisTestCode, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
            return test;
        }
    }
}
