using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models.Reports
{
    /// <summary>
    /// Active assignment of a template to System / Department / Test scope.
    /// Resolution priority: Test → Department → System.
    /// </summary>
    [Table("ReportTemplateAssignment")]
    public class ReportTemplateAssignment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Diagnostic | Radiology</summary>
        [Required]
        [MaxLength(40)]
        public string ReportType { get; set; }

        /// <summary>System | Department | Test | Profile. Phase 3 UI exposes System (Generic) and Test/Profile only.</summary>
        [Required]
        [MaxLength(20)]
        public string ScopeType { get; set; }

        /// <summary>Department.Code when ScopeType=Department (legacy Phase 1; not Phase 3 UI).</summary>
        [MaxLength(15)]
        public string DepartmentCode { get; set; }

        /// <summary>HISTestMaster.Id when ScopeType=Test</summary>
        public int? TestId { get; set; }

        /// <summary>TestProfileMaster.Id when ScopeType=Profile</summary>
        public int? ProfileId { get; set; }

        [ForeignKey("Template")]
        public int TemplateId { get; set; }

        public virtual ReportTemplate Template { get; set; }

        public bool IsActive { get; set; }

        [MaxLength(80)]
        public string CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        [MaxLength(80)]
        public string ModifiedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }
    }
}
