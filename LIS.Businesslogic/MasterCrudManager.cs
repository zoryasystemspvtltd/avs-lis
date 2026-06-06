using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LIS.BusinessLogic
{
    /// <summary>
    /// Generic list/search CRUD for masters with int Id, Code, Name, IsActive.
    /// </summary>
    public class MasterCrudManager<T> where T : class
    {
        protected ModuleRepo<T> Repo;
        protected ILogger Logger;
        protected IModuleIdentity Identity;
        protected GenericUnitOfWork UnitOfWork;
        protected Func<T, string> CodeSelector;
        protected Func<T, string> NameSelector;
        protected Func<T, bool> IsActiveSelector;
        protected string DefaultSortColumn;

        public MasterCrudManager(
            ILogger logger,
            IModuleIdentity identity,
            GenericUnitOfWork unitOfWork,
            Func<T, string> codeSelector,
            Func<T, string> nameSelector,
            Func<T, bool> isActiveSelector,
            string defaultSortColumn = "Name")
        {
            Logger = logger;
            Identity = identity;
            UnitOfWork = unitOfWork;
            CodeSelector = codeSelector;
            NameSelector = nameSelector;
            IsActiveSelector = isActiveSelector;
            DefaultSortColumn = defaultSortColumn;
            Repo = new ModuleRepo<T>(logger, identity, unitOfWork);
        }

        public virtual long Add(T item)
        {
            return Repo.Add(item);
        }

        public virtual void Update(T item)
        {
            Repo.Update(item);
        }

        public virtual void Delete(T item)
        {
            var isActiveProp = typeof(T).GetProperty("IsActive");
            var idProp = typeof(T).GetProperty("Id");
            if (isActiveProp != null && idProp != null)
            {
                var id = Convert.ToInt32(idProp.GetValue(item));
                var existing = Repo.Get(id);
                if (existing != null)
                {
                    isActiveProp.SetValue(existing, false);
                    Repo.Update(existing);
                    return;
                }
            }

            Repo.Delete(item);
        }

        public virtual T GetById(int id)
        {
            return Repo.Get(id);
        }

        public virtual IEnumerable<T> GetAllActive()
        {
            return Repo.Get().Where(IsActiveSelector).OrderBy(NameSelector).ToList();
        }

        public virtual ItemList<T> Get(ListOptions option)
        {
            if (option == null)
            {
                return null;
            }

            var result = new ItemList<T>();
            var query = Repo.Get().AsEnumerable();

            if (!string.IsNullOrEmpty(option.SearchText))
            {
                var search = option.SearchText.Trim();
                query = query.Where(p =>
                    (CodeSelector(p) != null && CodeSelector(p).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (NameSelector(p) != null && NameSelector(p).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0));
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

        protected virtual string ResolveSortColumn(string sortColumnName)
        {
            if (string.IsNullOrWhiteSpace(sortColumnName))
            {
                return DefaultSortColumn;
            }

            var col = sortColumnName.Trim();
            if (col.Equals("Code", StringComparison.OrdinalIgnoreCase) ||
                col.Equals("Name", StringComparison.OrdinalIgnoreCase) ||
                col.Equals("Id", StringComparison.OrdinalIgnoreCase))
            {
                var resolved = char.ToUpper(col[0]) + col.Substring(1);
                if (HasSortProperty(resolved))
                {
                    return resolved;
                }
            }

            if (HasSortProperty(col))
            {
                return col;
            }

            return DefaultSortColumn;
        }

        private bool HasSortProperty(string propertyName)
        {
            return typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) != null;
        }
    }
}
