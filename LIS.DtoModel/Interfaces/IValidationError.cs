using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace LIS.DtoModel
{
    public interface IValidationError
    {
        string Field { get; }
        string Message { get; }
    }
}
