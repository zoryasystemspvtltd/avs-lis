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
    public class DepartmentController : ApiController
    {
        private readonly IDepartmentManager manager;
        private readonly IResponseManager responseMgr;
        private readonly ILogger logger;

        public DepartmentController(IDepartmentManager departmentManager, IResponseManager responseManager, ILogger Logger)
        {
            manager = departmentManager;
            responseMgr = responseManager;
            logger = Logger;
        }

        private ListOptions ApiOption
        {
            get
            {
                var apiOption = System.Web.HttpContext.Current.Request.Headers.GetValues("ApiOption");
                if (apiOption == null || !apiOption.Any())
                {
                    return null;
                }

                return JsonConvert.DeserializeObject<ListOptions>(apiOption.FirstOrDefault(),
                    new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
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

                return manager.Get();
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        [HttpGet]
        public Departments Get(string Id)
        {
            try
            {
                return manager.Get(Id);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        [QAuthorize(ModuleName = "Masters", ModulePermissionTypes = ModulePermissionType.CanAdd)]
        public HttpResponseMessage Post(Departments department)
        {
            try
            {
                manager.Add(department);
                var response = responseMgr.CreateResponse(HttpStatusCode.OK, "Department added successfully", null, null);
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [QAuthorize(ModuleName = "Masters", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        [HttpPost]
        public HttpResponseMessage Put(Departments department)
        {
            try
            {
                manager.Update(department);
                var response = responseMgr.CreateResponse(HttpStatusCode.OK, "Department updated successfully", null, null);
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [QAuthorize(ModuleName = "Masters", ModulePermissionTypes = ModulePermissionType.CanDelete)]
        [HttpPut]
        public HttpResponseMessage Delete(Departments department)
        {
            try
            {
                manager.Delete(department);
                var response = responseMgr.CreateResponse(HttpStatusCode.OK, "Department deleted successfully", null, null);
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }
    }
}

