using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models
{
    [Table("TestRequestDetails")]
    public class TestRequestDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        [MaxLength(30)]
        public string SampleNo { get; set; }
        [MaxLength(20)]
        public string HISTestCode { get; set; }
        [MaxLength(100)]
        public string HISTestName { get; set; }
        public DateTime SampleCollectionDate { get; set; }
        public DateTime SampleReceivedDate { get; set; }
        [MaxLength(20)]
        public string SpecimenCode { get; set; }
        [MaxLength(100)]
        public string SpecimenName { get; set; }
        [MaxLength(80)]
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public ReportStatusType ReportStatus { get; set; }        
        [MaxLength(20)]
        public string IPNo { get; set; }
        [MaxLength(20)]
        public string BedNo { get; set; }
        [MaxLength(20)]
        public string MRNo { get; set; }
        [MaxLength(20)]
        public string HISRequestId { get; set; }
        [MaxLength(20)]
        public string HISRequestNo { get; set; }
        [MaxLength(20)]
        public string DepartmentId { get; set; }
        [MaxLength(80)]
        public string Department { get; set; }
        [NotMapped]
        public bool RequireReOpenion { get; set; }
        [NotMapped]
        public string LISTestCode { get; set; }

        /* DTO Relations*/
        [ForeignKey("Patient")]
        public long PatientId { get; set; }

        public virtual PatientDetail Patient { get; set; }

        [JsonIgnore]
        public virtual IEnumerable<TestResult> TestResults { get; set; }

        [JsonIgnore]
        public virtual IEnumerable<TestParameter> TestParameters { get; set; }
    }
}
