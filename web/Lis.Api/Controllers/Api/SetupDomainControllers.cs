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

        [HttpPost, Route("")]
        public override HttpResponseMessage Post(HISParameterMaster item)
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
            catch (InvalidOperationException ex)
            {
                return Request.CreateResponse(HttpStatusCode.Conflict, new { message = ex.Message });
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [HttpPost, Route("Put")]
        public override HttpResponseMessage Put(HISParameterMaster item)
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
            catch (InvalidOperationException ex)
            {
                return Request.CreateResponse(HttpStatusCode.Conflict, new { message = ex.Message });
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [HttpPost, Route("Delete")]
        public override HttpResponseMessage Delete(HISParameterMaster item) => base.Delete(item);
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
        [Route("NextRangeCode")]
        public IHttpActionResult GetNextRangeCode()
        {
            try
            {
                var mgr = Manager as HisParameterRangeCrudManager;
                var code = mgr != null ? mgr.GenerateNextRangeCode() : string.Empty;
                return Ok(new { rangeCode = code });
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return Ok(new { rangeCode = string.Empty });
            }
        }

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

        [HttpPost, Route("")]
        public override HttpResponseMessage Post(HISParameterRangMaster item)
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
            catch (InvalidOperationException ex)
            {
                return Request.CreateResponse(HttpStatusCode.Conflict, new { message = ex.Message });
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [HttpPost, Route("Put")]
        public override HttpResponseMessage Put(HISParameterRangMaster item)
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
            catch (InvalidOperationException ex)
            {
                return Request.CreateResponse(HttpStatusCode.Conflict, new { message = ex.Message });
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [HttpPost, Route("Delete")]
        public override HttpResponseMessage Delete(HISParameterRangMaster item) => base.Delete(item);
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

        [HttpPost, Route("")]
        public override HttpResponseMessage Post(TestMappingMaster item) => base.Post(item);

        [HttpPost, Route("Put")]
        public override HttpResponseMessage Put(TestMappingMaster item) => base.Put(item);

        [HttpPost, Route("Delete")]
        public override HttpResponseMessage Delete(TestMappingMaster item) => base.Delete(item);
    }

    [RoutePrefix("api/TestParameterMappingMaster")]
    public class TestParameterMappingMasterController : MasterApiControllerBase<TestParameterMappingMaster>
    {
        public TestParameterMappingMasterController(TestParameterMappingCrudManager manager, IResponseManager responseManager, ILogger logger)
            : base(manager, responseManager, logger, "Masters") { }

        [HttpGet, Route(""), Route("{id:int}")]
        public IHttpActionResult Get(int? id = null) => GetCore(id);

        [HttpPost, Route("")]
        public override HttpResponseMessage Post(TestParameterMappingMaster item)
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
            catch (InvalidOperationException ex)
            {
                return Request.CreateResponse(HttpStatusCode.Conflict, new { message = ex.Message });
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [HttpPost, Route("Put")]
        public override HttpResponseMessage Put(TestParameterMappingMaster item)
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
            catch (InvalidOperationException ex)
            {
                return Request.CreateResponse(HttpStatusCode.Conflict, new { message = ex.Message });
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [HttpPost, Route("Delete")]
        public override HttpResponseMessage Delete(TestParameterMappingMaster item) => base.Delete(item);
    }

    [RoutePrefix("api/PatientMaster")]
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
        [Route("")]
        [QAuthorize(ModuleName = "PatientDetails", AlternateModuleName = "Masters",
            ModulePermissionTypes = ModulePermissionType.CanView)]
        public ItemList<PatientDetail> Get()
        {
            try { return manager.Get(ApiOption) ?? new ItemList<PatientDetail> { TotalRecord = 0, Items = new List<PatientDetail>() }; }
            catch (Exception e) { logger.LogException(e); return new ItemList<PatientDetail> { TotalRecord = 0, Items = new List<PatientDetail>() }; }
        }

        [HttpGet]
        [Route("{id:long}")]
        [QAuthorize(ModuleName = "PatientDetails", AlternateModuleName = "Masters",
            ModulePermissionTypes = ModulePermissionType.CanView)]
        public PatientDetail Get(long id)
        {
            try { return manager.GetById(id); }
            catch (Exception e) { logger.LogException(e); return null; }
        }

        [HttpGet]
        [Route("NextPatientId")]
        [QAuthorize(ModuleName = "PatientDetails", AlternateModuleName = "Masters",
            ModulePermissionTypes = ModulePermissionType.CanView | ModulePermissionType.CanAdd)]
        public IHttpActionResult NextPatientId()
        {
            try { return Ok(new { patientId = manager.GenerateNextPatientId() }); }
            catch (Exception e) { logger.LogException(e); return Ok(new { patientId = "" }); }
        }

        [HttpGet]
        [Route("NextMrNo")]
        [QAuthorize(ModuleName = "PatientDetails", AlternateModuleName = "Masters",
            ModulePermissionTypes = ModulePermissionType.CanView | ModulePermissionType.CanAdd)]
        public IHttpActionResult NextMrNo()
        {
            try { return Ok(new { mrNo = manager.GenerateNextMrNo() }); }
            catch (Exception e) { logger.LogException(e); return Ok(new { mrNo = "" }); }
        }

        [HttpGet]
        [Route("NextVisitId")]
        [QAuthorize(ModuleName = "PatientDetails", AlternateModuleName = "Masters",
            ModulePermissionTypes = ModulePermissionType.CanView | ModulePermissionType.CanAdd)]
        public IHttpActionResult NextVisitId()
        {
            try { return Ok(new { visitId = manager.GenerateNextVisitId() }); }
            catch (Exception e) { logger.LogException(e); return Ok(new { visitId = "" }); }
        }

        [HttpGet]
        [Route("GetAll")]
        [QAuthorize(ModuleName = "PatientDetails", AlternateModuleName = "Masters,SaleInvoices",
            ModulePermissionTypes = ModulePermissionType.CanView)]
        public IEnumerable<PatientDetail> GetAll()
        {
            try { return manager.GetAllActive(); }
            catch (Exception e) { logger.LogException(e); return null; }
        }

        [HttpPost]
        [Route("")]
        [QAuthorize(ModuleName = "PatientDetails", AlternateModuleName = "Masters",
            ModulePermissionTypes = ModulePermissionType.CanAdd)]
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
        [QAuthorize(ModuleName = "PatientDetails", AlternateModuleName = "Masters",
            ModulePermissionTypes = ModulePermissionType.CanEdit)]
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
        [QAuthorize(ModuleName = "PatientDetails", AlternateModuleName = "Masters",
            ModulePermissionTypes = ModulePermissionType.CanDelete)]
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

    [RoutePrefix("api/EquipmentHeartbeat")]
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
        [Route("")]
        public IEnumerable<EquipmentMaster> Get()
        {
            try
            {
                return equipmentManager.Get()
                    .Where(e => e.IsActive)
                    .OrderBy(e => e.Name);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }
    }

    [RoutePrefix("api/PatientVisit")]
    public class PatientVisitController : ApiController
    {
        private readonly PatientVisitManager manager;
        private readonly IResponseManager responseMgr;
        private readonly ILogger logger;

        public PatientVisitController(PatientVisitManager manager, IResponseManager responseManager, ILogger logger)
        {
            this.manager = manager;
            responseMgr = responseManager;
            this.logger = logger;
        }

        [HttpPost]
        [Route("StartVisit/{patientId:long}")]
        [QAuthorize(ModuleName = "PatientDetails", AlternateModuleName = "Masters",
            ModulePermissionTypes = ModulePermissionType.CanAdd | ModulePermissionType.CanEdit)]
        public HttpResponseMessage StartVisit(long patientId)
        {
            try
            {
                var visit = manager.StartVisit(patientId);
                return Request.CreateResponse(
                    HttpStatusCode.OK,
                    responseMgr.CreateResponse(HttpStatusCode.OK, "Visit started successfully", null, visit));
            }
            catch (InvalidOperationException ex)
            {
                return Request.CreateResponse(HttpStatusCode.Conflict, new { message = ex.Message });
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [HttpGet]
        [Route("Current/{patientId:long}")]
        [QAuthorize(ModuleName = "PatientDetails", AlternateModuleName = "Masters,SaleInvoices",
            ModulePermissionTypes = ModulePermissionType.CanView)]
        public PatientVisit GetCurrent(long patientId)
        {
            try { return manager.GetCurrentVisit(patientId); }
            catch (Exception e) { logger.LogException(e); return null; }
        }

        [HttpGet]
        [Route("{patientId:long}")]
        [QAuthorize(ModuleName = "PatientDetails", AlternateModuleName = "Masters,SaleInvoices",
            ModulePermissionTypes = ModulePermissionType.CanView)]
        public IEnumerable<PatientVisitHistoryItem> GetHistory(long patientId)
        {
            try { return manager.GetVisitHistory(patientId) ?? Enumerable.Empty<PatientVisitHistoryItem>(); }
            catch (Exception e) { logger.LogException(e); return Enumerable.Empty<PatientVisitHistoryItem>(); }
        }
    }
}
