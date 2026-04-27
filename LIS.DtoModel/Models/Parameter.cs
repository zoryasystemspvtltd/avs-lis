using LIS.DtoModel.Interfaces;
using System.Collections.Generic;

namespace LIS.DtoModel.Models
{
    public class Parameter: IParameter
    {
        public TestParameter TestParameter { get; set; }
        public IEnumerable<HISParameterRangMaster> ParameterRanges { get; set; }
        
    }
}
