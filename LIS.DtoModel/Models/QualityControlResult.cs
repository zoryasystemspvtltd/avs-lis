using LIS.DtoModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIS.DtoModel.Models
{
    public class QualityControlResult : IQualityControlResult
    {
        public ControlResult ControlResult { get; set; }
        public IEnumerable<ControlResultDetails> ControlResultDetails { get; set; }
    }
}
