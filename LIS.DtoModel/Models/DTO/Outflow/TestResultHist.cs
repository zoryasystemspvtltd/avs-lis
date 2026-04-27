using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models
{
    [Table("TestResultsHist")]
    public class TestResultHist 
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long TestResultId { get; set; }
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
        public long TestRequestId { get; set; }
        public int EquipmentId { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public ReportStatusType ReportStatus { get; set; }
    }
}
