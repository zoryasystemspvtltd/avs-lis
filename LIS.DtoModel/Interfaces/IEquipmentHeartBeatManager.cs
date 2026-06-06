using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface IEquipmentHeartBeatManager
    {
        long Update(bool isAlive);   
    }
}
