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
    public class ResetPasswordController : ApiController
    {
        private ApplicationUserManager userManager;

        public ResetPasswordController(ApplicationUserManager userManager)
        {
            this.userManager = userManager;
        }


        /// <summary>
        /// Reset user password Step 1, Send mail
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public async Task Post([FromBody]ResetPasswordModel item)
        {
            ApplicationUser user = await userManager.FindByEmailAsync(item.Email);
            if (user == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            string code = userManager.GeneratePasswordResetToken(user.Id);
            var callbackUrl = Url.Content(string.Format("~/login/#!/changepassword?email={0}&code={1}", user.Email, code));

            EmailHandler mail = new EmailHandler();
            //Client Mail
            //var path = HostingEnvironment.MapPath(@"~/App_Data/EmailTemplate/ContactUs.txt");
            //var template = await mail.ReadTemplate(path);
            var body = $"Click the following link to reset password <br> <a href=\"{callbackUrl}\">{callbackUrl}</a>";
            mail.Body = body;
            mail.Subject = "Reset Password!";
            mail.ToAddress = user.Email;
            await mail.Send();


            //await userManager.SendEmailAsync(user.Id, "Reset Password", Properties.Resources.RESETPASSWORD_POST+"< a href=\"" + callbackUrl + "\">here</a>");

            //Send Email after new Registration ---------------------------------------------
            try
            {
                /*
                EmailContent objEmailContent = new EmailContent();
                Email objSendEmail = new Email(EmailTemplate.ForgotPassword, objEmailContent);

                //Build Email Body -----------------------------------------------------------
                string strEmailBody = objSendEmail.GetEmailBody();
                strEmailBody = strEmailBody.Replace("#UserFullName#", user.FirstName + " " + user.LastName);
                //var portalLoginUrl = Request.RequestUri.AbsoluteUri.Replace("/api/ResetPassword", "/#!/login");
                strEmailBody = strEmailBody.Replace("#PORTAL_URL#", callbackUrl);

                // Populate other email infos --------------------------------------------------
                objEmailContent.ToAddress = user.Email;
                objEmailContent.IsBodyHTML = true;
                objEmailContent.Subject = "OnePay - Forgot password";
                objEmailContent.MailBody = strEmailBody;
                objSendEmail.SendMail();
                */
            }
            catch
            {
                // DO Nothing.
            }
            // End of Email Send -------------------------------------------------
        }

        /// <summary>
        /// Reset user password Step 1, Send mail
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public async Task<HttpResponseMessage> Put([FromBody]ResetPasswordModel item)
        {
            ApplicationUser user = await userManager.FindByEmailAsync(item.Email);
            if (user == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            item.Password = "Admin@123";

            string code = userManager.GeneratePasswordResetToken(user.Id);

            var result = await userManager.ResetPasswordAsync(user.Id, code, item.Password);

            if (result.Succeeded)
            {
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            else
            {
                return GetErrorResult(result);
            }
        }


        private HttpResponseMessage GetErrorResult(IdentityResult result)
        {
            if (result == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Internal Server error.");
            }

            if (!result.Succeeded)
            {
                if (result.Errors != null)
                {
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError("Errors", error);
                    }
                }

                if (ModelState.IsValid)
                {
                    // No ModelState errors are available to send, so just return an empty BadRequest.
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }

                return Request.CreateResponse(HttpStatusCode.BadRequest, ModelState);
            }

            return null;
        }

    }
}
