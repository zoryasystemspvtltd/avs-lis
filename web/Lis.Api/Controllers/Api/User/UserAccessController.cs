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
using System.Data.SqlClient;
using Microsoft.Owin.Security;
using Lis.Api.Models;

namespace Lis.Api.Controllers
{
    [Authorize]
    public class UserAccessController : ApiController
    {
        private Models.IdentityDbContext dbContext;

        public UserAccessController(Models.IdentityDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Get List of Module permission
        /// </summary>
        /// <param name="id">Client Id</param>
        /// <returns></returns>
        public string Get(string id)
        {
            var userid = User.Identity.GetUserId();
            var user = dbContext.Users.FirstOrDefault(p => p.Id.Equals(userid, StringComparison.OrdinalIgnoreCase));
            return user.GetModulePermission(this.dbContext,id);
        }

        /// <summary>
        /// Get List of Client Application
        /// </summary>
        /// <returns></returns>
        public List<ClientApplication> Get()
        {
            if (User.IsInRole("Administrator"))
            {
                var allapplications = dbContext.ClientApplications
                .ToList();

                return allapplications;
            }
            var userid = User.Identity.GetUserId();
            var applications = dbContext.UserApplicationMappings
                .Include("ClientApplication")
                .Where(p => p.UserId.Equals(userid, StringComparison.OrdinalIgnoreCase))
                .Select(q=>q.ClientApplication)
                .ToList();

            return applications;
        }
    }
}
