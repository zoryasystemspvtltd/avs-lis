using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIS.BusinessLogic
{
    public class EquipmentParamMappingManager : IEquipmentParamMappingManager
    {
        public IEnumerable<HISParameterMaster> Get(int equeipmentId)
        {
            throw new NotImplementedException();
        }

        public string[] Get()
        {
            throw new NotImplementedException();
        }

        public void Save(HISParameterMaster[] mappings)
        {
            throw new NotImplementedException();
        }
    }
}
