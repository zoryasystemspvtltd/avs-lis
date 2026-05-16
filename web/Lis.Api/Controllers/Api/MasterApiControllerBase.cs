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
                var defaultOption = new ListOptions
                {
                    RecordPerPage = 10,
                    CurrentPage = 1,
                    SortColumnName = "Name",
                    SortDirection = true
                };

                var headers = System.Web.HttpContext.Current?.Request?.Headers;
                if (headers == null)
                {
                    return defaultOption;
                }

                var apiOption = headers.GetValues("ApiOption");
                if (apiOption == null || !apiOption.Any())
                {
                    return defaultOption;
                }

                return JsonConvert.DeserializeObject<ListOptions>(apiOption.FirstOrDefault(),
                    new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }) ?? defaultOption;
            }
        }

        protected IHttpActionResult GetCore(int? id = null)
        {
            try
            {
                if (id.HasValue)
                {
                    return Ok(Manager.GetById(id.Value));
                }

                return Ok(Manager.Get(ApiOption) ?? EmptyList());
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return id.HasValue ? (IHttpActionResult)Ok() : Ok(EmptyList());
            }
        }

        protected IEnumerable<T> FetchAllActiveCore()
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

        private ItemList<T> EmptyList()
        {
            return new ItemList<T> { TotalRecord = 0, Items = new List<T>() };
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
        [ActionName("Put")]
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
        [HttpPost]
        [ActionName("Delete")]
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
