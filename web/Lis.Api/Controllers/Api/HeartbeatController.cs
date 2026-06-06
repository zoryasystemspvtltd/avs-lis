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
    public class HeartbeatController : ApiController
    {
        private IEquipmentHeartBeatManager hearbeat;
        private IEquipmentManager manager;
        private ILogger logger;
        private IResponseManager responseMgr;
        public HeartbeatController(IEquipmentManager equipmentManager, IEquipmentHeartBeatManager hearbeatmanager, IResponseManager responseManager, ILogger Logger)
        {
            manager = equipmentManager;
            hearbeat = hearbeatmanager;
            responseMgr = responseManager;
            logger = Logger;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="isAlive"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        public HttpResponseMessage Post(HeartBeatStatus bitstatus)
        {
            try
            {
                logger.LogDebug("Heartbeat post method started!");

                hearbeat.Update(bitstatus.IsAlive);

                logger.LogDebug("Heartbeat post method completed!");
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

    }
}

