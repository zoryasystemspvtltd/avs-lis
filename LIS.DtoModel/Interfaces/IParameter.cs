using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface IParameter
    {
         TestParameter TestParameter { get; set; }
         IEnumerable<HISParameterRangMaster> ParameterRanges { get; set; }
    }
}
