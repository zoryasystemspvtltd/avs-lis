using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models
{
    [Table("TestParameters")]
    public class TestParameter 
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string HISParamCode { get; set; }
        public string HISParamName { get; set; }
        public string HISTestCode { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }

        /* DTO Relations */
        [ForeignKey("TestRequestDetail")]
        public long TestRequestDetailsId { get; set; }
        [JsonIgnore]
        public virtual TestRequestDetail TestRequestDetail { get; set; }
    }
}
