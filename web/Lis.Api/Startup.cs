using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Owin;
using Owin;
using System.Web.Mvc;
using System.Web.Http;
using System.Web.Routing;
using Lis.Api.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Configuration;
using SimpleInjector;
using LIS.Logger;
using LIS.DtoModel.Interfaces;

[assembly: OwinStartup(typeof(Lis.Api.Startup))]

namespace Lis.Api
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {

            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            //RouteTable.Routes.MapHubs();
            app.MapSignalR("/lis", new Microsoft.AspNet.SignalR.HubConfiguration());
            var container = SimpleInjectorInitializer.Initialize(app);
            ConfigureAuth(app, container);

            var logger = container.GetInstance<ILogger>();
            GlobalScheduler.StartScheduler(logger);

#if DEBUG
            CreateRolesandUsers(container);
#endif
        }

        private void CreateRolesandUsers(Container container)
        {

            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(container.GetInstance<Models.IdentityDbContext>()));
            var UserManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(container.GetInstance<Models.IdentityDbContext>()));


            // In Startup iam creating first Admin Role and creating a default Admin User     
            if (!roleManager.RoleExists("Administrator"))
            {


                // Administrator is not editable 
                var roleAdmin = new IdentityRole();
                roleAdmin.Name = "Administrator";
                roleManager.Create(roleAdmin);

                // first we create Technician role    
                var roleTech = new IdentityRole();
                roleTech.Name = "Technician";
                roleManager.Create(roleTech);

                var roleDoct = new IdentityRole();
                roleDoct.Name = "Doctor";
                roleManager.Create(roleDoct);

                //Here we create a Admin super user who will maintain the website                   

                var superuser = new ApplicationUser()
                {
                    FirstName = "Administrator",
                    LastName = "LIS",
                    Email = "admin@zorya.co.in",
                    UserName = "admin@zorya.co.in",
                    PhoneNumber = "0000000000",
                    IsBlocked = false,
                    EmailConfirmed = true
                };

                string userPWD = "zorKol@1";

                var chkUser = UserManager.Create(superuser, userPWD);

                //Add default User to Role Admin    
                if (chkUser.Succeeded)
                {
                    var result0 = UserManager.AddToRole(superuser.Id, "Administrator");
                    var result1 = UserManager.AddToRole(superuser.Id, "Technician");
                    var result2 = UserManager.AddToRole(superuser.Id, "Doctor");
                }

                var dbContext = container.GetInstance<Models.IdentityDbContext>();
                var clientAdmin = new ClientApplication()
                {
                    Name = "LIS",
                    Description = "LIS Server",
                    AccessKey = "DUMMY",
                    //Status = 0,
                    AllowedOrigin = "https://localhost:44392/"
                };
                dbContext.ClientApplications.Add(clientAdmin);

                // Permission

                var perApp = new UserModule()
                {
                    Name = "Applications",
                    Url = "/client-application",
                    Order = 1,
                    Application = clientAdmin,
                    IsSyatem = true
                };

                dbContext.Modules.Add(perApp);

                var perRole = new UserModule()
                {
                    Name = "Roles",
                    Url = "/roles",
                    Order = 2,
                    Application = clientAdmin,
                    IsSyatem = true
                };

                dbContext.Modules.Add(perRole);

                var perUser = new UserModule()
                {
                    Name = "Users",
                    Url = "/users",
                    Order = 3,
                    Application = clientAdmin,
                    IsSyatem = true
                };

                dbContext.Modules.Add(perUser);

                var perReport = new UserModule()
                {
                    Name = "Reports",
                    Url = "/reports",
                    Order = 4,
                    Application = clientAdmin,
                    IsSyatem = false
                };

                dbContext.Modules.Add(perReport);

                var perEquipment = new UserModule()
                {
                    Name = "Equipments",
                    Url = "/equipments",
                    Order = 5,
                    Application = clientAdmin,
                    IsSyatem = false
                };

                dbContext.Modules.Add(perEquipment);

                var perPatients = new UserModule()
                {
                    Name = "Samples",
                    Url = "/samples",
                    Order = 6,
                    Application = clientAdmin,
                    IsSyatem = false
                };

                dbContext.Modules.Add(perPatients);

                var perDocApproval = new UserModule()
                {
                    Name = "DoctorsApprovals",
                    Url = "/doctorapprovals",
                    Order = 7,
                    Application = clientAdmin,
                    IsSyatem = false
                };

                dbContext.Modules.Add(perDocApproval);

                dbContext.SaveChanges();
            }


        }
    }
}
