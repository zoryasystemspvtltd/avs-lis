using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using Microsoft.Owin;
using Microsoft.AspNet.Identity.Owin;
using System.Threading.Tasks;
using System.Globalization;
using System.Web.Security;
using System.Web.Hosting;
using Lis.Api.Models;
using Lis.Api;
using Lis.Api.Providers;

namespace QuestionsForU.Authentication.Controllers
{
    public class UsersController : ApiController
    {
        private const string DoctorRoleName = "Doctor";
        private Lis.Api.Models.IdentityDbContext dbContext;

        private ApplicationUserManager userManager;



        public UsersController(ApplicationUserManager userManager, Lis.Api.Models.IdentityDbContext dbContext)
        {
            this.dbContext = dbContext;
            this.userManager = userManager;
        }


        /// <summary>
        /// Active users for dropdown lookups (id + display name).
        /// </summary>
        [HttpGet]
        [Route("~/api/Users/Lookup")]
        [QAuthorize(ModuleName = "Reports", ModulePermissionTypes = ModulePermissionType.CanView)]
        public IHttpActionResult GetLookup()
        {
            var users = userManager.Users
                .Where(u => !u.IsBlocked)
                .AsEnumerable()
                .Select(u => new
                {
                    id = u.Id,
                    name = BuildUserDisplayName(u)
                })
                .Where(u => !string.IsNullOrWhiteSpace(u.name))
                .OrderBy(u => u.name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Ok(users);
        }

        private static string BuildUserDisplayName(ApplicationUser user)
        {
            if (user == null)
            {
                return string.Empty;
            }

            var fullName = string.Join(" ",
                new[] { user.FirstName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                return fullName;
            }

            return user.Email ?? user.UserName ?? string.Empty;
        }

        /// <summary>
        /// Get all User list
        /// </summary>
        /// <returns></returns>

        //TODO Authorization
        public dynamic Get()
        {
            dynamic users = new
            {
                items = new List<dynamic>()
            };

            foreach (var user in userManager.Users.Where(u => u.Email != "admin@zorya.co.in"))
            {
                users.items.Add(new
                {
                    id = user.Id,
                    email = user.Email,
                    first_name = user.FirstName,
                    last_name = user.LastName,
                    phone_number = user.PhoneNumber,
                    status = user.IsBlocked ? 1 : 2,
                    role = getRoles(user.Roles)
                });
            }
            return users;
        }

        private object getRoles(ICollection<IdentityUserRole> roles)
        {
            string role = "";
            foreach (var item in roles)
            {
                var roledetails = dbContext.Roles.Where(p => p.Id == item.RoleId).FirstOrDefault();

                if (role.Length > 0)
                {
                    role += ", " + roledetails.Name;
                }
                else
                {
                    role = roledetails.Name;
                }
            }
            return role;
        }


        /// <summary>
        /// Get user details
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        //[Authorize(Roles = "SuperAdmin")]
        //TODO Authorization
        public dynamic Get(string id)
        {
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
                doctor_designation = user.DoctorDesignation,
                doctor_signature_path = user.DoctorSignaturePath,
                address = user.Address,
                zip = user.Zip,
                roles = new List<dynamic>(),
                applications = new List<dynamic>()
            };

            foreach (var role in dbContext.Roles.Where(p => !p.Name.Equals("Administrator", StringComparison.OrdinalIgnoreCase)))
            {
                result.roles.Add(new
                {
                    id = role.Id,
                    name = role.Name,
                    IsInRole = userManager.IsInRole(user.Id, role.Name)
                });
            }

            var userApps = dbContext.UserApplicationMappings
                .Where(p => p.UserId.Equals(user.Id, StringComparison.OrdinalIgnoreCase))
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

            return result;
        }

        /// <summary>
        /// Add new User
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public async Task<dynamic> Post(User value)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateResponse(HttpStatusCode.PreconditionFailed, ModelState.ToKeyValuePair());
            }

            var doctorValidationError = ValidateDoctorFields(value, null, true);
            if (!string.IsNullOrWhiteSpace(doctorValidationError))
            {
                return Request.CreateResponse(HttpStatusCode.PreconditionFailed, new
                {
                    Status = false,
                    Message = doctorValidationError
                });
            }

            var user = new ApplicationUser();
            user.UserName = value.email;
            user.FirstName = value.first_name;
            user.LastName = value.last_name;
            user.Email = value.email;
            user.PhoneNumber = value.phone_number;
            user.IsBlocked = value.is_blocked;
            user.EmailConfirmed = false;
            user.AlternativeEmail = value.alternative_email;
            user.DOB = value.dob;
            user.AreaOfInterest = value.area_of_interest;
            user.Qualification = value.qualification;
            user.Country = value.country;
            user.State = value.state;
            user.Address = value.address;
            user.Zip = value.zip;
            ApplyDoctorFields(user, value);

