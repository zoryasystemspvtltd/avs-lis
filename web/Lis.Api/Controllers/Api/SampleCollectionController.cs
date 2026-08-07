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
    [RoutePrefix("api/SampleCollection")]
    public class SampleCollectionController : ApiController
    {
        private readonly ISampleCollectionManager manager;
        private readonly ILogger logger;

        public SampleCollectionController(ISampleCollectionManager manager, ILogger logger)
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
        [QAuthorize(ModuleName = "SampleCollection", ModulePermissionTypes = ModulePermissionType.CanView)]
        public ItemList<SampleWorkflowQueueRow> GetPendingQueue()
        {
            try
            {
                return manager.GetPendingQueue(SearchOptions);
            }
            catch (ArgumentException ex)
            {
                logger.LogError(ex.Message);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex.Message);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message));
            }
        }

        [HttpGet]
        [Route("ByBarcode")]
        [QAuthorize(ModuleName = "SampleCollection", ModulePermissionTypes = ModulePermissionType.CanView)]
        public SampleWorkflowQueueRow GetByBarcode(string barcode)
        {
            try
            {
                return manager.GetByBarcode(barcode);
            }
            catch (ArgumentException ex)
            {
                logger.LogError(ex.Message);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex.Message);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message));
            }
        }

        [HttpPost]
        [Route("Collect")]
        [QAuthorize(ModuleName = "SampleCollection", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        public IHttpActionResult Collect(SampleCollectionAction action)
        {
            try
            {
                manager.CollectSample(action);
                return Ok(new { message = "Sample collected successfully." });
            }
            catch (ArgumentException ex)
            {
                logger.LogError(ex.Message);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex.Message);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message));
            }
        }

        [HttpPost]
        [Route("Reject")]
        [QAuthorize(ModuleName = "SampleCollection", ModulePermissionTypes = ModulePermissionType.CanReject)]
        public IHttpActionResult Reject(SampleRejectionAction action)
        {
            try
            {
                action.IsReceiving = false;
                manager.RejectCollection(action);
                return Ok(new { message = "Sample rejected successfully." });
            }
            catch (ArgumentException ex)
            {
                logger.LogError(ex.Message);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex.Message);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message));
            }
        }

        [HttpPost]
        [Route("Recollect/{id:long}")]
        [QAuthorize(ModuleName = "SampleCollection", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        public IHttpActionResult Recollect(long id)
        {
            try
            {
                manager.TriggerRecollection(id);
                return Ok(new { message = "Recollection initiated successfully." });
            }
            catch (ArgumentException ex)
            {
                logger.LogError(ex.Message);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex.Message);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message));
            }
        }

        [HttpGet]
        [Route("EnsureBarcode/{id:long}")]
        [QAuthorize(ModuleName = "SampleCollection", ModulePermissionTypes = ModulePermissionType.CanView)]
        public IHttpActionResult EnsureBarcode(long id)
        {
            try
            {
                return Ok(new { barcode = manager.EnsureBarcode(id) });
            }
            catch (ArgumentException ex)
            {
                logger.LogError(ex.Message);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex.Message);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message));
            }
        }
    }
}
