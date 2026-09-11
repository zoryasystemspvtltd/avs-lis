using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models.Reports
{
    /// <summary>
    /// Physical print-layout settings for Diagnostic or Radiology reports (one active row per ReportType).
    /// Does not store clinical content or stationery graphics — only clearance and signature placement.
    /// </summary>
    [Table("ReportLayoutConfiguration")]
    public class ReportLayoutConfiguration
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Diagnostic | Radiology</summary>
        [Required]
        [MaxLength(40)]
        public string ReportType { get; set; }

        [Required]
        [MaxLength(20)]
        public string PageSize { get; set; }

        [Required]
        [MaxLength(20)]
        public string Orientation { get; set; }

        /// <summary>Pre-printed stationery header clearance (mm).</summary>
        public decimal HeaderHeightMm { get; set; }

        /// <summary>Pre-printed stationery footer clearance (mm).</summary>
        public decimal FooterHeightMm { get; set; }

        public decimal LeftMarginMm { get; set; }

        public decimal RightMarginMm { get; set; }

        public bool DoctorSignatureEnabled { get; set; }

        /// <summary>Left | Center | Right</summary>
        [MaxLength(20)]
        public string DoctorSignatureHorizontal { get; set; }

        /// <summary>Bottom (content/footer relative)</summary>
        [MaxLength(20)]
        public string DoctorSignatureVertical { get; set; }

        public decimal DoctorSignatureWidthMm { get; set; }

        public decimal DoctorSignatureHeightMm { get; set; }

        /// <summary>Reserved for future use — no technician signature source in current product.</summary>
        public bool TechnicianSignatureEnabled { get; set; }

        [MaxLength(20)]
        public string TechnicianSignatureHorizontal { get; set; }

        [MaxLength(20)]
        public string TechnicianSignatureVertical { get; set; }

        public decimal TechnicianSignatureWidthMm { get; set; }

        public decimal TechnicianSignatureHeightMm { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedOn { get; set; }

        [MaxLength(80)]
        public string CreatedBy { get; set; }

        public DateTime ModifiedOn { get; set; }

        [MaxLength(80)]
        public string ModifiedBy { get; set; }
    }
}
