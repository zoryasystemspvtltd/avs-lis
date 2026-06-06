using Lis.Api.Providers;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Lis.Api.Controllers.Api
{
    public class ResultsController : ApiController
    {
        private IResultManager manager;
        private ILogger logger;
        private IResponseManager responseMgr;
        public ResultsController(IResultManager resultManager, IResponseManager responseManager, ILogger Logger)
        {
            manager = resultManager;
            responseMgr = responseManager;
            logger = Logger;
        }
        /// <summary>
        /// Add results
        /// </summary>
        /// <param name="result"> Result object of type LIS.DtoModel</param>
        /// <returns>HttpResponseMessage</returns>
        [AllowAnonymous]
        public HttpResponseMessage Post(Result result)
        {
            try
            {
                var responseStrign = Helper.GetSerilizedObject<Result>(result);

                Logger.LogInstance.LogInfo("Result Request '{0}'", responseStrign);

                APIResponse aPIResponse = null;

                if (ModelState.IsValid)
                {
                    try
                    {
                        manager.Add(result);
                        aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, "Result added successfully", null, null);
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
        public HttpResponseMessage Put(Result result)
        {
            try
            {
                APIResponse aPIResponse = null;

                if (ModelState.IsValid)
                {
                    try
                    {
                        manager.Update(result);
                        aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, "Result updated successfully", null, null);
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
        [HttpPut]
        public HttpResponseMessage Delete(Result result)
        {
            try
            {
                APIResponse aPIResponse = null;

                if (ModelState.IsValid)
                {
                    try
                    {
                        manager.Delete(result);
                        aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, "Result deleted successfully", null, null);
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
        [ActionName("GetByString")]
        public Result Get(string ResultId)
        {
            try
            {
                var result = new Result();
                try
                {
                    result = manager.Get(ResultId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }
                return result;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }
    }
}
