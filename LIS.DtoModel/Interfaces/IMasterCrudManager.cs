using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface IMasterCrudManager<T> where T : class
    {
        long Add(T item);
        void Update(T item);
        void Delete(T item);
        T GetById(int id);
        IEnumerable<T> GetAllActive();
        ItemList<T> Get(ListOptions option);
    }
}
