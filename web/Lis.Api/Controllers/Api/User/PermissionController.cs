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
using System.ComponentModel.DataAnnotations;
using Lis.Api.Models;
using LIS.Logger;

namespace QuestionsForU.Authentication.Controllers
{
    [Authorize]
    //[Authorize(Roles = "Admin")]
    public class PermissionController : ApiController
    {
        private ApplicationDbContext dbContext;

        public PermissionController()
        {
            dbContext = new ApplicationDbContext();
        }

        public IEnumerable<dynamic> Get()
        {
            var permissions = new List<dynamic>();
            try
            {
                var permissionList = dbContext.Permissions.OrderBy(p => p.Order).ToList();
                foreach (var item in permissionList)
                {
                    var permission = new
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Url = item.Url,
                        IsActive = false
                    };
                    permissions.Add(permission);
                }

                return permissions;
            }
            catch (Exception ex)
            {
                Logger.LogInstance.LogException(ex);
                throw new Exception(ex.Message);
            }
        }

        // GET: api/Role/5
        public IEnumerable<dynamic> Get(int id)
        {
            try
            {
                var perModule = dbContext.ModulePermissions
                    .Where(p => p.PermissionId == id)
                    .Select(p => p.ModuleId)
                    .ToList();

                var modules = dbContext.Modules
                                 .Where(p => p.IsSystemModule == false)
                                 .Select(item => new
                                 {
                                     Id = item.Id,
                                     Name = item.Name,
                                     DisplayName = string.IsNullOrEmpty(item.DisplayName) ? item.Name : item.DisplayName,
                                     IsActive = perModule.Any(m => m == item.Id)
                                 })
                                 .ToList();

                return modules;
            }
            catch (Exception ex)
            {
                Logger.LogInstance.LogException(ex);
                throw new Exception(ex.Message);
            }
        }

        public void Post(PermissionWithModules item)
        {
            try
            {
                var perModule = dbContext.ModulePermissions
                   .Where(p => p.PermissionId == item.PermissionId)
                   .ToList();

                dbContext.ModulePermissions.RemoveRange(perModule);

                foreach (var mod in item.Modules.Where(p => p.IsActive))
                {
                    dbContext.ModulePermissions.Add(new Models.ModulePermission()
                    {
                        PermissionId = item.PermissionId,
                        ModuleId = mod.Id
                    });
                }

                dbContext.SaveChanges();

            }
            catch (Exception ex)
            {
                Logger.LogInstance.LogException(ex);
                throw new Exception(ex.Message);
            }
        }

    }
    public class PermissionWithModules
    {
        [Required]
        public long PermissionId { get; set; }
        public List<ModuleView> Modules { get; set; }

    }

    public class ModuleView
    {
        public long Id { get; set; }
        public bool IsActive { get; set; }
    }
}
