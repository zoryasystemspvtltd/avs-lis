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
    public class HisTestController : ApiController
    {
        private IHisMasterManager hisManager;
        private ILogger logger;
        private IResponseManager responseMgr;
        public HisTestController(IHisMasterManager hisManager, IResponseManager responseManager, ILogger Logger)
        {
            this.hisManager = hisManager;
            responseMgr = responseManager;
            logger = Logger;
        }

        [AllowAnonymous]
        [HttpGet]
        public IEnumerable<HisTestMaster> Get()
        {
            try
            {
                var tests = hisManager.GetTests();
                return tests;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        /// <summary>
        /// Get HIS Test details by his test code
        /// </summary>
        /// <param name="Id">HIS Test Code</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        public HisTestMaster Get(string Id)
        {
            try
            {
                var tests = hisManager.GetTestById(Id);
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
