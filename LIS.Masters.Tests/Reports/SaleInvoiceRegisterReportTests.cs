using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LIS.Masters.Tests.Reports
{
    [TestClass]
    public class SaleInvoiceRegisterReportTests : IntegrationTestBase
    {
        private static ReportFilterOptions June2026Options(string invoiceNo = null)
        {
            return new ReportFilterOptions
            {
                FromDate = new DateTime(2026, 6, 1),
                ToDate = new DateTime(2026, 6, 30),
                InvoiceNo = invoiceNo,
                RecordPerPage = 100,
                CurrentPage = 1,
                SortColumnName = "InvoiceDate",
                SortDirection = false
            };
        }

        [TestMethod]
        public void SaleInvoiceRegister_NoInvoiceFilter_ReturnsRows()
        {
            var result = Services.Report.GetSaleInvoiceRegister(June2026Options());
            Assert.IsTrue(result.TotalRecord > 0, "Expected invoices in June 2026.");
        }

        [TestMethod]
        public void SaleInvoiceRegister_ExactMatch_ReturnsRow()
        {
            var baseline = Services.Report.GetSaleInvoiceRegister(June2026Options());
            var sample = baseline.Items.FirstOrDefault(i => !string.IsNullOrWhiteSpace(i.InvoiceNo));
            Assert.IsNotNull(sample, "Need at least one invoice with InvoiceNo.");

            var result = Services.Report.GetSaleInvoiceRegister(June2026Options(sample.InvoiceNo));
            Assert.IsTrue(result.TotalRecord >= 1, "Exact invoice search should return matches.");
            Assert.IsTrue(result.Items.Any(i =>
                string.Equals(i.InvoiceNo, sample.InvoiceNo, StringComparison.OrdinalIgnoreCase)));
        }

        [TestMethod]
        public void SaleInvoiceRegister_ContainsMatch_ReturnsRows()
        {
            var result = Services.Report.GetSaleInvoiceRegister(June2026Options("INV"));
            Assert.IsTrue(result.TotalRecord > 0, "Contains search for INV should return rows.");
        }

        [TestMethod]
        public void SaleInvoiceRegister_PrefixMatch_ReturnsRows()
        {
            var baseline = Services.Report.GetSaleInvoiceRegister(June2026Options());
            var sample = baseline.Items.FirstOrDefault(i =>
                !string.IsNullOrWhiteSpace(i.InvoiceNo) && i.InvoiceNo.Length >= 4);
            Assert.IsNotNull(sample, "Need sample invoice for prefix test.");

            var prefix = sample.InvoiceNo.Substring(0, 4);
            var result = Services.Report.GetSaleInvoiceRegister(June2026Options(prefix));
            Assert.IsTrue(result.TotalRecord > 0, "Prefix search should return rows.");
            Assert.IsTrue(result.Items.All(i =>
                i.InvoiceNo != null &&
                i.InvoiceNo.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        [TestMethod]
        public void SaleInvoiceRegister_SuffixMatch_ReturnsRows()
        {
            var baseline = Services.Report.GetSaleInvoiceRegister(June2026Options());
            var sample = baseline.Items.FirstOrDefault(i =>
                !string.IsNullOrWhiteSpace(i.InvoiceNo) && i.InvoiceNo.Length >= 4);
            Assert.IsNotNull(sample, "Need sample invoice for suffix test.");

            var suffix = sample.InvoiceNo.Substring(sample.InvoiceNo.Length - 4);
            var result = Services.Report.GetSaleInvoiceRegister(June2026Options(suffix));
            Assert.IsTrue(result.TotalRecord > 0, "Suffix search should return rows.");
            Assert.IsTrue(result.Items.All(i =>
                i.InvoiceNo != null &&
                i.InvoiceNo.IndexOf(suffix, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        [TestMethod]
        public void SaleInvoiceRegister_CreatedByUserName_ReturnsOnlyMatching()
        {
            var baseline = Services.Report.GetSaleInvoiceRegister(June2026Options());
            var sample = baseline.Items.FirstOrDefault(i => !string.IsNullOrWhiteSpace(i.CreatedBy));
            Assert.IsNotNull(sample, "Need invoice with CreatedBy.");

            var options = June2026Options();
            options.CreatedByUserName = sample.CreatedBy;
            var result = Services.Report.GetSaleInvoiceRegister(options);
            Assert.IsTrue(result.TotalRecord > 0);
            Assert.IsTrue(result.Items.All(i =>
                string.Equals(i.CreatedBy, sample.CreatedBy, StringComparison.OrdinalIgnoreCase)));
        }

        [TestMethod]
        public void SaleInvoiceRegister_CreatedByUserName_NoMatch_ReturnsEmpty()
        {
            var options = June2026Options();
            options.CreatedByUserName = "non-existent-creator-" + Guid.NewGuid().ToString("N");
            var result = Services.Report.GetSaleInvoiceRegister(options);
            Assert.AreEqual(0, result.TotalRecord);
        }

        [TestMethod]
        public void SaleInvoiceRegister_CreatedByWithPatientFilter_Combined()
        {
            var baseline = Services.Report.GetSaleInvoiceRegister(June2026Options());
            var sample = baseline.Items.FirstOrDefault(i => !string.IsNullOrWhiteSpace(i.CreatedBy));
            Assert.IsNotNull(sample, "Need invoice with CreatedBy.");

            var invoice = Services.Db.SaleInvoices.FirstOrDefault(i => i.InvoiceNo == sample.InvoiceNo);
            Assert.IsNotNull(invoice, "Need invoice row for combined filter test.");

            var options = June2026Options();
            options.CreatedByUserName = sample.CreatedBy;
            options.PatientId = invoice.PatientId;
            var result = Services.Report.GetSaleInvoiceRegister(options);
            Assert.IsTrue(result.TotalRecord > 0);
            Assert.IsTrue(result.Items.All(i =>
                string.Equals(i.CreatedBy, sample.CreatedBy, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
