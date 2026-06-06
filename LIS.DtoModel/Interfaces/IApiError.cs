using System.Collections.Generic;
using System.Linq;

namespace LIS.DtoModel
{
    public interface IApiError
    {
         bool IsError { get; set; }
         string ExceptionMessage { get; set; }
         string Details { get; set; }
         string ReferenceErrorCode { get; set; }
         string ReferenceDocumentLink { get; set; }
         IEnumerable<IValidationError> ValidationErrors { get; set; }
    }
}
