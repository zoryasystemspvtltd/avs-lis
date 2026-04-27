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
                var apiOption = System.Web.HttpContext.Current.Request.Headers.GetValues("ApiOption");
                if (apiOption == null || apiOption.Count() == 0)
                {
                    throw new KeyNotFoundException("Invalid Option specified");
                }

                var option = JsonConvert.DeserializeObject<ListOptions>(apiOption.FirstOrDefault(),
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                });
                return option;
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public ItemList<TestRequestDetail> Get()
        {
            try
            {
                var patients = manager.Get(ApiOption);

                return patients;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public PatientDetail Get(long Id)
        {
            try
            {
                var patient = manager.Get(Id);

                return patient;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        [AllowAnonymous]
        [HttpPut]
        public HttpResponseMessage Put(List<AuthorizeRequest> request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed, ModelState.Keys);
                }
                foreach(var sample in request)
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
                return null;
            }
        }
    }
}
