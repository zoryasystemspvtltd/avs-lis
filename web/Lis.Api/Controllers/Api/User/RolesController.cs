using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using Microsoft.Owin;
using Microsoft.AspNet.Identity.Owin;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Lis.Api.Models;
using LIS.Logger;
using Lis.Api.Providers;

namespace Lis.Api.Controllers
{
    public class RolesController : ApiController
    {
        private Models.IdentityDbContext dbContext;
        private ApplicationUserManager userManager;

        private string AccessKey
        {
            get
            {
                var accessKey = System.Web.HttpContext.Current.Request.Headers.GetValues("AccessKey");
                if (accessKey == null || accessKey.Count() == 0)
                {
                    throw new KeyNotFoundException("Invalid AccessKey specified");
                }

                return accessKey.First();
            }
        }

        public RolesController(ApplicationUserManager userManager, Models.IdentityDbContext dbContext)
        {
            this.dbContext = dbContext;
            this.userManager = userManager;
        }

        /// <summary>
        /// Get Role with permission
        /// </summary>
        /// <param name="id">Role Id</param>
        /// <returns></returns>
        [AllowAnonymous]
        public dynamic Get(string id)
        {
            dynamic roles = new
            {
                items = new List<dynamic>()
            };
            foreach (var role in dbContext.Roles
                .Where(p => p.Id == id).ToList())
            {
                roles.items.Add(new
                {
                    id = role.Id,
                    name = role.Name,
                    RolePermission = GetPermissions(role.Id),
                    status=2
                });
            }
            return roles;
        }

        /// <summary>
        /// Get List of role
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        public dynamic Get()
        {
            dynamic roles = new
            {
                items = new List<dynamic>()
            };
            foreach (var role in dbContext.Roles.Where(p => !p.Name
                .Equals("Administrator", StringComparison.OrdinalIgnoreCase)))
            {
                roles.items.Add(new
                {
                    id = role.Id,
                    name = role.Name,
                    status = 2
                });
            }
            return roles;
        }

        [QAuthorize(ModuleName = "Roles"
        , ModulePermissionTypes = ModulePermissionType.CanAdd
        )]
        public dynamic Post(RoleView role)
        {
            try
            {
                var roleExists = dbContext
                        .Roles
                        .FirstOrDefault(p => p.Name.Equals(role.Name, StringComparison.InvariantCultureIgnoreCase));

                if (roleExists != null)
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed, "Role with same name already exists.");
                }

                IdentityRole roleToAdd = new IdentityRole();
                roleToAdd.Id = Guid.NewGuid().ToString();
                roleToAdd.Name = role.Name;
                dbContext.Roles.Add(roleToAdd);
                dbContext.SaveChanges();
                
