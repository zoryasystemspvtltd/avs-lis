using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LIS.DtoModel.Interfaces
{
    public interface IResponseManager
    {
        APIResponse CreateResponse(HttpStatusCode httpStatusCode, string message, IApiError apiError, object result);
    }
}
