using LIS.BusinessLogic;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models.Reports;
using LIS.Logger;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Lis.Api.Providers;

namespace Lis.Api.Controllers.Api
{
    [RoutePrefix("api/ReportTemplateConfiguration")]
    public class ReportTemplateConfigurationController : ApiController
    {
        private readonly IReportTemplateManager templateManager;
        private readonly ILogger logger;

        public ReportTemplateConfigurationController(IReportTemplateManager templateManager, ILogger logger)
        {
            this.templateManager = templateManager;
            this.logger = logger;
        }

        [HttpGet]
        [Route("")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration")]
        public IHttpActionResult List(string reportType = null)
        {
            try
            {
                return Ok(templateManager.List(reportType));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpGet]
        [Route("{id:int}")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration")]
        public IHttpActionResult Get(int id)
        {
            try
            {
                return Ok(templateManager.Get(id));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpPost]
        [Route("")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration", ModulePermissionTypes = ModulePermissionType.CanAdd)]
        public IHttpActionResult Create(ReportTemplateCreateRequest request)
        {
            try
            {
                return Ok(templateManager.Create(request));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpGet]
        [Route("{templateId:int}/versions")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration")]
        public IHttpActionResult ListVersions(int templateId)
        {
            try
            {
                return Ok(templateManager.ListVersions(templateId));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpPost]
        [Route("draft")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        public IHttpActionResult SaveDraft(ReportTemplateSaveDraftRequest request)
        {
            try
            {
                return Ok(templateManager.SaveDraft(request));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpPost]
        [Route("{templateId:int}/draft-from-published")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        public IHttpActionResult CreateDraftFromPublished(int templateId)
        {
            try
            {
                return Ok(templateManager.CreateDraftFromPublished(templateId));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpPost]
        [Route("versions/{versionId:int}/publish")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        public IHttpActionResult Publish(int versionId)
        {
            try
            {
                return Ok(templateManager.Publish(versionId));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpPost]
        [Route("versions/{versionId:int}/archive")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        public IHttpActionResult Archive(int versionId)
        {
            try
            {
                return Ok(templateManager.Archive(versionId));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpGet]
        [Route("assignments")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration")]
        public IHttpActionResult ListAssignments(string reportType = null)
        {
            try
            {
                return Ok(templateManager.ListAssignments(reportType));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpPost]
        [Route("assignments")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        public IHttpActionResult UpsertAssignment(ReportTemplateAssignmentRequest request)
        {
            try
            {
                return Ok(templateManager.UpsertAssignment(request));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpPost]
        [Route("assignments/{id:int}/deactivate")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        public IHttpActionResult DeactivateAssignment(int id)
        {
            try
            {
                templateManager.DeactivateAssignment(id);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpPost]
        [Route("resolve")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration")]
        public IHttpActionResult Resolve(ReportTemplateResolveRequest request)
        {
            try
            {
                return Ok(templateManager.Resolve(request));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpPost]
        [Route("ensure-defaults")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        public IHttpActionResult EnsureDefaults()
        {
            try
            {
                templateManager.EnsureSystemDefaults();
                return Ok(templateManager.List(null));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpGet]
        [Route("components")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration")]
        public IHttpActionResult ListComponents(string reportType = null)
        {
            try
            {
                return Ok(templateManager.GetComponentRegistry(reportType));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        /// <summary>
        /// Admin preview of conceptual declarative template with controlled sample data (no PHI).
        /// Does not affect production print.
        /// </summary>
        [HttpPost]
        [Route("preview/sample")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration")]
        public IHttpActionResult PreviewSample(ReportTemplatePreviewRequest request)
        {
            try
            {
                var type = request?.ReportType ?? ReportTemplateTypes.Diagnostic;
                return Ok(templateManager.PreviewSample(type));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        /// <summary>
        /// Admin preview of Draft/Published version using sample data only.
        /// </summary>
        [HttpPost]
        [Route("preview/version/{versionId:int}")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration")]
        public IHttpActionResult PreviewVersion(int versionId)
        {
            try
            {
                return Ok(templateManager.PreviewVersion(versionId));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        /// <summary>
        /// Admin preview of arbitrary definition JSON with sample data. Validates before render.
        /// </summary>
        [HttpPost]
        [Route("preview/definition")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration")]
        public IHttpActionResult PreviewDefinition(ReportTemplatePreviewRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.DefinitionJson))
                {
                    throw new ArgumentException("DefinitionJson is required.");
                }

                return Ok(templateManager.PreviewDefinition(request.ReportType, request.DefinitionJson));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        // ---- Phase 3 designer APIs (Admin) ----

        [HttpGet]
        [Route("mode")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration")]
        public IHttpActionResult GetMode(string reportType)
        {
            try { return Ok(templateManager.GetMode(reportType)); }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpPost]
        [Route("mode")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        public IHttpActionResult SetMode(ReportTemplateModeDto request)
        {
            try { return Ok(templateManager.SetMode(request?.ReportType, request?.Mode)); }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpGet]
        [Route("workspace")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration")]
        public IHttpActionResult Workspace(string reportType)
        {
            try { return Ok(templateManager.GetDesignerWorkspace(reportType)); }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpGet]
        [Route("system-default")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration")]
        public IHttpActionResult SystemDefault(string reportType)
        {
            try { return Ok(templateManager.GetSystemDefaultTemplate(reportType)); }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpPost]
        [Route("designer/create")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration", ModulePermissionTypes = ModulePermissionType.CanAdd)]
        public IHttpActionResult DesignerCreate(ReportTemplateDesignerCreateRequest request)
        {
            try { return Ok(templateManager.CreateCustomTemplate(request)); }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpPost]
        [Route("designer/save")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        public IHttpActionResult DesignerSave(ReportTemplateDesignerSaveRequest request)
        {
            try { return Ok(templateManager.SaveDesign(request)); }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpPost]
        [Route("designer/validate")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration")]
        public IHttpActionResult DesignerValidate(ReportTemplatePreviewRequest request)
        {
            try { return Ok(templateManager.ValidateDesign(request?.ReportType, request?.DefinitionJson)); }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpPost]
        [Route("designer/{templateId:int}/activate")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        public IHttpActionResult DesignerActivate(int templateId)
        {
            try { return Ok(templateManager.ActivateTemplate(templateId)); }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpPost]
        [Route("designer/{templateId:int}/deactivate")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration", ModulePermissionTypes = ModulePermissionType.CanEdit)]
        public IHttpActionResult DesignerDeactivate(int templateId)
        {
            try { return Ok(templateManager.DeactivateTemplate(templateId)); }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpGet]
        [Route("design-fields")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration")]
        public IHttpActionResult DesignFields(string reportType = null)
        {
            try { return Ok(templateManager.GetDesignFields(reportType)); }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }

        [HttpGet]
        [Route("targets")]
        [QAuthorize(ModuleName = "ReportTemplateConfiguration")]
        public IHttpActionResult Targets(string search = null)
        {
            try { return Ok(templateManager.ListTestProfileTargets(search)); }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
        }
    }
}
