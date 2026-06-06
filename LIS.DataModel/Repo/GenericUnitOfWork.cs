using LIS.DtoModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIS.DataAccess.Repo
{
    public class GenericUnitOfWork:IDisposable
    {
        private ApplicationDBContext dbContext;
        public GenericUnitOfWork(ApplicationDBContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public Type type { get; set; }

        public GenericRepository<TEntity> GetRepoInstance<TEntity>() where TEntity : class
        {
            return new GenericRepository<TEntity>(this.dbContext);
        }
        public void SaveChanges()
        {
            this.dbContext.SaveChanges();
        }

        public void SetActivityLog<T>(T item, IModuleIdentity identity) where T : class
        {
            var entry = this.dbContext.Entry(item);
            var entityType = typeof(T);
            if (entityType.GetProperty("CreatedOn") != null)
            {
                entry.Property("CreatedOn").CurrentValue = DateTime.Now;
            }

            if (entityType.GetProperty("CreatedBy") != null)
            {
                entry.Property("CreatedBy").CurrentValue = identity?.ActivityMember;
            }
        }
        public int GetId<T>(T item) where T : class
        {
            var idProp = typeof(T).GetProperty("Id");
            if (idProp == null)
            {
                return 0;
            }

            return Convert.ToInt32(this.dbContext.Entry(item).Property("Id").CurrentValue);
        }

        ~GenericUnitOfWork()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.dbContext.Dispose();
                this.dbContext = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
