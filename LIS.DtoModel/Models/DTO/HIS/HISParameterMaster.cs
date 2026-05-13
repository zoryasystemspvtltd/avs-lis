using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models
{
    [Table("HISParameterMaster")]
    public class HISParameterMaster
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string HISTestCode { get; set; }
        public string HISParamCode { get; set; }
        public string HISParamDescription { get; set; }
        public string HISParamUnit { get; set; }
        public string HISParamMethod { get; set; }
        public string LISParamCode { get; set; }
        public DateTime CreatedOn { get; set; }

        /* DTO Relation */

        [ForeignKey("HisTest")]
        public int HisTestId { get; set; }

        [JsonIgnore]
        public virtual HisTestMaster HisTest { get; set; }

        [JsonIgnore]
        public virtual IEnumerable<HISParameterRangMaster> HISParameterRangMaster { get; set; }
    }
}
