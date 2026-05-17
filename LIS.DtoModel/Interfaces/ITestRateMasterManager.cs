using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface ITestRateMasterManager
    {
        long Add(TestRateMaster testRate);
        void Update(TestRateMaster testRate);
        void Delete(TestRateMaster testRate);
        TestRateMaster GetTestRateById(long Id);
        IEnumerable<TestRateMaster> Get();
        ItemList<TestRateMaster> Get(ListOptions options);
    }
}
