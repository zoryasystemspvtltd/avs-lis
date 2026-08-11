using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LIS.Masters.Tests.Transactions
{
    /// <summary>
    /// Integration coverage: multi-department sale invoice with many tests, fully paid.
    /// </summary>
    [TestClass]
    public class SaleInvoiceMultiDepartmentPaidTests : IntegrationTestBase
    {
        private const int DepartmentCount = 5;
        private const int TestsPerDepartment = 3;
        private const int ExpectedTestCount = DepartmentCount * TestsPerDepartment; // 15

        [TestMethod]
        public void SaleInvoice_Fifteen_Tests_Across_Departments_Is_Fully_Paid()
        {
            var specimen = Services.Specimen.Get().Cast<HISSpecimenMaster>().FirstOrDefault();
            if (specimen == null)
            {
                Assert.Inconclusive("No specimen master available.");
                return;
            }

            var tag = Guid.NewGuid().ToString("N").Substring(0, 6);
            var patientId = Services.PatientMaster.Add(MasterTestDataBuilder.Patient($"MD{tag}"));
            Assert.IsTrue(patientId > 0);

            var departmentCodes = new List<string>();
            var testIds = new List<int>();
            var rateIds = new List<int>();

            for (var d = 0; d < DepartmentCount; d++)
            {
                var deptCode = UniqueCode($"D{d}", maxLength: 15);
                Services.Department.Add(new Departments
                {
                    Code = deptCode,
                    Name = $"Multi-Dept {d + 1} {tag}",
                    ProcessingCategory = DepartmentProcessingCategories.Laboratory
                });
                departmentCodes.Add(deptCode);

                for (var t = 0; t < TestsPerDepartment; t++)
                {
                    var testCode = UniqueCode($"T{d}{t}");
                    var testId = (int)Services.HisTest.Add(
                        MasterTestDataBuilder.HisTest(testCode, deptCode, specimen.Code));
                    var rate = 100m + (d * 10m) + t;
                    var rateId = (int)Services.TestRate.Add(MasterTestDataBuilder.StandardRate(testId, rate));
                    testIds.Add(testId);
                    rateIds.Add(rateId);
                }
            }

            Assert.AreEqual(ExpectedTestCount, testIds.Count);

            var invoiceNo = UniqueCode("INV15");
            var details = testIds.Select((testId, index) => new SaleInvoiceDetail
            {
                TestId = testId,
                Quantity = 1,
                Rate = 0,
                RequestDetailId = 0,
                SampleNo = $"{invoiceNo}-{index + 1:00}"
            }).ToList();

            var invoiceId = Services.SaleInvoice.Save(new SaleInvoiceDto
            {
                Invoice = new SaleInvoice
                {
                    InvoiceNo = invoiceNo,
                    InvoiceDate = DateTime.Today,
                    PatientId = patientId,
                    InvoiceStatus = (int)InvoiceStatusType.Draft,
                    PaymentStatus = (int)PaymentStatusType.Unpaid,
                    IsActive = true
                },
                Details = details
            });
            Assert.IsTrue(invoiceId > 0, "Invoice save should return a positive id.");

            var draft = Services.SaleInvoice.GetById(invoiceId);
            Assert.IsNotNull(draft);
            Assert.AreEqual(ExpectedTestCount, draft.Details.Count(), "Invoice must contain all 15 test lines.");
            Assert.IsTrue(draft.Invoice.NetAmount > 0, "Net amount must be calculated from test rates.");
            Assert.AreEqual(ExpectedTestCount, draft.Details.Count(x => x.Rate > 0), "Each line should resolve a rate.");

            Services.SaleInvoice.UpdateStatus(
                invoiceId,
                (int)InvoiceStatusType.Confirmed,
                (int)PaymentStatusType.Paid);

            var paid = Services.SaleInvoice.GetById(invoiceId);
            Assert.AreEqual((int)InvoiceStatusType.Confirmed, paid.Invoice.InvoiceStatus);
            Assert.AreEqual((int)PaymentStatusType.Paid, paid.Invoice.PaymentStatus);
            Assert.AreEqual(paid.Invoice.NetAmount, paid.Invoice.PaidAmount, "Fully paid invoice PaidAmount must equal NetAmount.");
            Assert.AreEqual(0m, paid.Invoice.DueAmount, "Fully paid invoice DueAmount must be zero.");

            var requests = Services.Db.TestRequestDetails
                .Where(r => r.PatientId == patientId && r.HISRequestNo == invoiceNo)
                .ToList();
            Assert.AreEqual(ExpectedTestCount, requests.Count, "One TestRequestDetail should be created per test line.");

            var requestDepartments = requests
                .Select(r => (r.DepartmentId ?? string.Empty).Trim())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            Assert.IsTrue(
                requestDepartments.Count >= DepartmentCount,
                $"Expected requests across {DepartmentCount} departments, got {requestDepartments.Count}: {string.Join(",", requestDepartments)}");

            foreach (var deptCode in departmentCodes)
            {
                var countForDept = requests.Count(r =>
                    string.Equals(r.DepartmentId, deptCode, StringComparison.OrdinalIgnoreCase));
                Assert.AreEqual(
                    TestsPerDepartment,
                    countForDept,
                    $"Department {deptCode} should have {TestsPerDepartment} test requests.");
            }

            // Cleanup invoice (leave seeded masters; rates cleaned to reduce rate-table noise).
            Services.SaleInvoice.Cancel(invoiceId);
            foreach (var rateId in rateIds)
            {
                try { Services.TestRate.Delete(new TestRateMaster { Id = rateId }); } catch { /* best effort */ }
            }
        }
    }
}
