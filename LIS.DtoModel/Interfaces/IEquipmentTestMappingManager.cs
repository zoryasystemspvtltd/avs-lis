using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface IEquipmentTestMappingManager
    {
        void Save(int equipmentId, TestMappingMaster[] mappings);
        IEnumerable<TestPanelMapping> Get(int equeipmentId);
        string[] Get();
        List<TestNameItem> Get(string model);
    }
}
