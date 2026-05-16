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
    public abstract class MasterApiControllerBase<T> : ApiController where T : class
    {
        protected IMasterCrudManager<T> Manager;
        protected IResponseManager ResponseMgr;
        protected ILogger Logger;
        protected string ModuleName;

        protected MasterApiControllerBase(
            IMasterCrudManager<T> manager,
            IResponseManager responseManager,
            ILogger logger,
            string moduleName)
        {
            Manager = manager;
            ResponseMgr = responseManager;
            Logger = logger;
            ModuleName = moduleName;
        }

        protected ListOptions ApiOption
        {
            get
            {
                var apiOption = System.Web.HttpContext.Current.Request.Headers.GetValues("ApiOption");
                if (apiOption == null || !apiOption.Any())
                {
                    throw new KeyNotFoundException("Invalid Option specified");
                }

                return JsonConvert.DeserializeObject<ListOptions>(apiOption.FirstOrDefault(),
                    new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            }
        }

        [HttpGet]
        public virtual ItemList<T> Get()
        {
            try
            {
                return Manager.Get(ApiOption);
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return null;
            }
        }

        [HttpGet]
        public virtual T Get(int Id)
        {
            try
            {
                return Manager.GetById(Id);
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return null;
            }
        }

        [HttpGet]
        [ActionName("GetAll")]
        public virtual IEnumerable<T> GetAll()
        {
            try
            {
                return Manager.GetAllActive();
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return null;
            }
        }

        [QAuthorize(ModuleName = "Masters", ModulePermissionTypes = ModulePermissionType.CanAdd)]
        public virtual HttpResponseMessage Post(T item)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed, ModelState);
                }

                var id = Manager.Add(item);
                var response = ResponseMgr.CreateResponse(HttpStatusCode.OK, "Record added successfully", null, id);
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [QAuthorize(ModuleName = "Masters", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        [HttpPost]
        public virtual HttpResponseMessage Put(T item)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed, ModelState);
                }

                Manager.Update(item);
                var response = ResponseMgr.CreateResponse(HttpStatusCode.OK, "Record updated successfully", null, null);
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [QAuthorize(ModuleName = "Masters", ModulePermissionTypes = ModulePermissionType.CanDelete)]
        [HttpPut]
        public virtual HttpResponseMessage Delete(T item)
        {
            try
            {
                Manager.Delete(item);
                var response = ResponseMgr.CreateResponse(HttpStatusCode.OK, "Record deleted successfully", null, null);
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }
    }
}
