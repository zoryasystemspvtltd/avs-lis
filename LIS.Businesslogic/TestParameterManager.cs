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
    public class TestParameterManager : ITestParameterManager
    {
        private ILogger logger;
        private ModuleRepo<TestParameter> parameterRangeRepo;
        private IModuleIdentity identity;
        private GenericUnitOfWork genericUnitOfWork;
        public TestParameterManager(ILogger Logger, IModuleIdentity identity, GenericUnitOfWork genericUnitOfWork)
        {
            this.identity = identity;
            logger = Logger;
            this.genericUnitOfWork = genericUnitOfWork;
            parameterRangeRepo = new ModuleRepo<TestParameter>(logger, this.identity,this.genericUnitOfWork);
        }
        public long Add(TestParameter testParameter)
        {
            return parameterRangeRepo.Add(testParameter);
        }

        public void Delete(TestParameter testParameter)
        {
            parameterRangeRepo.Delete(testParameter);
        }

        public IEnumerable<TestParameter> Get()
        {
            var lstSpecimens = new List<TestParameter>();

            return parameterRangeRepo.Get(); ;
        }

        public TestParameter Get(int Id)
        {
            return parameterRangeRepo.Get(Id);
        }

        public TestParameter Get(string Code)
        {
            return parameterRangeRepo.Get(Code);
        }
       
        public void Update(TestParameter testParameter)
        {
            parameterRangeRepo.Update(testParameter);
        }
    }
}
