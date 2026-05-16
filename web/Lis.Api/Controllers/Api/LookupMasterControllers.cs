using Lis.Api.Providers;
using LIS.BusinessLogic;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Lis.Api.Controllers.Api
{
    [RoutePrefix("api/ReferralDoctor")]
    public class ReferralDoctorController : MasterApiControllerBase<ReferralDoctorMaster>
    {
        public ReferralDoctorController(ReferralDoctorManager manager, IResponseManager responseManager, ILogger logger)
            : base(manager, responseManager, logger, "Masters") { }

        [HttpGet, Route(""), Route("{id:int}")]
        public IHttpActionResult Get(int? id = null) => GetCore(id);

        [HttpGet, Route("GetAll")]
        public IEnumerable<ReferralDoctorMaster> GetAllRecords() => FetchAllActiveCore();
    }

    [RoutePrefix("api/Corporate")]
    public class CorporateController : MasterApiControllerBase<CorporateMaster>
    {
        public CorporateController(CorporateManager manager, IResponseManager responseManager, ILogger logger)
            : base(manager, responseManager, logger, "Masters") { }

        [HttpGet, Route(""), Route("{id:int}")]
        public IHttpActionResult Get(int? id = null) => GetCore(id);

        [HttpGet, Route("GetAll")]
        public IEnumerable<CorporateMaster> GetAllRecords() => FetchAllActiveCore();
    }

    [RoutePrefix("api/TestGroup")]
    public class TestGroupController : MasterApiControllerBase<TestGroupMaster>
    {
        public TestGroupController(TestGroupManager manager, IResponseManager responseManager, ILogger logger)
            : base(manager, responseManager, logger, "Masters") { }

        [HttpGet, Route(""), Route("{id:int}")]
        public IHttpActionResult Get(int? id = null) => GetCore(id);

        [HttpGet, Route("GetAll")]
        public IEnumerable<TestGroupMaster> GetAllRecords() => FetchAllActiveCore();
    }

    [RoutePrefix("api/TestCategory")]
    public class TestCategoryController : MasterApiControllerBase<TestCategoryMaster>
    {
        public TestCategoryController(TestCategoryManager manager, IResponseManager responseManager, ILogger logger)
            : base(manager, responseManager, logger, "Masters") { }

        [HttpGet, Route(""), Route("{id:int}")]
        public IHttpActionResult Get(int? id = null) => GetCore(id);

        [HttpGet, Route("GetAll")]
        public IEnumerable<TestCategoryMaster> GetAllRecords() => FetchAllActiveCore();
    }

    [RoutePrefix("api/Unit")]
    public class UnitController : MasterApiControllerBase<UnitMaster>
    {
        public UnitController(UnitManager manager, IResponseManager responseManager, ILogger logger)
            : base(manager, responseManager, logger, "Masters") { }

        [HttpGet, Route(""), Route("{id:int}")]
        public IHttpActionResult Get(int? id = null) => GetCore(id);

        [HttpGet, Route("GetAll")]
        public IEnumerable<UnitMaster> GetAllRecords() => FetchAllActiveCore();
    }

    [RoutePrefix("api/Method")]
    public class MethodController : MasterApiControllerBase<MethodMaster>
    {
        public MethodController(MethodManager manager, IResponseManager responseManager, ILogger logger)
            : base(manager, responseManager, logger, "Masters") { }

        [HttpGet, Route(""), Route("{id:int}")]
        public IHttpActionResult Get(int? id = null) => GetCore(id);

        [HttpGet, Route("GetAll")]
        public IEnumerable<MethodMaster> GetAllRecords() => FetchAllActiveCore();
    }

    [RoutePrefix("api/SampleType")]
    public class SampleTypeController : MasterApiControllerBase<SampleTypeMaster>
    {
        public SampleTypeController(SampleTypeManager manager, IResponseManager responseManager, ILogger logger)
            : base(manager, responseManager, logger, "Masters") { }

        [HttpGet, Route(""), Route("{id:int}")]
        public IHttpActionResult Get(int? id = null) => GetCore(id);

        [HttpGet, Route("GetAll")]
        public IEnumerable<SampleTypeMaster> GetAllRecords() => FetchAllActiveCore();
    }

    [RoutePrefix("api/Container")]
    public class ContainerController : MasterApiControllerBase<ContainerMaster>
    {
        public ContainerController(ContainerManager manager, IResponseManager responseManager, ILogger logger)
            : base(manager, responseManager, logger, "Masters") { }

        [HttpGet, Route(""), Route("{id:int}")]
        public IHttpActionResult Get(int? id = null) => GetCore(id);

        [HttpGet, Route("GetAll")]
        public IEnumerable<ContainerMaster> GetAllRecords() => FetchAllActiveCore();
    }

    [RoutePrefix("api/TestProfile")]
    public class TestProfileController : MasterApiControllerBase<TestProfileMaster>
    {
        private readonly ITestProfileMasterManager profileManager;

        public TestProfileController(ITestProfileMasterManager manager, IResponseManager responseManager, ILogger logger)
            : base(manager, responseManager, logger, "Masters")
        {
            profileManager = manager;
        }

        [HttpGet, Route(""), Route("{id:int}")]
        public IHttpActionResult Get(int? id = null)
        {
            try
            {
                if (id.HasValue)
                {
                    return Ok(profileManager.GetWithDetails(id.Value));
                }

                return GetCore(null);
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return Ok(new ItemList<TestProfileMaster> { TotalRecord = 0, Items = new List<TestProfileMaster>() });
            }
        }

        [HttpGet, Route("GetAll")]
        public IEnumerable<TestProfileMaster> GetAllRecords() => FetchAllActiveCore();

        [QAuthorize(ModuleName = "Masters", ModulePermissionTypes = ModulePermissionType.CanAdd | ModulePermissionType.CanEdit)]
        public override HttpResponseMessage Post(TestProfileMaster profile)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed, ModelState);
                }

                profileManager.SaveWithDetails(profile, profile?.ProfileDetails);
                var response = ResponseMgr.CreateResponse(HttpStatusCode.OK, "Profile saved successfully", null, profile.Id);
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
