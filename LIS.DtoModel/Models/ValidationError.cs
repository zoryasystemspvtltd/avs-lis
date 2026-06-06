using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace LIS.DtoModel
{
    public class ValidationError:IValidationError
    {
        public string Field { get; }
        public string Message { get; }
    }
}
