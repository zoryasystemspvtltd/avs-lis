using LIS.BusinessLogic;
using LIS.BusinessLogic.Reports;
using LIS.DtoModel.Models.Reports;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LIS.Masters.Tests.Reports
{
    [TestClass]
    public class ReportTemplateEnginePhase2Tests : IntegrationTestBase
    {
        private readonly DeclarativeReportRenderer renderer = new DeclarativeReportRenderer();

        private ReportTemplateManager CreateManager()
        {
            return new ReportTemplateManager(Services.Logger, Services.Identity, Services.Uow, renderer);
        }

        [TestMethod]
        public void FeatureFlag_ProductionDeclarativePrint_Is_Off()
        {
            Assert.IsFalse(ReportTemplateEngineFeatureFlags.UseDeclarativeRendererForProductionPrint);
        }

        [TestMethod]
        public void Validator_Accepts_Compatibility_Diagnostic_Definition()
        {
            var json = ReportTemplateCompatibilityDefinitions.BuildDiagnosticConceptual();
            ReportTemplateDefinitionValidator.Validate(json, ReportTemplateTypes.Diagnostic);
        }

        [TestMethod]
        public void Validator_Rejects_Unknown_Component()
        {
            var json = "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\",\"body\":{\"type\":\"UNKNOWN_WIDGET\",\"children\":[]}}";
            Expect.Throws<ArgumentException>(() =>
                ReportTemplateDefinitionValidator.Validate(json, ReportTemplateTypes.Diagnostic));
        }

        [TestMethod]
        public void Validator_Rejects_Script()
        {
            var json = "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\",\"body\":{\"type\":\"TEXT\",\"text\":\"<script>alert(1)</script>\"}}";
            Expect.Throws<ArgumentException>(() =>
                ReportTemplateDefinitionValidator.Validate(json, ReportTemplateTypes.Diagnostic));
        }

        [TestMethod]
        public void Validator_Rejects_Javascript_Url()
        {
            var json = "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\",\"body\":{\"type\":\"IMAGE\",\"src\":\"javascript:alert(1)\"}}";
            Expect.Throws<ArgumentException>(() =>
                ReportTemplateDefinitionValidator.Validate(json, ReportTemplateTypes.Diagnostic));
        }

        [TestMethod]
        public void Validator_Rejects_Sqlish_Content()
        {
            var json = "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\",\"body\":{\"type\":\"TEXT\",\"text\":\"select * from Patients\"}}";
            Expect.Throws<ArgumentException>(() =>
                ReportTemplateDefinitionValidator.Validate(json, ReportTemplateTypes.Diagnostic));
        }

        [TestMethod]
        public void Validator_Rejects_Unknown_Binding()
        {
            var json = "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\",\"body\":{\"type\":\"TEXT\",\"binding\":\"Sql.Query\"}}";
            Expect.Throws<ArgumentException>(() =>
                ReportTemplateDefinitionValidator.Validate(json, ReportTemplateTypes.Diagnostic));
        }

        [TestMethod]
        public void Validator_Rejects_Invalid_Condition()
        {
            var json = "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\",\"body\":{\"type\":\"TEXT\",\"binding\":\"Patient.Name\",\"visibleWhen\":{\"op\":\"Eval\",\"binding\":\"Patient.Name\"}}}";
            Expect.Throws<ArgumentException>(() =>
                ReportTemplateDefinitionValidator.Validate(json, ReportTemplateTypes.Diagnostic));
        }

        [TestMethod]
        public void Validator_Rejects_Excessive_Nesting()
        {
            var inner = "{\"type\":\"SECTION\",\"children\":[]}";
            for (var i = 0; i < 20; i++)
            {
                inner = "{\"type\":\"SECTION\",\"children\":[" + inner + "]}";
            }
            var json = "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\",\"body\":" + inner + "}";
            Expect.Throws<ArgumentException>(() =>
                ReportTemplateDefinitionValidator.Validate(json, ReportTemplateTypes.Diagnostic));
        }

        [TestMethod]
        public void Validator_Rejects_Radiology_Component_On_Diagnostic()
        {
            var json = "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\",\"body\":{\"type\":\"ACCESSION_FIELD\",\"binding\":\"Accession.AccessionNo\"}}";
            Expect.Throws<ArgumentException>(() =>
                ReportTemplateDefinitionValidator.Validate(json, ReportTemplateTypes.Diagnostic));
        }

        [TestMethod]
        public void Renderer_Diagnostic_Sample_Contains_Patient_And_Parameters()
        {
            var result = renderer.Render(new ReportRenderRequestDto
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                DefinitionJson = ReportTemplateCompatibilityDefinitions.BuildDiagnosticConceptual(),
                ReportData = ReportTemplateSampleDataFactory.CreateDiagnosticSample()
            });

            Assert.IsTrue(result.Success);
            Assert.IsFalse(result.UsesBuiltInRenderer);
            StringAssert.Contains(result.Html, "Sample Patient");
            StringAssert.Contains(result.Html, "Blood Glucose Fasting");
            StringAssert.Contains(result.Html, "Glucose Fasting");
            StringAssert.Contains(result.Html, "98");
            StringAssert.Contains(result.Html, "70 - 110");
            StringAssert.Contains(result.Html, "H");
            StringAssert.Contains(result.Html, "rte-parameter-table");
            StringAssert.Contains(result.Html, "Dr. Sample Approver");
            StringAssert.Contains(result.Css, "mm");
            Assert.AreEqual("A4", result.PageSize);
        }

        [TestMethod]
        public void Renderer_Radiology_Sample_Contains_Narrative_And_Signature()
        {
            var result = renderer.Render(new ReportRenderRequestDto
            {
                ReportType = ReportTemplateTypes.Radiology,
                DefinitionJson = ReportTemplateCompatibilityDefinitions.BuildRadiologyConceptual(),
                ReportData = ReportTemplateSampleDataFactory.CreateRadiologySample()
            });

            Assert.IsTrue(result.Success);
            StringAssert.Contains(result.Html, "Sample findings narrative");
            StringAssert.Contains(result.Html, "Sample impression");
            StringAssert.Contains(result.Html, "Sample recommendation");
            StringAssert.Contains(result.Html, "Dr. Sample Radiologist");
            StringAssert.Contains(result.Html, "SAMPLE-ACC-001");
        }

        [TestMethod]
        public void Renderer_Empty_Parameter_Collection_Is_Safe()
        {
            var sample = ReportTemplateSampleDataFactory.CreateDiagnosticSample();
            sample.DepartmentGroups[0].Sections[0].Parameters.Clear();
            var result = renderer.Render(new ReportRenderRequestDto
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                DefinitionJson = ReportTemplateCompatibilityDefinitions.BuildDiagnosticConceptual(),
                ReportData = sample
            });
            Assert.IsTrue(result.Success);
            StringAssert.Contains(result.Html, "Blood Glucose Fasting");
        }

        [TestMethod]
        public void Renderer_Condition_Hides_Empty_Recommendation()
        {
            var sample = ReportTemplateSampleDataFactory.CreateRadiologySample();
            sample.Recommendation = null;
            var result = renderer.Render(new ReportRenderRequestDto
            {
                ReportType = ReportTemplateTypes.Radiology,
                DefinitionJson = ReportTemplateCompatibilityDefinitions.BuildRadiologyConceptual(),
                ReportData = sample
            });
            Assert.IsTrue(result.Success);
            Assert.IsFalse((result.Html ?? string.Empty).Contains("Sample recommendation"));
        }

        [TestMethod]
        public void Renderer_Builtin_Definition_Does_Not_Emit_Html()
        {
            var stub = ReportTemplateDefinitionValidator.BuildBuiltinStub(ReportTemplateTypes.Diagnostic, ReportTemplateBuiltInKeys.Diagnostic);
            var result = renderer.Render(new ReportRenderRequestDto
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                DefinitionJson = stub,
                ReportData = ReportTemplateSampleDataFactory.CreateDiagnosticSample()
            });
            Assert.IsTrue(result.UsesBuiltInRenderer);
            Assert.IsNull(result.Html);
        }

        [TestMethod]
        public void Renderer_Encodes_Html_In_Values()
        {
            var sample = ReportTemplateSampleDataFactory.CreateDiagnosticSample();
            sample.Header.PatientName = "<b>Evil</b>";
            var result = renderer.Render(new ReportRenderRequestDto
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                DefinitionJson = ReportTemplateCompatibilityDefinitions.BuildDiagnosticConceptual(),
                ReportData = sample
            });
            StringAssert.Contains(result.Html, "&lt;b&gt;Evil&lt;/b&gt;");
            Assert.IsFalse(result.Html.Contains("<b>Evil</b>"));
        }

        [TestMethod]
        public void ComponentRegistry_Lists_Diagnostic_And_Radiology()
        {
            var all = ReportComponentRegistry.List(null);
            Assert.IsTrue(all.Any(c => c.Type == ReportTemplateComponentTypes.ParameterTable));
            Assert.IsTrue(all.Any(c => c.Type == ReportTemplateComponentTypes.AccessionField));
            var diag = ReportComponentRegistry.List(ReportTemplateTypes.Diagnostic);
            Assert.IsFalse(diag.Any(c => c.Type == ReportTemplateComponentTypes.AccessionField));
        }

        [TestMethod]
        public void Manager_PreviewSample_Diagnostic_Works()
        {
            var mgr = CreateManager();
            var result = mgr.PreviewSample(ReportTemplateTypes.Diagnostic);
            Assert.IsTrue(result.Success);
            StringAssert.Contains(result.Html, "Sample Patient");
        }

        [TestMethod]
        public void Manager_PreviewSample_Radiology_Works()
        {
            var mgr = CreateManager();
            var result = mgr.PreviewSample(ReportTemplateTypes.Radiology);
            Assert.IsTrue(result.Success);
            StringAssert.Contains(result.Html, "Findings");
        }

        [TestMethod]
        public void Manager_Draft_Is_Not_Production_Resolved()
        {
            var mgr = CreateManager();
            mgr.EnsureSystemDefaults();
            mgr.SetMode(ReportTemplateTypes.Diagnostic, ReportTemplateModes.Custom);
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 6);
            var tpl = mgr.Create(new ReportTemplateCreateRequest
            {
                Name = "DraftOnly-" + suffix,
                ReportType = ReportTemplateTypes.Diagnostic,
                DefinitionJson = ReportTemplateCompatibilityDefinitions.BuildDiagnosticConceptual()
            });
            // Mark as custom specific target via assignment without publish
            var testId = 910000 + Math.Abs(suffix.GetHashCode() % 1000);
            mgr.UpsertAssignment(new ReportTemplateAssignmentRequest
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                ScopeType = ReportTemplateScopeTypes.Test,
                TestId = testId,
                TemplateId = tpl.Id
            });

            var resolved = mgr.Resolve(new ReportTemplateResolveRequest
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                TestId = testId
            });

            Assert.IsTrue(resolved.Found);
            // Draft-only: no published version on the assigned template
            Assert.IsNull(resolved.VersionId);
        }

        [TestMethod]
        public void Manager_PreviewVersion_Allows_Draft()
        {
            var mgr = CreateManager();
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 6);
            var tpl = mgr.Create(new ReportTemplateCreateRequest
            {
                Name = "DraftPreview-" + suffix,
                ReportType = ReportTemplateTypes.Diagnostic,
                DefinitionJson = ReportTemplateCompatibilityDefinitions.BuildDiagnosticConceptual()
            });
            var draft = mgr.ListVersions(tpl.Id).First(v => v.Status == ReportTemplateVersionStatuses.Draft);
            var preview = mgr.PreviewVersion(draft.Id);
            Assert.IsTrue(preview.Success);
            StringAssert.Contains(preview.Html, "Sample Patient");
        }

        [TestMethod]
        public void Manager_Archive_Not_Previewable()
        {
            var mgr = CreateManager();
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 6);
            var tpl = mgr.Create(new ReportTemplateCreateRequest
            {
                Name = "ArchPreview-" + suffix,
                ReportType = ReportTemplateTypes.Diagnostic,
                DefinitionJson = ReportTemplateCompatibilityDefinitions.BuildDiagnosticConceptual()
            });
            var draft = mgr.ListVersions(tpl.Id).First(v => v.Status == ReportTemplateVersionStatuses.Draft);
            mgr.Publish(draft.Id);
            var published = mgr.ListVersions(tpl.Id).First(v => v.Status == ReportTemplateVersionStatuses.Published);
            // Create new draft then archive old published via publish cycle
            var draft2 = mgr.CreateDraftFromPublished(tpl.Id);
            mgr.Publish(draft2.Id);
            var archived = mgr.ListVersions(tpl.Id).First(v => v.Status == ReportTemplateVersionStatuses.Archived);
            Expect.Throws<ArgumentException>(() => mgr.PreviewVersion(archived.Id));
        }

        [TestMethod]
        public void Compatibility_Diagnostic_And_Radiology_Are_Distinct()
        {
            var d = ReportTemplateCompatibilityDefinitions.BuildDiagnosticConceptual();
            var r = ReportTemplateCompatibilityDefinitions.BuildRadiologyConceptual();
            StringAssert.Contains(d, "PARAMETER_TABLE");
            StringAssert.Contains(r, "Radiology.Findings");
            Assert.IsFalse(d.Contains("Radiology.Findings"));
        }
    }

    internal static class Expect
    {
        public static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
                Assert.Fail("Expected " + typeof(T).Name);
            }
            catch (T)
            {
                // expected
            }
        }
    }
}
