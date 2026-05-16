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
            departmentRepo = new ModuleRepo<Departments>(logger, this.identity, this.genericUnitOfWork);
        }

        public IEnumerable<Departments> Get()
        {
            return departmentRepo.Get();
        }

        public ItemList<Departments> Get(ListOptions option)
        {
            if (option == null)
            {
                return null;
            }

            var result = new ItemList<Departments>();
            var query = departmentRepo.Get().AsEnumerable();

            if (!string.IsNullOrEmpty(option.SearchText))
            {
                var search = option.SearchText.Trim();
                query = query.Where(d =>
                    d.Code.Contains(search) ||
                    d.Name.Contains(search));
            }

            var list = query.ToList();
            result.TotalRecord = list.Count;

            var sortColumn = string.IsNullOrEmpty(option.SortColumnName) ? "Name" : option.SortColumnName;
            if (sortColumn != "Code" && sortColumn != "Name")
            {
                sortColumn = "Name";
            }

            int minRow = (option.CurrentPage - 1) * option.RecordPerPage;
            int pageSize = option.RecordPerPage == 0 ? result.TotalRecord : option.RecordPerPage;

            result.Items = list
                .OrderBy(sortColumn, option.SortDirection)
                .Skip(minRow)
                .Take(pageSize)
                .ToList();

            return result;
        }

        public void Add(Departments department)
        {
            departmentRepo.Add(department);
        }

        public void Update(Departments department)
        {
            departmentRepo.Update(department);
        }

        public void Delete(Departments department)
        {
            departmentRepo.Delete(department);
        }

        public Departments Get(string code)
        {
            return departmentRepo.Get(code);
        }
    }
}
