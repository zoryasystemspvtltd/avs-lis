using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LIS.Businesslogic
{
    public class ResponseManager : IResponseManager
    {
        public APIResponse CreateResponse(HttpStatusCode httpStatusCode, string message, IApiError apiError, object result)
        {
            return new APIResponse()
            {
                StatusCode = (int)httpStatusCode,
                Message = message,
                Result = result,
                ResponseException = apiError
            };
        }
    }
}
