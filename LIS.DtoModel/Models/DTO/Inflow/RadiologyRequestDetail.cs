using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models
{
    [Table("RadiologyRequestDetail")]
    public class RadiologyRequestDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [ForeignKey("Patient")]
        public long PatientId { get; set; }

        public virtual PatientDetail Patient { get; set; }

        [MaxLength(20)]
        public string HISRequestNo { get; set; }

        [MaxLength(30)]
        public string AccessionNo { get; set; }

        [MaxLength(30)]
        public string Modality { get; set; }

        [MaxLength(20)]
        public string HISTestCode { get; set; }

        [MaxLength(100)]
        public string HISTestName { get; set; }

        [MaxLength(80)]
        public string Department { get; set; }

        public RadiologyReportStatus ReportStatus { get; set; }

        [MaxLength(80)]
        public string CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        [MaxLength(80)]
        public string ModifiedBy { get; set; }

        public DateTime ModifiedOn { get; set; }
    }
}
