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
    [RoutePrefix("api/TestRate")]
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
                    return new ListOptions
                    {
                        RecordPerPage = 10,
                        CurrentPage = 1,
                        SortColumnName = "EffectiveStart",
                        SortDirection = true
                    };
                }

                var option = JsonConvert.DeserializeObject<ListOptions>(apiOption.FirstOrDefault(),
                    new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

                return option ?? new ListOptions
                {
                    RecordPerPage = 10,
                    CurrentPage = 1,
                    SortColumnName = "EffectiveStart",
                    SortDirection = true
                };
            }
        }

        [HttpGet]
        [Route("")]
        [Route("{id:int}")]
        public IHttpActionResult Get(int? id = null)
        {
            try
            {
                if (id.HasValue)
                {
                    return Ok(manager.GetById(id.Value));
                }

                return Ok(manager.Get(ApiOption) ?? new ItemList<TestRateMaster> { TotalRecord = 0, Items = new List<TestRateMaster>() });
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return id.HasValue ? (IHttpActionResult)Ok() : Ok(new ItemList<TestRateMaster> { TotalRecord = 0, Items = new List<TestRateMaster>() });
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
        [ActionName("Put")]
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
        [HttpPost]
        [ActionName("Delete")]
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
