using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface ITestParameterManager
    {
        long Add(TestParameter testParameter);
        void Update(TestParameter testParameter);
        IEnumerable<TestParameter> Get();
        TestParameter Get(int Id);
        TestParameter Get(string Code);
        void Delete(TestParameter testParameter);
    }
}
