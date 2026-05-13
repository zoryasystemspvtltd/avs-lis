using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Lis.Api.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        
        public string FirstName { get; internal set; }
        public string LastName { get; internal set; }

        [StringLength(256)]
        public string AlternativeEmail { get; set; }

        public DateTime? DOB { get; set; }

        [StringLength(256)]
        public string Country { get; internal set; }

        [StringLength(256)]
        public string State { get; internal set; }

        [StringLength(256)]
        public string Address { get; set; }

        [StringLength(10)]
        public string Zip { get; set; }
        
        [StringLength(256)]
        public string AreaOfInterest { get; set; }

        [StringLength(256)]
        public string Qualification { get; set; }

        public bool IsBlocked { get; internal set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager, string authenticationType)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, authenticationType);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class IdentityDbContext : IdentityDbContext<ApplicationUser>
    {
        public IdentityDbContext()
           : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public virtual DbSet<RoleModuleMappings> RoleModuleMappings { get; set; }
        public virtual DbSet<UserModule> Modules { get; set; }
        public virtual DbSet<ClientApplication> ClientApplications { get; set; }
        public virtual DbSet<UserApplicationMapping> UserApplicationMappings { get; set; }
        public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
    }
}