using LIS.DtoModel.Models.TestResultEdit;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface ITestResultEditManager
    {
        IList<TestResultEditSearchRow> Search(TestResultEditSearchOptions options);
        TestResultEditSampleDto GetBySampleNo(string sampleNo, bool isAdministrator);
        TestResultEditSaveResult Save(TestResultEditSaveRequest request, bool isAdministrator);
    }
}
