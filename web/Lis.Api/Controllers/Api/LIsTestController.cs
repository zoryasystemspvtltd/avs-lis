using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Linq;

namespace Lis.Api.Controllers.Api
{
    public class LisTestController : ApiController
    {
        private IEquipmentTestMappingManager manager;
        private ILogger logger;
        private IResponseManager responseMgr;
        public LisTestController(IEquipmentTestMappingManager testMappingManager, IResponseManager responseManager, ILogger Logger)
        {
            manager = testMappingManager;
            responseMgr = responseManager;
            logger = Logger;
        }


        [AllowAnonymous]
        [HttpGet]
        public IEnumerable<TestNameItem> Get(string Id)
        {
            try
            {
                var tests = manager.Get(Id);
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

