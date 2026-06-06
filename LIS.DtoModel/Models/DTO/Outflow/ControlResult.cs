using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models
{
    [Table("ControlResults")]
    public class ControlResult
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string SampleNo { get; set; }
        public DateTime ResultDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        [NotMapped]
        public string EquipmentName { get; set; }

        [ForeignKey("Equipment")]
        public int EquipmentId { get; set; }

        [JsonIgnore]
        public virtual EquipmentMaster Equipment { get; set; }

        [JsonIgnore]
        public virtual IEnumerable<ControlResultDetails> ControlResultDetails { get; set; }
    }
}
