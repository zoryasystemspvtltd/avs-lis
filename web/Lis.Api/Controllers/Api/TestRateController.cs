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
    public class TestRateController : ApiController
    {
        private readonly ITestRateMasterManager manager;
        private readonly IResponseManager responseMgr;
        private readonly ILogger logger;

        public TestRateController(ITestRateMasterManager manager, IResponseManager responseManager, ILogger logger)
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
                    throw new KeyNotFoundException("Invalid Option specified");
                }

                return JsonConvert.DeserializeObject<ListOptions>(apiOption.FirstOrDefault(),
                    new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            }
        }

        [HttpGet]
        public ItemList<TestRateMaster> Get()
        {
            try
            {
                return manager.Get(ApiOption);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        [HttpGet]
        public TestRateMaster Get(int Id)
        {
            try
            {
                return manager.GetById(Id);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        [HttpGet]
        [ActionName("GetByTest")]
        public IEnumerable<TestRateMaster> GetByTest(int testId)
        {
            try
            {
                return manager.GetByTestId(testId);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        [HttpGet]
        [ActionName("GetEffective")]
        public TestRateMaster GetEffective(int testId, int rateType, int? corporateId = null, int? referralDoctorId = null, int? profileId = null)
        {
            try
            {
                return manager.GetEffectiveRate(testId, rateType, corporateId, referralDoctorId, profileId);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        [QAuthorize(ModuleName = "TestRates", ModulePermissionTypes = ModulePermissionType.CanAdd)]
        public HttpResponseMessage Post(TestRateMaster item)
        {
            try
            {
                var id = manager.Add(item);
                var response = responseMgr.CreateResponse(HttpStatusCode.OK, "Rate added successfully", null, id);
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [QAuthorize(ModuleName = "TestRates", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        [HttpPost]
        public HttpResponseMessage Put(TestRateMaster item)
        {
            try
            {
                manager.Update(item);
                var response = responseMgr.CreateResponse(HttpStatusCode.OK, "Rate updated successfully", null, null);
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [QAuthorize(ModuleName = "TestRates", ModulePermissionTypes = ModulePermissionType.CanDelete)]
        [HttpPut]
        public HttpResponseMessage Delete(TestRateMaster item)
        {
            try
            {
                manager.Delete(item);
                var response = responseMgr.CreateResponse(HttpStatusCode.OK, "Rate deleted successfully", null, null);
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }
    }
}
