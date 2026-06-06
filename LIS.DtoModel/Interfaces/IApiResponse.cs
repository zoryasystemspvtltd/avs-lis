using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace LIS.DtoModel
{
    [JsonObject]
    public interface IAPIResponse
    {
         int StatusCode { get; set; }
         string Message { get; set; }
         IApiError ResponseException { get; set; }
         object Result { get; set; }
    }
}
