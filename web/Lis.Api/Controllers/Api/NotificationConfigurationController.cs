using Lis.Api.Providers;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models.Notification;
using LIS.Logger;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Lis.Api.Controllers.Api
{
    [RoutePrefix("api/NotificationConfiguration")]
    public class NotificationConfigurationController : ApiController
    {
        private readonly INotificationConfigurationManager configurationManager;
        private readonly INotificationTemplateManager templateManager;
        private readonly INotificationManager notificationManager;
        private readonly IResponseManager responseManager;
        private readonly ILogger logger;

        public NotificationConfigurationController(
            INotificationConfigurationManager configurationManager,
            INotificationTemplateManager templateManager,
            INotificationManager notificationManager,
            IResponseManager responseManager,
            ILogger logger)
        {
            this.configurationManager = configurationManager;
            this.templateManager = templateManager;
            this.notificationManager = notificationManager;
            this.responseManager = responseManager;
            this.logger = logger;
        }

        [HttpGet, Route("")]
        [QAuthorize(ModuleName = "NotificationConfiguration")]
        public IHttpActionResult GetConfiguration()
        {
            return Ok(configurationManager.GetConfigurationDto());
        }

        [HttpPost, Route("")]
        [QAuthorize(ModuleName = "NotificationConfiguration", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        public HttpResponseMessage SaveConfiguration(NotificationConfigurationDto dto)
        {
            try
            {
                configurationManager.SaveConfiguration(dto);
                var response = responseManager.CreateResponse(HttpStatusCode.OK, "Notification configuration saved.", null, null);
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet, Route("Templates")]
        [QAuthorize(ModuleName = "NotificationConfiguration")]
        public IHttpActionResult GetTemplates()
        {
            return Ok(templateManager.GetAllTemplates());
        }

        [HttpGet, Route("Audit")]
        [QAuthorize(ModuleName = "NotificationConfiguration")]
        public IHttpActionResult GetAudit(int take = 100)
        {
            return Ok(notificationManager.GetAuditHistory(take));
        }
    }
}
