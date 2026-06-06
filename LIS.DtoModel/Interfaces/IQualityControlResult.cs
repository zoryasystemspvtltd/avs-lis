using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface IQualityControlResult
    {
        ControlResult ControlResult { get; set; }
        IEnumerable<ControlResultDetails> ControlResultDetails { get; set; }
    }
}
