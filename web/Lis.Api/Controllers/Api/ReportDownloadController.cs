using Lis.Api.Providers;
using LIS.DtoModel.Interfaces;
using LIS.Logger;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Lis.Api.Controllers.Api
{
    [RoutePrefix("api/report/download")]
    public class ReportDownloadController : ApiController
    {
        private readonly INotificationSecureDownloadManager secureDownloadManager;
        private readonly ILogger logger;

        public ReportDownloadController(INotificationSecureDownloadManager secureDownloadManager, ILogger logger)
        {
            this.secureDownloadManager = secureDownloadManager;
            this.logger = logger;
        }

        [HttpGet]
        [Route("{token}")]
        [AllowAnonymous]
        public IHttpActionResult Download(string token)
        {
            try
            {
                var result = secureDownloadManager.ValidateAndGetReport(token);
                if (!result.Success)
                {
                    return Content(HttpStatusCode.BadRequest, new { message = result.ErrorMessage });
                }

                secureDownloadManager.MarkTokenUsed(token);
                return Ok(result.ReportData);
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                return Content(HttpStatusCode.InternalServerError, new { message = "Unable to download report." });
            }
        }
    }
}
