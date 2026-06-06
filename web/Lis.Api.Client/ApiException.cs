using System;
using System.Collections.Generic;
using System.Text;

namespace B2P.CommunicationClient
{
    public class ApiException
    {
        public int StatusCode { get; set; }
        public IEnumerable<ValidationError> Errors { get; set; }
        public string ReferenceErrorCode { get; set; }
        public string ReferenceDocumentLink { get; set; }
    }
}