            // Providing default password for the newly created user with EmailConfirmed = false
            // Depending on the condition EmailConfirmed = false, user will be prompted to change password.
            var password = "Admin@123";//GeneratePassword(12);

            var result = userManager.Create(user, password);
            value.isValid = result.Succeeded;

            if (!result.Succeeded)
            {
                var message = string.Empty;

                foreach (var fault in result.Errors)
                {
                    if (!fault.StartsWith("Name"))
                    {
                        var faultmessage = string.Format(CultureInfo.InvariantCulture, "{0}", fault);
                        message = message + faultmessage + "<br />";
                    }
                }
                var response = new
                {
                    Status = false,
                    Message = message
                };

                return Request.CreateResponse(HttpStatusCode.PreconditionFailed, response);
            }

            //-- insert user  roles -------------------------------------------------------  
            if (value.roles != null)
            {
                foreach (var role in value.roles)
                {
                    if (role.IsInRole)
                    {
                        user.Roles.Add(new IdentityUserRole()
                        {
                            RoleId = role.Id,
                            UserId = user.Id
                        });
                    }
                }
                var result_2 = userManager.Update(user);

                //-- End ------------------------------------------------------------------------
            }


            // -- insert application------------------------------------------------------------
            if (value.applications != null)
            {
                var selectedAppKeys = value.applications
                    .Where(p => p.IsInApp)
                    .Select(q => q.Key)
                    .ToList();

                var selecteedApp = dbContext.ClientApplications
                    .Where(p => selectedAppKeys.Contains(p.AccessKey))
                    .ToList();


                foreach (var app in selecteedApp)
                {

                    dbContext.UserApplicationMappings.Add(new UserApplicationMapping()
                    {
                        UserId = user.Id,
                        ClientApplicationId = app.Id
                    });
                }


                dbContext.SaveChanges();
            }

            return new
            {
                id = user.Id,
                email = user.Email,
                first_name = user.FirstName,
                last_name = user.LastName,
                phone_number = user.PhoneNumber,
                doctor_designation = user.DoctorDesignation,
                doctor_signature_path = user.DoctorSignaturePath,
                roles = value.roles
            };
        }

        private async Task SendNewUserEmail(ClientApplication app, User value, string password)
        {
            //Send Email after new Registration ---------------------------------------------
            try
            {
                EmailHandler mail = new EmailHandler();
                //Client Mail
                var path = HostingEnvironment.MapPath(@"~/App_Data/EmailTemplate/NewUserRegistration.txt");
                var template = await mail.ReadTemplate(path);
                template = template.Replace("#APP#", app.Name);
                template = template.Replace("#UserFullName#", value.first_name + " " + value.last_name);
                template = template.Replace("#Username#", value.email);

                mail.Body = template;
                mail.Subject = string.Format("{0} - Welcome to {0} Portal", app.Name);
                mail.ToAddress = value.email;
                await mail.Send();

                mail = new EmailHandler();
                path = HostingEnvironment.MapPath(@"~/App_Data/EmailTemplate/NewUserPassword.txt");
                template = await mail.ReadTemplate(path);
                template = template.Replace("#APP#", app.Name);
                template = template.Replace("#UserFullName#", value.first_name + " " + value.last_name);
                template = template.Replace("#Password#", password);
                var portalLoginUrl = string.Format("{0}/login/#!/login", app.AllowedOrigin);
                template = template.Replace("#PORTAL_LOGIN#", portalLoginUrl);

                mail.Body = template;
                mail.Subject = string.Format("{0} - Credentials for {0} Portal", app.Name);
                mail.ToAddress = value.email;
                await mail.Send();
            }
            catch
            {
                // Do nothing
            }
            // End of Email Send -------------------------------------------------

        }

