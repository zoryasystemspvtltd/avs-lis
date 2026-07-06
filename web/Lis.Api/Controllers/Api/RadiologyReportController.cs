using Lis.Api.Providers;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Lis.Api.Controllers.Api
{
    [RoutePrefix("api/RadiologyReport")]
    public class RadiologyReportController : ApiController
    {
        private readonly IRadiologyReportManager manager;
        private readonly ILogger logger;

        public RadiologyReportController(IRadiologyReportManager manager, ILogger logger)
        {
            this.manager = manager;
            this.logger = logger;
        }

        private SampleWorkflowSearchOptions SearchOptions
        {
            get
            {
                var apiOption = System.Web.HttpContext.Current.Request.Headers.GetValues("ApiOption");
                if (apiOption == null || !apiOption.Any())
                {
                    return new SampleWorkflowSearchOptions { RecordPerPage = 25, CurrentPage = 1 };
                }

                return JsonConvert.DeserializeObject<SampleWorkflowSearchOptions>(
                    apiOption.FirstOrDefault(),
                    new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore })
                    ?? new SampleWorkflowSearchOptions();
            }
        }

        [HttpGet]
        [Route("PendingQueue")]
        [QAuthorize(ModuleName = "RadiologyReportEntry", ModulePermissionTypes = ModulePermissionType.CanView)]
        public ItemList<RadiologyQueueRow> GetPendingQueue()
        {
            try
            {
                return manager.GetPendingQueue(SearchOptions);
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpGet]
        [Route("DoctorApprovalQueue")]
        [QAuthorize(ModuleName = "RadiologyDoctorApprovals", ModulePermissionTypes = ModulePermissionType.CanView)]
        public ItemList<RadiologyQueueRow> GetDoctorApprovalQueue()
        {
            try
            {
                return manager.GetDoctorApprovalQueue(SearchOptions);
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpGet]
        [Route("ApprovedQueue")]
        [QAuthorize(ModuleName = "RadiologyDoctorApprovals", ModulePermissionTypes = ModulePermissionType.CanView)]
        public ItemList<RadiologyQueueRow> GetApprovedQueue()
        {
            try
            {
                return manager.GetApprovedQueue(SearchOptions);
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpGet]
        [Route("DoctorApproval/{id:long}")]
        [QAuthorize(ModuleName = "RadiologyDoctorApprovals", ModulePermissionTypes = ModulePermissionType.CanView)]
        public RadiologyReportDetailDto GetDoctorApprovalReport(long id)
        {
            try
            {
                return manager.GetReport(id);
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpGet]
        [Route("{id:long}")]
        [QAuthorize(ModuleName = "RadiologyReportEntry", ModulePermissionTypes = ModulePermissionType.CanView)]
        public RadiologyReportDetailDto GetReport(long id)
        {
            try
            {
                return manager.GetReport(id);
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpPost]
        [Route("")]
        [QAuthorize(ModuleName = "RadiologyReportEntry", ModulePermissionTypes = ModulePermissionType.CanAdd)]
        public IHttpActionResult Create(RadiologyRequestDetail request)
        {
            try
            {
                var id = manager.CreateRequest(request);
                return Ok(new { id, message = "Radiology request created." });
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpPost]
        [Route("Save")]
        [QAuthorize(ModuleName = "RadiologyReportEntry", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        public IHttpActionResult Save(RadiologyReportSaveRequest request)
        {
            try
            {
                manager.SaveReport(request);
                return Ok(new { message = "Radiology report saved." });
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpPost]
        [Route("Authorize")]
        [QAuthorize(ModuleName = "RadiologyDoctorApprovals", ModulePermissionTypes = ModulePermissionType.CanAuthorize)]
        public IHttpActionResult Authorize(RadiologyAuthorizeRequest request)
        {
            try
            {
                manager.AuthorizeReport(request, true);
                return Ok(new { message = "Radiology report authorized." });
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }
    }
}
