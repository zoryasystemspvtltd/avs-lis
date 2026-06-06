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
    public class EquipmentHeartBeatManager : IEquipmentHeartBeatManager
    {
        private ILogger logger;
        private ModuleRepo<EquipmentHeartBeat> equipmentRepo;
        private IModuleIdentity identity;
        private GenericUnitOfWork genericUnitOfWork;
        public EquipmentHeartBeatManager(ILogger Logger, IModuleIdentity identity, GenericUnitOfWork genericUnitOfWork)
        {
            this.identity = identity;
            this.logger = Logger;
            this.genericUnitOfWork = genericUnitOfWork;
            this.equipmentRepo = new ModuleRepo<EquipmentHeartBeat>(logger, this.identity, this.genericUnitOfWork);
        }
        public long Update(bool isAlive)
        {
            var exitingHeartbeat = equipmentRepo.Get(p => p.AccessKey.Equals(identity.AccessKey, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (exitingHeartbeat != null)
            {
                exitingHeartbeat.IsAlive = isAlive;
                exitingHeartbeat.CreatedOn = DateTime.Now;

                equipmentRepo.Update(exitingHeartbeat);
                logger.LogInfo("Heartbeat Alive '{0}'", identity.AccessKey);
            }
            else
            {
                var equipment = new EquipmentHeartBeat()
                {
                    AccessKey = identity.AccessKey,
                    IsAlive = isAlive,
                    CreatedBy = identity.ActivityMember,
                    CreatedOn = DateTime.Now
                };
                equipmentRepo.Add(equipment);
            }

            return 1;
        }

    }
}
