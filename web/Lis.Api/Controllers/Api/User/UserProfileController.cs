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
using Lis.Api.Models;

namespace Lis.Api.Controllers
{


    public class UserProfileController : ApiController
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
        public UserProfileController(ApplicationUserManager userManager, Models.IdentityDbContext dbContext)
        {
            this.dbContext = dbContext;
            this.userManager = userManager;
        }

        // GET: api/UserProfile
        public dynamic Get()
        {
            var id = User.Identity.GetUserId();
            var user = userManager.FindById(id);
            var result = new
            {
                id = user.Id,
                email = user.Email,
                first_name = user.FirstName,
                last_name = user.LastName,
                locked = user.LockoutEnabled,
                locked_end = user.LockoutEndDateUtc,
                email_confirmed = user.EmailConfirmed,
                state = user.State,
                country = user.Country,
                phone_number = user.PhoneNumber,
                status = user.IsBlocked ? 1 : 2,
                alternative_email = user.AlternativeEmail,
                dob = user.DOB,
                area_of_interest = user.AreaOfInterest,
                qualification = user.Qualification,
                address = user.Address,
                zip = user.Zip,
                roles = new List<dynamic>(),
                applications = new List<dynamic>()
            };

            if (User.IsInRole("Administrator"))
            {
                foreach (var role in dbContext.Roles.Where(p => !p.Name.Equals("Administrator", StringComparison.OrdinalIgnoreCase)))
                {
                    result.roles.Add(new
                    {
                        id = role.Id,
                        name = role.Name,
                        IsInRole = true
                    });
                }

                foreach (var app in dbContext.ClientApplications)
                {
                    result.applications.Add(new
                    {
                        Key = app.AccessKey,
                        name = app.Name,
                        IsInApp = true
                    });
                }
            }
            else
            {

                foreach (var role in dbContext.Roles.Where(p => !p.Name.Equals("Administrator", StringComparison.OrdinalIgnoreCase)))
                {
                    result.roles.Add(new
                    {
                        id = role.Id,
                        name = role.Name,
                        IsInRole = userManager.IsInRole(user.Id, role.Name)
                    });
                }

                //result.roles.RemoveAll(p => !p.IsInRole);

                var ApplicationId = dbContext.ClientApplications.First(p => p.AccessKey.Equals(AccessKey)).Id;

                var userApps = dbContext.UserApplicationMappings
                    .Where(p => p.UserId.Equals(user.Id, StringComparison.OrdinalIgnoreCase)
                        && p.ClientApplicationId == ApplicationId)
                    .Select(q => q.ClientApplicationId)
                    .ToList();

                foreach (var app in dbContext.ClientApplications)
                {
                    result.applications.Add(new
                    {
                        Key = app.AccessKey,
                        name = app.Name,
                        IsInApp = userApps.Contains(app.Id)
                    });
                }

                result.applications.RemoveAll(p => !p.IsInApp);
            }

            return result;
        }

        public List<dynamic> Get(string id)
        {

            var per = dbContext.Modules
                    .Include("Roles")
                   .Where(p => p.Url.Equals(id, StringComparison.OrdinalIgnoreCase))
                   .FirstOrDefault();
            if(per == null)
            {
                return null;
            }

            var roles = per.Roles
                //.Where(p => p.Permissions)
                .Select(p => p.RoleId.ToString())
                .ToList();

            var rusers = userManager.Users
                .Where(p => p.Roles.Any(ur => roles.Any(r => r.Equals(ur.RoleId, StringComparison.OrdinalIgnoreCase))))
                .ToList();

            var users = new List<dynamic>();

            foreach (var user in rusers)
            {
                users.Add(new
                {
                    id = user.Id,
                    name = string.Format("{0} {1}", user.FirstName, user.LastName)
                });
            }
            
            return users;
        }
    }
}
