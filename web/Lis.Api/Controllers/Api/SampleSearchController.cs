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
    public class SampleSearchController : ApiController
    {
        private ITestRequestDetailsManager manager;
        private ILogger logger;
        private IResponseManager responseMgr;

        public SampleSearchController(ITestRequestDetailsManager testRequestDetailsManager, IResponseManager responseManager, ILogger Logger)
        {
            manager = testRequestDetailsManager;
            responseMgr = responseManager;
            logger = Logger;
        }


        /// <summary>
        /// Get result by request id
        /// </summary>
        /// <param name="Id">Request Id</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        public long[] Get(string Id)
        {
            try
            {
                var testRequestList = manager.GetTestResultByRequestId(Id);

                return testRequestList;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }
    }
}

