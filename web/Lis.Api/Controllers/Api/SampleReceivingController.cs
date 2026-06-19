using Lis.Api.Providers;
using LIS.DataAccess.Repo;
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
    [RoutePrefix("api/SampleReceiving")]
    public class SampleReceivingController : ApiController
    {
        private readonly ISampleReceivingManager manager;
        private readonly ModuleRepo<SampleRejectionReasonMaster> rejectionRepo;
        private readonly ILogger logger;

        public SampleReceivingController(
            ISampleReceivingManager manager,
            ILogger logger,
            IModuleIdentity identity,
            GenericUnitOfWork uow)
        {
            this.manager = manager;
            this.logger = logger;
            rejectionRepo = new ModuleRepo<SampleRejectionReasonMaster>(logger, identity, uow);
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
        [Route("Queue")]
        [QAuthorize(ModuleName = "SampleReceiving", ModulePermissionTypes = ModulePermissionType.CanView)]
        public ItemList<SampleWorkflowQueueRow> GetQueue()
        {
            try
            {
                return manager.GetReceivingQueue(SearchOptions);
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpGet]
        [Route("ByBarcode")]
        [QAuthorize(ModuleName = "SampleReceiving", ModulePermissionTypes = ModulePermissionType.CanView)]
        public SampleWorkflowQueueRow GetByBarcode(string barcode)
        {
            try
            {
                return manager.GetByBarcode(barcode);
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpGet]
        [Route("RejectionReasons")]
        [QAuthorize(ModuleName = "SampleReceiving", ModulePermissionTypes = ModulePermissionType.CanView)]
        public IHttpActionResult GetRejectionReasons()
        {
            try
            {
                var reasons = rejectionRepo.Get(r => r.IsActive && r.Category == "Receiving")
                    .OrderBy(r => r.Code)
                    .Select(r => new { r.Id, r.Code, r.Name })
                    .ToList();
                return Ok(reasons);
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message));
            }
        }

        [HttpPost]
        [Route("Receive")]
        [QAuthorize(ModuleName = "SampleReceiving", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        public IHttpActionResult Receive(SampleReceivingAction action)
        {
            try
            {
                manager.ReceiveSample(action);
                return Ok(new { message = "Sample received successfully." });
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpPost]
        [Route("Reject")]
        [QAuthorize(ModuleName = "SampleReceiving", ModulePermissionTypes = ModulePermissionType.CanReject)]
        public IHttpActionResult Reject(SampleRejectionAction action)
        {
            try
            {
                action.IsReceiving = true;
                manager.RejectSample(action);
                return Ok(new { message = "Sample rejected successfully." });
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpPost]
        [Route("Recollect/{id:long}")]
        [QAuthorize(ModuleName = "SampleReceiving", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        public IHttpActionResult Recollect(long id)
        {
            try
            {
                manager.TriggerRecollection(id);
                return Ok(new { message = "Recollection initiated successfully." });
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }
    }
}
