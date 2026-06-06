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
    [RoutePrefix("api/Patients")]
    public class PatientsController : ApiController
    {
        private ITestRequestDetailsManager testmanager;
        private IPatientDetailsManager manager;
        private ILogger logger;
        public PatientsController(IPatientDetailsManager equipmentManager
            , ILogger Logger
            , ITestRequestDetailsManager testmanager)
        {
            manager = equipmentManager;
            logger = Logger;
            this.testmanager = testmanager;
        }

        private ListOptions ApiOption
        {
            get
            {
                var defaultOption = new ListOptions
                {
                    RecordPerPage = 10,
                    CurrentPage = 1,
                    SortColumnName = "SampleCollectionDate",
                    SortDirection = false,
                    Status = ReportStatusType.New
                };

                var headers = System.Web.HttpContext.Current?.Request?.Headers;
                if (headers == null)
                {
                    return defaultOption;
                }

                var apiOption = headers.GetValues("ApiOption");
                if (apiOption == null || !apiOption.Any())
                {
                    return defaultOption;
                }

                return JsonConvert.DeserializeObject<ListOptions>(apiOption.FirstOrDefault(),
                    new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }) ?? defaultOption;
            }
        }

        [HttpGet]
        [Route("Billing")]
        public ItemList<PatientDetail> GetBilling()
        {
            try
            {
                return manager.GetForBilling(ApiOption);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return new ItemList<PatientDetail> { TotalRecord = 0, Items = new List<PatientDetail>() };
            }
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("")]
        public ItemList<TestRequestDetail> GetList()
        {
            try
            {
                return manager.Get(ApiOption) ?? new ItemList<TestRequestDetail> { TotalRecord = 0, Items = new List<TestRequestDetail>() };
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return new ItemList<TestRequestDetail> { TotalRecord = 0, Items = new List<TestRequestDetail>() };
            }
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("{id:long}")]
        public PatientDetail GetById(long id)
        {
            try
            {
                return manager.Get(id);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        [AllowAnonymous]
        [HttpPut]
        [Route("")]
        public HttpResponseMessage Put(List<AuthorizeRequest> request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed, ModelState.Keys);
                }
                foreach (var sample in request)
                {
                    if (sample.Status == ReportStatusType.DoctorApproved
                        || sample.Status == ReportStatusType.DoctorRejected)
                    {
                        testmanager.DoctorReview(sample.Id, sample.Status, sample.Note, sample.RunIndex);
                    }
                    if (sample.Status == ReportStatusType.TechnicianApproved
                        || sample.Status == ReportStatusType.TechnicianRejected
                        || sample.Status == ReportStatusType.New)
                    {
                        testmanager.TechnicianReview(sample.Id, sample.Status, sample.Note, sample.RunIndex);
                    }

                }

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }
    }
}
