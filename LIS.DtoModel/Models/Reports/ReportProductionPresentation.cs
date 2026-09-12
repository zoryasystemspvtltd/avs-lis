using System;

namespace LIS.DtoModel.Models.Reports
{
    /// <summary>
    /// Phase 4 production presentation switch. Default OFF (fail-closed).
    /// Operational value is read from Web.config appSettings via <see cref="ConfigureReader"/>;
    /// when unconfigured or on any read failure, production declarative print remains disabled.
    /// </summary>
    public static class ReportTemplateEngineFeatureFlags
    {
        public const string AppSettingKey = "ReportTemplate:UseDeclarativeRendererForProductionPrint";

        private static Func<bool> _reader = () => false;

        /// <summary>
        /// When true, production print may attempt declarative Custom templates (see presentation adapter).
        /// Always false unless ops explicitly configure appSettings and Gate 6 is approved.
        /// </summary>
        public static bool UseDeclarativeRendererForProductionPrint
        {
            get
            {
                try
                {
                    return _reader != null && _reader();
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Wire the appSettings (or test) reader once at application start.
        /// Null reader restores fail-closed false.
        /// </summary>
        public static void ConfigureReader(Func<bool> reader)
        {
            _reader = reader ?? (() => false);
        }

        /// <summary>Parse appSettings-style values; anything not explicitly true → false.</summary>
        public static bool ParseAppSettingValue(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            var v = raw.Trim();
            return string.Equals(v, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(v, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(v, "yes", StringComparison.OrdinalIgnoreCase);
        }
    }

    public static class ReportProductionPresentationModes
    {
        public const string Existing = "Existing";
        public const string Declarative = "Declarative";
    }

    public static class ReportProductionFallbackReasons
    {
        public const string FlagOff = "FlagOff";
        public const string SystemDefault = "SystemDefault";
        public const string Builtin = "Builtin";
        public const string NoTemplate = "NoTemplate";
        public const string NoPublishedDefinition = "NoPublishedDefinition";
        public const string ValidationOrRenderFailed = "ValidationOrRenderFailed";
        public const string IntegrityFailed = "IntegrityFailed";
        public const string UnsupportedComponent = "UnsupportedComponent";
        public const string EmptyHtml = "EmptyHtml";
        public const string RenderException = "RenderException";
        public const string ResolveException = "ResolveException";
    }

    /// <summary>Non-PHI presentation payload attached to production print DTOs.</summary>
    public class ReportProductionPresentationDto
    {
        /// <summary>Existing | Declarative</summary>
        public string PresentationMode { get; set; }

        public string Html { get; set; }
        public string Css { get; set; }

        /// <summary>Stable reason code when PresentationMode is Existing after an attempt or decision.</summary>
        public string FallbackReason { get; set; }

        public string PresentationTraceId { get; set; }
        public string ResolutionSource { get; set; }
        public int? TemplateId { get; set; }
        public int? VersionId { get; set; }
        public long? ResolveMs { get; set; }
        public long? RenderMs { get; set; }
    }

    public class ReportProductionPresentationContext
    {
        public string ReportType { get; set; }
        public object ReportData { get; set; }

        /// <summary>Diagnostic Print Specific: selected TestRequestDetail.Id.</summary>
        public long? TestRequestDetailId { get; set; }

        /// <summary>Invoice number for SaleInvoiceDetail context (Diagnostic or Radiology).</summary>
        public string InvoiceNo { get; set; }

        /// <summary>Radiology: request id to load HISTestCode for invoice-line filtering (not a Resolve field).</summary>
        public long? RadiologyRequestId { get; set; }

        /// <summary>Radiology: HISTestCode on the accession (used only to filter invoice-line TestIds).</summary>
        public string RadiologyHisTestCode { get; set; }
    }

    public interface IReportProductionPresentationAdapter
    {
        ReportProductionPresentationDto Apply(ReportProductionPresentationContext context);
    }
}
