using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface IPatientDetailsManager
    {
        long Add(PatientDetail patientDetail);
        void Update(PatientDetail patientDetail);
        IEnumerable<PatientDetail> Get();
        PatientDetail Get(long Id);
        PatientDetail Get(string Code);
        void Delete(PatientDetail patientDetail);
        ItemList<TestRequestDetail> Get(ListOptions options);
        long CreateNewOrder(NewOrder newOrder);
    }
}
