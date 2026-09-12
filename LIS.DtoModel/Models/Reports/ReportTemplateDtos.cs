using System.Collections.Generic;

namespace LIS.DtoModel.Models.Reports
{
    public static class ReportTemplateTypes
    {
        public const string Diagnostic = "Diagnostic";
        public const string Radiology = "Radiology";
    }

    public static class ReportTemplateVersionStatuses
    {
        public const string Draft = "Draft";
        public const string Published = "Published";
        public const string Archived = "Archived";
    }

    public static class ReportTemplateScopeTypes
    {
        public const string System = "System";
        public const string Department = "Department";
        public const string Test = "Test";
    }

    public static class ReportTemplateBuiltInKeys
    {
        public const string Diagnostic = "Builtin.Diagnostic";
        public const string Radiology = "Builtin.Radiology";
    }

    public class ReportTemplateDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ReportType { get; set; }
        public string Description { get; set; }
        public string BuiltInRendererKey { get; set; }
        public bool IsSystemDefault { get; set; }
        public bool IsActive { get; set; }
        public int? PublishedVersionNumber { get; set; }
        public int? DraftVersionNumber { get; set; }
        public string CreatedBy { get; set; }
        public System.DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public System.DateTime? ModifiedOn { get; set; }
    }

    public class ReportTemplateVersionDto
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public int VersionNumber { get; set; }
        public string Status { get; set; }
        public string DefinitionJson { get; set; }
        public int SchemaVersion { get; set; }
        public string CreatedBy { get; set; }
        public System.DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public System.DateTime? ModifiedOn { get; set; }
        public string PublishedBy { get; set; }
        public System.DateTime? PublishedOn { get; set; }
        public string ArchivedBy { get; set; }
        public System.DateTime? ArchivedOn { get; set; }
    }

    public class ReportTemplateAssignmentDto
    {
        public int Id { get; set; }
        public string ReportType { get; set; }
        public string ScopeType { get; set; }
        public string DepartmentCode { get; set; }
        public int? TestId { get; set; }
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public bool IsActive { get; set; }
    }

    public class ReportTemplateCreateRequest
    {
        public string Name { get; set; }
        public string ReportType { get; set; }
        public string Description { get; set; }
        /// <summary>Optional initial definition JSON; defaults to builtin stub when BuiltInRendererKey set.</summary>
        public string DefinitionJson { get; set; }
        public string BuiltInRendererKey { get; set; }
        public bool IsSystemDefault { get; set; }
    }

    public class ReportTemplateSaveDraftRequest
    {
        public int TemplateId { get; set; }
        public int? VersionId { get; set; }
        public string DefinitionJson { get; set; }
    }

    public class ReportTemplateAssignmentRequest
    {
        public string ReportType { get; set; }
        public string ScopeType { get; set; }
        public string DepartmentCode { get; set; }
        public int? TestId { get; set; }
        public int TemplateId { get; set; }
    }

    public class ReportTemplateResolveRequest
    {
        public string ReportType { get; set; }
        public string DepartmentCode { get; set; }
        public int? TestId { get; set; }
        /// <summary>Phase 3: optional profile target for Specific resolution.</summary>
        public int? ProfileId { get; set; }
    }

    public class ReportTemplateResolveResultDto
    {
        public bool Found { get; set; }
        public string ResolutionSource { get; set; }
        public int? TemplateId { get; set; }
        public string TemplateName { get; set; }
        public string ReportType { get; set; }
        public string BuiltInRendererKey { get; set; }
        public bool UsesBuiltInRenderer { get; set; }
        public int? VersionId { get; set; }
        public int? VersionNumber { get; set; }
        public string DefinitionJson { get; set; }
        public int? SchemaVersion { get; set; }
        public string Message { get; set; }
    }

    // IReportRenderer / IReportViewer live in ReportTemplateEnginePhase2.cs (Phase 2).
}
