using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models
{
    [Table("PatientDetails")]
    public class PatientDetail 
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        [MaxLength(20)]
        public string HisPatientId { get; set; }
        [MaxLength(100)]
        public string Name { get; set; }
        public decimal Age { get; set; }
        [MaxLength(10)]
        public string Gender { get; set; }        
        [MaxLength(15)]
        public string Phone { get; set; }
        public bool IsActive { get; set; }
        public DateTime DateOfBirth { get; set; }       
        [NotMapped]
        public ReportStatusType PatientStatus { get; set; }
        [NotMapped]
        [StringLength(30)]
        public string SampleNo { get; set; }
        [MaxLength(80)]
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }

        [JsonIgnore]
        public virtual IEnumerable<TestRequestDetail> TestRequestDetails { get; set; }

        [JsonIgnore]
        public virtual IEnumerable<TestResult> TestResults { get; set; }
        
    }
}
