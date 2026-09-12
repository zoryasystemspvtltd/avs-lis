using System.Collections.Generic;

namespace LIS.DtoModel.Models.Reports
{
    // ReportTemplateEngineFeatureFlags lives in ReportProductionPresentation.cs (Phase 4 fail-closed reader).

    public static class ReportTemplateComponentTypes
    {
        public const string Text = "TEXT";
        public const string PatientField = "PATIENT_FIELD";
        public const string OrderField = "ORDER_FIELD";
        public const string InvoiceField = "INVOICE_FIELD";
        public const string VisitField = "VISIT_FIELD";
        public const string TestField = "TEST_FIELD";
        public const string SampleField = "SAMPLE_FIELD";
        public const string ParameterTable = "PARAMETER_TABLE";
        public const string DoctorField = "DOCTOR_FIELD";
        public const string TechnicianField = "TECHNICIAN_FIELD";
        public const string Signature = "SIGNATURE";
        public const string Image = "IMAGE";
        public const string Line = "LINE";
        public const string Section = "SECTION";
        public const string Spacer = "SPACER";
        public const string AccessionField = "ACCESSION_FIELD";
        public const string RadiologyField = "RADIOLOGY_FIELD";
        /// <summary>Phase 3 user-facing Diagnostic repeating group (maps to structured SECTION+repeat).</summary>
        public const string RepeatingParameterGroup = "REPEATING_PARAMETER_GROUP";
        public const string CommentField = "COMMENT_FIELD";
        public const string DoctorApprovalComment = "DOCTOR_APPROVAL_COMMENT";
    }

    public static class ReportTemplateConditionOps
    {
        public const string Exists = "Exists";
        public const string NotEmpty = "NotEmpty";
        public const string EqualsValue = "Equals";
        public const string NotEquals = "NotEquals";
    }

    public static class ReportTemplateRepeatSources
    {
        public const string DepartmentGroups = "DepartmentGroups";
        public const string ProfileGroups = "ProfileGroups";
        public const string Sections = "Sections";
        public const string Parameters = "Parameters";
    }

    public class ReportComponentDescriptorDto
    {
        public string Type { get; set; }
        public string DisplayName { get; set; }
        public string[] ReportTypes { get; set; }
        public string[] AllowedBindings { get; set; }
        public string Category { get; set; }
        public string Notes { get; set; }
    }

    public class ReportRenderRequestDto
    {
        public string ReportType { get; set; }
        public string DefinitionJson { get; set; }
        /// <summary>Already-assembled Diagnostic or Radiology DTO (preview uses sample factory).</summary>
        public object ReportData { get; set; }
        public ReportLayoutConfigurationDto LayoutOverride { get; set; }
        public bool AllowDraftDefinition { get; set; }
    }

    public class ReportRenderResultDto
    {
        public bool Success { get; set; }
        public string ReportType { get; set; }
        public bool UsesBuiltInRenderer { get; set; }
        public string Html { get; set; }
        public string Css { get; set; }
        public string Message { get; set; }
        public IList<string> Warnings { get; set; }
        public string PageSize { get; set; }
        public string Orientation { get; set; }
        public decimal HeaderHeightMm { get; set; }
        public decimal FooterHeightMm { get; set; }
        public decimal LeftMarginMm { get; set; }
        public decimal RightMarginMm { get; set; }
    }

    public class ReportTemplatePreviewRequest
    {
        public string ReportType { get; set; }
        public int? VersionId { get; set; }
        public string DefinitionJson { get; set; }
        /// <summary>Phase 2: always uses controlled sample data (no real PHI).</summary>
        public bool UseSampleData { get; set; } = true;
    }

    /// <summary>
    /// Declarative HTML renderer boundary. Production print does not call this while feature flag is false.
    /// </summary>
    public interface IReportRenderer
    {
        ReportRenderResultDto Render(ReportRenderRequestDto request);
    }

    /// <summary>
    /// Viewer boundary for Admin preview of rendered HTML (not production print).
    /// </summary>
    public interface IReportViewer
    {
        ReportRenderResultDto PreviewSample(string reportType);
        ReportRenderResultDto PreviewVersion(int versionId);
        ReportRenderResultDto PreviewDefinition(string reportType, string definitionJson);
    }
}
