using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface IHisTestMasterManager
    {
        long Add(HisTestMaster test);
        void Update(HisTestMaster test);
        void Delete(HisTestMaster test);
        HisTestMaster GetTestById(long TestId);
        IEnumerable<HISParameterMaster> GetParameterByTestId(int TestId);
        IEnumerable<HISParameterRangMaster> GetRangesByParameterId(int ParameterId);
        IEnumerable<HisTestMaster> GetTests();
        ItemList<HisTestMaster> Get(ListOptions options);
    }
}
