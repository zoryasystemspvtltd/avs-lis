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
    public class ReportTemplateEnginePhase3Tests : IntegrationTestBase
    {
        private ReportTemplateManager CreateManager()
        {
            return new ReportTemplateManager(Services.Logger, Services.Identity, Services.Uow, new DeclarativeReportRenderer());
        }

        [TestMethod]
        public void FeatureFlag_Still_Off()
        {
            Assert.IsFalse(ReportTemplateEngineFeatureFlags.UseDeclarativeRendererForProductionPrint);
        }

        [TestMethod]
        public void Mode_Defaults_To_SystemDefault()
        {
            var mgr = CreateManager();
            mgr.SetMode(ReportTemplateTypes.Diagnostic, ReportTemplateModes.SystemDefault);
            var mode = mgr.GetMode(ReportTemplateTypes.Diagnostic);
            Assert.AreEqual(ReportTemplateModes.SystemDefault, mode.Mode);
        }

        [TestMethod]
        public void Save_Does_Not_Activate()
        {
            var mgr = CreateManager();
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 6);
            var created = mgr.CreateCustomTemplate(new ReportTemplateDesignerCreateRequest
            {
                Name = "SaveOnly-" + suffix,
                ReportType = ReportTemplateTypes.Diagnostic,
                TemplateCategory = ReportTemplateCategories.CustomGeneric,
                CreateFrom = ReportTemplateCreateFrom.SystemDefault
            });
            Assert.IsFalse(created.IsActivated);

            mgr.SaveDesign(new ReportTemplateDesignerSaveRequest
            {
                TemplateId = created.Id,
                DefinitionJson = ReportTemplateCompatibilityDefinitions.BuildDiagnosticConceptual()
            });

            var ws = mgr.GetDesignerWorkspace(ReportTemplateTypes.Diagnostic);
            var item = ws.CustomGenerics.First(g => g.Id == created.Id);
            Assert.IsFalse(item.IsActivated);
        }

        [TestMethod]
        public void Only_One_Active_Generic()
        {
            var mgr = CreateManager();
            mgr.SetMode(ReportTemplateTypes.Diagnostic, ReportTemplateModes.Custom);
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 6);
            var a = mgr.CreateCustomTemplate(new ReportTemplateDesignerCreateRequest
            {
                Name = "GenA-" + suffix,
                ReportType = ReportTemplateTypes.Diagnostic,
                TemplateCategory = ReportTemplateCategories.CustomGeneric,
                CreateFrom = ReportTemplateCreateFrom.SystemDefault
            });
            var b = mgr.CreateCustomTemplate(new ReportTemplateDesignerCreateRequest
            {
                Name = "GenB-" + suffix,
                ReportType = ReportTemplateTypes.Diagnostic,
                TemplateCategory = ReportTemplateCategories.CustomGeneric,
                CreateFrom = ReportTemplateCreateFrom.SystemDefault
            });
            mgr.ActivateTemplate(a.Id);
            mgr.ActivateTemplate(b.Id);
            var ws = mgr.GetDesignerWorkspace(ReportTemplateTypes.Diagnostic);
            Assert.AreEqual(1, ws.CustomGenerics.Count(g => g.IsActivated));
            Assert.AreEqual(b.Id, ws.ActiveGeneric.Id);
        }

        [TestMethod]
        public void SystemDefault_Is_Locked()
        {
            var mgr = CreateManager();
            var sd = mgr.GetSystemDefaultTemplate(ReportTemplateTypes.Diagnostic);
            Assert.IsTrue(sd.IsLocked);
            Assert.IsFalse(sd.CanEdit);
            try
            {
                mgr.SaveDesign(new ReportTemplateDesignerSaveRequest
                {
                    TemplateId = sd.Id,
                    DefinitionJson = ReportTemplateCompatibilityDefinitions.BuildDiagnosticConceptual()
                });
                Assert.Fail("Expected lock");
            }
            catch (InvalidOperationException)
            {
            }
        }

        [TestMethod]
        public void Switching_Mode_Preserves_Customs()
        {
            var mgr = CreateManager();
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 6);
            var created = mgr.CreateCustomTemplate(new ReportTemplateDesignerCreateRequest
            {
                Name = "Keep-" + suffix,
                ReportType = ReportTemplateTypes.Radiology,
                TemplateCategory = ReportTemplateCategories.CustomGeneric,
                CreateFrom = ReportTemplateCreateFrom.SystemDefault
            });
            mgr.SetMode(ReportTemplateTypes.Radiology, ReportTemplateModes.Custom);
            mgr.ActivateTemplate(created.Id);
            mgr.SetMode(ReportTemplateTypes.Radiology, ReportTemplateModes.SystemDefault);
            var ws = mgr.GetDesignerWorkspace(ReportTemplateTypes.Radiology);
            Assert.IsTrue(ws.CustomGenerics.Any(g => g.Id == created.Id));
            var resolved = mgr.Resolve(new ReportTemplateResolveRequest { ReportType = ReportTemplateTypes.Radiology });
            Assert.AreEqual(ReportTemplateModes.SystemDefault, resolved.ResolutionSource);
        }

        [TestMethod]
        public void Activate_Blocked_When_TemplateValidationFails()
        {
            var mgr = CreateManager();
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 6);
            var created = mgr.CreateCustomTemplate(new ReportTemplateDesignerCreateRequest
            {
                Name = "BadActivate-" + suffix,
                ReportType = ReportTemplateTypes.Diagnostic,
                TemplateCategory = ReportTemplateCategories.CustomGeneric,
                CreateFrom = ReportTemplateCreateFrom.SystemDefault
            });

            // Establish a valid Published version so EnsurePublished skips re-publish.
            var draftId = mgr.ListVersions(created.Id)
                .First(v => v.Status == ReportTemplateVersionStatuses.Draft).Id;
            mgr.Publish(draftId);
            mgr.CreateDraftFromPublished(created.Id);

            // Corrupt working draft outside SaveDesign (simulates invalid definition reaching Activate).
            var versionRepo = Services.Uow.GetRepoInstance<ReportTemplateVersion>();
            var corruptDraft = versionRepo.Search(v =>
                    v.TemplateId == created.Id &&
                    v.Status == ReportTemplateVersionStatuses.Draft)
                .OrderByDescending(v => v.VersionNumber)
                .First();
            corruptDraft.DefinitionJson =
                "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\"," +
                "\"body\":{\"type\":\"UNKNOWN_X\",\"children\":[]}}";
            versionRepo.Update(corruptDraft);

            try
            {
                mgr.ActivateTemplate(created.Id);
                Assert.Fail("Expected activation to be rejected for invalid template.");
            }
            catch (ArgumentException ex)
            {
                StringAssert.Contains(ex.Message, "cannot be activated");
            }

            var ws = mgr.GetDesignerWorkspace(ReportTemplateTypes.Diagnostic);
            var item = ws.CustomGenerics.First(g => g.Id == created.Id);
            Assert.IsFalse(item.IsActivated, "Invalid template must not become active.");
        }

        [TestMethod]
        public void Repeating_Parameter_Group_Renders_DepartmentGroups()
        {
            var def = "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\",\"body\":{\"type\":\"REPEATING_PARAMETER_GROUP\",\"id\":\"rpg\"}}";
            var result = new DeclarativeReportRenderer().Render(new ReportRenderRequestDto
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                DefinitionJson = def,
                ReportData = ReportTemplateSampleDataFactory.CreateDiagnosticSample()
            });
            Assert.IsTrue(result.Success);
            StringAssert.Contains(result.Html, "Glucose Fasting");
            StringAssert.Contains(result.Html, "BIOCHEMISTRY");
        }

        [TestMethod]
        public void Repeating_Parameter_Group_Renders_ProfileGroups()
        {
            var def =
                "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\"," +
                "\"body\":{\"type\":\"REPEATING_PARAMETER_GROUP\",\"id\":\"rpg\"," +
                "\"repeat\":{\"source\":\"ProfileGroups\",\"alias\":\"group\"}}}";
            var result = new DeclarativeReportRenderer().Render(new ReportRenderRequestDto
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                DefinitionJson = def,
                ReportData = ReportTemplateSampleDataFactory.CreateDiagnosticSample()
            });
            Assert.IsTrue(result.Success);
            StringAssert.Contains(result.Html, "Sample Lipid Profile");
            StringAssert.Contains(result.Html, "HDL");
            StringAssert.Contains(result.Html, "LDL");
        }

        [TestMethod]
        public void ProfileGroups_Empty_Collection_Renders_Safely()
        {
            var sample = ReportTemplateSampleDataFactory.CreateDiagnosticSample();
            sample.ProfileGroups.Clear();
            var def =
                "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\"," +
                "\"body\":{\"type\":\"REPEATING_PARAMETER_GROUP\",\"id\":\"rpg\"," +
                "\"repeat\":{\"source\":\"ProfileGroups\",\"alias\":\"group\"}}}";
            var result = new DeclarativeReportRenderer().Render(new ReportRenderRequestDto
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                DefinitionJson = def,
                ReportData = sample
            });
            Assert.IsTrue(result.Success);
            Assert.IsFalse((result.Html ?? string.Empty).Contains("Sample Lipid Profile"));
        }

        [TestMethod]
        public void Design_Field_Catalog_Covers_Dto_Body_Fields()
        {
            var diag = ReportDesignFieldCatalog.List(ReportTemplateTypes.Diagnostic);
            Assert.IsTrue(diag.Any(f => f.Path == "Patient.Name"));
            Assert.IsTrue(diag.Any(f => f.Path == "Parameter.ResultValue"));
            Assert.IsTrue(diag.Any(f => f.Path == "DepartmentGroups" && f.IsCollection));
            Assert.IsTrue(diag.Any(f => f.Path == "ProfileGroups" && f.IsCollection));
            Assert.IsTrue(diag.Any(f => f.Path == "Branding.Address"));
            Assert.IsTrue(diag.Any(f => f.Path == "Branding.Email"));
            Assert.IsTrue(diag.Any(f => f.Path == "Branding.ContactNumbers"));
            Assert.IsTrue(diag.Any(f => f.Path == "Branding.LicenseName"));
            Assert.IsTrue(diag.Any(f => f.Path == "Test.SampleNo"));
            Assert.IsTrue(diag.Any(f => f.Path == "Test.Specimen"));

            var rad = ReportDesignFieldCatalog.List(ReportTemplateTypes.Radiology);
            Assert.IsTrue(rad.Any(f => f.Path == "Radiology.Findings"));
            Assert.IsTrue(rad.Any(f => f.Path == "Doctor.DigitalSignature"));
            Assert.IsTrue(rad.Any(f => f.Path == "Patient.Age"));
            Assert.IsTrue(rad.Any(f => f.Path == "Patient.Gender"));
            Assert.IsTrue(rad.Any(f => f.Path == "Radiology.ReportStatus"));
            Assert.IsTrue(rad.Any(f => f.Path == "Header.AuthorizedOn"));
        }

        [TestMethod]
        public void Style_Properties_Affect_Renderer_Output()
        {
            var def =
                "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\",\"body\":{" +
                "\"type\":\"TEXT\",\"id\":\"t1\",\"binding\":\"Patient.Name\"," +
                "\"style\":{\"width\":\"80%\",\"fontFamily\":\"Arial\",\"fontSize\":\"14pt\"," +
                "\"fontWeight\":\"bold\",\"textAlign\":\"center\",\"border\":\"1px solid #000\"," +
                "\"marginTop\":\"4mm\"}}}";
            var result = new DeclarativeReportRenderer().Render(new ReportRenderRequestDto
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                DefinitionJson = def,
                ReportData = ReportTemplateSampleDataFactory.CreateDiagnosticSample()
            });
            Assert.IsTrue(result.Success);
            StringAssert.Contains(result.Html, "Sample Patient");
            StringAssert.Contains(result.Html, "width:80%");
            StringAssert.Contains(result.Html, "font-size:14pt");
            StringAssert.Contains(result.Html, "font-weight:bold");
            StringAssert.Contains(result.Html, "text-align:center");
            StringAssert.Contains(result.Html, "border:1px solid #000");
            StringAssert.Contains(result.Html, "margin-top:4mm");
        }

        [TestMethod]
        public void Visibility_Hidden_Omits_Component()
        {
            var def =
                "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\",\"body\":{" +
                "\"type\":\"TEXT\",\"id\":\"hidden\",\"binding\":\"Patient.Name\"," +
                "\"style\":{\"visibility\":\"hidden\"}}}";
            var result = new DeclarativeReportRenderer().Render(new ReportRenderRequestDto
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                DefinitionJson = def,
                ReportData = ReportTemplateSampleDataFactory.CreateDiagnosticSample()
            });
            Assert.IsTrue(result.Success);
            Assert.IsFalse((result.Html ?? string.Empty).Contains("Sample Patient"));
        }

        [TestMethod]
        public void Conditions_Exists_NotEmpty_Equals_NotEquals()
        {
            var sample = ReportTemplateSampleDataFactory.CreateDiagnosticSample();
            var renderer = new DeclarativeReportRenderer();

            var exists = renderer.Render(new ReportRenderRequestDto
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                ReportData = sample,
                DefinitionJson =
                    "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\",\"body\":{" +
                    "\"type\":\"TEXT\",\"binding\":\"Patient.Name\",\"visibleWhen\":{\"op\":\"Exists\",\"binding\":\"Patient.Name\"}}}"
            });
            StringAssert.Contains(exists.Html, "Sample Patient");

            var notEmpty = renderer.Render(new ReportRenderRequestDto
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                ReportData = sample,
                DefinitionJson =
                    "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\",\"body\":{" +
                    "\"type\":\"TEXT\",\"binding\":\"Order.Corporate\",\"visibleWhen\":{\"op\":\"NotEmpty\",\"binding\":\"Order.Corporate\"}}}"
            });
            StringAssert.Contains(notEmpty.Html, "Sample Corporate");

            var equals = renderer.Render(new ReportRenderRequestDto
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                ReportData = sample,
                DefinitionJson =
                    "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\",\"body\":{" +
                    "\"type\":\"TEXT\",\"text\":\"MATCH\",\"visibleWhen\":{\"op\":\"Equals\",\"binding\":\"Order.Status\",\"value\":\"Final\"}}}"
            });
            StringAssert.Contains(equals.Html, "MATCH");

            var notEquals = renderer.Render(new ReportRenderRequestDto
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                ReportData = sample,
                DefinitionJson =
                    "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\",\"body\":{" +
                    "\"type\":\"TEXT\",\"text\":\"HIDDEN\",\"visibleWhen\":{\"op\":\"NotEquals\",\"binding\":\"Order.Status\",\"value\":\"Final\"}}}"
            });
            Assert.IsFalse((notEquals.Html ?? string.Empty).Contains("HIDDEN"));
        }

        [TestMethod]
        public void SystemDefault_Diagnostic_Includes_Core_Body_Bindings()
        {
            var json = ReportTemplateCompatibilityDefinitions.BuildDiagnosticConceptual();
            StringAssert.Contains(json, "Patient.Name");
            StringAssert.Contains(json, "DepartmentGroups");
            StringAssert.Contains(json, "PARAMETER_TABLE");
            StringAssert.Contains(json, "Technician.Name");
            StringAssert.Contains(json, "Doctor.Name");
            var result = new DeclarativeReportRenderer().Render(new ReportRenderRequestDto
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                DefinitionJson = json,
                ReportData = ReportTemplateSampleDataFactory.CreateDiagnosticSample()
            });
            Assert.IsTrue(result.Success);
            StringAssert.Contains(result.Html, "Glucose Fasting");
        }

        [TestMethod]
        public void SystemDefault_Radiology_Includes_Status_And_PatientId()
        {
            var json = ReportTemplateCompatibilityDefinitions.BuildRadiologyConceptual();
            StringAssert.Contains(json, "Radiology.ReportStatus");
            StringAssert.Contains(json, "Patient.PatientId");
            StringAssert.Contains(json, "Radiology.Findings");
        }
    }
}
