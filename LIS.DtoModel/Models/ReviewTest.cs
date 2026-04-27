using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIS.DtoModel.Models
{
    public class ReviewTest
    {
        public Test Test { get; set; }
        public IEnumerable<TestRun> TestRuns { get; set; }
    }

    public class Test
    {
        public string HisPatientId { get; set; }
        public long PatientId { get; set; }
        public string PatientName { get; set; }
        public decimal Age { get; set; }
        public string Gender { get; set; }
        public string Department { get; set; }        
        public string SampleNo { get; set; }
        public string TestName { get; set; }
        public string SpecimenName { get; set; }
        public DateTime SampleCollectionDate { get; set; }
        public DateTime SampleReceivedDate { get; set; }
        public DateTime ReportDate { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public string ReviewedBy { get; set; }
        public DateTime? ReviewDate { get; set; }
        public string TechnicianNote { get; set; }
        public string DoctorNote { get; set; }
    }

    public class TestValues
    {
        public string HISParamCode { get; set; }
        public string HISParamName { get; set; }
        public string ParamValue { get; set; }
        public string ParamUnit { get; set; }
        public decimal MinValue { get; set; }
        public decimal MaxValue { get; set; }
        public string[] HISRangeValues { get; set; }

        public string LISParamCode { get; set; }

    }

    public class TestRun
    {
        public long RunIndex { get; set; }
        public DateTime? ReviewDate { get; set; }
        public ReportStatusType ReportStatus { get; set; }
        public string ReviewedBy { get; set; }

        public IEnumerable<TestValues> TestValues { get; set; }
    }
}
