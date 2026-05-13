using System;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace LIS.DtoModel.Models
{
    [Table("TestMappingMaster")]
    public class TestMappingMaster
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string HISTestCode { get; set; }
        public string HISTestCodeDescription { get; set; }
        public string SpecimenCode { get; set; }
        public string SpecimenName { get; set; }
        public string LISTestCode { get; set; }
        public string LISTestCodeDescription { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string GroupName { get; set; }

        /* DTO Relation */

        [ForeignKey("Equipment")]
        public int EquipmentId { get; set; }

        [JsonIgnore]
        public virtual EquipmentMaster Equipment { get; set; }

        [JsonIgnore]
        public virtual IEnumerable<HISParameterMaster> HisParameters { get; set; }
    }
}
