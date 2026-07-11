using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lis.Api.Models
{
    /// <summary>
    /// Optional menu-level permission overlay for a role.
    /// When no active rows exist for a role+module, module-level RoleModuleMappings apply (backward compatible).
    /// Scalar FKs only — no navigation properties (avoids EF6 IdentityRole relationship failures).
    /// </summary>
    [Table("RoleMenuPermission")]
    public class RoleMenuPermission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [StringLength(128)]
        [Column(TypeName = "nvarchar")]
        public string RoleId { get; set; }

        public long ModuleId { get; set; }

        /// <summary>Stable menu identifier, e.g. WORKING_BOARD_SAMPLES</summary>
        [Required]
        [StringLength(100)]
        public string MenuKey { get; set; }

        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanAuthorize { get; set; }
        public bool CanReject { get; set; }

        public bool IsActive { get; set; }

        /// <summary>Scopes menu rows to the same client application as RoleModuleMappings.</summary>
        public int? ApplicationId { get; set; }

        [StringLength(128)]
        public string CreatedBy { get; set; }

        public DateTime? CreatedOn { get; set; }

        [StringLength(128)]
        public string ModifiedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }
    }
}
