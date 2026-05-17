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
        private ITestRateMasterManager testRateManager;
        private ILogger logger;
        private IResponseManager responseMgr;

        public TestRateController(ITestRateMasterManager testRateManager, IResponseManager responseManager, ILogger Logger)
        {
            this.testRateManager = testRateManager;
            responseMgr = responseManager;
            logger = Logger;
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

        /// <summary>
        /// Add Test Rate Master details to database
        /// </summary>
        /// <param name="testRate">Test Rate object of type LIS.DtoModel</param>
        /// <returns>HttpResponseMessage</returns>
        [HttpPost]
        [QAuthorize(ModuleName = "TestRate"
        , ModulePermissionTypes = ModulePermissionType.CanAdd
        )]
        public HttpResponseMessage Post(TestRateMaster testRate)
        {
            try
            {
                APIResponse aPIResponse = null;

                if (ModelState.IsValid)
                {
                    try
                    {
                        testRateManager.Add(testRate);
                        aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, "Test Rate added successfully", null, null);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex.Message);
                        aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, ex.Message, null, ex);
                        return Request.CreateResponse<APIResponse>(HttpStatusCode.InternalServerError, aPIResponse);
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed, ModelState.Keys);
                }

                return Request.CreateResponse<APIResponse>(HttpStatusCode.OK, aPIResponse);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        /// <summary>
        /// Update Test Rate Master details
        /// </summary>
        /// <param name="testRate">Test Rate object of type LIS.DtoModel</param>
        /// <returns>HttpResponseMessage</returns>
        [HttpPut]
        [QAuthorize(ModuleName = "TestRate"
        , ModulePermissionTypes = ModulePermissionType.CanEdit
        )]
        public HttpResponseMessage Put(TestRateMaster testRate)
        {
            try
            {
                APIResponse aPIResponse = null;

                if (ModelState.IsValid)
                {
                    try
                    {
                        testRateManager.Update(testRate);
                        aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, "Test Rate updated successfully", null, null);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex.Message);
                        aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, ex.Message, null, ex);
                        return Request.CreateResponse<APIResponse>(HttpStatusCode.InternalServerError, aPIResponse);
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed, ModelState.Keys);
                }

                return Request.CreateResponse<APIResponse>(HttpStatusCode.OK, aPIResponse);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        /// <summary>
        /// Get all Test Rate Master details
        /// </summary>
        /// <returns>ItemList of Test Rate</returns>
        [AllowAnonymous]
        [HttpGet]
        public ItemList<TestRateMaster> Get()
        {
            try
            {
                var testRates = testRateManager.Get(ApiOption);
                return testRates;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        /// <summary>
        /// Get Test Rate Master details by Id
        /// </summary>
        /// <param name="Id">Test Rate Id</param>
        /// <returns>TestRateMaster object</returns>
        [AllowAnonymous]
        [HttpGet]
        public TestRateMaster Get(string Id)
        {
            try
            {
                var testRate = testRateManager.GetTestRateById(long.Parse(Id));
                return testRate;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }
    }
}
