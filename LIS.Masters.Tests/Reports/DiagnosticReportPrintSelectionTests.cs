using LIS.BusinessLogic;
using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LIS.Masters.Tests.Reports
{
    [TestClass]
    public class DiagnosticReportPrintSelectionTests : IntegrationTestBase
    {
        private TestReportManager CreateManager()
        {
            return new TestReportManager(
                Services.Logger,
                Services.Identity,
                Services.Uow,
                Services.TestRequest);
        }

        [TestMethod]
        public void PrintAll_Without_Filter_Matches_Existing_Behaviour()
        {
            var manager = CreateManager();
            var labs = manager.GetPrintableLabNumbers()?.ToList();
            if (labs == null || !labs.Any())
            {
                Assert.Inconclusive("No printable lab numbers available.");
            }

            var labNo = labs[0].LabNo;
            var all = manager.GetDiagnosticTestReport(labNo, null, null);
            Assert.IsNotNull(all);
            Assert.IsNotNull(all.Header);

            var sectionCount = CountSections(all);
            Assert.IsTrue(sectionCount >= 1, "Print All should return at least one section.");

            // Every section must carry TestRequestDetailId for selection UI.
            foreach (var section in EnumerateSections(all))
            {
                Assert.IsTrue(section.TestRequestDetailId > 0, "Section must expose TestRequestDetailId.");
            }
        }

        [TestMethod]
        public void PrintSpecific_Returns_Only_Selected_Test()
        {
            var manager = CreateManager();
            var labs = manager.GetPrintableLabNumbers()?.ToList();
            if (labs == null || !labs.Any())
            {
                Assert.Inconclusive("No printable lab numbers available.");
            }

            var labNo = labs[0].LabNo;
            var all = manager.GetDiagnosticTestReport(labNo, null, null);
            var sections = EnumerateSections(all).ToList();
            if (sections.Count < 1)
            {
                Assert.Inconclusive("No printable sections on first lab.");
            }

            var selectedId = sections[0].TestRequestDetailId;
            var filtered = manager.GetDiagnosticTestReport(labNo, null, selectedId);
            var filteredSections = EnumerateSections(filtered).ToList();

            Assert.AreEqual(1, filteredSections.Count, "Specific print must return exactly one test section.");
            Assert.AreEqual(selectedId, filteredSections[0].TestRequestDetailId);
            Assert.AreEqual(all.Header.InvoiceNo, filtered.Header.InvoiceNo);
            Assert.AreEqual(all.Header.PatientName, filtered.Header.PatientName);
        }

        [TestMethod]
        public void PrintSpecific_Rejects_Test_Not_Belonging_To_Order()
        {
            var manager = CreateManager();
            var labs = manager.GetPrintableLabNumbers()?.ToList();
            if (labs == null || !labs.Any())
            {
                Assert.Inconclusive("No printable lab numbers available.");
            }

            var labNo = labs[0].LabNo;
            try
            {
                manager.GetDiagnosticTestReport(labNo, null, long.MaxValue);
                Assert.Fail("Expected TestReportValidationException for foreign testRequestDetailId.");
            }
            catch (TestReportValidationException ex)
            {
                StringAssert.Contains(ex.Message, "does not belong");
            }
        }

        [TestMethod]
        public void PrintSpecific_Invalid_Lab_Still_Rejected()
        {
            var manager = CreateManager();
            try
            {
                manager.GetDiagnosticTestReport("___NO_SUCH_LAB___", null, 1);
                Assert.Fail("Expected TestReportValidationException for invalid lab.");
            }
            catch (TestReportValidationException)
            {
                // expected
            }
        }

        [TestMethod]
        public void PrintAll_Omitting_Optional_Id_Remains_Backward_Compatible()
        {
            var manager = CreateManager();
            var labs = manager.GetPrintableLabNumbers()?.ToList();
            if (labs == null || labs.Count < 1)
            {
                Assert.Inconclusive("No printable lab numbers available.");
            }

            var labNo = labs[0].LabNo;
            var a = manager.GetDiagnosticTestReport(labNo, null);
            var b = manager.GetDiagnosticTestReport(labNo, null, null);
            Assert.AreEqual(CountSections(a), CountSections(b));
        }

        private static int CountSections(DiagnosticTestReportDto report)
        {
            return EnumerateSections(report)
                .Select(s => s.TestRequestDetailId)
                .Where(id => id > 0)
                .Distinct()
                .Count();
        }

        private static System.Collections.Generic.IEnumerable<DiagnosticTestReportSection> EnumerateSections(DiagnosticTestReportDto report)
        {
            // Prefer department grouping (primary print layout); fall back to profile/flat.
            if (report?.DepartmentGroups != null && report.DepartmentGroups.Any(g => g?.Sections != null && g.Sections.Any()))
            {
                foreach (var g in report.DepartmentGroups)
                {
                    if (g?.Sections == null)
                    {
                        continue;
                    }
                    foreach (var s in g.Sections)
                    {
                        yield return s;
                    }
                }
                yield break;
            }

            if (report?.ProfileGroups != null)
            {
                foreach (var g in report.ProfileGroups)
                {
                    if (g?.Sections == null)
                    {
                        continue;
                    }
                    foreach (var s in g.Sections)
                    {
                        yield return s;
                    }
                }
            }

            if (report?.Sections != null)
            {
                foreach (var s in report.Sections)
                {
                    yield return s;
                }
            }
        }
    }
}
