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
            specimenRepo = new ModuleRepo<HISSpecimenMaster>(logger, this.identity, this.genericUnitOfWork);
        }

        public long Add(HISSpecimenMaster specimen)
        {
            specimen.CreatedOn = DateTime.Now;
            specimen.CreatedBy = identity?.ActivityMember;
            return specimenRepo.Add(specimen);
        }

        public void Delete(HISSpecimenMaster specimen)
        {
            specimenRepo.Delete(specimen);
        }

        public IEnumerable<HISSpecimenMaster> Get()
        {
            return specimenRepo.Get();
        }

        public ItemList<HISSpecimenMaster> Get(ListOptions option)
        {
            if (option == null)
            {
                return null;
            }

            var result = new ItemList<HISSpecimenMaster>();
            var query = specimenRepo.Get().AsEnumerable();

            if (!string.IsNullOrEmpty(option.SearchText))
            {
                var search = option.SearchText.Trim();
                query = query.Where(s =>
                    (s.Code != null && s.Code.Contains(search)) ||
                    (s.Name != null && s.Name.Contains(search)));
            }

            var list = query.ToList();
            result.TotalRecord = list.Count;

            var sortColumn = string.IsNullOrEmpty(option.SortColumnName) ? "Name" : option.SortColumnName;
            int minRow = (option.CurrentPage - 1) * option.RecordPerPage;
            int pageSize = option.RecordPerPage == 0 ? result.TotalRecord : option.RecordPerPage;

            result.Items = list
                .OrderBy(sortColumn, option.SortDirection)
                .Skip(minRow)
                .Take(pageSize)
                .ToList();

            return result;
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
