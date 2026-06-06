using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace LIS.DtoModel
{
    [JsonObject]
    public class APIResponse : IAPIResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public IApiError ResponseException { get; set; }
        public object Result { get; set; }
    }
}
