using Newtonsoft.Json;
using System.Collections.Generic;

namespace LIS.DtoModel.Models.ExternalApi
{

    // TestAcknowledgement myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse); 

    public class TestParameterAcknowledgement
    {
        [JsonProperty("parameterCode")]
        public string ParameterCode { get; set; }

        [JsonProperty("parameter")]
        public string Parameter { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }
    }

    public class OrderAcknowledgement
    {
        [JsonProperty("testCode")]
        public string TestCode { get; set; }

        [JsonProperty("testName")]
        public string TestName { get; set; }

        [JsonProperty("barcodeNo")]
        public string BarcodeNo { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("testParameter")]
        public List<TestParameterAcknowledgement> TestParameter { get; set; }
    }

    public class TestAcknowledgement
    {
        [JsonProperty("requesitionID")]
        public string RequesitionId { get; set; }
        [JsonProperty("requesitionNumber")]
        public string RequesitionNumber { get; set; }
        [JsonProperty("testID")]
        public string TestId { get; set; }

        [JsonProperty("patientID")]
        public string PatientID { get; set; }

        [JsonProperty("siteID")]
        public object SiteID { get; set; }

        [JsonProperty("orders")]
        public List<OrderAcknowledgement> Orders { get; set; }
    }
}
