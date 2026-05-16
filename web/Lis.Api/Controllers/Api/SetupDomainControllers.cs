using Lis.Api.Providers;
using LIS.BusinessLogic;
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
    [RoutePrefix("api/HisParameterMaster")]
    public class HisParameterMasterController : MasterApiControllerBase<HISParameterMaster>
    {
        public HisParameterMasterController(HisParameterMasterManager manager, IResponseManager responseManager, ILogger logger)
            : base(manager, responseManager, logger, "Masters") { }

        [HttpGet, Route(""), Route("{id:int}")]
        public IHttpActionResult Get(int? id = null) => GetCore(id);

        [HttpGet, Route("GetAll")]
        public IEnumerable<HISParameterMaster> GetAllRecords() => FetchAllActiveCore();
    }

    [RoutePrefix("api/HisParameterRangeMaster")]
    public class HisParameterRangeMasterController : MasterApiControllerBase<HISParameterRangMaster>
    {
        private readonly IHisMasterManager legacyManager;

        public HisParameterRangeMasterController(
            HisParameterRangeCrudManager manager,
            IHisMasterManager legacyManager,
            IResponseManager responseManager,
            ILogger logger)
            : base(manager, responseManager, logger, "Masters")
        {
            this.legacyManager = legacyManager;
        }

        [HttpGet, Route(""), Route("{id:int}")]
        public IHttpActionResult Get(int? id = null) => GetCore(id);

        [HttpGet, Route("GetAll")]
        public IEnumerable<HISParameterRangMaster> GetAllRecords() => FetchAllActiveCore();

        [HttpGet]
        [Route("ByParameter")]
        public IEnumerable<HISParameterRangMaster> GetByParameter(int parameterId)
        {
            try
            {
                return legacyManager.GetRangesByParameterId(parameterId);
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return null;
            }
        }
    }

    [RoutePrefix("api/TestMappingMaster")]
    public class TestMappingMasterController : MasterApiControllerBase<TestMappingMaster>
    {
        public TestMappingMasterController(TestMappingCrudManager manager, IResponseManager responseManager, ILogger logger)
            : base(manager, responseManager, logger, "Masters") { }

        [HttpGet, Route(""), Route("{id:int}")]
        public IHttpActionResult Get(int? id = null) => GetCore(id);

        [HttpGet, Route("GetAll")]
        public IEnumerable<TestMappingMaster> GetAllRecords() => FetchAllActiveCore();
    }

    public class PatientMasterController : ApiController
    {
        private readonly PatientMasterManager manager;
        private readonly IResponseManager responseMgr;
        private readonly ILogger logger;

        public PatientMasterController(PatientMasterManager manager, IResponseManager responseManager, ILogger logger)
        {
            this.manager = manager;
            responseMgr = responseManager;
            this.logger = logger;
        }

        private ListOptions ApiOption
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

        [HttpGet]
        [Route("api/PatientMaster")]
        public ItemList<PatientDetail> Get()
        {
            try { return manager.Get(ApiOption) ?? new ItemList<PatientDetail> { TotalRecord = 0, Items = new List<PatientDetail>() }; }
            catch (Exception e) { logger.LogException(e); return new ItemList<PatientDetail> { TotalRecord = 0, Items = new List<PatientDetail>() }; }
        }

        [HttpGet]
        [Route("api/PatientMaster/{id:long}")]
        public PatientDetail Get(long id)
        {
            try { return manager.GetById(id); }
            catch (Exception e) { logger.LogException(e); return null; }
        }

        [HttpGet]
        [ActionName("GetAll")]
        public IEnumerable<PatientDetail> GetAll()
        {
            try { return manager.GetAllActive(); }
            catch (Exception e) { logger.LogException(e); return null; }
        }

        [QAuthorize(ModuleName = "Masters", ModulePermissionTypes = ModulePermissionType.CanAdd)]
        public HttpResponseMessage Post(PatientDetail item)
        {
            try
            {
                var id = manager.Add(item);
                return Request.CreateResponse(HttpStatusCode.OK, responseMgr.CreateResponse(HttpStatusCode.OK, "Record added successfully", null, id));
            }
            catch (Exception e) { logger.LogException(e); return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message); }
        }

        [HttpPost]
        [ActionName("Put")]
        [QAuthorize(ModuleName = "Masters", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        public HttpResponseMessage Put(PatientDetail item)
        {
            try
            {
                manager.Update(item);
                return Request.CreateResponse(HttpStatusCode.OK, responseMgr.CreateResponse(HttpStatusCode.OK, "Record updated successfully", null, null));
            }
            catch (Exception e) { logger.LogException(e); return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message); }
        }

        [HttpPost]
        [ActionName("Delete")]
        [QAuthorize(ModuleName = "Masters", ModulePermissionTypes = ModulePermissionType.CanDelete)]
        public HttpResponseMessage Delete(PatientDetail item)
        {
            try
            {
                manager.Delete(item);
                return Request.CreateResponse(HttpStatusCode.OK, responseMgr.CreateResponse(HttpStatusCode.OK, "Record deleted successfully", null, null));
            }
            catch (Exception e) { logger.LogException(e); return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message); }
        }
    }

    public class TestParameterCatalogController : ApiController
    {
        private readonly TestParameterCatalogManager manager;
        private readonly ILogger logger;

        public TestParameterCatalogController(TestParameterCatalogManager manager, ILogger logger)
        {
            this.manager = manager;
            this.logger = logger;
        }

        private ListOptions ApiOption
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

        [HttpGet]
        [Route("api/TestParameterCatalog")]
        public ItemList<TestParameter> Get()
        {
            try { return manager.Get(ApiOption) ?? new ItemList<TestParameter> { TotalRecord = 0, Items = new List<TestParameter>() }; }
            catch (Exception e) { logger.LogException(e); return new ItemList<TestParameter> { TotalRecord = 0, Items = new List<TestParameter>() }; }
        }

        [HttpGet]
        [Route("api/TestParameterCatalog/{id:long}")]
        public TestParameter Get(long id)
        {
            try { return manager.GetById(id); }
            catch (Exception e) { logger.LogException(e); return null; }
        }

        [HttpPost]
        [ActionName("Put")]
        [QAuthorize(ModuleName = "Masters", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        public HttpResponseMessage Put(TestParameter item)
        {
            try
            {
                manager.Update(item);
                return Request.CreateResponse(HttpStatusCode.OK, "Record updated successfully");
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }
    }

    public class EquipmentHeartbeatController : ApiController
    {
        private readonly IEquipmentManager equipmentManager;
        private readonly ILogger logger;

        public EquipmentHeartbeatController(IEquipmentManager equipmentManager, ILogger logger)
        {
            this.equipmentManager = equipmentManager;
            this.logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IEnumerable<EquipmentMaster> Get()
        {
            try
            {
                return equipmentManager.Get().Where(e => e.IsActive);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }
    }
}
