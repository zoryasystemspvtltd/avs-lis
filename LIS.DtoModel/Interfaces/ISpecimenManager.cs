using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface ISpecimenManager
    {

        long Add(HISSpecimenMaster specimen);
        void Update(HISSpecimenMaster specimen);
        IEnumerable<HISSpecimenMaster> Get();
        ItemList<HISSpecimenMaster> Get(ListOptions option);
        HISSpecimenMaster Get(int Id);
        HISSpecimenMaster Get(string Code);
        void Delete(HISSpecimenMaster specimen);
    }
}
