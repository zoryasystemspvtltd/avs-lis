using LIS.DtoModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models
{
    [Table("TestResultDetails")]
    public class TestResultDetails 
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string LISParamCode { get; set; }
        public string LISParamValue { get; set; }
        public string LISParamUnit { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }

        /* DTO Relations */
        [ForeignKey("TestResult")]
        public long TestResultId { get; set; }
        [JsonIgnore]
        public virtual TestResult TestResult { get; set; }

        
    }
}

