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
                    (d.Code != null && d.Code.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (d.Name != null && d.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            var list = query.ToList();
            result.TotalRecord = list.Count;

            var sortColumn = ResolveSortColumn(option.SortColumnName);

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
            if (department == null)
            {
                throw new ArgumentNullException(nameof(department));
            }

            department.Code = department.Code?.Trim();
            department.Name = department.Name?.Trim();
            if (string.IsNullOrWhiteSpace(department.Code) || string.IsNullOrWhiteSpace(department.Name))
            {
                throw new InvalidOperationException("Department code and name are required.");
            }

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

        private static string ResolveSortColumn(string sortColumnName)
        {
            if (string.IsNullOrWhiteSpace(sortColumnName))
            {
                return "Name";
            }

            var col = sortColumnName.Trim();
            if (col.Equals("code", StringComparison.OrdinalIgnoreCase))
            {
                return "Code";
            }

            if (col.Equals("name", StringComparison.OrdinalIgnoreCase))
            {
                return "Name";
            }

            return "Name";
        }
    }
}
