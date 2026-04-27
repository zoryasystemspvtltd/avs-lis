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
    public class EquipmentTestMappingsController : ApiController
    {
        private IEquipmentTestMappingManager manager;
        private ILogger logger;
        private IResponseManager responseMgr;
        public EquipmentTestMappingsController(IEquipmentTestMappingManager testMappingManager, IResponseManager responseManager, ILogger Logger)
        {
            manager = testMappingManager;
            responseMgr = responseManager;
            logger = Logger;
        }
        /// <summary>
        /// Add tests
        /// </summary>
        /// <param name="equipmentTestMapping"> EquipmentTestMapping object of type LIS.DtoModel</param>
        /// <returns>HttpResponseMessage</returns>
        [AllowAnonymous]
        [HttpPost]
        public HttpResponseMessage Post(TestEquipment equipment)
        {
            try
            {
                APIResponse aPIResponse = null;

                manager.Save(equipment.EquipmentId, equipment.mappings);
                aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, "EquipmentTestMapping added successfully", null, null);

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
        public IEnumerable<TestPanelMapping> Get(int Id)
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

    public class TestEquipment
    {
        public int EquipmentId { get; set; }
        public TestMappingMaster[] mappings { get; set; }
    }
}

