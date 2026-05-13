using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface IDepartmentManager
    {
        IEnumerable<Departments> Get();
    }
}
