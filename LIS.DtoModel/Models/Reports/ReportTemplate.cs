using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models.Reports
{
    /// <summary>
    /// Report template header (Diagnostic or Radiology). Clinical content lives in versions.
    /// Does not store PHI or result data.
    /// </summary>
    [Table("ReportTemplate")]
    public class ReportTemplate
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(120)]
        public string Name { get; set; }

        /// <summary>Diagnostic | Radiology</summary>
        [Required]
        [MaxLength(40)]
        public string ReportType { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// When set (e.g. Builtin.Diagnostic), Phase 1 points at the existing hard-coded Angular renderer.
        /// Null = declarative template definition on versions.
        /// </summary>
        [MaxLength(80)]
        public string BuiltInRendererKey { get; set; }

        public bool IsSystemDefault { get; set; }

        public bool IsActive { get; set; }

        /// <summary>
        /// Phase 3: CustomGeneric | Specific. Null/empty with IsSystemDefault = System Default factory template.
        /// </summary>
        [MaxLength(40)]
        public string TemplateCategory { get; set; }

        /// <summary>HISTestMaster.Id when TemplateCategory=Specific (test target).</summary>
        public int? TargetTestId { get; set; }

        /// <summary>TestProfileMaster.Id when TemplateCategory=Specific (profile target).</summary>
        public int? TargetProfileId { get; set; }

        [MaxLength(80)]
        public string CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        [MaxLength(80)]
        public string ModifiedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }
    }
}
