using Lis.Api.Providers;
using LIS.BusinessLogic;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Lis.Api.Controllers.Api
{
    public class ReferralDoctorController : MasterApiControllerBase<ReferralDoctorMaster>
    {
        public ReferralDoctorController(ReferralDoctorManager manager, IResponseManager responseManager, ILogger logger)
            : base(manager, responseManager, logger, "Masters") { }
    }

    public class CorporateController : MasterApiControllerBase<CorporateMaster>
    {
        public CorporateController(CorporateManager manager, IResponseManager responseManager, ILogger logger)
            : base(manager, responseManager, logger, "Masters") { }
    }

    public class TestGroupController : MasterApiControllerBase<TestGroupMaster>
    {
        public TestGroupController(TestGroupManager manager, IResponseManager responseManager, ILogger logger)
            : base(manager, responseManager, logger, "Masters") { }
    }

    public class TestCategoryController : MasterApiControllerBase<TestCategoryMaster>
    {
        public TestCategoryController(TestCategoryManager manager, IResponseManager responseManager, ILogger logger)
            : base(manager, responseManager, logger, "Masters") { }
    }

    public class UnitController : MasterApiControllerBase<UnitMaster>
    {
        public UnitController(UnitManager manager, IResponseManager responseManager, ILogger logger)
            : base(manager, responseManager, logger, "Masters") { }
    }

    public class MethodController : MasterApiControllerBase<MethodMaster>
    {
        public MethodController(MethodManager manager, IResponseManager responseManager, ILogger logger)
            : base(manager, responseManager, logger, "Masters") { }
    }

    public class SampleTypeController : MasterApiControllerBase<SampleTypeMaster>
    {
        public SampleTypeController(SampleTypeManager manager, IResponseManager responseManager, ILogger logger)
            : base(manager, responseManager, logger, "Masters") { }
    }

    public class ContainerController : MasterApiControllerBase<ContainerMaster>
    {
        public ContainerController(ContainerManager manager, IResponseManager responseManager, ILogger logger)
            : base(manager, responseManager, logger, "Masters") { }
    }

    public class TestProfileController : MasterApiControllerBase<TestProfileMaster>
    {
        private readonly ITestProfileMasterManager profileManager;

        public TestProfileController(ITestProfileMasterManager manager, IResponseManager responseManager, ILogger logger)
            : base(manager, responseManager, logger, "Masters")
        {
            profileManager = manager;
        }

        [HttpGet]
        public new TestProfileMaster Get(int Id)
        {
            try
            {
                return profileManager.GetWithDetails(Id);
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return null;
            }
        }

        [QAuthorize(ModuleName = "Masters", ModulePermissionTypes = ModulePermissionType.CanAdd | ModulePermissionType.CanEdit)]
        public override HttpResponseMessage Post(TestProfileMaster profile)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Request.CreateResponse(System.Net.HttpStatusCode.PreconditionFailed, ModelState);
                }

                profileManager.SaveWithDetails(profile, profile?.ProfileDetails);
                var response = ResponseMgr.CreateResponse(System.Net.HttpStatusCode.OK, "Profile saved successfully", null, profile.Id);
                return Request.CreateResponse(System.Net.HttpStatusCode.OK, response);
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return Request.CreateResponse(System.Net.HttpStatusCode.InternalServerError, e.Message);
            }
        }

    }
}
