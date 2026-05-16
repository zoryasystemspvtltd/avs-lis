using System;
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

        [ForeignKey("HisTestMaster")]
        public int TestId { get; set; }

        public virtual HisTestMaster HisTestMaster { get; set; }

        public decimal Rate { get; set; }

        public decimal EmergencyRate { get; set; }

        public decimal DiscountPercent { get; set; }

        public decimal TaxPercent { get; set; }

        public int RateType { get; set; }

        public int? CorporateId { get; set; }

        public int? ReferralDoctorId { get; set; }

        public int? TestProfileId { get; set; }

        public DateTime EffectiveStart { get; set; }

        public DateTime EffectiveEnd { get; set; }

        public bool IsActive { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        public string ModifiedBy { get; set; }

        public DateTime ModifiedOn { get; set; }

        [NotMapped]
        public string TestName { get; set; }

        [NotMapped]
        public string TestCode { get; set; }

        [NotMapped]
        public string CorporateName { get; set; }

        [NotMapped]
        public string ReferralDoctorName { get; set; }

        [NotMapped]
        public string ProfileName { get; set; }
    }
}
