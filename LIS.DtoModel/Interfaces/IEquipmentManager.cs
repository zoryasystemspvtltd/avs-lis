using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface IEquipmentManager
    {
        long Add(EquipmentMaster equipment);
        void Update(EquipmentMaster equipment);
        IEnumerable<EquipmentMaster> Get();
        EquipmentMaster Get(int Id);
        EquipmentMaster Get(string Code);
        EquipmentMaster GetHeartBeat(string Key);
        void Delete(EquipmentMaster equipment);

        void UpdateHartbit(bool isAlive);
    }
}
