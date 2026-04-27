using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;
using System.Collections.Generic;

namespace LIS.Businesslogic
{
    public class DepartmentManager : IDepartmentManager
    {
        private ILogger logger;
        private ModuleRepo<Departments> departmentRepo;
        private IModuleIdentity identity;
        private GenericUnitOfWork genericUnitOfWork;
        public DepartmentManager(ILogger Logger, IModuleIdentity identity, GenericUnitOfWork genericUnitOfWork)
        {
            this.identity = identity;
            logger = Logger;
            this.genericUnitOfWork = genericUnitOfWork;
            departmentRepo = new ModuleRepo<Departments>(logger, this.identity,this.genericUnitOfWork);
        }
       
        public IEnumerable<Departments> Get()
        {
            return departmentRepo.Get(); ;
        }

        //public HISSpecimenMaster Get(int Id)
        //{
        //    return departmentRepo.Get(Id);
        //}

        //public HISSpecimenMaster Get(string Code)
        //{
        //    return departmentRepo.Get(Code);
        //}

    }
}
