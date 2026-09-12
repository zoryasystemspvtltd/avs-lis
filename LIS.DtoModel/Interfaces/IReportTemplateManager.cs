using LIS.DtoModel.Models.Reports;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface IReportTemplateResolver
    {
        ReportTemplateResolveResultDto Resolve(ReportTemplateResolveRequest request);
    }

    public interface IReportTemplateManager
    {
        IList<ReportTemplateDto> List(string reportType = null);
        ReportTemplateDto Get(int templateId);
        ReportTemplateDto Create(ReportTemplateCreateRequest request);
        ReportTemplateVersionDto GetVersion(int versionId);
        IList<ReportTemplateVersionDto> ListVersions(int templateId);
        ReportTemplateVersionDto SaveDraft(ReportTemplateSaveDraftRequest request);
        ReportTemplateVersionDto CreateDraftFromPublished(int templateId);
        ReportTemplateVersionDto Publish(int versionId);
        ReportTemplateVersionDto Archive(int versionId);
        ReportTemplateAssignmentDto UpsertAssignment(ReportTemplateAssignmentRequest request);
        IList<ReportTemplateAssignmentDto> ListAssignments(string reportType = null);
        void DeactivateAssignment(int assignmentId);
        ReportTemplateResolveResultDto Resolve(ReportTemplateResolveRequest request);
        void EnsureSystemDefaults();

        /// <summary>Phase 2: component palette contract for future designer.</summary>
        IList<ReportComponentDescriptorDto> GetComponentRegistry(string reportType = null);

        /// <summary>Phase 2 Admin preview with controlled sample data (no PHI).</summary>
        ReportRenderResultDto PreviewSample(string reportType);

        /// <summary>Phase 2 Admin preview of a Draft or Published version using sample data.</summary>
        ReportRenderResultDto PreviewVersion(int versionId);

        /// <summary>Phase 2 Admin preview of arbitrary definition JSON using sample data.</summary>
        ReportRenderResultDto PreviewDefinition(string reportType, string definitionJson);

        // ---- Phase 3 designer (Admin) ----
        ReportTemplateModeDto GetMode(string reportType);
        ReportTemplateModeDto SetMode(string reportType, string mode);
        ReportTemplateDesignerListDto GetDesignerWorkspace(string reportType);
        ReportTemplateDesignerItemDto GetSystemDefaultTemplate(string reportType);
        ReportTemplateDesignerItemDto CreateCustomTemplate(ReportTemplateDesignerCreateRequest request);
        ReportTemplateDesignerItemDto SaveDesign(ReportTemplateDesignerSaveRequest request);
        ReportTemplateValidateResultDto ValidateDesign(string reportType, string definitionJson);
        ReportTemplateDesignerItemDto ActivateTemplate(int templateId);
        ReportTemplateDesignerItemDto DeactivateTemplate(int templateId);
        IList<ReportDesignFieldDto> GetDesignFields(string reportType = null);
        IList<ReportTemplateTargetOptionDto> ListTestProfileTargets(string search = null);
    }
}
