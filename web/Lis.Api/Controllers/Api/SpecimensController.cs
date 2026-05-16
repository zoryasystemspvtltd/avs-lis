using Lis.Api.Providers;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Lis.Api.Controllers.Api
{
    public class SpecimensController : ApiController
    {
        private ISpecimenManager manager;
        private ILogger logger;
        private IResponseManager responseMgr;
        public SpecimensController(ISpecimenManager specimenManager, IResponseManager responseManager, ILogger Logger)
        {
            manager = specimenManager;
            responseMgr = responseManager;
            logger = Logger;
        }
        /// <summary>
        /// Add specimens
        /// </summary>
        /// <param name="specimen"> Specimen object of type LIS.DtoModel</param>
        /// <returns>HttpResponseMessage</returns>
        [QAuthorize(ModuleName = "Masters", ModulePermissionTypes = ModulePermissionType.CanAdd)]
        public HttpResponseMessage Post(HISSpecimenMaster specimen)
        {
            try
            {
                APIResponse aPIResponse = null;

                if (ModelState.IsValid)
                {
                    try
                    {
                        manager.Add(specimen);
                        aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, "Specimen added successfully", null, null);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex.Message);
                        aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, ex.Message, null, ex);
                        return Request.CreateResponse<APIResponse>(HttpStatusCode.InternalServerError, aPIResponse);
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed, ModelState.Keys);
                }

                return Request.CreateResponse<APIResponse>(HttpStatusCode.OK, aPIResponse);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        [QAuthorize(ModuleName = "Masters", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        [HttpPost]
        [ActionName("Put")]
        public HttpResponseMessage Put(HISSpecimenMaster specimen)
        {
            try
            {
                APIResponse aPIResponse = null;

                if (ModelState.IsValid)
                {
                    try
                    {
                        manager.Update(specimen);
                        aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, "Specimen updated successfully", null, null);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex.Message);
                        aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, ex.Message, null, ex);
                        return Request.CreateResponse<APIResponse>(HttpStatusCode.InternalServerError, aPIResponse);
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed, ModelState.Keys);
                }

                return Request.CreateResponse<APIResponse>(HttpStatusCode.OK, aPIResponse);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        [QAuthorize(ModuleName = "Masters", ModulePermissionTypes = ModulePermissionType.CanDelete)]
        [HttpPost]
        [ActionName("Delete")]
        public HttpResponseMessage Delete(HISSpecimenMaster specimen)
        {
            try
            {
                APIResponse aPIResponse = null;

                if (ModelState.IsValid)
                {
                    try
                    {
                        manager.Delete(specimen);
                        aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, "Specimen deleted successfully", null, null);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex.Message);
                        aPIResponse = responseMgr.CreateResponse(HttpStatusCode.OK, ex.Message, null, ex);
                        return Request.CreateResponse<APIResponse>(HttpStatusCode.InternalServerError, aPIResponse);
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed, ModelState.Keys);
                }

                return Request.CreateResponse<APIResponse>(HttpStatusCode.OK, aPIResponse);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        private ListOptions ApiOption
        {
            get
            {
                var apiOption = System.Web.HttpContext.Current.Request.Headers.GetValues("ApiOption");
                if (apiOption == null || apiOption.Count() == 0)
                {
                    return null;
                }

                return JsonConvert.DeserializeObject<ListOptions>(apiOption.FirstOrDefault(),
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                });
            }
        }

        [HttpGet]
        public object Get()
        {
            try
            {
                if (ApiOption != null)
                {
                    return manager.Get(ApiOption);
                }

                return manager.Get().ToList();
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        [AllowAnonymous]
        [HttpGet]
        [ActionName("GetById")]
        public HISSpecimenMaster Get(int Id)
        {
            try
            {
                var specimen = new HISSpecimenMaster();
                try
                {
                    specimen = manager.Get(Id);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }
                return specimen;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        [AllowAnonymous]
        [HttpGet]
        [ActionName("GetByString")]
        public HISSpecimenMaster Get(string Code)
        {
            try
            {
                var specimen = new HISSpecimenMaster();
                try
                {
                    specimen = manager.Get(Code);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }
                return specimen;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }
    }
}

