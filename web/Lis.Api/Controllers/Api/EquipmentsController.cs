using Lis.Api.App_Start;
using Lis.Api.Providers;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace Lis.Api.Controllers.Api
{
    public class EquipmentsController : ApiController
    {
        private IHubContext<ILisClient> hubContext;
        private IEquipmentManager manager;
        private ILogger logger;
        private IResponseManager responseMgr;
        public EquipmentsController(IEquipmentManager equipmentManager, IResponseManager responseManager, IHubContext<ILisClient> context, ILogger Logger)
        {
            manager = equipmentManager;
            responseMgr = responseManager;
            this.hubContext = context;
            logger = Logger;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="equipment"></param>
        /// <returns></returns>
        [QAuthorize(ModuleName = "Equipments"
        , ModulePermissionTypes = ModulePermissionType.CanAdd
        )]
        public HttpResponseMessage Post(EquipmentMaster equipment)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed, ModelState.Keys);
                }

                var id = manager.Add(equipment);
                APIResponse aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, "Equipment added successfully", null, id);

                return Request.CreateResponse<APIResponse>(HttpStatusCode.OK, aPIResponse);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        [QAuthorize(ModuleName = "Equipments"
                , ModulePermissionTypes = ModulePermissionType.CanEdit
                )]
        public HttpResponseMessage Put(EquipmentMaster equipment)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed, ModelState.Keys);
                }

                manager.Update(equipment);
                APIResponse aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, "Equipment updated successfully", null, null);

                return Request.CreateResponse<APIResponse>(HttpStatusCode.OK, aPIResponse);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        [AllowAnonymous]
        public async Task<dynamic> Get()
        {
            try
            {
                //manager.UpdateHartbit(false);
                //await this.hubContext.Clients.All.CallHeartBeat(true);

                // Wait 10 second to get response from the console.
                //System.Threading.Thread.Sleep(10 * 1000);

                dynamic equipments = new
                {
                    items = new List<EquipmentMaster>()
                };

                foreach (var equipment in manager.Get())
                {
                    equipments.items.Add(equipment);
                }

                return equipments;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        [HttpGet]
        public EquipmentMaster Get(int Id)
        {
            try
            {
                var equipment = manager.Get(Id);

                return equipment;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }


    }
}
