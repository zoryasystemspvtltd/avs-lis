using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models.Notification
{
    [Table("NotificationTemplate")]
    public class NotificationTemplate
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string EventCode { get; set; }

        public int Channel { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        public string Body { get; set; }

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; }

        public int Version { get; set; }

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public DateTime CreatedOn { get; set; }

        [MaxLength(80)]
        public string CreatedBy { get; set; }

        public DateTime ModifiedOn { get; set; }

        [MaxLength(80)]
        public string ModifiedBy { get; set; }
    }
}
