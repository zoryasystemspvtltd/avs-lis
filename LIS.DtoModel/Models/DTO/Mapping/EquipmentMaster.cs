using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIS.DtoModel.Models
{
    [Table("EquipmentMaster")]
    public class EquipmentMaster
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Model { get; set; }
        [Required]
        [StringLength(50)]
        public string AccessKey { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }        
        [JsonIgnore]
        public virtual IEnumerable<TestMappingMaster> TestMapping { get; set; }
        [JsonIgnore]
        public virtual IEnumerable<TestResult> TestResults { get; set; }

        [NotMapped]
        public bool IsAlive { get; set; }
        [NotMapped]
        public DateTime HeartBeatTime { get; set; }
    }
}
