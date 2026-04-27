using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LIS.DataAccess.Repo
{
    internal interface IGenericRepository<TEntity> where TEntity : class 
    {
        IQueryable<TEntity> GetAllRecords();       
        bool Add(TEntity entity);
        bool Update(TEntity entity);
        TEntity GetFirstOrDefault(int Id);
        TEntity GetFirstOrDefault(long Id);
        TEntity GetFirstOrDefault(string Code);
        IQueryable<TEntity> Search(Expression<Func<TEntity, bool>> predicate);
        bool Delete(TEntity entity);
        IEnumerable<TEntity> ExecuteSQL(string Query, params string[] Parameter);
    }
}
