

namespace Lis.Api.Models
{
    using Microsoft.AspNet.Identity.EntityFramework;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;
    public class RoleModuleMappings
    {
        public RoleModuleMappings() { }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public bool CanAdd { get; set; }
        [Required]
        public bool CanEdit { get; set; }
        [Required]
        public bool CanAuthorize { get; set; }
        [Required]
        public bool CanDelete { get; set; }
        [Required]
        public bool CanView { get; set; }
        [Required]
        public bool CanReject { get; set; }
        
        [ForeignKey("Module")]
        public long? ModuleId { get; set; }

        public virtual UserModule Module { get; set; }

        [Required]
        [MaxLength(128)]
        [ForeignKey("Role")]
        public string RoleId { get; set; }

        public virtual IdentityRole Role { get; set; }

        [ForeignKey("ClientSite")]
        public int? ApplicationId { get; set; }

        [JsonIgnore]
        public virtual ClientApplication ClientSite { get; set; }
    }
}
