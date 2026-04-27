using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models
{
    [Table("HISParameterRangMaster")]
    public class HISParameterRangMaster 
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string HISRangeCode { get; set; }
        public string HISRangeValue { get; set; }
        public string Gender { get; set; }
        public decimal AgeFrom { get; set; }
        public decimal AgeTo { get; set; }
        public string AgeType { get; set; }
        public decimal MinValue { get; set; }
        public decimal MaxValue { get; set; }
        public DateTime CreatedOn { get; set; }

        /* DTO Relation */

        [ForeignKey("HisParameter")]
        public int HisParameterId { get; set; }

        [JsonIgnore]
        public virtual HISParameterMaster HisParameter { get; set; }
    }
}
