using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models
{
    [Table("RadiologyResultDetail")]
    public class RadiologyResultDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [ForeignKey("RadiologyRequest")]
        public long RadiologyRequestId { get; set; }

        [JsonIgnore]
        public virtual RadiologyRequestDetail RadiologyRequest { get; set; }

        public string ClinicalHistory { get; set; }

        public string Findings { get; set; }

        public string Impression { get; set; }

        public string Recommendation { get; set; }

        [MaxLength(80)]
        public string AuthorizedBy { get; set; }

        public DateTime? AuthorizedOn { get; set; }

        [MaxLength(200)]
        public string DigitalSignature { get; set; }

        [MaxLength(80)]
        public string CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        [MaxLength(80)]
        public string ModifiedBy { get; set; }

        public DateTime ModifiedOn { get; set; }
    }
}
