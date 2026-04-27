using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIS.DtoModel.Models
{
    [Table("Department")]
    public partial class Departments
    {
        [Key]
        [Required]
        [StringLength(15)]
        public string Code { get; set; }

        [Required]
        [StringLength(55)]
        public string Name { get; set; }
    }
}
