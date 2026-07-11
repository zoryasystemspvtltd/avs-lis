using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using Lis.Api.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Lis.Api.Providers
{
    [AttributeUsageAttribute(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]

    public class QAuthorizeAttribute : AuthorizationFilterAttribute
    {
        public string ModuleName { get; set; }

        /// <summary>Optional second module — user is authorized if either module grants the required permission bits.</summary>
        public string AlternateModuleName { get; set; }

        /// <summary>
        /// Optional menu key. When set and the role has menu rows for the module,
        /// menu-level bits are also required. When unset or no menu rows exist for the module,
        /// existing module-only authorization applies (backward compatible).
        /// </summary>
        public string MenuKey { get; set; }

        public ModulePermissionType ModulePermissionTypes { get; set; }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            base.OnAuthorization(actionContext);

            if (HttpContext.Current?.User?.Identity?.IsAuthenticated != true)
            {
                InvalidResponse(actionContext, false);
                return;
            }

            if (HttpContext.Current.User.IsInRole("Administrator"))
            {
                return;
            }

            var identity = HttpContext.Current.User.Identity as ClaimsIdentity;
            if (identity == null)
            {
                InvalidResponse(actionContext, false);
                return;
            }

            var modulePermisionsClaim = identity.Claims.FirstOrDefault(p => p.Type.Equals("modulePermisions", StringComparison.OrdinalIgnoreCase));

            if (modulePermisionsClaim == null)
            {
                InvalidResponse(actionContext, true);
                return;
            }

            var rolePermission = JsonConvert.DeserializeObject<List<ModulePermission>>(modulePermisionsClaim.Value,
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                });

            if (rolePermission == null)
            {
                InvalidResponse(actionContext, true);
                return;
            }

            if (IsAuthorizedForModule(rolePermission, ModuleName)
                || HasAlternateModuleAccess(rolePermission))
            {
                if (IsAuthorizedForOptionalMenu(identity))
                {
                    return;
                }
            }

            InvalidResponse(actionContext, true);
        }

        private bool HasAlternateModuleAccess(List<ModulePermission> rolePermission)
        {
            if (string.IsNullOrWhiteSpace(AlternateModuleName))
            {
                return false;
            }

            foreach (var moduleName in AlternateModuleName.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (IsAuthorizedForModule(rolePermission, moduleName.Trim()))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsAuthorizedForModule(List<ModulePermission> rolePermission, string moduleName)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                return false;
            }

            var modulePermisions = rolePermission
                .Where(p => p.Name.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!modulePermisions.Any())
            {
                return false;
            }

            var access = modulePermisions
                .GroupBy(item => new { item.Id, item.Name, item.Url })
                .Select(group => group.Aggregate(0, (acc, curr) => acc | curr.Access))
                .First();

            return (((int)ModulePermissionTypes & access) != 0);
        }

        /// <summary>
        /// Menu check is additive and optional. No MenuKey or no configured menu rows for the module → allow.
        /// </summary>
        private bool IsAuthorizedForOptionalMenu(ClaimsIdentity identity)
        {
            if (string.IsNullOrWhiteSpace(MenuKey))
            {
                return true;
            }

            var menuClaim = identity.Claims.FirstOrDefault(p => p.Type.Equals("menuPermissions", StringComparison.OrdinalIgnoreCase));
            if (menuClaim == null || string.IsNullOrWhiteSpace(menuClaim.Value) || menuClaim.Value == "[]")
            {
                return true;
            }

            List<MenuPermissionClaim> menus;
            try
            {
                menus = JsonConvert.DeserializeObject<List<MenuPermissionClaim>>(menuClaim.Value,
                    new JsonSerializerSettings()
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });
            }
            catch
            {
                return true;
            }

            if (menus == null || !menus.Any())
            {
                return true;
            }

            var moduleName = ModuleName;
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                var def = MenuCatalog.Find(MenuKey);
                moduleName = def?.ModuleName;
            }

            if (string.IsNullOrWhiteSpace(moduleName))
            {
                return true;
            }

            var moduleMenus = menus
                .Where(m => string.Equals(m.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // No menu overlay for this module → module-only behaviour.
            if (!moduleMenus.Any())
            {
                return true;
            }

            var normalizedKey = MenuCatalog.NormalizeKey(MenuKey);
            var menu = moduleMenus
                .Where(m => string.Equals(MenuCatalog.NormalizeKey(m.MenuKey), normalizedKey, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!menu.Any())
            {
                return false;
            }

            var access = menu.Aggregate(0, (acc, curr) => acc | curr.Access);
            return (((int)ModulePermissionTypes & access) != 0);
        }

        private void InvalidResponse(HttpActionContext actionContext, bool isAuthenticated)
        {
            var message = isAuthenticated ? "Insufficient privilege." : "Authentication required.";
            var status = isAuthenticated ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized;

            actionContext.Response = actionContext.Request.CreateResponse(status, message);
            actionContext.Response.ReasonPhrase = message;
        }
    }


    public enum ModulePermissionType
    {
        CanAdd = 1,
        CanEdit = 2,
        CanAuthorize = 4,
        CanReject = 8,
        CanDelete = 16,
        CanView = 32
    }

    public class ModulePermission
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public int Access { get; set; }

    }

    public class MenuPermissionClaim
    {
        public string MenuKey { get; set; }
        public long ModuleId { get; set; }
        public string ModuleName { get; set; }
        public int Access { get; set; }
    }
}
