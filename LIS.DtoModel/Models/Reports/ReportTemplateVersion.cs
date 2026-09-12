using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models.Reports
{
    /// <summary>
    /// Immutable published versions; drafts are editable. DefinitionJson is declarative layout only (no PHI).
    /// </summary>
    [Table("ReportTemplateVersion")]
    public class ReportTemplateVersion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey("Template")]
        public int TemplateId { get; set; }

        public virtual ReportTemplate Template { get; set; }

        public int VersionNumber { get; set; }

        /// <summary>Draft | Published | Archived</summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; }

        /// <summary>JSON layout definition (schemaVersioned). Never store patient/result PHI.</summary>
        [Required]
        public string DefinitionJson { get; set; }

        public int SchemaVersion { get; set; }

        [MaxLength(80)]
        public string CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        [MaxLength(80)]
        public string ModifiedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        [MaxLength(80)]
        public string PublishedBy { get; set; }

        public DateTime? PublishedOn { get; set; }

        [MaxLength(80)]
        public string ArchivedBy { get; set; }

        public DateTime? ArchivedOn { get; set; }
    }
}
