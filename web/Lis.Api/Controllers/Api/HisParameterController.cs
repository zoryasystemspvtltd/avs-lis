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
    public class HisParameterController : ApiController
    {
        private IHisMasterManager hisManager;
        private ILogger logger;
        private IResponseManager responseMgr;
        public HisParameterController(IHisMasterManager hisManager, IResponseManager responseManager, ILogger Logger)
        {
            this.hisManager = hisManager;
            responseMgr = responseManager;
            logger = Logger;
        }


        [AllowAnonymous]
        [HttpGet]
        public IEnumerable<HISParameterMaster> Get(int Id)
        {
            try
            {
                var parameters = hisManager.GetParameterByTestId(Id);
                return parameters;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

    }
}
