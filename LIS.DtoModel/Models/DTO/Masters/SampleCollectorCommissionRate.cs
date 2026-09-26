using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models
{
    [Table("SampleCollectorCommissionRate")]
    public class SampleCollectorCommissionRate
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CollectorId { get; set; }

        public decimal CommissionPercent { get; set; }

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; }

        /// <summary>Set when a payout closes this slice. Not editable from the rate form.</summary>
        public bool IsLocked { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        public string ModifiedBy { get; set; }

        public DateTime ModifiedOn { get; set; }

        [NotMapped]
        public string RecipientName { get; set; }
    }
}
