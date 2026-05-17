using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models
{
    [Table("TestRateMaster")]
    public class TestRateMaster
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public decimal Rate { get; set; }
        public DateTime EffectiveStart { get; set; }
        public DateTime EffectiveEnd { get; set; }
        public DateTime CreatedOn { get; set; }= DateTime.Now;
        public DateTime? ModifiedOn { get; set; }
        public bool IsActive { get; set; } = true;
        public string CreatedBy { get; set; }
        [ForeignKey("HisTestMaster")]
        public int TestId { get; set; }
       
        [JsonIgnore]
        public virtual HisTestMaster HisTestMaster { get; set; }

        [NotMapped]
        public string TestName { get; set; }
    }
}
