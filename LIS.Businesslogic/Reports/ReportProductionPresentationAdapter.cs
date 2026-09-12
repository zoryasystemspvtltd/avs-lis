using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.DtoModel.Models.Reports;
using LIS.Logger;
using System;
using System.Diagnostics;
using System.Linq;

namespace LIS.BusinessLogic.Reports
{
    /// <summary>
    /// Phase 4 production presentation boundary. Runs only after eligibility + enrichment.
    /// Never authenticates, authorizes, or decides payment/approval. Failures → Existing Angular.
    /// </summary>
    public sealed class ReportProductionPresentationAdapter : IReportProductionPresentationAdapter
    {
        private readonly IReportTemplateResolver templateResolver;
        private readonly IReportRenderer renderer;
        private readonly GenericUnitOfWork unitOfWork;
        private readonly ILogger logger;

        public ReportProductionPresentationAdapter(
            IReportTemplateResolver templateResolver,
            IReportRenderer renderer,
            GenericUnitOfWork unitOfWork,
            ILogger logger)
        {
            this.templateResolver = templateResolver;
            this.renderer = renderer;
            this.unitOfWork = unitOfWork;
            this.logger = logger;
        }

        public ReportProductionPresentationDto Apply(ReportProductionPresentationContext context)
        {
            var traceId = Guid.NewGuid().ToString("N");
            var presentation = Existing(ReportProductionFallbackReasons.FlagOff, traceId);

            if (context == null || context.ReportData == null || string.IsNullOrWhiteSpace(context.ReportType))
            {
                LogFallback(presentation, null, null);
                return presentation;
            }

            if (!ReportTemplateEngineFeatureFlags.UseDeclarativeRendererForProductionPrint)
            {
                presentation.FallbackReason = ReportProductionFallbackReasons.FlagOff;
                // High-volume path: no per-print Info log when flag is OFF.
                return presentation;
            }

            long? resolveMs = null;
            long? renderMs = null;
            ReportTemplateResolveResultDto resolved = null;

            try
            {
                var resolveRequest = BuildResolveRequest(context);
                var swResolve = Stopwatch.StartNew();
                resolved = templateResolver.Resolve(resolveRequest);
                swResolve.Stop();
                resolveMs = swResolve.ElapsedMilliseconds;

                presentation.ResolveMs = resolveMs;
                presentation.ResolutionSource = resolved?.ResolutionSource;
                presentation.TemplateId = resolved?.TemplateId;
                presentation.VersionId = resolved?.VersionId;

                if (resolved == null || !resolved.Found)
                {
                    return FinalizeFallback(presentation, ReportProductionFallbackReasons.NoTemplate, resolveMs, null);
                }

                if (resolved.UsesBuiltInRenderer ||
                    !string.IsNullOrWhiteSpace(resolved.BuiltInRendererKey) ||
                    string.Equals(resolved.ResolutionSource, ReportTemplateModes.SystemDefault, StringComparison.OrdinalIgnoreCase))
                {
                    return FinalizeFallback(presentation, ReportProductionFallbackReasons.SystemDefault, resolveMs, null);
                }

                if (string.IsNullOrWhiteSpace(resolved.DefinitionJson))
                {
                    return FinalizeFallback(presentation, ReportProductionFallbackReasons.NoPublishedDefinition, resolveMs, null);
                }

                if (resolved.DefinitionJson.IndexOf("\"renderer\":\"builtin\"", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return FinalizeFallback(presentation, ReportProductionFallbackReasons.Builtin, resolveMs, null);
                }

                LogInfo("RTE_PRESENTATION_ATTEMPT ReportType={0} TraceId={1} TemplateId={2} VersionId={3} Source={4} ResolveMs={5}",
                    context.ReportType, traceId, resolved.TemplateId, resolved.VersionId, resolved.ResolutionSource, resolveMs);

                ReportRenderResultDto rendered;
                var swRender = Stopwatch.StartNew();
                try
                {
                    rendered = renderer.Render(new ReportRenderRequestDto
                    {
                        ReportType = context.ReportType,
                        DefinitionJson = resolved.DefinitionJson,
                        ReportData = context.ReportData,
                        AllowDraftDefinition = false
                    });
                }
                finally
                {
                    swRender.Stop();
                    renderMs = swRender.ElapsedMilliseconds;
                }

                presentation.RenderMs = renderMs;

                var integrity = EvaluateIntegrity(rendered);
                if (integrity != null)
                {
                    return FinalizeFallback(presentation, integrity, resolveMs, renderMs);
                }

                presentation.PresentationMode = ReportProductionPresentationModes.Declarative;
                presentation.Html = rendered.Html;
                presentation.Css = rendered.Css;
                presentation.FallbackReason = null;
                presentation.PresentationTraceId = traceId;

                LogInfo("RTE_PRESENTATION_SUCCESS ReportType={0} TraceId={1} TemplateId={2} VersionId={3} Source={4} ResolveMs={5} RenderMs={6}",
                    context.ReportType, traceId, resolved.TemplateId, resolved.VersionId, resolved.ResolutionSource, resolveMs, renderMs);

                return presentation;
            }
            catch (Exception ex)
            {
                presentation.PresentationMode = ReportProductionPresentationModes.Existing;
                presentation.Html = null;
                presentation.Css = null;
                presentation.FallbackReason = ReportProductionFallbackReasons.RenderException;
                presentation.PresentationTraceId = traceId;
                presentation.ResolveMs = resolveMs;
                presentation.RenderMs = renderMs;
                logger?.LogError(
                    "RTE_PRESENTATION_FALLBACK ReportType={0} TraceId={1} Reason={2} ExceptionType={3} ResolveMs={4} RenderMs={5}",
                    context.ReportType,
                    traceId,
                    presentation.FallbackReason,
                    ex.GetType().Name,
                    resolveMs,
                    renderMs);
                return presentation;
            }
        }

        private ReportTemplateResolveRequest BuildResolveRequest(ReportProductionPresentationContext context)
        {
            var request = new ReportTemplateResolveRequest
            {
                ReportType = context.ReportType
            };

            if (string.Equals(context.ReportType, ReportTemplateTypes.Diagnostic, StringComparison.OrdinalIgnoreCase))
            {
                ApplyDiagnosticSpecificContext(context, request);
            }
            else if (string.Equals(context.ReportType, ReportTemplateTypes.Radiology, StringComparison.OrdinalIgnoreCase))
            {
                ApplyRadiologySpecificContext(context, request);
            }

            return request;
        }

        /// <summary>
        /// Print Specific: SaleInvoiceDetail.RequestDetailId → TestId (+ optional TestProfileId).
        /// Print All / missing link: omit Specific (no HISTestCode→Id guess).
        /// </summary>
        private void ApplyDiagnosticSpecificContext(ReportProductionPresentationContext context, ReportTemplateResolveRequest request)
        {
            if (!context.TestRequestDetailId.HasValue || context.TestRequestDetailId.Value <= 0)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(context.InvoiceNo) || unitOfWork == null)
            {
                return;
            }

            try
            {
                var invoice = unitOfWork.GetRepoInstance<SaleInvoice>()
                    .Search(i => i.InvoiceNo == context.InvoiceNo)
                    .FirstOrDefault();
                if (invoice == null)
                {
                    return;
                }

                var lines = unitOfWork.GetRepoInstance<SaleInvoiceDetail>()
                    .Search(d => d.SaleInvoiceId == invoice.Id &&
                              d.IsActive &&
                              d.RequestDetailId == context.TestRequestDetailId.Value)
                    .ToList();

                if (lines.Count != 1)
                {
                    return;
                }

                var line = lines[0];
                if (line.TestId > 0)
                {
                    request.TestId = line.TestId;
                }

                if (line.TestProfileId.HasValue && line.TestProfileId.Value > 0)
                {
                    request.ProfileId = line.TestProfileId.Value;
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning("RTE_PRESENTATION context Diagnostic Specific lookup failed: {0}", ex.GetType().Name);
            }
        }

        /// <summary>
        /// Prefer deterministic invoice-line TestId for the accession's test.
        /// Match by filtering invoice lines' TestIds (not HISTestCode→Id alone). Omit if ambiguous.
        /// </summary>
        private void ApplyRadiologySpecificContext(ReportProductionPresentationContext context, ReportTemplateResolveRequest request)
        {
            if (string.IsNullOrWhiteSpace(context.InvoiceNo) || unitOfWork == null)
            {
                return;
            }

            try
            {
                var hisTestCode = context.RadiologyHisTestCode;
                if (string.IsNullOrWhiteSpace(hisTestCode) &&
                    context.RadiologyRequestId.HasValue &&
                    context.RadiologyRequestId.Value > 0)
                {
                    var radRequest = unitOfWork.GetRepoInstance<RadiologyRequestDetail>()
                        .GetFirstOrDefault(context.RadiologyRequestId.Value);
                    hisTestCode = radRequest?.HISTestCode;
                }

                var invoice = unitOfWork.GetRepoInstance<SaleInvoice>()
                    .Search(i => i.InvoiceNo == context.InvoiceNo)
                    .FirstOrDefault();
                if (invoice == null)
                {
                    return;
                }

                var lines = unitOfWork.GetRepoInstance<SaleInvoiceDetail>()
                    .Search(d => d.SaleInvoiceId == invoice.Id && d.IsActive && d.TestId > 0)
                    .ToList();

                if (lines.Count == 0)
                {
                    return;
                }

                if (lines.Count == 1)
                {
                    request.TestId = lines[0].TestId;
                    if (lines[0].TestProfileId.HasValue && lines[0].TestProfileId.Value > 0)
                    {
                        request.ProfileId = lines[0].TestProfileId.Value;
                    }
                    return;
                }

                // Multiple lines: filter using accession HISTestCode against masters pointed by invoice TestIds only.
                if (string.IsNullOrWhiteSpace(hisTestCode))
                {
                    return;
                }

                var code = hisTestCode.Trim();
                var matchingIds = new System.Collections.Generic.List<int>();
                foreach (var testId in lines.Select(l => l.TestId).Distinct())
                {
                    var master = unitOfWork.GetRepoInstance<HisTestMaster>().GetFirstOrDefault(testId);
                    if (master != null &&
                        string.Equals((master.HISTestCode ?? string.Empty).Trim(), code, StringComparison.OrdinalIgnoreCase))
                    {
                        matchingIds.Add(testId);
                    }
                }

                if (matchingIds.Count != 1)
                {
                    return;
                }

                var matchLines = lines.Where(l => l.TestId == matchingIds[0]).ToList();
                if (matchLines.Count == 0)
                {
                    return;
                }

                request.TestId = matchingIds[0];
                var profileIds = matchLines
                    .Where(l => l.TestProfileId.HasValue && l.TestProfileId.Value > 0)
                    .Select(l => l.TestProfileId.Value)
                    .Distinct()
                    .ToList();
                if (profileIds.Count == 1)
                {
                    request.ProfileId = profileIds[0];
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning("RTE_PRESENTATION context Radiology Specific lookup failed: {0}", ex.GetType().Name);
            }
        }

        private static string EvaluateIntegrity(ReportRenderResultDto rendered)
        {
            if (rendered == null || !rendered.Success)
            {
                return ReportProductionFallbackReasons.ValidationOrRenderFailed;
            }

            if (rendered.UsesBuiltInRenderer)
            {
                return ReportProductionFallbackReasons.Builtin;
            }

            if (string.IsNullOrWhiteSpace(rendered.Html))
            {
                return ReportProductionFallbackReasons.EmptyHtml;
            }

            if (rendered.Warnings != null &&
                rendered.Warnings.Any(w =>
                    w != null &&
                    w.IndexOf("Skipped unsupported component", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return ReportProductionFallbackReasons.UnsupportedComponent;
            }

            if (rendered.Html.IndexOf("rte-report", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return ReportProductionFallbackReasons.IntegrityFailed;
            }

            return null;
        }

        private ReportProductionPresentationDto FinalizeFallback(
            ReportProductionPresentationDto presentation,
            string reason,
            long? resolveMs,
            long? renderMs)
        {
            presentation.PresentationMode = ReportProductionPresentationModes.Existing;
            presentation.Html = null;
            presentation.Css = null;
            presentation.FallbackReason = reason;
            presentation.ResolveMs = resolveMs ?? presentation.ResolveMs;
            presentation.RenderMs = renderMs ?? presentation.RenderMs;
            LogFallback(presentation, resolveMs, renderMs);
            return presentation;
        }

        private static ReportProductionPresentationDto Existing(string reason, string traceId)
        {
            return new ReportProductionPresentationDto
            {
                PresentationMode = ReportProductionPresentationModes.Existing,
                FallbackReason = reason,
                PresentationTraceId = traceId
            };
        }

        private void LogFallback(ReportProductionPresentationDto presentation, long? resolveMs, long? renderMs)
        {
            if (presentation == null)
            {
                return;
            }

            if (string.Equals(presentation.FallbackReason, ReportProductionFallbackReasons.FlagOff, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var levelIsError =
                string.Equals(presentation.FallbackReason, ReportProductionFallbackReasons.RenderException, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(presentation.FallbackReason, ReportProductionFallbackReasons.ValidationOrRenderFailed, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(presentation.FallbackReason, ReportProductionFallbackReasons.UnsupportedComponent, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(presentation.FallbackReason, ReportProductionFallbackReasons.IntegrityFailed, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(presentation.FallbackReason, ReportProductionFallbackReasons.EmptyHtml, StringComparison.OrdinalIgnoreCase);

            var msg = string.Format(
                "RTE_PRESENTATION_FALLBACK TraceId={0} Reason={1} Source={2} TemplateId={3} VersionId={4} ResolveMs={5} RenderMs={6}",
                presentation.PresentationTraceId,
                presentation.FallbackReason,
                presentation.ResolutionSource,
                presentation.TemplateId,
                presentation.VersionId,
                resolveMs ?? presentation.ResolveMs,
                renderMs ?? presentation.RenderMs);

            if (levelIsError)
            {
                logger?.LogError(msg);
            }
            else
            {
                logger?.LogInfo(msg);
            }
        }

        private void LogInfo(string format, params object[] args)
        {
            logger?.LogInfo(format, args);
        }
    }
}