                return new
                {
                    Id = roleToAdd.Id,
                    Name = roleToAdd.Name
                };
            }
            catch (Exception ex)
            {
                Logger.LogInstance.LogException(ex);
                throw new Exception(ex.Message);
            }
        }

        [QAuthorize(ModuleName = "Roles"
        , ModulePermissionTypes = ModulePermissionType.CanEdit
        )]
        public dynamic Put(RoleView role)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateResponse(HttpStatusCode.PreconditionFailed, ModelState.ToKeyValuePair());
            }

            try
            {
                var roleExists = dbContext
                        .Roles
                        .FirstOrDefault(p => p.Name.Equals(role.Name, StringComparison.InvariantCultureIgnoreCase)
                        && p.Id != role.Id);

                if (roleExists != null)
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed, "Role with same name already exists.");
                }

                var roleToEdit = dbContext
                            .Roles
                            .FirstOrDefault(p => p.Id.Equals(role.Id, StringComparison.InvariantCultureIgnoreCase));

                if (roleToEdit != null)
                {
                    roleToEdit.Name = role.Name;
                }
                dbContext.SaveChanges();
                var ApplicationId = dbContext.ClientApplications.First(p => p.AccessKey.Equals(AccessKey)).Id;
                var existingmappings = dbContext.RoleModuleMappings.Where(p => p.RoleId.Equals(roleToEdit.Id) 
                    && p.ApplicationId == ApplicationId);
                dbContext.RoleModuleMappings.RemoveRange(existingmappings);
                dbContext.SaveChanges();

                foreach (var par in role.RolePermission)
                {
                    dbContext.RoleModuleMappings.Add(new Models.RoleModuleMappings()
                    {
                        CanAdd = par.CanAdd,
                        CanEdit = par.CanEdit,
                        CanAuthorize = par.CanAuthorize,
                        CanReject = par.CanReject,
                        CanDelete = par.CanDelete,
                        CanView = par.CanView,
                        RoleId = roleToEdit.Id,
                        ModuleId = par.Id,
                        ApplicationId = par.ApplicationId
                    });
                }

                dbContext.SaveChanges();

                return new
                {
                    Id = roleToEdit.Id,
                    Name = roleToEdit.Name,
                    RolePermission = role.RolePermission
                };
            }
            catch (Exception ex)
            {
                Logger.LogInstance.LogException(ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [QAuthorize(ModuleName = "Roles"
        , ModulePermissionTypes = ModulePermissionType.CanDelete
        )]
        public void Delete(string Id)
        {
            try
            {
                var roleToDelete = dbContext.Roles.FirstOrDefault(p => p.Id.Equals(Id, StringComparison.InvariantCultureIgnoreCase));
                if (roleToDelete != null)
                {
                    dbContext.Roles.Remove(roleToDelete);
                    dbContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Logger.LogInstance.LogException(ex);
            }
        }

        private IEnumerable<dynamic> GetPermissions(string RoleId)
        {
            var permissions = new List<dynamic>();

            try
            {
                var ApplicationId = dbContext.ClientApplications.First(p => p.AccessKey.Equals(AccessKey)).Id;
                var permissionList = dbContext.Modules.OrderBy(p => p.Order).ToList();

                foreach (var item in permissionList.Where(p=>p.Name != "Applications"))
                {
                    var ifPermissionExists = dbContext.RoleModuleMappings
                        .FirstOrDefault(p => p.ModuleId == item.Id
                                && p.RoleId.Equals(RoleId, StringComparison.OrdinalIgnoreCase)
                                && p.ApplicationId == ApplicationId);

                    if (ifPermissionExists != null)
                    {
                        var permission = new
                        {
                            Id = item.Id,
                            Name = item.Name,
                            Url = item.Url,
                            CanAdd = ifPermissionExists.CanAdd,
                            CanEdit = ifPermissionExists.CanEdit,
                            CanAuthorize = ifPermissionExists.CanAuthorize,
                            CanReject = ifPermissionExists.CanReject,
                            CanDelete = ifPermissionExists.CanDelete,
                            CanView = ifPermissionExists.CanView,
                            ApplicationId = ifPermissionExists.ApplicationId
                        };
                        permissions.Add(permission);
                    }
                    else
                    {
                        var permission = new
                        {
                            Id = item.Id,
                            Name = item.Name,
                            Url = item.Url,
                            CanView = false,
                            CanAdd = false,
                            CanEdit = false,
                            CanAuthorize = false,
                            CanReject = false,
                            ApplicationId = ApplicationId
                        };
                        permissions.Add(permission);
                    }

                }

                return permissions;
            }
            catch (Exception ex)
            {
                Logger.LogInstance.LogException(ex);
                throw new Exception(ex.Message);
            }
        }
    }
    public class RoleView
    {
        [Required]
        public string Name { get; set; }
        public string Id { get; set; }

        public int TotalUsers { get; set; }

        public List<RolePermission> RolePermission { get; set; }
    }

    public class RolePermission
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanAuthorize { get; set; }
        public bool CanReject { get; set; }
        public bool CanDelete { get; set; }
        public bool CanView { get; set; }
        public int ApplicationId { get; set; }
    }
}
