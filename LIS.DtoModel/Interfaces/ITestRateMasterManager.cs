using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface ITestRateMasterManager : IMasterCrudManager<TestRateMaster>
    {
        TestRateMaster GetEffectiveRate(int testId, int rateType, int? corporateId, int? referralDoctorId, int? profileId);
        IEnumerable<TestRateMaster> GetByTestId(int testId);
    }
}
