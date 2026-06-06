using LIS.DtoModel;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LIS.DataAccess.Repo
{
    public class ModuleRepo<T> : IModuleManager<T> where T : class
    {
        private GenericUnitOfWork genericUnitOfWork;
        private ILogger logger;
        private IModuleIdentity identity;
        public ModuleRepo(ILogger Logger, IModuleIdentity identity, GenericUnitOfWork genericUnitOfWork)
        {
            this.identity = identity;
            logger = Logger;
            this.genericUnitOfWork = genericUnitOfWork;
        }

        public long Add(T item)
        {
            try
            {
                genericUnitOfWork.SetActivityLog<T>(item, identity);
                genericUnitOfWork.GetRepoInstance<T>().Add(item);
                genericUnitOfWork.SaveChanges();
                return genericUnitOfWork.GetId<T>(item);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                throw ex;
            }
        }

        public void Delete(T item)
        {
            try
            {
                genericUnitOfWork.GetRepoInstance<T>().Delete(item);
                genericUnitOfWork.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                throw ex;
            }
        }

        public void Delete(Expression<Func<T, bool>> predicate)
        {
            try
            {
                genericUnitOfWork.GetRepoInstance<T>().Delete(predicate);
                genericUnitOfWork.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                throw ex;
            }
        }

        public IEnumerable<T> ExecuteSQL(string Query, params string[] Parameter)
        {
            return genericUnitOfWork.GetRepoInstance<T>().ExecuteSQL(Query, Parameter);
        }

        public IEnumerable<T> Get()
        {
            try
            {
                return genericUnitOfWork
                    .GetRepoInstance<T>().GetAllRecords()
                    .ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                throw ex;
            }
        }

        public T Get(int Id)
        {
            try
            {
                return genericUnitOfWork.GetRepoInstance<T>().GetFirstOrDefault(Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                throw ex;
            }
        }

        public T Get(long Id)
        {
            try
            {
                return genericUnitOfWork.GetRepoInstance<T>().GetFirstOrDefault(Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                throw ex;
            }
        }

        public T Get(string Code)
        {
            try
            {
                return genericUnitOfWork.GetRepoInstance<T>().GetFirstOrDefault(Code);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                throw ex;
            }
        }

        public IQueryable<T> Get(Expression<Func<T, bool>> predicate)
        {
            try
            {
                return genericUnitOfWork.GetRepoInstance<T>().Search(predicate);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                throw ex;
            }
        }

        public void Update(T item)
        {
            try
            {
                genericUnitOfWork.SetActivityLog<T>(item, identity);
                genericUnitOfWork.GetRepoInstance<T>().Update(item);
                genericUnitOfWork.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                throw ex;
            }
        }
    }
}
