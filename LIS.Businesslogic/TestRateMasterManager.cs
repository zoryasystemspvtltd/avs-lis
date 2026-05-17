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
    public class TestRateMasterManager : ITestRateMasterManager
    {
        private ILogger logger;
        private ModuleRepo<TestRateMaster> testRateRepo;
        private ModuleRepo<HisTestMaster> testRepo;
        private IModuleIdentity identity;
        private GenericUnitOfWork genericUnitOfWork;

        public TestRateMasterManager(ILogger Logger, IModuleIdentity identity, GenericUnitOfWork genericUnitOfWork)
        {
            this.identity = identity;
            logger = Logger;
            this.genericUnitOfWork = genericUnitOfWork;
            testRateRepo = new ModuleRepo<TestRateMaster>(logger, this.identity, this.genericUnitOfWork);
            testRepo = new ModuleRepo<HisTestMaster>(logger, this.identity, this.genericUnitOfWork);
        }

        public long Add(TestRateMaster testRate)
        {
            return testRateRepo.Add(testRate);
        }

        public void Update(TestRateMaster testRate)
        {
            testRateRepo.Update(testRate);
        }

        public void Delete(TestRateMaster testRate)
        {
            testRateRepo.Delete(testRate);
        }

        public TestRateMaster GetTestRateById(long Id)
        {
            var testRate = testRateRepo.Get(p => p.Id == Id).FirstOrDefault();
            return testRate;
        }

        public IEnumerable<TestRateMaster> Get()
        {
            return testRateRepo.Get();
        }

        public ItemList<TestRateMaster> Get(ListOptions option)
        {
            try
            {
                if (option == null)
                {
                    return null;
                }

                ItemList<TestRateMaster> result = new ItemList<TestRateMaster>();

                var query = testRateRepo.Get()
                    .Join(
                        testRepo.Get(d => d.IsActive == true),
                        rate => rate.TestId,
                        test => test.Id,
                        (rate, test) => new TestRateMaster
                        {
                            Id = rate.Id,
                            TestName = test.HISTestCodeDescription,
                            EffectiveStart = rate.EffectiveStart,
                            EffectiveEnd = rate.EffectiveEnd,
                            Rate = rate.Rate
                        });

                if (option.RecordPerPage == 0)
                {
                    option.RecordPerPage = 10;
                }

                result.TotalRecord = query.Count();

                int minRow = (option.CurrentPage - 1) * option.RecordPerPage;

                option.SortColumnName = string.IsNullOrEmpty(option.SortColumnName)
                    ? "TestName"
                    : option.SortColumnName;

                result.Items = query
                    .OrderBy(option.SortColumnName, option.SortDirection)
                    .Skip(minRow)
                    .Take(option.RecordPerPage)
                    .ToList();

                return result;
            }
            catch (Exception ex)
            {

                throw;
            }

        }
    }
}
