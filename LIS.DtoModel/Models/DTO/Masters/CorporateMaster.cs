using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models
{
    [Table("CorporateMaster")]
    public class CorporateMaster
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Code { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(200)]
        public string Address { get; set; }

        [MaxLength(15)]
        public string Phone { get; set; }

        [MaxLength(100)]
        public string ContactPerson { get; set; }

        public decimal DefaultDiscountPercent { get; set; }

        public bool IsActive { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        public string ModifiedBy { get; set; }

        public DateTime ModifiedOn { get; set; }
    }
}
