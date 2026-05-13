using LIS.DtoModel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIS.DtoModel
{
    public interface IFileHandler
    {
        List<TestNameItem> GetJsonMappings(string model);
        string[] GetModels();
    }
}
