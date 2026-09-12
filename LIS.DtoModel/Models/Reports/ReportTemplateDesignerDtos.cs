using System.Collections.Generic;

namespace LIS.DtoModel.Models.Reports
{
    public static class ReportTemplateModes
    {
        public const string SystemDefault = "SystemDefault";
        public const string Custom = "Custom";
    }

    public static class ReportTemplateCategories
    {
        public const string SystemDefault = "SystemDefault";
        public const string CustomGeneric = "CustomGeneric";
        public const string Specific = "Specific";
    }

    public static class ReportTemplateCreateFrom
    {
        public const string SystemDefault = "SystemDefault";
        public const string Blank = "Blank";
    }

    /// <summary>Phase 3 extends Phase 1 scopes with Profile. Department kept for legacy rows only.</summary>
    public static class ReportTemplateScopeTypesPhase3
    {
        public const string Profile = "Profile";
    }

    public class ReportTemplateModeDto
    {
        public string ReportType { get; set; }
        public string Mode { get; set; }
    }

    public class ReportTemplateDesignerListDto
    {
        public string ReportType { get; set; }
        public string Mode { get; set; }
        public ReportTemplateDesignerItemDto SystemDefault { get; set; }
        public ReportTemplateDesignerItemDto ActiveGeneric { get; set; }
        public IList<ReportTemplateDesignerItemDto> CustomGenerics { get; set; }
        public IList<ReportTemplateDesignerItemDto> SpecificTemplates { get; set; }
    }

    public class ReportTemplateDesignerItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ReportType { get; set; }
        public string TemplateCategory { get; set; }
        public bool IsSystemDefault { get; set; }
        public bool IsLocked { get; set; }
        public bool IsActivated { get; set; }
        public int? TargetTestId { get; set; }
        public int? TargetProfileId { get; set; }
        public string TargetLabel { get; set; }
        public string DefinitionJson { get; set; }
        public bool CanEdit { get; set; }
        public bool CanActivate { get; set; }
        public bool CanDeactivate { get; set; }
        public string ModifiedBy { get; set; }
        public System.DateTime? ModifiedOn { get; set; }
    }

    public class ReportTemplateDesignerCreateRequest
    {
        public string ReportType { get; set; }
        public string Name { get; set; }
        public string CreateFrom { get; set; }
        /// <summary>CustomGeneric | Specific</summary>
        public string TemplateCategory { get; set; }
        public int? TargetTestId { get; set; }
        public int? TargetProfileId { get; set; }
    }

    public class ReportTemplateDesignerSaveRequest
    {
        public int TemplateId { get; set; }
        public string Name { get; set; }
        public string DefinitionJson { get; set; }
    }

    public class ReportTemplateValidateResultDto
    {
        public bool IsValid { get; set; }
        public IList<string> Errors { get; set; }
        public IList<string> Warnings { get; set; }
    }

    public class ReportDesignFieldDto
    {
        public string Path { get; set; }
        public string Label { get; set; }
        public string Group { get; set; }
        public string ReportType { get; set; }
        public bool IsCollection { get; set; }
        public bool DisplayedInCurrentAngular { get; set; }
        public string Notes { get; set; }
    }

    public class ReportTemplateTargetOptionDto
    {
        public string TargetType { get; set; }
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Label { get; set; }
    }
}
