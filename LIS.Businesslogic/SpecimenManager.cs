using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;
using System.Collections.Generic;

namespace LIS.Businesslogic
{
    public class SpecimenManager : ISpecimenManager
    {
        private ILogger logger;
        private ModuleRepo<HISSpecimenMaster> specimenRepo;
        private IModuleIdentity identity;
        private GenericUnitOfWork genericUnitOfWork;
        public SpecimenManager(ILogger Logger, IModuleIdentity identity, GenericUnitOfWork genericUnitOfWork)
        {
            this.identity = identity;
            logger = Logger;
            this.genericUnitOfWork = genericUnitOfWork;
            specimenRepo = new ModuleRepo<HISSpecimenMaster>(logger, this.identity,this.genericUnitOfWork);
        }
        public long Add(HISSpecimenMaster specimen)
        {
            return specimenRepo.Add(specimen);
        }

        public void Delete(HISSpecimenMaster specimen)
        {
            specimenRepo.Delete(specimen);
        }

        public IEnumerable<HISSpecimenMaster> Get()
        {
            return specimenRepo.Get(); ;
        }

        public HISSpecimenMaster Get(int Id)
        {
            return specimenRepo.Get(Id);
        }

        public HISSpecimenMaster Get(string Code)
        {
            return specimenRepo.Get(Code);
        }

        public void Update(HISSpecimenMaster specimen)
        {
            specimenRepo.Update(specimen);
        }
    }
}
