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

        private const ModulePermissionType EditTestResultsPermission =
            ModulePermissionType.CanEdit | ModulePermissionType.CanAuthorize;

        [HttpGet]
        [Route("search")]
        [QAuthorize(ModuleName = "Reports", ModulePermissionTypes = EditTestResultsPermission)]
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
        [QAuthorize(ModuleName = "Reports", ModulePermissionTypes = EditTestResultsPermission)]
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
        [QAuthorize(ModuleName = "Reports", ModulePermissionTypes = EditTestResultsPermission)]
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
