using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface INewOrder
    {
        PatientDetail PatientDetail { get; set; }
        IEnumerable<TestRequestDetail> TestRequestDetails { get; set; }
    }
}
