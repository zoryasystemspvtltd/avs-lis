using System.Collections.Generic;
using System.Linq;

namespace LIS.DtoModel.Models
{
    public interface IModuleManager<T>
    {
        long Add(T module);
        void Update(T module);
        IEnumerable<T> Get();
        T Get(int Id);
        T Get(long Id);
        T Get(string Code);
        void Delete(T module);
        IEnumerable<T> ExecuteSQL(string Query, params string[] Parameter);
    }
}
