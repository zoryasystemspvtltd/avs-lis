using LIS.DtoModel.Models;
using LIS.DtoModel.Models.ExternalApi;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LIS.DtoModel.Interfaces
{
    public interface IExternalApiManager
    {
        void SaveHISTestDetails(IEnumerable<TestDetail> testDetails);        
        ResultDto PrepareHISTestResult(LIS.DtoModel.Models.TestRequestDetail testRequestDetail);
        Task SubmitHISTestResult(ResultDto testResult);
    }
}
