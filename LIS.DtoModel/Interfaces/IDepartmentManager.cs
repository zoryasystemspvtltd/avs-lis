using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface IDepartmentManager
    {
        IEnumerable<Departments> Get();
        ItemList<Departments> Get(ListOptions option);
        void Add(Departments department);
        void Update(Departments department);
        void Delete(Departments department);
        Departments Get(string code);
    }
}
