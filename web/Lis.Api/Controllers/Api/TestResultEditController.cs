using Lis.Api.Providers;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models.TestResultEdit;
using LIS.Logger;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace Lis.Api.Controllers.Api
{
    [RoutePrefix("api/TestResultEdit")]
    public class TestResultEditController : ApiController
    {
        private readonly ITestResultEditManager manager;
        private readonly ILogger logger;

        public TestResultEditController(ITestResultEditManager manager, ILogger logger)
        {
            this.manager = manager;
            this.logger = logger;
        }

        private TestResultEditSearchOptions SearchOptions
        {
            get
            {
                var headers = HttpContext.Current?.Request?.Headers?.GetValues("ApiOption");
                if (headers == null || !headers.Any())
                {
                    return new TestResultEditSearchOptions();
                }

                return JsonConvert.DeserializeObject<TestResultEditSearchOptions>(headers.FirstOrDefault())
                    ?? new TestResultEditSearchOptions();
            }
        }

        private bool IsAdministrator()
        {
            return HttpContext.Current?.User?.IsInRole("Administrator") == true;
        }

        private const ModulePermissionType ViewTestResultsPermission =
            ModulePermissionType.CanView | ModulePermissionType.CanAdd | ModulePermissionType.CanEdit | ModulePermissionType.CanAuthorize;

        private const ModulePermissionType SaveTestResultsPermission =
            ModulePermissionType.CanAdd | ModulePermissionType.CanEdit | ModulePermissionType.CanAuthorize;

        [HttpGet]
        [Route("search")]
        [QAuthorize(ModuleName = "Reports", AlternateModuleName = "Samples", ModulePermissionTypes = ViewTestResultsPermission)]
        public IHttpActionResult Search()
        {
            try
            {
                return Ok(manager.Search(SearchOptions));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("{sampleNo}")]
        [QAuthorize(ModuleName = "Reports", AlternateModuleName = "Samples", ModulePermissionTypes = ViewTestResultsPermission)]
        public IHttpActionResult Get(string sampleNo)
        {
            try
            {
                return Ok(manager.GetBySampleNo(sampleNo, IsAdministrator()));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [Route("")]
        [QAuthorize(ModuleName = "Reports", AlternateModuleName = "Samples", ModulePermissionTypes = SaveTestResultsPermission)]
        public IHttpActionResult Put(TestResultEditSaveRequest request)
        {
            try
            {
                return Ok(manager.Save(request, IsAdministrator()));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                return BadRequest(ex.Message);
            }
        }
    }
}
