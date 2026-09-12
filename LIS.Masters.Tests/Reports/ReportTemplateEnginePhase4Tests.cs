using LIS.BusinessLogic;
using LIS.BusinessLogic.Reports;
using LIS.DtoModel.Models;
using LIS.DtoModel.Models.Reports;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LIS.Masters.Tests.Reports
{
    [TestClass]
    public class ReportTemplateEnginePhase4Tests : IntegrationTestBase
    {
        [TestInitialize]
        public void ResetFlagReader()
        {
            // Fail-closed default for all tests unless a test explicitly configures a reader.
            ReportTemplateEngineFeatureFlags.ConfigureReader(() => false);
        }

        [TestMethod]
        public void FeatureFlag_Defaults_Off_And_FailClosed()
        {
            Assert.IsFalse(ReportTemplateEngineFeatureFlags.UseDeclarativeRendererForProductionPrint);
            Assert.IsFalse(ReportTemplateEngineFeatureFlags.ParseAppSettingValue(null));
            Assert.IsFalse(ReportTemplateEngineFeatureFlags.ParseAppSettingValue(""));
            Assert.IsFalse(ReportTemplateEngineFeatureFlags.ParseAppSettingValue("false"));
            Assert.IsFalse(ReportTemplateEngineFeatureFlags.ParseAppSettingValue("nope"));
            Assert.IsTrue(ReportTemplateEngineFeatureFlags.ParseAppSettingValue("true"));
            Assert.IsTrue(ReportTemplateEngineFeatureFlags.ParseAppSettingValue("1"));

            ReportTemplateEngineFeatureFlags.ConfigureReader(() => { throw new InvalidOperationException("boom"); });
            Assert.IsFalse(ReportTemplateEngineFeatureFlags.UseDeclarativeRendererForProductionPrint);
        }

        [TestMethod]
        public void Adapter_FlagOff_Returns_Existing_Without_Html()
        {
            var adapter = CreateAdapter();
            var sample = ReportTemplateSampleDataFactory.CreateSample(ReportTemplateTypes.Diagnostic);
            var result = adapter.Apply(new ReportProductionPresentationContext
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                ReportData = sample
            });

            Assert.AreEqual(ReportProductionPresentationModes.Existing, result.PresentationMode);
            Assert.AreEqual(ReportProductionFallbackReasons.FlagOff, result.FallbackReason);
            Assert.IsTrue(string.IsNullOrEmpty(result.Html));
        }

        [TestMethod]
        public void Adapter_FlagOn_SystemDefault_Falls_Back_To_Existing()
        {
            ReportTemplateEngineFeatureFlags.ConfigureReader(() => true);
            var mgr = CreateManager();
            mgr.EnsureSystemDefaults();
            mgr.SetMode(ReportTemplateTypes.Diagnostic, ReportTemplateModes.SystemDefault);

            var adapter = CreateAdapter();
            var sample = ReportTemplateSampleDataFactory.CreateSample(ReportTemplateTypes.Diagnostic);
            var result = adapter.Apply(new ReportProductionPresentationContext
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                ReportData = sample
            });

            Assert.AreEqual(ReportProductionPresentationModes.Existing, result.PresentationMode);
            Assert.IsTrue(
                result.FallbackReason == ReportProductionFallbackReasons.SystemDefault ||
                result.FallbackReason == ReportProductionFallbackReasons.Builtin ||
                result.FallbackReason == ReportProductionFallbackReasons.NoPublishedDefinition,
                result.FallbackReason);
            Assert.IsTrue(string.IsNullOrEmpty(result.Html));
        }

        [TestMethod]
        public void Adapter_FlagOn_CustomGeneric_Successful_Render_Is_Declarative()
        {
            ReportTemplateEngineFeatureFlags.ConfigureReader(() => true);
            var mgr = CreateManager();
            mgr.EnsureSystemDefaults();
            mgr.SetMode(ReportTemplateTypes.Diagnostic, ReportTemplateModes.Custom);

            var created = mgr.CreateCustomTemplate(new ReportTemplateDesignerCreateRequest
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                Name = "P4-Generic-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                CreateFrom = "SystemDefault",
                TemplateCategory = ReportTemplateCategories.CustomGeneric
            });

            // Ensure published declarative definition (compatibility twin is declarative).
            mgr.SaveDesign(new ReportTemplateDesignerSaveRequest
            {
                TemplateId = created.Id,
                Name = created.Name,
                DefinitionJson = ReportTemplateCompatibilityDefinitions.BuildDiagnosticConceptual()
            });
            mgr.ActivateTemplate(created.Id);

            var adapter = CreateAdapter();
            var sample = ReportTemplateSampleDataFactory.CreateSample(ReportTemplateTypes.Diagnostic);
            var result = adapter.Apply(new ReportProductionPresentationContext
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                ReportData = sample
            });

            Assert.AreEqual(ReportProductionPresentationModes.Declarative, result.PresentationMode);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Html));
            Assert.IsTrue(result.Html.IndexOf("rte-report", StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.IsTrue(string.IsNullOrEmpty(result.FallbackReason));
        }

        [TestMethod]
        public void Adapter_UnsupportedComponent_SoftSkip_Falls_Back()
        {
            ReportTemplateEngineFeatureFlags.ConfigureReader(() => true);

            // Bypass Activate validation by rendering a definition with an unknown type through a stub resolver path:
            // Use a custom renderer wrapper that returns Success with unsupported warning.
            var stubRenderer = new StubRenderer(new ReportRenderResultDto
            {
                Success = true,
                UsesBuiltInRenderer = false,
                Html = "<div class=\"rte-report\">partial</div>",
                Css = "",
                Warnings = new List<string> { "Skipped unsupported component at render time: WEIRD" }
            });

            var stubResolver = new StubResolver(new ReportTemplateResolveResultDto
            {
                Found = true,
                ResolutionSource = "CustomGeneric",
                TemplateId = 1,
                VersionId = 1,
                UsesBuiltInRenderer = false,
                DefinitionJson = "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\",\"body\":{\"type\":\"TEXT\",\"text\":\"x\"}}"
            });

            var adapter = new ReportProductionPresentationAdapter(stubResolver, stubRenderer, Services.Uow, Services.Logger);
            var result = adapter.Apply(new ReportProductionPresentationContext
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                ReportData = ReportTemplateSampleDataFactory.CreateSample(ReportTemplateTypes.Diagnostic)
            });

            Assert.AreEqual(ReportProductionPresentationModes.Existing, result.PresentationMode);
            Assert.AreEqual(ReportProductionFallbackReasons.UnsupportedComponent, result.FallbackReason);
            Assert.IsTrue(string.IsNullOrEmpty(result.Html));
        }

        [TestMethod]
        public void Adapter_RenderException_Falls_Back_Existing()
        {
            ReportTemplateEngineFeatureFlags.ConfigureReader(() => true);
            var stubResolver = new StubResolver(new ReportTemplateResolveResultDto
            {
                Found = true,
                ResolutionSource = "CustomGeneric",
                TemplateId = 1,
                VersionId = 1,
                UsesBuiltInRenderer = false,
                DefinitionJson = "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\",\"body\":{\"type\":\"TEXT\",\"text\":\"x\"}}"
            });
            var adapter = new ReportProductionPresentationAdapter(
                stubResolver,
                new ThrowingRenderer(),
                Services.Uow,
                Services.Logger);

            var result = adapter.Apply(new ReportProductionPresentationContext
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                ReportData = ReportTemplateSampleDataFactory.CreateSample(ReportTemplateTypes.Diagnostic)
            });

            Assert.AreEqual(ReportProductionPresentationModes.Existing, result.PresentationMode);
            Assert.AreEqual(ReportProductionFallbackReasons.RenderException, result.FallbackReason);
        }

        [TestMethod]
        public void Preview_And_Activate_Remain_Usable_When_Flag_Reader_True()
        {
            ReportTemplateEngineFeatureFlags.ConfigureReader(() => true);
            var mgr = CreateManager();
            mgr.EnsureSystemDefaults();
            var preview = mgr.PreviewSample(ReportTemplateTypes.Diagnostic);
            Assert.IsTrue(preview.Success);

            var created = mgr.CreateCustomTemplate(new ReportTemplateDesignerCreateRequest
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                Name = "P4-Activate-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                CreateFrom = "SystemDefault",
                TemplateCategory = ReportTemplateCategories.CustomGeneric
            });
            mgr.SaveDesign(new ReportTemplateDesignerSaveRequest
            {
                TemplateId = created.Id,
                Name = created.Name,
                DefinitionJson = ReportTemplateCompatibilityDefinitions.BuildDiagnosticConceptual()
            });
            var activated = mgr.ActivateTemplate(created.Id);
            Assert.IsTrue(activated.IsActivated);
        }

        [TestMethod]
        public void Adapter_PrintAll_Omits_TestId_And_ProfileId()
        {
            ReportTemplateEngineFeatureFlags.ConfigureReader(() => true);
            ReportTemplateResolveRequest captured = null;
            var resolver = new CapturingResolver(req =>
            {
                captured = req;
                return SystemDefaultResolve();
            });
            var adapter = new ReportProductionPresentationAdapter(
                resolver, new DeclarativeReportRenderer(), Services.Uow, Services.Logger);

            adapter.Apply(new ReportProductionPresentationContext
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                ReportData = ReportTemplateSampleDataFactory.CreateSample(ReportTemplateTypes.Diagnostic),
                TestRequestDetailId = null,
                InvoiceNo = "ANY-INVOICE"
            });

            Assert.IsNotNull(captured);
            Assert.AreEqual(ReportTemplateTypes.Diagnostic, captured.ReportType);
            Assert.IsFalse(captured.TestId.HasValue);
            Assert.IsFalse(captured.ProfileId.HasValue);
        }

        [TestMethod]
        public void Adapter_Unresolved_Specific_Context_Does_Not_Guess_TestId()
        {
            ReportTemplateEngineFeatureFlags.ConfigureReader(() => true);
            ReportTemplateResolveRequest captured = null;
            var resolver = new CapturingResolver(req =>
            {
                captured = req;
                return SystemDefaultResolve();
            });
            var adapter = new ReportProductionPresentationAdapter(
                resolver, new DeclarativeReportRenderer(), Services.Uow, Services.Logger);

            adapter.Apply(new ReportProductionPresentationContext
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                ReportData = ReportTemplateSampleDataFactory.CreateSample(ReportTemplateTypes.Diagnostic),
                TestRequestDetailId = 999999001,
                InvoiceNo = "NO-SUCH-INVOICE-" + Guid.NewGuid().ToString("N")
            });

            Assert.IsNotNull(captured);
            Assert.IsFalse(captured.TestId.HasValue, "Must not invent TestId when SaleInvoiceDetail link is missing.");
            Assert.IsFalse(captured.ProfileId.HasValue);
        }

        [TestMethod]
        public void Adapter_TestSpecific_Resolution_Source_Renders_Declarative()
        {
            ReportTemplateEngineFeatureFlags.ConfigureReader(() => true);
            var definition = ReportTemplateCompatibilityDefinitions.BuildDiagnosticConceptual();
            var resolver = new StubResolver(new ReportTemplateResolveResultDto
            {
                Found = true,
                ResolutionSource = ReportTemplateScopeTypes.Test,
                TemplateId = 42,
                VersionId = 7,
                UsesBuiltInRenderer = false,
                DefinitionJson = definition
            });
            var adapter = new ReportProductionPresentationAdapter(
                resolver, new DeclarativeReportRenderer(), Services.Uow, Services.Logger);

            var result = adapter.Apply(new ReportProductionPresentationContext
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                ReportData = ReportTemplateSampleDataFactory.CreateSample(ReportTemplateTypes.Diagnostic)
            });

            Assert.AreEqual(ReportProductionPresentationModes.Declarative, result.PresentationMode);
            Assert.AreEqual(ReportTemplateScopeTypes.Test, result.ResolutionSource);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Html));
        }

        [TestMethod]
        public void Adapter_ProfileSpecific_Resolution_Source_Renders_Declarative()
        {
            ReportTemplateEngineFeatureFlags.ConfigureReader(() => true);
            var definition = ReportTemplateCompatibilityDefinitions.BuildDiagnosticConceptual();
            var resolver = new StubResolver(new ReportTemplateResolveResultDto
            {
                Found = true,
                ResolutionSource = ReportTemplateScopeTypesPhase3.Profile,
                TemplateId = 43,
                VersionId = 8,
                UsesBuiltInRenderer = false,
                DefinitionJson = definition
            });
            var adapter = new ReportProductionPresentationAdapter(
                resolver, new DeclarativeReportRenderer(), Services.Uow, Services.Logger);

            var result = adapter.Apply(new ReportProductionPresentationContext
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                ReportData = ReportTemplateSampleDataFactory.CreateSample(ReportTemplateTypes.Diagnostic)
            });

            Assert.AreEqual(ReportProductionPresentationModes.Declarative, result.PresentationMode);
            Assert.AreEqual(ReportTemplateScopeTypesPhase3.Profile, result.ResolutionSource);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Html));
        }

        [TestMethod]
        public void Adapter_EmptyHtml_Falls_Back_To_Existing()
        {
            ReportTemplateEngineFeatureFlags.ConfigureReader(() => true);
            var adapter = new ReportProductionPresentationAdapter(
                new StubResolver(CustomDeclarativeResolve()),
                new StubRenderer(new ReportRenderResultDto
                {
                    Success = true,
                    UsesBuiltInRenderer = false,
                    Html = "   ",
                    Css = ".x{}"
                }),
                Services.Uow,
                Services.Logger);

            var result = adapter.Apply(new ReportProductionPresentationContext
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                ReportData = ReportTemplateSampleDataFactory.CreateSample(ReportTemplateTypes.Diagnostic)
            });

            Assert.AreEqual(ReportProductionPresentationModes.Existing, result.PresentationMode);
            Assert.AreEqual(ReportProductionFallbackReasons.EmptyHtml, result.FallbackReason);
        }

        [TestMethod]
        public void Adapter_InvalidDefinition_Falls_Back_To_Existing()
        {
            ReportTemplateEngineFeatureFlags.ConfigureReader(() => true);
            var adapter = new ReportProductionPresentationAdapter(
                new StubResolver(new ReportTemplateResolveResultDto
                {
                    Found = true,
                    ResolutionSource = "CustomGeneric",
                    TemplateId = 9,
                    VersionId = 1,
                    UsesBuiltInRenderer = false,
                    DefinitionJson = "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\",\"body\":{\"type\":\"NOT_A_REAL_COMPONENT\"}}"
                }),
                new DeclarativeReportRenderer(),
                Services.Uow,
                Services.Logger);

            var result = adapter.Apply(new ReportProductionPresentationContext
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                ReportData = ReportTemplateSampleDataFactory.CreateSample(ReportTemplateTypes.Diagnostic)
            });

            Assert.AreEqual(ReportProductionPresentationModes.Existing, result.PresentationMode);
            Assert.IsTrue(string.IsNullOrEmpty(result.Html));
            Assert.IsTrue(
                result.FallbackReason == ReportProductionFallbackReasons.RenderException ||
                result.FallbackReason == ReportProductionFallbackReasons.ValidationOrRenderFailed,
                result.FallbackReason);
        }

        [TestMethod]
        public void Adapter_NoPublishedDefinition_Falls_Back_To_Existing()
        {
            ReportTemplateEngineFeatureFlags.ConfigureReader(() => true);
            var adapter = new ReportProductionPresentationAdapter(
                new StubResolver(new ReportTemplateResolveResultDto
                {
                    Found = true,
                    ResolutionSource = "CustomGeneric",
                    TemplateId = 11,
                    VersionId = null,
                    UsesBuiltInRenderer = false,
                    DefinitionJson = null
                }),
                new DeclarativeReportRenderer(),
                Services.Uow,
                Services.Logger);

            var result = adapter.Apply(new ReportProductionPresentationContext
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                ReportData = ReportTemplateSampleDataFactory.CreateSample(ReportTemplateTypes.Diagnostic)
            });

            Assert.AreEqual(ReportProductionPresentationModes.Existing, result.PresentationMode);
            Assert.AreEqual(ReportProductionFallbackReasons.NoPublishedDefinition, result.FallbackReason);
        }

        [TestMethod]
        public void Adapter_Radiology_Unresolved_MultiLine_Without_Code_Omits_Specific()
        {
            ReportTemplateEngineFeatureFlags.ConfigureReader(() => true);
            ReportTemplateResolveRequest captured = null;
            var resolver = new CapturingResolver(req =>
            {
                captured = req;
                return SystemDefaultResolve();
            });
            var adapter = new ReportProductionPresentationAdapter(
                resolver, new DeclarativeReportRenderer(), Services.Uow, Services.Logger);

            // No InvoiceNo → cannot establish Specific; must not invent TestId.
            adapter.Apply(new ReportProductionPresentationContext
            {
                ReportType = ReportTemplateTypes.Radiology,
                ReportData = ReportTemplateSampleDataFactory.CreateSample(ReportTemplateTypes.Radiology),
                InvoiceNo = null,
                RadiologyRequestId = null
            });

            Assert.IsNotNull(captured);
            Assert.AreEqual(ReportTemplateTypes.Radiology, captured.ReportType);
            Assert.IsFalse(captured.TestId.HasValue);
            Assert.IsFalse(captured.ProfileId.HasValue);
        }

        [TestMethod]
        public void WebConfig_Production_Flag_Key_Is_False()
        {
            var path = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Lis.Api.dll.config");
            if (!System.IO.File.Exists(path))
            {
                path = System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\web\Lis.Api\Web.config"));
            }

            Assert.IsTrue(System.IO.File.Exists(path), "Expected Web.config / Lis.Api.dll.config at " + path);
            var text = System.IO.File.ReadAllText(path);
            Assert.IsTrue(
                text.IndexOf("ReportTemplate:UseDeclarativeRendererForProductionPrint\" value=\"false\"", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("ReportTemplate:UseDeclarativeRendererForProductionPrint\" value='false'", StringComparison.OrdinalIgnoreCase) >= 0,
                "Production flag must remain false in config.");
        }

        private static ReportTemplateResolveResultDto SystemDefaultResolve()
        {
            return new ReportTemplateResolveResultDto
            {
                Found = true,
                ResolutionSource = ReportTemplateModes.SystemDefault,
                UsesBuiltInRenderer = true,
                BuiltInRendererKey = ReportTemplateBuiltInKeys.Diagnostic
            };
        }

        private static ReportTemplateResolveResultDto CustomDeclarativeResolve()
        {
            return new ReportTemplateResolveResultDto
            {
                Found = true,
                ResolutionSource = "CustomGeneric",
                TemplateId = 1,
                VersionId = 1,
                UsesBuiltInRenderer = false,
                DefinitionJson = "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\",\"body\":{\"type\":\"TEXT\",\"text\":\"x\"}}"
            };
        }

        private ReportTemplateManager CreateManager()
        {
            return new ReportTemplateManager(Services.Logger, Services.Identity, Services.Uow, new DeclarativeReportRenderer());
        }

        private ReportProductionPresentationAdapter CreateAdapter()
        {
            var mgr = CreateManager();
            return new ReportProductionPresentationAdapter(mgr, new DeclarativeReportRenderer(), Services.Uow, Services.Logger);
        }

        private sealed class StubResolver : LIS.DtoModel.Interfaces.IReportTemplateResolver
        {
            private readonly ReportTemplateResolveResultDto _result;
            public StubResolver(ReportTemplateResolveResultDto result) { _result = result; }
            public ReportTemplateResolveResultDto Resolve(ReportTemplateResolveRequest request) => _result;
        }

        private sealed class CapturingResolver : LIS.DtoModel.Interfaces.IReportTemplateResolver
        {
            private readonly Func<ReportTemplateResolveRequest, ReportTemplateResolveResultDto> _fn;
            public CapturingResolver(Func<ReportTemplateResolveRequest, ReportTemplateResolveResultDto> fn) { _fn = fn; }
            public ReportTemplateResolveResultDto Resolve(ReportTemplateResolveRequest request) => _fn(request);
        }

        private sealed class StubRenderer : IReportRenderer
        {
            private readonly ReportRenderResultDto _result;
            public StubRenderer(ReportRenderResultDto result) { _result = result; }
            public ReportRenderResultDto Render(ReportRenderRequestDto request) => _result;
        }

        private sealed class ThrowingRenderer : IReportRenderer
        {
            public ReportRenderResultDto Render(ReportRenderRequestDto request)
            {
                throw new InvalidOperationException("render failed");
            }
        }
    }
}
