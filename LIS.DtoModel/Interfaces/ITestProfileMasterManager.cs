using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface ITestProfileMasterManager : IMasterCrudManager<TestProfileMaster>
    {
        TestProfileMaster GetWithDetails(int id);
        void SaveWithDetails(TestProfileMaster profile, IEnumerable<TestProfileDetail> details);
    }
}
