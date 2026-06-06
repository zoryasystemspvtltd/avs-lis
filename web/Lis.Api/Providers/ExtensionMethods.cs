using Newtonsoft.Json;
using Lis.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
//using System.Web.Script.Serialization;

namespace Lis.Api
{
    public static class ExtensionMethods
    {
        public static string GetModulePermission(this ApplicationUser user, Models.IdentityDbContext dbContext, string ClientId)
        {
            var modulePermisions = "";

            var roleIds = ((ApplicationUser)user).Roles.Select(p => p.RoleId.ToLower());

            if (roleIds != null)
            {
                var adminrole = dbContext.Roles.FirstOrDefault(p => p.Name.Equals("Administrator", StringComparison.OrdinalIgnoreCase));

                if (roleIds.Contains(adminrole.Id))
                {
                    var roleModuleMappings = dbContext.Modules
                        .Where(p => p.ApplicationId == 1) // Application 1 is the base 
                        .Select(p => new
                        {
                            id = p.Id,
                            name = p.Name,
                            url = p.Url,
                            access = 63
                        })
                        .ToList();

                    if (roleModuleMappings != null)
                    {
                        modulePermisions = JsonConvert.SerializeObject(roleModuleMappings,
                            Formatting.None,
                            new JsonSerializerSettings()
                            {
                                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                            });
                    }
                }
                else
                {
                    // Key is case sensitive
                    var applicationId = dbContext.ClientApplications.First(p => p.AccessKey.Equals(ClientId)).Id;

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


                    if (roleModuleMappings != null)
                    {
                        modulePermisions = JsonConvert.SerializeObject(roleModuleMappings,
                            Formatting.None,
                            new JsonSerializerSettings()
                            {
                                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                            });
                    }
                }

            }



            return modulePermisions;
        }


    }
}