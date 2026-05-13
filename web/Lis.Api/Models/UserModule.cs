
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Spatial;

namespace Lis.Api.Models
{
    public partial class UserModule
    {
        public UserModule()
        {
            Roles = new HashSet<RoleModuleMappings>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        /// Used as Menu Name
        /// </summary>
        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        [Required]
        [StringLength(128)]
        public string Url { get; set; }

        public int Order { get; set; }

        [Required]
        [ForeignKey("Application")]
        public int ApplicationId { get; set; }

        public bool IsSyatem { get; set; }

        public virtual ClientApplication Application { get; set; }

        [JsonIgnore]
        public virtual ICollection<RoleModuleMappings> Roles { get; set; }


    }
}
