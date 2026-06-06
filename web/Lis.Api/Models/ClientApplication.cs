namespace Lis.Api.Models
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("ClientApplication")]
    public partial class ClientApplication
    {
        public int Id { get; set; }

        [Required]
        [StringLength(256)]
        public string Name { get; set; }

        [Required]
        [StringLength(1024)]
        public string Description { get; set; }

        [Required]
        [StringLength(50)]
        public string AccessKey { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? ActivityDate { get; set; }

        [StringLength(256)]
        public string ActivityMember { get; set; }

        public int RefreshTokenLifeTime { get; set; }
        [MaxLength(100)]
        public string AllowedOrigin { get; set; }

        public string ModulesAPI { get; set; }

        [JsonIgnore]
        public virtual ICollection<UserModule> Modules { get; set; }

    }

    public class UserApplicationMapping
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey("ClientApplication")]
        public int? ClientApplicationId { get; set; }

        public virtual ClientApplication ClientApplication { get; set; }

        [ForeignKey("ApplicationUser")]
        public string UserId { get; set; }

        public virtual ApplicationUser ApplicationUser { get; set; }
    }
}
