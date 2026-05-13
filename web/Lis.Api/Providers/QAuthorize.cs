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
//using System.Web.Script.Serialization;

namespace Lis.Api.Providers
{
    [AttributeUsageAttribute(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]

    public class QAuthorizeAttribute : AuthorizationFilterAttribute
    {
        public string ModuleName { get; set; }

        public ModulePermissionType ModulePermissionTypes { get; set; }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            base.OnAuthorization(actionContext);

            //TODO

            if (HttpContext.Current.User.IsInRole("Administrator"))
            {
                return;
            }

            var identity = (ClaimsIdentity)HttpContext.Current.User.Identity;


            var modulePermisionsClaim = identity.Claims.FirstOrDefault(p => p.Type.Equals("modulePermisions", StringComparison.OrdinalIgnoreCase));

            if (modulePermisionsClaim == null)
            {
                InvalidResponse(actionContext);
                return;
            }

            var rolePermission = JsonConvert.DeserializeObject<List<ModulePermission>>(modulePermisionsClaim.Value,
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                });

            
            if (rolePermission == null)
            {
                InvalidResponse(actionContext);
                return;
            }

            var modulePermisions = rolePermission.Where(p => p.Name.Equals(ModuleName, StringComparison.OrdinalIgnoreCase));

            if (modulePermisions == null)
            {
                InvalidResponse(actionContext);
                return;
            }

            var modulePermision = modulePermisions.GroupBy(item => new { item.Id, item.Name, item.Url })
                .Select(group => new
                {
                    Id = group.Key.Id,
                    Name = group.Key.Name,
                    Url = group.Key.Url,
                    Access = group.Aggregate(0, (acc, curr) => acc | curr.Access)
                })
                .First();

            bool isAuthorised = (((int)this.ModulePermissionTypes & modulePermision.Access) != 0);

            if (!isAuthorised)
            {
                InvalidResponse(actionContext);
            }
        }

        private void InvalidResponse(HttpActionContext actionContext)
        {
            var message = "Insufficient privilege.";

            actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized, message);
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
}