using System.Collections.Generic;
using System.Linq;

namespace LIS.DtoModel
{
    public class ApiError:IApiError
    {
        public bool IsError { get; set; }
        public string ExceptionMessage { get; set; }
        public string Details { get; set; }
        public string ReferenceErrorCode { get; set; }
        public string ReferenceDocumentLink { get; set; }
        public IEnumerable<IValidationError> ValidationErrors { get; set; }
    }
}
