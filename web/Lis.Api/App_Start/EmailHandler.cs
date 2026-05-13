using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Configuration;
using System.Web.Configuration;
using System.Web;
using System.Net;
using LIS.Logger;

namespace Lis.Api
{
    public class EmailHandler
    {
        MailMessage message;
        public EmailHandler()
        {
            message = new MailMessage();
        }
        public string ToAddress { get; set; }

        public string CcAddress { get; set; }

        public string Body { get; set; }

        public string Subject { get; set; }


        public async Task Send()
        {
            throw new NotFiniteNumberException();
            /*
            try
            {

                if (string.IsNullOrEmpty(ToAddress))
                {
                    throw new Exception("Invalid To Address.");
                }

                if (string.IsNullOrEmpty(Subject))
                {
                    throw new Exception("Invalid Subject.");
                }
                message.To.Add(new MailAddress(ToAddress));
                if (!string.IsNullOrEmpty(CcAddress))
                {
                    message.CC.Add(new MailAddress(CcAddress));
                }
                //message.From = new MailAddress(email.Trim());  // replace with sender's email id 
                message.Subject = Subject;

                message.Body = Body;
                message.IsBodyHtml = true;

                var config = WebConfigurationManager.OpenWebConfiguration(HttpContext.Current.Request.ApplicationPath);
                var settings = (MailSettingsSectionGroup)config.GetSectionGroup("system.net/mailSettings");

                using (var smtp = new SmtpClient(settings.Smtp.Network.Host, settings.Smtp.Network.Port))
                {
                    smtp.Credentials = new NetworkCredential(settings.Smtp.Network.UserName, settings.Smtp.Network.Password);
                    smtp.EnableSsl = settings.Smtp.Network.EnableSsl;

                    await smtp.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                Logger.LogInstance.LogException(ex);
                throw;
            }
            */
        }

        public async Task<string> ReadTemplate(string path)
        {
            try
            {
                using (var file = new StreamReader(path))
                {
                    return await file.ReadToEndAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogInstance.LogException(ex);
                throw;
            }
        }
    }
}
