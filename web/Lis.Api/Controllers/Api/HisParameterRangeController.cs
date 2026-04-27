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
    public class HisParameterRangeController : ApiController
    {
        private IHisMasterManager hisManager;
        private ILogger logger;
        private IResponseManager responseMgr;
        public HisParameterRangeController(IHisMasterManager hisManager, IResponseManager responseManager, ILogger Logger)
        {
            this.hisManager = hisManager;
            responseMgr = responseManager;
            logger = Logger;
        }


        [AllowAnonymous]
        [HttpGet]
        public IEnumerable<HISParameterRangMaster> Get(int Id)
        {
            try
            {
                var ranges = hisManager.GetRangesByParameterId(Id);
                return ranges;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

    }
}
