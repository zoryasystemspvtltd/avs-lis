using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LIS.Businesslogic
{
    public class EquipmentManager : IEquipmentManager
    {
        private ILogger logger;
        private ModuleRepo<EquipmentMaster> equipmentRepo;
        private ModuleRepo<EquipmentHeartBeat> heartbeatRepo;
        private IModuleIdentity identity;
        private GenericUnitOfWork genericUnitOfWork;
        public EquipmentManager(ILogger Logger, IModuleIdentity identity, GenericUnitOfWork genericUnitOfWork)
        {
            this.identity = identity;
            this.logger = Logger;
            this.genericUnitOfWork = genericUnitOfWork;
            this.equipmentRepo = new ModuleRepo<EquipmentMaster>(logger, this.identity, this.genericUnitOfWork);
            this.heartbeatRepo = new ModuleRepo<EquipmentHeartBeat>(logger, this.identity, this.genericUnitOfWork);
        }
        public long Add(EquipmentMaster equipment)
        {
            equipment.IsActive = true;

            return equipmentRepo.Add(equipment);
        }

        public void Delete(EquipmentMaster equipment)
        {
            equipmentRepo.Delete(equipment);
        }

        public IEnumerable<EquipmentMaster> Get()
        {
            var equipments = equipmentRepo.Get();
            foreach (var equip in equipments)
            {
                var hearbeartTime = heartbeatRepo.Get(p => p.AccessKey.Equals(equip.AccessKey, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                if (hearbeartTime != null)
                {
                    DateTime currentTime = DateTime.Now;
                    int diff = (currentTime - hearbeartTime.CreatedOn).Minutes;
                    equip.HeartBeatTime = hearbeartTime.CreatedOn;
                    equip.IsAlive = diff < 1;
                }
            }

            return equipments;
        }

        public EquipmentMaster Get(int Id)
        {
            var equip = equipmentRepo.Get(Id);
            var hearbeartTime = heartbeatRepo.Get(p => p.AccessKey.Equals(equip.AccessKey, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            
            if (hearbeartTime != null)
            {
                DateTime currentTime = DateTime.Now;
                int diff = (currentTime - hearbeartTime.CreatedOn).Minutes;
                equip.HeartBeatTime = hearbeartTime.CreatedOn;
                equip.IsAlive = diff < 1;
            }
            return equip;
        }

        public EquipmentMaster Get(string Code)
        {
            return equipmentRepo.Get(Code);
        }

        public EquipmentMaster GetHeartBeat(string Key)
        {
            var equipment = equipmentRepo.Get(e => e.AccessKey.Equals(Key, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            return equipment;
        }
        public void Update(EquipmentMaster equipment)
        {
            var exustingEquipment = equipmentRepo.Get(equipment.Id);
            exustingEquipment.IsActive = true;
            exustingEquipment.Name = equipment.Name;

            equipmentRepo.Update(exustingEquipment);
        }

        public void UpdateHartbit(bool isAlive)
        {
            foreach(var heartBeat in heartbeatRepo.Get())
            {
                heartBeat.IsAlive = isAlive;
                heartbeatRepo.Update(heartBeat);
            }

        }
    }
}
