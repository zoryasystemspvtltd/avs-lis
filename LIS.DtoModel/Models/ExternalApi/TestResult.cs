using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LIS.DtoModel.Models.ExternalApi
{
    // Root TestResult = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class TestParameterResult
    {
        [JsonProperty("parameterCode")]
        public string ParameterCode { get; set; }

        [JsonProperty("parameter")]
        public string Parameter { get; set; }

        [JsonProperty("result")]
        public string Result { get; set; }
    }

    public class OrderResult
    {
        [JsonProperty("barcodeNo")]
        public string BarcodeNo { get; set; }

        [JsonProperty("testCode")]
        public string TestCode { get; set; }

        [JsonProperty("testName")]
        public string TestName { get; set; }

        [JsonProperty("testParameter")]
        public List<TestParameterResult> TestParameter { get; set; }
    }
    public class TestResult
    {
        [JsonProperty("patientID")]
        public string PatientID { get; set; }

        [JsonProperty("patientName")]
        public string PatientName { get; set; }

        [JsonProperty("DOB")]
        public object DOB { get; set; }

        [JsonProperty("gender")]
        public string Gender { get; set; }

        [JsonProperty("siteID")]
        public string SiteID { get; set; }

        [JsonProperty("orders")]
        public List<OrderResult> Orders { get; set; }
    }

    public class TestResultDto
    {
        public long Id { get; set; }
        public string SampleNo { get; set; }
        public string HISTestCode { get; set; }
        public string LISTestCode { get; set; }
        public string SpecimenCode { get; set; }
        public string SpecimenName { get; set; }
        public DateTime ResultDate { get; set; }
        public DateTime SampleCollectionDate { get; set; }
        public DateTime SampleReceivedDate { get; set; }
        public DateTime? AuthorizationDate { get; set; }
        public string AuthorizedBy { get; set; }
        public DateTime? ReviewDate { get; set; }
        public string ReviewedBy { get; set; }
        public string TechnicianNote { get; set; }
        public string DoctorNote { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public long PatientId { get; set; }
        public virtual PatientDetail Patient { get; set; }
        public long TestRequestId { get; set; }
        public int EquipmentId { get; set; }
    }

    public class TestResultDetailsDto
    {
        public long Id { get; set; }
        public string LISParamCode { get; set; }
        public string LISParamValue { get; set; }
        public string LISParamUnit { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public long TestResultId { get; set; }

    }

    public class ResultDto
    {
        public TestResultDto TestResult { get; set; }
        public IEnumerable<TestResultDetailsDto> TestResultDetails { get; set; }
    }
}
