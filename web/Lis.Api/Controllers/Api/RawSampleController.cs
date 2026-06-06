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
    public class RawSampleController : ApiController
    {
        private ITestRequestDetailsManager manager;
        private ILogger logger;
        private IResponseManager responseMgr;

        public RawSampleController(ITestRequestDetailsManager testRequestDetailsManager, IResponseManager responseManager, ILogger Logger)
        {
            manager = testRequestDetailsManager;
            responseMgr = responseManager;
            logger = Logger;
        }

        [AllowAnonymous]
        [HttpGet]
        public TestRequestDetail Get(long Id)
        {
            try
            {
                var testRequestDetail = manager.GetTestRequestByRequestId(Id);

                return testRequestDetail;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }
    }
}
