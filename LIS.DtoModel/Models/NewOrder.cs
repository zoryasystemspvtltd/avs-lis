using LIS.DtoModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIS.DtoModel.Models
{
    public class NewOrder : INewOrder
    {
        public PatientDetail PatientDetail { get; set; }
        public IEnumerable<TestRequestDetail> TestRequestDetails { get; set; }
    }
}
