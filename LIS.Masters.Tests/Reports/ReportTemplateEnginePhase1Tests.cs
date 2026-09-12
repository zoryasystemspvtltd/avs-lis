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
    public class ReportTemplateEnginePhase1Tests : IntegrationTestBase
    {
        private ReportTemplateManager CreateManager()
        {
            return new ReportTemplateManager(Services.Logger, Services.Identity, Services.Uow, new DeclarativeReportRenderer());
        }

        [TestMethod]
        public void EnsureSystemDefaults_Creates_Builtin_Diagnostic_And_Radiology()
        {
            var mgr = CreateManager();
            mgr.EnsureSystemDefaults();
            var list = mgr.List(null);
            Assert.IsTrue(list.Any(t => t.ReportType == ReportTemplateTypes.Diagnostic && t.IsSystemDefault));
            Assert.IsTrue(list.Any(t => t.ReportType == ReportTemplateTypes.Radiology && t.IsSystemDefault));
        }

        [TestMethod]
        public void Resolver_SystemDefault_When_No_Department_Or_Test()
        {
            var mgr = CreateManager();
            mgr.EnsureSystemDefaults();
            // Phase 3 mode must be SystemDefault for factory fallback (DB may retain Custom from prior designer tests).
            mgr.SetMode(ReportTemplateTypes.Diagnostic, ReportTemplateModes.SystemDefault);
            var result = mgr.Resolve(new ReportTemplateResolveRequest { ReportType = ReportTemplateTypes.Diagnostic });
            Assert.IsTrue(result.Found);
            Assert.IsTrue(result.UsesBuiltInRenderer);
            Assert.AreEqual(ReportTemplateTypes.Diagnostic, result.ReportType);
        }

        [TestMethod]
        public void Resolver_Phase3_Specific_Over_Generic_Over_SystemDefault_When_Custom_Mode()
        {
            var mgr = CreateManager();
            mgr.EnsureSystemDefaults();
            mgr.SetMode(ReportTemplateTypes.Diagnostic, ReportTemplateModes.Custom);
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 6);

            var generic = mgr.CreateCustomTemplate(new ReportTemplateDesignerCreateRequest
            {
                Name = "Generic-" + suffix,
                ReportType = ReportTemplateTypes.Diagnostic,
                TemplateCategory = ReportTemplateCategories.CustomGeneric,
                CreateFrom = ReportTemplateCreateFrom.SystemDefault
            });
            mgr.ActivateTemplate(generic.Id);

            var testId = 900000 + Math.Abs(suffix.GetHashCode() % 100000);
            var specific = mgr.CreateCustomTemplate(new ReportTemplateDesignerCreateRequest
            {
                Name = "Specific-" + suffix,
                ReportType = ReportTemplateTypes.Diagnostic,
                TemplateCategory = ReportTemplateCategories.Specific,
                CreateFrom = ReportTemplateCreateFrom.SystemDefault,
                TargetTestId = testId
            });
            mgr.ActivateTemplate(specific.Id);

            var byTest = mgr.Resolve(new ReportTemplateResolveRequest
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                TestId = testId
            });
            Assert.AreEqual(ReportTemplateScopeTypes.Test, byTest.ResolutionSource);
            Assert.AreEqual(specific.Id, byTest.TemplateId);

            var byGeneric = mgr.Resolve(new ReportTemplateResolveRequest
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                TestId = testId + 1
            });
            Assert.AreEqual("CustomGeneric", byGeneric.ResolutionSource);
            Assert.AreEqual(generic.Id, byGeneric.TemplateId);

            mgr.SetMode(ReportTemplateTypes.Diagnostic, ReportTemplateModes.SystemDefault);
            var bySystemMode = mgr.Resolve(new ReportTemplateResolveRequest
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                TestId = testId
            });
            Assert.AreEqual(ReportTemplateModes.SystemDefault, bySystemMode.ResolutionSource);
            Assert.IsTrue(bySystemMode.Found);
        }

        [TestMethod]
        public void Publish_Archives_Prior_Published_And_Draft_Not_Directly_Editable_After_Publish()
        {
            var mgr = CreateManager();
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 6);
            var tpl = mgr.Create(new ReportTemplateCreateRequest
            {
                Name = "Lifecycle-" + suffix,
                ReportType = ReportTemplateTypes.Radiology,
                DefinitionJson = ReportTemplateDefinitionValidator.BuildBuiltinStub(ReportTemplateTypes.Radiology, null)
                    .Replace("\"renderer\":\"builtin\"", "\"renderer\":\"declarative\"")
            });
            var v1 = mgr.ListVersions(tpl.Id).First(v => v.Status == ReportTemplateVersionStatuses.Draft);
            mgr.Publish(v1.Id);
            var published = mgr.GetVersion(v1.Id);
            Assert.AreEqual(ReportTemplateVersionStatuses.Published, published.Status);

            try
            {
                mgr.SaveDraft(new ReportTemplateSaveDraftRequest
                {
                    TemplateId = tpl.Id,
                    VersionId = v1.Id,
                    DefinitionJson = published.DefinitionJson
                });
                Assert.Fail("Published version must not be editable.");
            }
            catch (InvalidOperationException)
            {
                // expected
            }

            var v2 = mgr.CreateDraftFromPublished(tpl.Id);
            Assert.AreEqual(ReportTemplateVersionStatuses.Draft, v2.Status);
            Assert.IsTrue(v2.VersionNumber > published.VersionNumber);
            mgr.Publish(v2.Id);
            Assert.AreEqual(ReportTemplateVersionStatuses.Archived, mgr.GetVersion(v1.Id).Status);
            Assert.AreEqual(ReportTemplateVersionStatuses.Published, mgr.GetVersion(v2.Id).Status);
        }

        [TestMethod]
        public void Conflicting_Department_Assignment_Rejected()
        {
            var mgr = CreateManager();
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 6);
            var def = ReportTemplateDefinitionValidator.BuildBuiltinStub(ReportTemplateTypes.Diagnostic, null)
                .Replace("\"renderer\":\"builtin\"", "\"renderer\":\"declarative\"");
            var a = mgr.Create(new ReportTemplateCreateRequest { Name = "A-" + suffix, ReportType = ReportTemplateTypes.Diagnostic, DefinitionJson = def });
            var b = mgr.Create(new ReportTemplateCreateRequest { Name = "B-" + suffix, ReportType = ReportTemplateTypes.Diagnostic, DefinitionJson = def });
            mgr.Publish(mgr.ListVersions(a.Id).First(v => v.Status == ReportTemplateVersionStatuses.Draft).Id);
            mgr.Publish(mgr.ListVersions(b.Id).First(v => v.Status == ReportTemplateVersionStatuses.Draft).Id);
            var dept = "X" + suffix.Substring(0, 4);
            mgr.UpsertAssignment(new ReportTemplateAssignmentRequest
            {
                ReportType = ReportTemplateTypes.Diagnostic,
                ScopeType = ReportTemplateScopeTypes.Department,
                DepartmentCode = dept,
                TemplateId = a.Id
            });
            try
            {
                mgr.UpsertAssignment(new ReportTemplateAssignmentRequest
                {
                    ReportType = ReportTemplateTypes.Diagnostic,
                    ScopeType = ReportTemplateScopeTypes.Department,
                    DepartmentCode = dept,
                    TemplateId = b.Id
                });
                Assert.Fail("Expected conflict.");
            }
            catch (InvalidOperationException ex)
            {
                StringAssert.Contains(ex.Message, "Conflicting");
            }
        }

        [TestMethod]
        public void Diagnostic_Template_Cannot_Resolve_As_Radiology_Mismatch_On_Assignment()
        {
            var mgr = CreateManager();
            mgr.EnsureSystemDefaults();
            var diag = mgr.List(ReportTemplateTypes.Diagnostic).First(t => t.IsSystemDefault);
            try
            {
                mgr.UpsertAssignment(new ReportTemplateAssignmentRequest
                {
                    ReportType = ReportTemplateTypes.Radiology,
                    ScopeType = ReportTemplateScopeTypes.System,
                    TemplateId = diag.Id
                });
                Assert.Fail("Cross report-type assignment must fail.");
            }
            catch (ArgumentException)
            {
                // expected
            }
        }

        [TestMethod]
        public void Definition_Rejects_Script_And_Unknown_Binding()
        {
            try
            {
                ReportTemplateDefinitionValidator.Validate(
                    "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\",\"components\":[{\"binding\":\"Evil.Field\"}]}",
                    ReportTemplateTypes.Diagnostic);
                Assert.Fail("Unknown binding should fail.");
            }
            catch (ArgumentException)
            {
            }

            try
            {
                ReportTemplateDefinitionValidator.Validate(
                    "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\",\"html\":\"<script>alert(1)</script>\"}",
                    ReportTemplateTypes.Diagnostic);
                Assert.Fail("Script should fail.");
            }
            catch (ArgumentException)
            {
            }
        }

        [TestMethod]
        public void Definition_Allows_Safe_Patient_Binding()
        {
            ReportTemplateDefinitionValidator.Validate(
                "{\"schemaVersion\":1,\"reportType\":\"Diagnostic\",\"renderer\":\"declarative\",\"components\":[{\"type\":\"PATIENT_FIELD\",\"binding\":\"Patient.Name\"}]}",
                ReportTemplateTypes.Diagnostic);
        }
    }
}
