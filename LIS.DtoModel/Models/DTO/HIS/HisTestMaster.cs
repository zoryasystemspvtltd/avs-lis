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
    [Table("HISTestMaster")]
    public class HisTestMaster
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string HISTestCode { get; set; }
        public string HISTestCodeDescription { get; set; }
        public string HISSpecimenCode { get; set; }
        public string HISSpecimenName { get; set; }
        public DateTime CreatedOn { get; set; }

        public bool IsActive { get; set; }
        [ForeignKey("Departments")]
        public string DepartmentCode { get; set; }

        [JsonIgnore]
        public virtual Departments Departments { get; set; }

        /* DTO Relation */

        [JsonIgnore]
        public virtual IEnumerable<HISParameterMaster> HISParameters { get; set; }
    }
}
