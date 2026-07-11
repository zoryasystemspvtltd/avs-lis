using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
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
        /// Role list — any authenticated user (login + role management grid).
        /// </summary>
        [Authorize]
        public dynamic Get()
        {
            return BuildRoleList();
        }

        /// <summary>
        /// Role details with permissions. Empty/missing id falls back to list
        /// (handles /api/Roles/ trailing-slash routing).
        /// </summary>
        [QAuthorize(ModuleName = "Roles", ModulePermissionTypes = ModulePermissionType.CanView)]
        public dynamic Get(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BuildRoleList();
            }

            dynamic roles = new
            {
                items = new List<dynamic>()
            };
            foreach (var role in dbContext.Roles
                .Where(p => p.Id == id).ToList())
            {
                IEnumerable<dynamic> menuPermissions;
                try
                {
                    menuPermissions = GetMenuPermissions(role.Id);
                }
                catch (Exception ex)
                {
                    Logger.LogInstance.LogException(ex);
                    menuPermissions = new List<dynamic>();
                }

                roles.items.Add(new
                {
                    id = role.Id,
                    name = role.Name,
                    RolePermission = GetPermissions(role.Id),
                    RoleMenuPermission = menuPermissions,
                    status = 2
                });
            }
            return roles;
        }

        private dynamic BuildRoleList()
        {
            dynamic roles = new
            {
                items = new List<dynamic>()
            };
            try
            {
                // Include Administrator so it is visible (system default, not deleted).
                // UI should treat it as read-only / non-editable.
                var allRoles = dbContext.Roles.OrderBy(p => p.Name).ToList();
                foreach (var role in allRoles)
                {
                    var isAdmin = string.Equals(role.Name, "Administrator", StringComparison.OrdinalIgnoreCase);
                    roles.items.Add(new
                    {
                        id = role.Id,
                        name = role.Name,
                        isSystem = isAdmin,
                        status = 2
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogInstance.LogException(ex);
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
                var roleToEdit = dbContext
                            .Roles
                            .FirstOrDefault(p => p.Id.Equals(role.Id, StringComparison.InvariantCultureIgnoreCase));

                if (roleToEdit == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Role not found.");
                }

                if (string.Equals(roleToEdit.Name, "Administrator", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(role.Name, "Administrator", StringComparison.OrdinalIgnoreCase))
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed,
                        "Administrator is a system role and cannot be edited.");
                }

                var roleExists = dbContext
                        .Roles
                        .FirstOrDefault(p => p.Name.Equals(role.Name, StringComparison.InvariantCultureIgnoreCase)
                        && p.Id != role.Id);

                if (roleExists != null)
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed, "Role with same name already exists.");
                }

                roleToEdit.Name = role.Name;
                dbContext.SaveChanges();
                var ApplicationId = dbContext.ClientApplications.First(p => p.AccessKey.Equals(AccessKey)).Id;
                var existingmappings = dbContext.RoleModuleMappings.Where(p => p.RoleId.Equals(roleToEdit.Id) 
                    && p.ApplicationId == ApplicationId);
                dbContext.RoleModuleMappings.RemoveRange(existingmappings);
                dbContext.SaveChanges();

                if (role.RolePermission != null)
                {
                    foreach (var par in role.RolePermission)
                    {
                        if (par == null || par.Id <= 0)
                        {
                            continue;
                        }

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
                            ApplicationId = par.ApplicationId > 0 ? par.ApplicationId : ApplicationId
                        });
                    }

                    dbContext.SaveChanges();
                }

                // null = legacy clients: leave menu rows unchanged. Non-null = replace overlay for this app.
                if (role.RoleMenuPermission != null)
                {
                    // Do not swallow — menu overlay must persist or the client must see the error.
                    SaveMenuPermissions(roleToEdit.Id, ApplicationId, role.RoleMenuPermission);
                }

                return new
                {
                    Id = roleToEdit.Id,
                    Name = roleToEdit.Name,
                    RolePermission = role.RolePermission,
                    RoleMenuPermission = role.RoleMenuPermission
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
                    if (string.Equals(roleToDelete.Name, "Administrator", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                    var menuRows = dbContext.RoleMenuPermissions.Where(p => p.RoleId == roleToDelete.Id).ToList();
                    if (menuRows.Count > 0)
                    {
                        dbContext.RoleMenuPermissions.RemoveRange(menuRows);
                    }
                    dbContext.Roles.Remove(roleToDelete);
                    dbContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Logger.LogInstance.LogException(ex);
            }
        }

        private void SaveMenuPermissions(string roleId, int applicationId, List<RoleMenuPermissionView> menus)
        {
            // IMPORTANT: do not use StringComparison inside EF Where — EF6 cannot translate it (throws → empty save).
            var existing = dbContext.RoleMenuPermissions
                .Where(p => p.RoleId == roleId && p.ApplicationId == applicationId)
                .ToList();
            if (existing.Count > 0)
            {
                dbContext.RoleMenuPermissions.RemoveRange(existing);
                dbContext.SaveChanges();
            }

            if (menus == null || menus.Count == 0)
            {
                return;
            }

            var userId = User.Identity.GetUserId();
            var now = DateTime.UtcNow;
            var modules = dbContext.Modules
                .Where(m => m.ApplicationId == applicationId)
                .ToList();

            foreach (var menu in menus)
            {
                if (menu == null || string.IsNullOrWhiteSpace(menu.MenuKey))
                {
                    continue;
                }

                if (!MenuCatalog.IsKnown(menu.MenuKey))
                {
                    continue;
                }

                var hasAny = menu.CanView || menu.CanAdd || menu.CanEdit || menu.CanDelete
                    || menu.CanAuthorize || menu.CanReject;
                if (!hasAny)
                {
                    continue;
                }

                var def = MenuCatalog.Find(menu.MenuKey);
                if (def == null)
                {
                    continue;
                }

                var module = modules.FirstOrDefault(m =>
                    m.Name != null && m.Name.Equals(def.ModuleName, StringComparison.OrdinalIgnoreCase));

                if (module == null)
                {
                    continue;
                }

                dbContext.RoleMenuPermissions.Add(new RoleMenuPermission
                {
                    RoleId = roleId,
                    ModuleId = module.Id,
                    MenuKey = def.MenuKey,
                    CanView = menu.CanView,
                    CanAdd = menu.CanAdd,
                    CanEdit = menu.CanEdit,
                    CanDelete = menu.CanDelete,
                    CanAuthorize = menu.CanAuthorize,
                    CanReject = menu.CanReject,
                    IsActive = true,
                    ApplicationId = applicationId,
                    CreatedBy = userId,
                    CreatedOn = now,
                    ModifiedBy = userId,
                    ModifiedOn = now
                });
            }

            dbContext.SaveChanges();
        }

        private IEnumerable<dynamic> GetMenuPermissions(string RoleId)
        {
            var result = new List<dynamic>();
            try
            {
                var clientApp = dbContext.ClientApplications.FirstOrDefault(p => p.AccessKey.Equals(AccessKey));
                if (clientApp == null)
                {
                    return result;
                }

                var applicationId = clientApp.Id;

                // Load overlay rows separately so a query failure cannot wipe the whole catalog response.
                var existing = new List<RoleMenuPermission>();
                try
                {
                    existing = dbContext.RoleMenuPermissions
                        .AsNoTracking()
                        .Where(p => p.RoleId == RoleId
                            && p.ApplicationId == applicationId
                            && p.IsActive)
                        .ToList();
                }
                catch (Exception ex)
                {
                    Logger.LogInstance.LogException(ex);
                    existing = new List<RoleMenuPermission>();
                }

                var modules = dbContext.Modules
                    .Where(p => p.ApplicationId == applicationId)
                    .ToList();

                foreach (var def in MenuCatalog.All)
                {
                    var module = modules.FirstOrDefault(m =>
                        m.Name != null && string.Equals(m.Name, def.ModuleName, StringComparison.OrdinalIgnoreCase));
                    if (module == null)
                    {
                        continue;
                    }

                    var row = existing.FirstOrDefault(p =>
                        string.Equals(MenuCatalog.NormalizeKey(p.MenuKey), def.MenuKey, StringComparison.OrdinalIgnoreCase));

                    result.Add(new
                    {
                        MenuKey = def.MenuKey,
                        ModuleId = module.Id,
                        ModuleName = module.Name,
                        Section = def.Section,
                        Label = def.Label,
                        Route = def.Route,
                        Order = def.Order,
                        CanView = row != null && row.CanView,
                        CanAdd = row != null && row.CanAdd,
                        CanEdit = row != null && row.CanEdit,
                        CanDelete = row != null && row.CanDelete,
                        CanAuthorize = row != null && row.CanAuthorize,
                        CanReject = row != null && row.CanReject,
                        ApplicationId = applicationId
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogInstance.LogException(ex);
            }

            return result;
        }

        private IEnumerable<dynamic> GetPermissions(string RoleId)
        {
            var permissions = new List<dynamic>();

            try
            {
                var ApplicationId = dbContext.ClientApplications.First(p => p.AccessKey.Equals(AccessKey)).Id;
                // Only modules for the active client application appear on Role Permission Details.
                var permissionList = dbContext.Modules
                    .Where(p => p.ApplicationId == ApplicationId)
                    .OrderBy(p => p.Order)
                    .ToList();

                foreach (var item in permissionList.Where(p => p.Name != "Applications"))
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
                            CanDelete = false,
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

        /// <summary>
        /// Optional. When null on PUT, existing RoleMenuPermission rows are left unchanged (legacy clients).
        /// When provided (including empty), rows for the current application are replaced.
        /// </summary>
        public List<RoleMenuPermissionView> RoleMenuPermission { get; set; }
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

    public class RoleMenuPermissionView
    {
        public string MenuKey { get; set; }
        public long ModuleId { get; set; }
        public string ModuleName { get; set; }
        public string Label { get; set; }
        public string Route { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanAuthorize { get; set; }
        public bool CanReject { get; set; }
        public bool CanDelete { get; set; }
        public bool CanView { get; set; }
        public int ApplicationId { get; set; }
    }
}