        /// <summary>
        /// Update User
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [QAuthorize(ModuleName = "Users"
        , ModulePermissionTypes = ModulePermissionType.CanEdit

        )]
        public dynamic Put(string id, User value)
        {
            ApplicationUser update = userManager.FindById(id);
            if (update == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            var doctorValidationError = ValidateDoctorFields(value, update, false);
            if (!string.IsNullOrWhiteSpace(doctorValidationError))
            {
                return Request.CreateResponse(HttpStatusCode.PreconditionFailed, new
                {
                    Status = false,
                    Message = doctorValidationError
                });
            }

            update.FirstName = value.first_name;
            update.LastName = value.last_name;
            update.Email = value.email;
            update.LockoutEnabled = value.locked;
            update.EmailConfirmed = value.email_confirmed;
            update.UserName = value.email;
            update.PhoneNumber = value.phone_number;
            update.IsBlocked = value.is_blocked;
            update.AlternativeEmail = value.alternative_email;
            update.DOB = value.dob;
            update.AreaOfInterest = value.area_of_interest;
            update.Qualification = value.qualification;
            update.Country = value.country;
            update.State = value.state;
            update.Address = value.address;
            update.Zip = value.zip;
            ApplyDoctorFields(update, value);

            update.Roles.Clear();
            foreach (var role in value.roles)
            {
                if (role.IsInRole)
                {
                    update.Roles.Add(new IdentityUserRole()
                    {
                        RoleId = role.Id,
                        UserId = update.Id
                    });
                }
            }

            var result = userManager.Update(update);

            // -- insert application------------------------------------------------------------
            if (value.applications != null)
            {
                var maps = dbContext.UserApplicationMappings.Where(p => p.UserId.Equals(id, StringComparison.OrdinalIgnoreCase));
                dbContext.UserApplicationMappings.RemoveRange(maps);
                dbContext.SaveChanges();

                var selectedAppKeys = value.applications
                    .Where(p => p.IsInApp)
                    .Select(q => q.Key)
                    .ToList();

                var selecteedApp = dbContext.ClientApplications
                    .Where(p => selectedAppKeys.Contains(p.AccessKey))
                    .ToList();


                foreach (var app in selecteedApp)
                {
                    dbContext.UserApplicationMappings.Add(new UserApplicationMapping()
                    {
                        UserId = update.Id,
                        ClientApplicationId = app.Id
                    });
                }

                dbContext.SaveChanges();
            }

            if (!result.Succeeded)
            {
                return Request.CreateResponse(HttpStatusCode.PreconditionFailed, ModelState.ToKeyValuePair());
            }

            if (result.Succeeded)
            {

                return new
                {
                    id = update.Id,
                    email = update.Email,
                    first_name = update.FirstName,
                    last_name = update.LastName,
                    locked = update.LockoutEnabled,
                    email_confirmed = update.EmailConfirmed,
                    doctor_designation = update.DoctorDesignation,
                    doctor_signature_path = update.DoctorSignaturePath
                };
            }
            else
            {
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }

        }

        /// <summary>
        /// Delete User
        /// </summary>
        /// <param name="id"></param>
        [QAuthorize(ModuleName = "Users"
        , ModulePermissionTypes = ModulePermissionType.CanDelete
        )]
        public void Delete(string id)
        {
            ApplicationUser user = userManager.FindById(id);
            if (user == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
            var maps = dbContext.UserApplicationMappings.Where(p => p.UserId.Equals(id, StringComparison.OrdinalIgnoreCase));
            dbContext.UserApplicationMappings.RemoveRange(maps);
            dbContext.SaveChanges();

            DoctorSignatureStorage.DeletePhysicalFile(user.DoctorSignaturePath);
            userManager.Delete(user);
        }

        /// <summary>
        /// Returns the currently logged-in user's digital signature (inline image),
        /// name and designation so approval screens can show it read-only.
        /// </summary>
        [HttpGet]
        [Route("~/api/Users/CurrentDoctorSignature")]
        public IHttpActionResult GetCurrentDoctorSignature()
        {
            var userId = User?.Identity?.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var user = userManager.FindById(userId);
            if (user == null)
            {
                return NotFound();
            }

            var name = string.Format("{0} {1}", user.FirstName, user.LastName).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                name = user.UserName;
            }

            var signatureDataUri = DoctorSignatureStorage.GetSignatureDataUri(user.DoctorSignaturePath);

            return Ok(new
            {
                name = name,
                designation = user.DoctorDesignation,
                signature_data_uri = signatureDataUri,
                has_signature = !string.IsNullOrWhiteSpace(signatureDataUri)
            });
        }

        [HttpPost]
        [Route("~/api/Users/{id}/DoctorSignature")]
        [QAuthorize(ModuleName = "Users", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        public IHttpActionResult PostDoctorSignature(string id)
        {
            var user = userManager.FindById(id);
            if (user == null)
            {
                return NotFound();
            }

            if (!userManager.IsInRole(user.Id, DoctorRoleName))
            {
                return Content(HttpStatusCode.PreconditionFailed, new
                {
                    Status = false,
                    Message = "Doctor signature can only be uploaded for Doctor users."
                });
            }

            var file = HttpContext.Current?.Request?.Files?["file"];
            if (file == null && HttpContext.Current?.Request?.Files?.Count > 0)
            {
                file = HttpContext.Current.Request.Files[0];
            }

            string validationError;
            if (!DoctorSignatureStorage.TryValidate(file, out validationError))
            {
                return Content(HttpStatusCode.PreconditionFailed, new
                {
                    Status = false,
                    Message = validationError
                });
            }

            try
            {
                var relativePath = DoctorSignatureStorage.SaveSignature(user.Id, file, user.DoctorSignaturePath);
                user.DoctorSignaturePath = relativePath;
                var result = userManager.Update(user);
                if (!result.Succeeded)
                {
                    DoctorSignatureStorage.DeletePhysicalFile(relativePath);
                    return Content(HttpStatusCode.PreconditionFailed, new
                    {
                        Status = false,
                        Message = string.Join(" ", result.Errors)
                    });
                }

                return Ok(new
                {
                    doctor_signature_path = user.DoctorSignaturePath
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    Status = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("~/api/Users/{id}/DoctorSignature")]
        [QAuthorize(ModuleName = "Users", ModulePermissionTypes = ModulePermissionType.CanView)]
        public IHttpActionResult GetDoctorSignature(string id)
        {
            var user = userManager.FindById(id);
            if (user == null || string.IsNullOrWhiteSpace(user.DoctorSignaturePath))
            {
                return NotFound();
            }

            var physicalPath = DoctorSignatureStorage.ResolvePhysicalPath(user.DoctorSignaturePath);
            if (physicalPath == null)
            {
                return NotFound();
            }

            var extension = Path.GetExtension(physicalPath).ToLowerInvariant();
            var contentType = extension == ".png" ? "image/png" : "image/jpeg";
            var stream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream)
            };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            return ResponseMessage(response);
        }

        private bool IsDoctorRoleSelected(User value)
        {
            if (value?.roles == null)
            {
                return false;
            }

            foreach (var role in value.roles.Where(r => r.IsInRole))
            {
                var roleEntity = dbContext.Roles.FirstOrDefault(p => p.Id == role.Id);
                if (roleEntity != null
                    && roleEntity.Name.Equals(DoctorRoleName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(role.Name)
                    && role.Name.Equals(DoctorRoleName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private string ValidateDoctorFields(User value, ApplicationUser existingUser, bool isCreate)
        {
            var isDoctor = IsDoctorRoleSelected(value);
            if (!isDoctor)
            {
                return null;
            }

            var designation = value.doctor_designation == null
                ? null
                : value.doctor_designation.Trim();
            if (string.IsNullOrWhiteSpace(designation))
            {
                return "Doctor designation is required for Doctor users.";
            }

            if (designation.Length > 100)
            {
                return "Doctor designation must not exceed 100 characters.";
            }

            return null;
        }

        private void ApplyDoctorFields(ApplicationUser user, User value)
        {
            if (IsDoctorRoleSelected(value))
            {
                user.DoctorDesignation = value.doctor_designation == null
                    ? null
                    : value.doctor_designation.Trim();
                return;
            }

            DoctorSignatureStorage.DeletePhysicalFile(user.DoctorSignaturePath);
            user.DoctorDesignation = null;
            user.DoctorSignaturePath = null;
        }

        private string GetRandomText(string text)
        {
            Random rng = new Random();
            int index = rng.Next(text.Length);
            return text[index].ToString();
        }

        private string GeneratePassword(int length)
        {
            if (length < 4)
            {
                throw new Exception("Minimum string length is 4");
            }

            var password = Membership.GeneratePassword((length - 3), 1);
            password = GetRandomText("QuestionsForU") + password + password.IndexOfAny("QuestionsForU".ToCharArray()).ToString() + GetRandomText("QuestionsForU");
            return password;
        }
    }

    public class User
    {
        public string id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        public bool is_blocked { get; set; }
        public string phone_number { get; set; }

        public string alternative_email { get; set; }
        public DateTime? dob { get; set; }
        public string area_of_interest { get; set; }
        public string qualification { get; set; }
        public string doctor_designation { get; set; }
        public string doctor_signature_path { get; set; }
        public string country { get; set; }
        public string state { get; set; }
        public string address { get; set; }
        public string zip { get; set; }
        public bool isSubscriberInly { get; set; }

        public bool locked { get; set; }
        public bool email_confirmed { get; set; }
        public string password { get; set; }
        public bool isValid { get; set; }
        public string error_Message { get; set; }

        public List<Role> roles { get; set; }

        public List<App> applications { get; set; }

    }


    public class Role
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public bool IsInRole { get; set; }
    }

    public class App
    {
        public string Key { get; set; }

        public string Name { get; set; }

        public bool IsInApp { get; set; }
    }
}
