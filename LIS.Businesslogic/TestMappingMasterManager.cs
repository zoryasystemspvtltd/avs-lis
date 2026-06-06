using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIS.BusinessLogic
{
    public class TestMappingMasterManager: ITestMappingMasterManager
    {
        private ILogger logger;
        private ModuleRepo<TestMappingMaster> moduleRepo;
        private IModuleIdentity identity;
        private GenericUnitOfWork genericUnitOfWork;
        public TestMappingMasterManager(ILogger Logger, IModuleIdentity identity, GenericUnitOfWork genericUnitOfWork)
        {
            this.identity = identity;
            logger = Logger;
            this.genericUnitOfWork = genericUnitOfWork;
            moduleRepo = new ModuleRepo<TestMappingMaster>(logger, this.identity, this.genericUnitOfWork);
        }

        public int Add(TestMappingMaster testParameter)
        {
            throw new NotImplementedException();
        }

        public void Delete(TestMappingMaster testParameter)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TestMappingMaster> Get()
        {
            throw new NotImplementedException();
        }

        public TestMappingMaster Get(int Id)
        {
            throw new NotImplementedException();
        }

        public TestMappingMaster Get(string Code)
        {
            throw new NotImplementedException();
        }

        public void Update(TestMappingMaster testParameter)
        {
            throw new NotImplementedException();
        }
    }
}
