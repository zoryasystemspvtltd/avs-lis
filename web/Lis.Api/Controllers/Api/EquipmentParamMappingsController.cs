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
    public class EquipmentParamMappingsController : ApiController
    {
        private IEquipmentParamMappingManager manager;
        private ILogger logger;
        private IResponseManager responseMgr;
        public EquipmentParamMappingsController(IEquipmentParamMappingManager paramMappingManager, IResponseManager responseManager, ILogger Logger)
        {
            manager = paramMappingManager;
            responseMgr = responseManager;
            logger = Logger;
        }
        /// <summary>
        /// Add Parameter
        /// </summary>
        /// <param name="equipmentParamMapping"> EquipmentParamMapping object of type LIS.DtoModel</param>
        /// <returns>HttpResponseMessage</returns>
        [AllowAnonymous]
        [HttpPost]
        public HttpResponseMessage Post(HISParameterMaster[] equipmentParamMapping)
        {
            try
            {
                APIResponse aPIResponse = null;

                manager.Save(equipmentParamMapping);
                aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, "EquipmentParamMapping added successfully", null, null);

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
        public IEnumerable<HISParameterMaster> Get(int Id)
        {
            try
            {
                var parameters = manager.Get(Id);
                return parameters;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public string[] Get()
        {
            try
            {
                var models = manager.Get();
                return models;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }
    }
}

