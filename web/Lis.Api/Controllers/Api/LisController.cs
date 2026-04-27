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
    public class LisController : ApiController
    {
        //TODO HMAC Authentication
        private ITestRequestDetailsManager testRequestManager;
        private IResultManager resultManager;
        private ILogger logger;
        private IResponseManager responseMgr;

        public LisController(
            ITestRequestDetailsManager testRequestDetailsManager,
            IResultManager resultHisManager,
            IResponseManager responseManager,
            ILogger Logger)
        {
            testRequestManager = testRequestDetailsManager;
            resultManager = resultHisManager;
            responseMgr = responseManager;
            logger = Logger;
        }

        /// <summary>
        /// Save Lis result
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        public HttpResponseMessage Post(Result result)
        {
            APIResponse aPIResponse = null;
            try
            {
                var requestStrign = JsonConvert.SerializeObject(result);
                logger.LogInfo($"Post result Request: {requestStrign}");

                var responsemessage = resultManager.Add(result);

                aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, "Result added successfully", null, responsemessage);
                logger.LogInfo($"Post result response{responsemessage}");
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, ex.Message, null, 0);
                return Request.CreateResponse<APIResponse>(HttpStatusCode.InternalServerError, aPIResponse);
            }
            return Request.CreateResponse<APIResponse>(HttpStatusCode.OK, aPIResponse);
        }


        /// <summary>
        /// Acknowledge receive of sample from LIS Console.
        /// </summary>
        /// <param name="Id">Sample Code</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPut]
        public HttpResponseMessage Put(long Id)
        {
            APIResponse aPIResponse = null;
            try
            {

                logger.LogInfo($"Acknowledge Sample Request: {Id}");

                var responsemessage = testRequestManager.UpdateStatus(Id, ReportStatusType.SentToEquipment);

                aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, "Result added successfully", null, responsemessage);

                logger.LogInfo($"Acknowledge Sample Response: {responsemessage}");
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, ex.Message, null, false);
                return Request.CreateResponse<APIResponse>(HttpStatusCode.InternalServerError, aPIResponse);
            }

            return Request.CreateResponse<APIResponse>(HttpStatusCode.OK, aPIResponse);
        }

        /// <summary>
        /// This methods is used to ping the API from LIS Console
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        public bool Get()
        {
            try
            {
                logger.LogInfo($"Ping API Request");
                var testRequestDetails = testRequestManager.Ping();
                logger.LogInfo($"Ping API Response");
                return testRequestDetails;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return false;
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public IEnumerable<TestRequestDetail> Get(string Id)
        {
            try
            {
                logger.LogInfo($"Get Sample Request: {Id}");

                var testRequestDetails = testRequestManager.GetBySampleNo(Id, ReportStatusType.New);

                var responseStrign = JsonConvert.SerializeObject(testRequestDetails);
                logger.LogInfo($"Get Sample Response: {responseStrign }");
                return testRequestDetails;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public bool? Get(string SampleNo, string LisHostCode)
        {
            try
            {
                logger.LogInfo($"Get Sample Request: {SampleNo}");

                var isPanel = testRequestManager.IsPanelTest(SampleNo, LisHostCode);

                var responseStrign = JsonConvert.SerializeObject(isPanel);
                logger.LogInfo($"Get Sample Response: {responseStrign }");
                return isPanel;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }
    }
}
