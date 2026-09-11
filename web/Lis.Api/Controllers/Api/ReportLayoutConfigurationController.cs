using Lis.Api.Providers;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models.Reports;
using LIS.Logger;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Lis.Api.Controllers.Api
{
    [RoutePrefix("api/ReportLayoutConfiguration")]
    public class ReportLayoutConfigurationController : ApiController
    {
        private readonly IReportLayoutConfigurationManager manager;
        private readonly IResponseManager responseManager;
        private readonly ILogger logger;

        public ReportLayoutConfigurationController(
            IReportLayoutConfigurationManager manager,
            IResponseManager responseManager,
            ILogger logger)
        {
            this.manager = manager;
            this.responseManager = responseManager;
            this.logger = logger;
        }

        [HttpGet, Route("{reportType}")]
        [QAuthorize(ModuleName = "ReportLayoutConfiguration")]
        public IHttpActionResult GetByReportType(string reportType)
        {
            try
            {
                return Ok(manager.GetByReportType(reportType));
            }
            catch (InvalidOperationException ex)
            {
                return Content(HttpStatusCode.BadRequest, ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                return InternalServerError(ex);
            }
        }

        [HttpGet, Route("defaults/{reportType}")]
        [QAuthorize(ModuleName = "ReportLayoutConfiguration")]
        public IHttpActionResult GetDefaults(string reportType)
        {
            try
            {
                return Ok(manager.GetDefaults(reportType));
            }
            catch (InvalidOperationException ex)
            {
                return Content(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        [HttpPost, Route("")]
        [QAuthorize(ModuleName = "ReportLayoutConfiguration", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        public HttpResponseMessage Save(ReportLayoutConfigurationDto dto)
        {
            try
            {
                var saved = manager.Save(dto);
                var response = responseManager.CreateResponse(HttpStatusCode.OK, "Report layout configuration saved.", null, saved);
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (InvalidOperationException ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost, Route("reset/{reportType}")]
        [QAuthorize(ModuleName = "ReportLayoutConfiguration", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        public HttpResponseMessage Reset(string reportType)
        {
            try
            {
                var saved = manager.ResetToDefault(reportType);
                var response = responseManager.CreateResponse(HttpStatusCode.OK, "Report layout reset to default.", null, saved);
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (InvalidOperationException ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}
