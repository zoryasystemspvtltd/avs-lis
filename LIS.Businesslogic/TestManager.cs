using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;
using System.Collections.Generic;

namespace LIS.Businesslogic
{
    public class TestManager : ITestManager
    {
        private ILogger logger;
        private ModuleRepo<Test> testRepo;
        private IModuleIdentity identity;
        private GenericUnitOfWork genericUnitOfWork;
        public TestManager(ILogger Logger, IModuleIdentity identity, GenericUnitOfWork genericUnitOfWork)
        {
            this.identity = identity;
            logger = Logger;
            this.genericUnitOfWork = genericUnitOfWork;
            testRepo = new ModuleRepo<Test>(logger, this.identity,this.genericUnitOfWork);
        }
        public int Add(Test test)
        {
            return testRepo.Add(test);
        }

        public void Delete(Test test)
        {
            testRepo.Delete(test);
        }

        public IEnumerable<Test> Get()
        {
            return testRepo.Get();
        }

        public Test Get(int Id)
        {
            return testRepo.Get(Id);
        }

        public Test Get(string Code)
        {
            return testRepo.Get(Code);
        }

        public void Update(Test test)
        {
            testRepo.Update(test);
        }
    }
}
