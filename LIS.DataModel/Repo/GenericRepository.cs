using LIS.DataAccess;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

namespace LIS.DataAccess.Repo
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
    {
        private DbSet<TEntity> entities;
        private ApplicationDBContext dbContext;

        public GenericRepository(ApplicationDBContext applicationDBContext)
        {
            dbContext = applicationDBContext;
            dbContext.Database.Initialize(force: false);
            entities = dbContext.Set<TEntity>();
        }

        public bool Add(TEntity entity)
        {
            var addedEntity = entities.Add(entity);
            if (addedEntity != null)
            {
                return true;
            }

            return false;
        }
        public bool Update(TEntity entity)
        {
            //TODO Error in attaching.
            if(dbContext.Entry(entity).State == EntityState.Detached)
            {
                entities.Attach(entity);
            }

            dbContext.Entry(entity).State = EntityState.Modified;
            
            return true;
        }
        public IQueryable<TEntity> GetAllRecords()
        {
            return entities.AsQueryable<TEntity>();
        }
        public TEntity GetFirstOrDefault(int Id)
        {
            return entities.Find(Id);
        }
        public TEntity GetFirstOrDefault(long Id)
        {
            return entities.Find(Id);
        }
        public TEntity GetFirstOrDefault(string Code)
        {
            return entities.Find(Code);
        }

        public IQueryable<TEntity> Search(Expression<Func<TEntity, bool>> predicate)
        {
            return entities.Where(predicate);
        }

        public bool Delete(TEntity entity)
        {
            if (dbContext.Entry(entity).State == EntityState.Detached)
            {
                entities.Attach(entity);
            }
            var deleted = entities.Remove(entity);

            if (deleted != null)
            {
                return true;
            }

            return false;
        }

        public bool Delete(Expression<Func<TEntity, bool>> predicate)
        {
            var items = this.Search(predicate);
            var deleted = entities.RemoveRange(items);

            if (deleted != null)
            {
                return true;
            }

            return false;
        }

        public IEnumerable<TEntity> ExecuteSQL(string Query, params string[] Parameter)
        {
            return dbContext.Database.SqlQuery<TEntity>(Query, Parameter).ToList();
        }

    }
}
