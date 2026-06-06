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
using System.Threading.Tasks;
using System.Web.Http;
using static System.Net.Mime.MediaTypeNames;

namespace Lis.Api.Controllers.Api
{
    [RoutePrefix("api/HisTest")]
    public class HisTestController : ApiController
    {
        private IHisTestMasterManager hisManager;
        private ILogger logger;
        private IResponseManager responseMgr;
        public HisTestController(IHisTestMasterManager hisManager, IResponseManager responseManager, ILogger Logger)
        {
            this.hisManager = hisManager;
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
                    return new ListOptions
                    {
                        RecordPerPage = 500,
                        CurrentPage = 1,
                        SortColumnName = "HISTestCode",
                        SortDirection = true
                    };
                }

                var option = JsonConvert.DeserializeObject<ListOptions>(apiOption.FirstOrDefault(),
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                });
                return option ?? new ListOptions
                {
                    RecordPerPage = 500,
                    CurrentPage = 1,
                    SortColumnName = "HISTestCode",
                    SortDirection = true
                };
            }
        }

        [HttpGet]
        [Route("NextTestCode")]
        public IHttpActionResult GetNextTestCode()
        {
            try
            {
                return Ok(new { testCode = hisManager.GenerateNextTestCode() });
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return Ok(new { testCode = string.Empty });
            }
        }

        /// <summary>
        /// Add Test Master details to database
        /// </summary>
        /// <param name="specimen"> Test object of type LIS.DtoModel</param>
        /// <returns>HttpResponseMessage</returns>
        [AllowAnonymous]
        [HttpPost]
        [Route("")]
        public HttpResponseMessage Post(HisTestMaster test)
        {
            try
            {
                APIResponse aPIResponse = null;

                if (ModelState.IsValid)
                {
                    try
                    {
                        hisManager.Add(test);
                        aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, "Test added successfully", null, null);
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

        [AllowAnonymous]
        [HttpPost]
        [Route("Put")]
        public HttpResponseMessage Put(HisTestMaster test)
        {
            try
            {
                APIResponse aPIResponse = null;

                if (ModelState.IsValid)
                {
                    try
                    {
                        hisManager.Update(test);
                        aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, "Test updated successfully", null, null);
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

        [AllowAnonymous]
        [HttpPost]
        [ActionName("Delete")]
        [Route("Delete")]
        public HttpResponseMessage Delete(HisTestMaster test)
        {
            try
            {
                APIResponse aPIResponse = null;

                if (ModelState.IsValid)
                {
                    try
                    {
                        hisManager.Delete(test);
                        aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, "Test deleted successfully", null, null);
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

        [AllowAnonymous]
        [HttpGet]
        [Route("")]
        public ItemList<HisTestMaster> Get()
        {
            try
            {
                var tests = hisManager.Get(ApiOption);
                return tests;

            }
            catch (Exception e)
            {
                logger.LogException(e);
                return new ItemList<HisTestMaster> { TotalRecord = 0, Items = new List<HisTestMaster>() };
            }
        }

        /// <summary>
        /// Get HIS Test details by his test code
        /// </summary>
        /// <param name="Id">HIS Test Code</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        [Route("{id}")]
        public HisTestMaster Get(string Id)
        {
            try
            {
                var tests = hisManager.GetTestById(long.Parse(Id));
                return tests;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

    }
}
