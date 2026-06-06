using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface IEquipmentParamMappingManager
    {
        void Save(HISParameterMaster[] mappings);
        IEnumerable<HISParameterMaster> Get(int equeipmentId);
        string[] Get();
    }
}
