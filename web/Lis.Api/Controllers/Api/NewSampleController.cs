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
    public class NewSampleController : ApiController
    {
        private IPatientDetailsManager manager;
        private ILogger logger;
        private IResponseManager responseMgr;
        private ITestRequestDetailsManager testRequestManager;
        public NewSampleController(IPatientDetailsManager patientManager,
            IResponseManager responseManager,
            ILogger Logger,
            ITestRequestDetailsManager testRequestDetailsManager)
        {
            manager = patientManager;
            responseMgr = responseManager;
            logger = Logger;
            testRequestManager = testRequestDetailsManager;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="newOrder"></param>
        /// <returns></returns>
        [QAuthorize(ModuleName = "Samples"
        , ModulePermissionTypes = ModulePermissionType.CanAdd
        )]
        public HttpResponseMessage Post(NewOrder newOrder)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed, ModelState.Keys);
                }

                var id = manager.CreateNewOrder(newOrder);
                APIResponse aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, "New Sample added successfully", null, id);

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
        public IEnumerable<TestRequestDetail> Get()
        {
            try
            {
                var testRequestDetails = testRequestManager.GetAllNewSamples(ReportStatusType.New);

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
        public IEnumerable<TestRequestDetail> Get(string Id)
        {
            try
            {
                logger.LogInfo($"Get RequestNo Request: {Id}");

                var testRequestDetails = testRequestManager.GetByHisRequestNo(Id, ReportStatusType.New);

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
    }
}
