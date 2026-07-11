using Newtonsoft.Json;
using Lis.Api.Models;
using Lis.Api.Providers;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace Lis.Api
{
    public static class ExtensionMethods
    {
        public static string GetModulePermission(this ApplicationUser user, Models.IdentityDbContext dbContext, string ClientId)
        {
            try
            {
                if (user == null || dbContext == null || string.IsNullOrWhiteSpace(ClientId))
                {
                    return "[]";
                }

                var modulePermisions = "[]";

                var roleIds = user.Roles.Select(p => p.RoleId.ToLower()).ToList();
                if (roleIds == null || !roleIds.Any())
                {
                    return "[]";
                }

                var clientApp = dbContext.ClientApplications.FirstOrDefault(p => p.AccessKey.Equals(ClientId));
                if (clientApp == null)
                {
                    return "[]";
                }

                var applicationId = clientApp.Id;
                var adminrole = dbContext.Roles.FirstOrDefault(p => p.Name.Equals("Administrator", StringComparison.OrdinalIgnoreCase));

                if (adminrole != null && roleIds.Contains(adminrole.Id.ToLower()))
                {
                    var roleModuleMappings = dbContext.Modules
                        .Where(p => p.ApplicationId == applicationId)
                        .Select(p => new
                        {
                            id = p.Id,
                            name = p.Name,
                            url = p.Url,
                            access = 63
                        })
                        .ToList();

                    modulePermisions = JsonConvert.SerializeObject(roleModuleMappings,
                        Formatting.None,
                        new JsonSerializerSettings()
                        {
                            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                        });
                }
                else
                {
                    var tempAccess = dbContext.RoleModuleMappings
                       .Where(p => roleIds.Contains(p.RoleId.ToLower())
                        && p.ApplicationId == applicationId)
                       .Select(p => new
                       {
                           id = p.ModuleId,
                           name = p.Module.Name,
                           url = p.Module.Url,
                           access = (p.CanAdd ? 1 : 0) + (p.CanEdit ? 2 : 0) + (p.CanAuthorize ? 4 : 0) + (p.CanReject ? 8 : 0) + (p.CanDelete ? 16 : 0) + (p.CanView ? 32 : 0)
                       })
                       .ToList();

                    var roleModuleMappings = tempAccess
                    .GroupBy(item => new { item.id, item.name, item.url })
                    .Select(group => new
                    {
                        id = group.Key.id,
                        name = group.Key.name,
                        url = group.Key.url,
                        access = group.Aggregate(0, (acc, curr) => acc | curr.access)
                    })
                    .ToList();

                    modulePermisions = JsonConvert.SerializeObject(roleModuleMappings,
                        Formatting.None,
                        new JsonSerializerSettings()
                        {
                            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                        });
                }

                return modulePermisions;
            }
            catch
            {
                return "[]";
            }
        }

        /// <summary>
        /// Returns active menu-level permissions for the user/client.
        /// Empty array means no menu overlay — callers must fall back to module permissions.
        /// Administrator returns empty array (full bypass handled elsewhere).
        /// Never throws — login/token must not depend on menu rows.
        /// </summary>
        public static string GetMenuPermission(this ApplicationUser user, Models.IdentityDbContext dbContext, string ClientId)
        {
            try
            {
                if (user == null || dbContext == null || string.IsNullOrWhiteSpace(ClientId))
                {
                    return "[]";
                }

                var roleIds = user.Roles.Select(p => p.RoleId.ToLower()).ToList();
                if (roleIds == null || !roleIds.Any())
                {
                    return "[]";
                }

                var adminrole = dbContext.Roles.FirstOrDefault(p => p.Name.Equals("Administrator", StringComparison.OrdinalIgnoreCase));
                if (adminrole != null && roleIds.Contains(adminrole.Id.ToLower()))
                {
                    return "[]";
                }

                var clientApp = dbContext.ClientApplications.FirstOrDefault(p => p.AccessKey.Equals(ClientId));
                if (clientApp == null)
                {
                    return "[]";
                }

                var applicationId = clientApp.Id;

                // Materialize first — avoid EF translation of RoleId.ToLower() inside Contains (can fail → "[]").
                var rawRows = dbContext.RoleMenuPermissions
                    .AsNoTracking()
                    .Where(p => p.ApplicationId == applicationId && p.IsActive)
                    .Select(p => new
                    {
                        p.MenuKey,
                        p.ModuleId,
                        p.RoleId,
                        p.CanAdd,
                        p.CanEdit,
                        p.CanAuthorize,
                        p.CanReject,
                        p.CanDelete,
                        p.CanView
                    })
                    .ToList()
                    .Where(p => p.RoleId != null && roleIds.Contains(p.RoleId.ToLower()))
                    .ToList();

                var moduleIds = rawRows.Select(r => r.ModuleId).Distinct().ToList();
                var moduleNames = dbContext.Modules
                    .AsNoTracking()
                    .Where(m => moduleIds.Contains(m.Id))
                    .Select(m => new { m.Id, m.Name })
                    .ToList()
                    .ToDictionary(m => m.Id, m => m.Name);

                var rows = rawRows.Select(p => new
                {
                    menuKey = p.MenuKey,
                    moduleId = p.ModuleId,
                    moduleName = moduleNames.ContainsKey(p.ModuleId) ? moduleNames[p.ModuleId] : "",
                    access = (p.CanAdd ? 1 : 0) + (p.CanEdit ? 2 : 0) + (p.CanAuthorize ? 4 : 0)
                        + (p.CanReject ? 8 : 0) + (p.CanDelete ? 16 : 0) + (p.CanView ? 32 : 0)
                }).ToList();

                var merged = rows
                    .GroupBy(item => new { menuKey = MenuCatalog.NormalizeKey(item.menuKey), item.moduleId, item.moduleName })
                    .Select(group => new
                    {
                        menuKey = group.Key.menuKey,
                        moduleId = group.Key.moduleId,
                        moduleName = group.Key.moduleName,
                        access = group.Aggregate(0, (acc, curr) => acc | curr.access)
                    })
                    .ToList();

                return JsonConvert.SerializeObject(merged,
                    Formatting.None,
                    new JsonSerializerSettings()
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });
            }
            catch
            {
                return "[]";
            }
        }
    }
}
