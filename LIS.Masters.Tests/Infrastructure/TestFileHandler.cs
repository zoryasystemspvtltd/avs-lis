using LIS.DtoModel;
using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.Masters.Tests.Infrastructure
{
    internal sealed class TestFileHandler : IFileHandler
    {
        public List<TestNameItem> GetJsonMappings(string model) => new List<TestNameItem>();

        public string[] GetModels() => new string[0];
    }
}
