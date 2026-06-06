using Lis.Api.Providers;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Lis.Api.Controllers.Api
{
    [RoutePrefix("api/SaleInvoice")]
    public class SaleInvoiceController : ApiController
    {
        private readonly ISaleInvoiceManager manager;
        private readonly IResponseManager responseMgr;
        private readonly ILogger logger;

        public SaleInvoiceController(ISaleInvoiceManager manager, IResponseManager responseManager, ILogger logger)
        {
            this.manager = manager;
            responseMgr = responseManager;
            this.logger = logger;
        }

        private ListOptions ApiOption
        {
            get
            {
                var apiOption = System.Web.HttpContext.Current.Request.Headers.GetValues("ApiOption");
                if (apiOption == null || !apiOption.Any())
                {
                    return new ListOptions
                    {
                        RecordPerPage = 10,
                        CurrentPage = 1,
                        SortColumnName = "InvoiceDate",
                        SortDirection = true
                    };
                }

                var option = JsonConvert.DeserializeObject<ListOptions>(apiOption.FirstOrDefault(),
                    new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

                return option ?? new ListOptions
                {
                    RecordPerPage = 10,
                    CurrentPage = 1,
                    SortColumnName = "InvoiceDate",
                    SortDirection = true
                };
            }
        }

        [HttpGet]
        [Route("")]
        public ItemList<SaleInvoice> Get()
        {
            try
            {
                var result = manager.Get(ApiOption);
                return result ?? new ItemList<SaleInvoice> { TotalRecord = 0, Items = new List<SaleInvoice>() };
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return new ItemList<SaleInvoice> { TotalRecord = 0, Items = new List<SaleInvoice>() };
            }
        }

        [HttpGet]
        [Route("{id:long}")]
        public SaleInvoiceDto Get(long id)
        {
            try
            {
                return manager.GetById(id);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        [HttpGet]
        [Route("NextInvoiceNo")]
        [ActionName("NextInvoiceNo")]
        public string GetNextInvoiceNo()
        {
            try
            {
                return manager.GenerateInvoiceNo();
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        [HttpPost]
        [Route("")]
        [QAuthorize(ModuleName = "SaleInvoices", ModulePermissionTypes = ModulePermissionType.CanAdd | ModulePermissionType.CanEdit)]
        public HttpResponseMessage Post(SaleInvoiceDto dto)
        {
            try
            {
                var id = manager.Save(dto);
                var response = responseMgr.CreateResponse(HttpStatusCode.OK, "Invoice saved successfully", null, id);
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [QAuthorize(ModuleName = "SaleInvoices", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        [HttpPost]
        [Route("Status")]
        public HttpResponseMessage PutStatus(StatusUpdateRequest request)
        {
            try
            {
                manager.UpdateStatus(request.Id, request.InvoiceStatus, request.PaymentStatus);
                var response = responseMgr.CreateResponse(HttpStatusCode.OK, "Status updated successfully", null, null);
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [QAuthorize(ModuleName = "SaleInvoices", ModulePermissionTypes = ModulePermissionType.CanDelete)]
        [HttpPut]
        [Route("Cancel/{id:long}")]
        public HttpResponseMessage Cancel(long id)
        {
            try
            {
                manager.Cancel(id);
                var response = responseMgr.CreateResponse(HttpStatusCode.OK, "Invoice cancelled successfully", null, null);
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }
    }

    public class StatusUpdateRequest
    {
        public long Id { get; set; }
        public int InvoiceStatus { get; set; }
        public int PaymentStatus { get; set; }
    }
}
