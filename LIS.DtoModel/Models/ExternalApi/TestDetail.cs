using Newtonsoft.Json;
using System.Collections.Generic;

namespace LIS.DtoModel.Models.ExternalApi
{
    // Root TestDetail = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class TestParameter
    {
        [JsonProperty("parameterCode")]
        public string ParameterCode { get; set; }

        [JsonProperty("parameter")]
        public string Parameter { get; set; }        
    }

    public class Order
    {
        [JsonProperty("testCode")]
        public string TestCode { get; set; }
        [JsonProperty("testName")]
        public string TestName { get; set; }
        [JsonProperty("barcodeNo")]
        public string BarcodeNo { get; set; }
        
        [JsonProperty("testParameter")]
        public IEnumerable<TestParameter> TestParameter { get; set; }
    }

    public class TestDetail
    {
        [JsonProperty("patientID")]
        public string PatientId { get; set; }
        [JsonProperty("patientName")]
        public string PatientName { get; set; }
        [JsonProperty("DOB")]
        public string DOB { get; set; }
        [JsonProperty("gender")]
        public string Gender { get; set; }
        [JsonProperty("requisitionId")]
        public string RequisitionId { get; set; }
        [JsonProperty("requisitionNo")]
        public string RequisitionNumber { get; set; }
        [JsonProperty("siteID")]
        public object SiteId { get; set; }
        [JsonProperty("ipNo")]
        public string IPNo { get; set; }
        [JsonProperty("bedNo")]
        public string BedNo { get; set; }
        [JsonProperty("mrNo")]
        public string MRNo { get; set; }
        [JsonProperty("hisRequestId")]
        public string HISRequestId { get; set; }
        [JsonProperty("hisRequestNo")]
        public string HISRequestNo { get; set; }
        [JsonProperty("departmentId")]
        public string DepartmentId { get; set; }
        [JsonProperty("department")]
        public string Department { get; set; }
        [JsonProperty("orders")]
        public IEnumerable<Order> Orders { get; set; }
    }
}
