using LIS.BusinessLogic;
using LIS.DtoModel;
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

        [TestMethod]
        public void PrintSpecific_Allows_Approved_Test_When_Sibling_Pending()
        {
            var manager = CreateManager();
            if (!TryFindMultiSectionLab(manager, out var labNo, out var readyId, out var siblingId))
            {
                Assert.Inconclusive("Need a paid multi-test fully-approved order to simulate partial completion.");
            }

            var sibling = Services.Db.TestRequestDetails.First(r => r.Id == siblingId);
            var originalStatus = sibling.ReportStatus;
            try
            {
                sibling.ReportStatus = ReportStatusType.New;
                Services.Db.SaveChanges();

                try
                {
                    manager.GetDiagnosticTestReport(labNo, null, null);
                    Assert.Fail("Print All must remain blocked while a sibling is pending.");
                }
                catch (TestReportValidationException ex)
                {
                    StringAssert.Contains(ex.Message, "not approved");
                }

                var specific = manager.GetDiagnosticTestReport(labNo, null, readyId);
                var sections = EnumerateSections(specific).ToList();
                Assert.AreEqual(1, sections.Count);
                Assert.AreEqual(readyId, sections[0].TestRequestDetailId);

                var options = manager.GetPrintableTestOptions(labNo, null);
                Assert.IsFalse(options.CanPrintAll, "CanPrintAll must be false for partial order.");
                Assert.IsTrue(options.Tests.Any(t => t.TestRequestDetailId == readyId && t.IsPrintable));
                Assert.IsTrue(options.Tests.Any(t => t.TestRequestDetailId == siblingId && !t.IsPrintable));
            }
            finally
            {
                var restore = Services.Db.TestRequestDetails.First(r => r.Id == siblingId);
                restore.ReportStatus = originalStatus;
                Services.Db.SaveChanges();
            }
        }

        [TestMethod]
        public void PrintSpecific_And_PrintAll_Blocked_When_Invoice_Unpaid()
        {
            var manager = CreateManager();
            var labs = manager.GetPrintableLabNumbers()?.ToList();
            if (labs == null || !labs.Any())
            {
                Assert.Inconclusive("No printable lab numbers available.");
            }

            var labNo = labs[0].LabNo;
            var all = manager.GetDiagnosticTestReport(labNo, null, null);
            var sectionId = EnumerateSections(all).Select(s => s.TestRequestDetailId).FirstOrDefault();
            if (sectionId <= 0)
            {
                Assert.Inconclusive("No section id on printable lab.");
            }

            var invoice = Services.Db.SaleInvoices.FirstOrDefault(i => i.InvoiceNo == all.Header.InvoiceNo);
            if (invoice == null)
            {
                Assert.Inconclusive("Invoice row not found for printable lab.");
            }

            var originalPayment = invoice.PaymentStatus;
            try
            {
                invoice.PaymentStatus = (int)PaymentStatusType.Unpaid;
                Services.Db.SaveChanges();

                AssertPaymentBlocked(() => manager.GetDiagnosticTestReport(labNo, null, null));
                AssertPaymentBlocked(() => manager.GetDiagnosticTestReport(labNo, null, sectionId));
                AssertPaymentBlocked(() => manager.GetPrintableTestOptions(labNo, null));
            }
            finally
            {
                var restore = Services.Db.SaleInvoices.First(i => i.Id == invoice.Id);
                restore.PaymentStatus = originalPayment;
                Services.Db.SaveChanges();
            }
        }

        [TestMethod]
        public void PrintOptions_CanPrintAll_True_When_Every_Test_Ready()
        {
            var manager = CreateManager();
            var labs = manager.GetPrintableLabNumbers()?.ToList();
            if (labs == null || !labs.Any())
            {
                Assert.Inconclusive("No printable lab numbers available.");
            }

            var labNo = labs[0].LabNo;
            DiagnosticTestReportDto all;
            try
            {
                all = manager.GetDiagnosticTestReport(labNo, null, null);
            }
            catch (TestReportValidationException)
            {
                Assert.Inconclusive("First printable lab is not fully ready for Print All.");
                return;
            }

            var options = manager.GetPrintableTestOptions(labNo, null);
            Assert.IsTrue(options.CanPrintAll);
            Assert.IsTrue(options.Tests.Count >= 1);
            Assert.IsTrue(options.Tests.All(t => t.IsPrintable));
            Assert.AreEqual(all.Header.InvoiceNo, options.InvoiceNo);
        }

        private static void AssertPaymentBlocked(Action action)
        {
            try
            {
                action();
                Assert.Fail("Expected payment pending validation.");
            }
            catch (TestReportValidationException ex)
            {
                StringAssert.Contains(ex.Message, "Payment pending");
            }
        }

        private bool TryFindMultiSectionLab(TestReportManager manager, out string labNo, out long readyId, out long siblingId)
        {
            labNo = null;
            readyId = 0;
            siblingId = 0;
            var labs = manager.GetPrintableLabNumbers()?.ToList();
            if (labs == null)
            {
                return false;
            }

            foreach (var lab in labs.Take(25))
            {
                DiagnosticTestReportDto all;
                try
                {
                    all = manager.GetDiagnosticTestReport(lab.LabNo, null, null);
                }
                catch (TestReportValidationException)
                {
                    continue;
                }

                var ids = EnumerateSections(all)
                    .Select(s => s.TestRequestDetailId)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();
                if (ids.Count < 2)
                {
                    continue;
                }

                labNo = lab.LabNo;
                readyId = ids[0];
                siblingId = ids[1];
                return true;
            }

            return false;
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
