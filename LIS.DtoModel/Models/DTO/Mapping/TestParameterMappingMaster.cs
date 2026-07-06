using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models
{
    [Table("TestParameterMappingMaster")]
    public class TestParameterMappingMaster
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [JsonProperty("id")]
        public int Id { get; set; }

        [ForeignKey("HisTest")]
        [JsonProperty("hisTestId")]
        public int HisTestId { get; set; }

        [JsonIgnore]
        public virtual HisTestMaster HisTest { get; set; }

        [ForeignKey("HisParameter")]
        [JsonProperty("hisParameterId")]
        public int HisParameterId { get; set; }

        [JsonIgnore]
        public virtual HISParameterMaster HisParameter { get; set; }

        [JsonProperty("isActive")]
        public bool IsActive { get; set; }

        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }

        [NotMapped]
        [JsonProperty("hisTestCode")]
        public string HISTestCode { get; set; }

        [NotMapped]
        [JsonProperty("hisTestCodeDescription")]
        public string HISTestCodeDescription { get; set; }

        [NotMapped]
        [JsonProperty("hisParamCode")]
        public string HISParamCode { get; set; }

        [NotMapped]
        [JsonProperty("hisParamDescription")]
        public string HISParamDescription { get; set; }
    }
}
