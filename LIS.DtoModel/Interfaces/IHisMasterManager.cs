using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface IHisMasterManager
    {
        HisTestMaster GetTestById(string TestId);
        IEnumerable<HISParameterMaster> GetParameterByTestId(int TestId);
        IEnumerable<HISParameterRangMaster> GetRangesByParameterId(int ParameterId);
        IEnumerable<HisTestMaster> GetTests();
    }
}
