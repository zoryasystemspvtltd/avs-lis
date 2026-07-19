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
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("hisTestCode")]
        public string HISTestCode { get; set; }
        [NotMapped]
        public string HISTestCodeDescription { get; set; }
        [JsonProperty("hisParamCode")]
        public string HISParamCode { get; set; }

        [JsonProperty("hisParamDescription")]
        public string HISParamDescription { get; set; }

        [JsonProperty("hisParamUnit")]
        public string HISParamUnit { get; set; }

        [JsonProperty("hisParamMethod")]
        public string HISParamMethod { get; set; }

        [JsonProperty("lisParamCode")]
        public string LISParamCode { get; set; }

        [JsonProperty("comments")]
        public string Comments { get; set; }

        public DateTime CreatedOn { get; set; }

        /* DTO Relation */

        [ForeignKey("HisTest")]
        [JsonProperty("hisTestId")]
        public int? HisTestId { get; set; }

        [JsonIgnore]
        public virtual HisTestMaster HisTest { get; set; }

        [JsonIgnore]
        public virtual IEnumerable<HISParameterRangMaster> HISParameterRangMaster { get; set; }
    }
}
