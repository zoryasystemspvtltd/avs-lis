using LIS.DtoModel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIS.DtoModel.Interfaces
{
    public interface ITestMappingMasterManager
    {
        int Add(TestMappingMaster testParameter);
        void Update(TestMappingMaster testParameter);
        IEnumerable<TestMappingMaster> Get();
        TestMappingMaster Get(int Id);
        TestMappingMaster Get(string Code);
        void Delete(TestMappingMaster testParameter);
    }
}
