using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LIS.Masters.Tests.Transactions
{
    /// <summary>
    /// Full regression matrix for Sale Invoice CRUD, calculations, and workflow combinations.
    /// </summary>
    [TestClass]
    public class SaleInvoiceRegressionTests : IntegrationTestBase
    {
        private long CreatePatient() =>
            Services.PatientMaster.Add(MasterTestDataBuilder.Patient(Guid.NewGuid().ToString("N").Substring(0, 8)));

        private int EnsureTestWithRate(decimal rate, out int rateId) =>
            EnsureTestWithStandardRate(rate, out rateId);

        private SaleInvoiceDto BuildDraft(long patientId, params SaleInvoiceDetail[] lines)
        {
            return new SaleInvoiceDto
            {
                Invoice = new SaleInvoice
                {
                    InvoiceNo = UniqueCode("INV"),
                    InvoiceDate = DateTime.Today,
                    PatientId = patientId,
                    InvoiceStatus = (int)InvoiceStatusType.Draft,
                    PaymentStatus = (int)PaymentStatusType.Unpaid,
                    IsActive = true
                },
                Details = lines.ToList()
            };
        }

        [TestMethod]
        public void SaleInvoice_Create_MultiLine_Totals_Sum_Correctly()
        {
            var patientId = CreatePatient();
            var rateId1 = 0;
            var rateId2 = 0;
            var test1 = EnsureTestWithRate(100m, out rateId1);
            var test2 = EnsureTestWithRate(250m, out rateId2);

            var dto = BuildDraft(patientId,
                new SaleInvoiceDetail { TestId = test1, Quantity = 2, Rate = 0, DiscountAmount = 5m },
                new SaleInvoiceDetail { TestId = test2, Quantity = 1, Rate = 0 });
            // Tax is entered once at the invoice level (no longer per line).
            dto.Invoice.TaxAmount = 12m;

            var id = Services.SaleInvoice.Save(dto);
            var loaded = Services.SaleInvoice.GetById(id);

            Assert.AreEqual(450m, loaded.Invoice.GrossAmount);
            Assert.AreEqual(5m, loaded.Invoice.DiscountAmount);
            Assert.AreEqual(12m, loaded.Invoice.TaxAmount);
            Assert.AreEqual(457m, loaded.Invoice.NetAmount);
            Assert.AreEqual(2, loaded.Details.Count());
            Assert.IsTrue(loaded.Details.All(d => d.TaxAmount == 0m), "Line tax must be zero; tax is invoice-level.");

            Services.SaleInvoice.Cancel(id);
            Services.TestRate.Delete(new TestRateMaster { Id = rateId1 });
            Services.TestRate.Delete(new TestRateMaster { Id = rateId2 });
        }

        [TestMethod]
        public void SaleInvoice_Header_Fixed_Discount_Overrides_Line_Discounts()
        {
            var patientId = CreatePatient();
            var rateId = 0;
            var testId = EnsureTestWithRate(200m, out rateId);

            var dto = BuildDraft(patientId,
                new SaleInvoiceDetail { TestId = testId, Quantity = 2, Rate = 0, DiscountAmount = 10m });
            dto.Invoice.DiscountType = "Fixed Amount";
            dto.Invoice.DiscountAmount = 75m;

            var id = Services.SaleInvoice.Save(dto);
            var loaded = Services.SaleInvoice.GetById(id);

            Assert.AreEqual(400m, loaded.Invoice.GrossAmount);
            Assert.AreEqual(75m, loaded.Invoice.DiscountAmount);
            Assert.AreEqual(0m, loaded.Invoice.TaxAmount);
            Assert.AreEqual(325m, loaded.Invoice.NetAmount);

            Services.SaleInvoice.Cancel(id);
            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }

        [TestMethod]
        public void SaleInvoice_Header_Percentage_Discount_Applied_To_Gross()
        {
            var patientId = CreatePatient();
            var rateId = 0;
            var testId = EnsureTestWithRate(100m, out rateId);

            var dto = BuildDraft(patientId,
                new SaleInvoiceDetail { TestId = testId, Quantity = 4, Rate = 0 });
            dto.Invoice.DiscountType = "Percentage";
            dto.Invoice.DiscountAmount = 10m;

            var id = Services.SaleInvoice.Save(dto);
            var loaded = Services.SaleInvoice.GetById(id);

            Assert.AreEqual(400m, loaded.Invoice.GrossAmount);
            Assert.AreEqual(40m, loaded.Invoice.DiscountAmount);
            Assert.AreEqual(0m, loaded.Invoice.TaxAmount);
            Assert.AreEqual(360m, loaded.Invoice.NetAmount);

            Services.SaleInvoice.Cancel(id);
            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }

        [TestMethod]
        public void SaleInvoice_Profile_Line_Saves_And_Links_Test_Requests()
        {
            var patientId = CreatePatient();
            var rateId = 0;
            var testId = EnsureTestWithRate(150m, out rateId);
            var profile = MasterTestDataBuilder.Profile(UniqueCode("PRF"), testId);
            profile.PackageRate = 499m;
            Services.TestProfile.SaveWithDetails(profile, profile.ProfileDetails);

            var dto = BuildDraft(patientId,
                new SaleInvoiceDetail { TestProfileId = profile.Id, Quantity = 1, Rate = 0 });
            var id = Services.SaleInvoice.Save(dto);
            var loaded = Services.SaleInvoice.GetById(id);

            Assert.AreEqual(1, loaded.Details.Count());
            Assert.AreEqual(499m, loaded.Details.First().Rate);
            Assert.AreEqual(499m, loaded.Invoice.NetAmount);
            Assert.IsTrue(loaded.Details.First().RequestDetailId > 0);

            Services.SaleInvoice.Cancel(id);
            Services.TestProfile.Delete(new TestProfileMaster { Id = profile.Id });
            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }

        [TestMethod]
        public void SaleInvoice_Diagnostic_Line_Saves_Without_TestRequest_FK()
        {
            var patientId = CreatePatient();
            var diagDeptCode = UniqueCode("RAD", maxLength: 15);
            Services.Department.Add(new Departments
            {
                Code = diagDeptCode,
                Name = "Regression Diagnostic",
                ProcessingCategory = DepartmentProcessingCategories.Diagnostic
            });

            var specimen = Services.Specimen.Get().Cast<HISSpecimenMaster>().First();
            var testCode = UniqueCode("DX");
            var testId = (int)Services.HisTest.Add(MasterTestDataBuilder.HisTest(testCode, diagDeptCode, specimen.Code));
            var diagRate = MasterTestDataBuilder.StandardRate(testId, 1200m);
            diagRate.TaxPercent = 0;
            var rateId = (int)Services.TestRate.Add(diagRate);

            var dto = BuildDraft(patientId,
                new SaleInvoiceDetail { TestId = testId, Quantity = 1, Rate = 0 });
            var id = Services.SaleInvoice.Save(dto);
            var loaded = Services.SaleInvoice.GetById(id);
            var line = loaded.Details.First();

            Assert.AreEqual(1200m, loaded.Invoice.NetAmount);
            Assert.IsFalse(line.RequestDetailId.HasValue && line.RequestDetailId > 0,
                "Diagnostic lines must not require TestRequestDetails FK");

            var radCount = Services.Db.RadiologyRequestDetail.Count(r =>
                r.PatientId == patientId && r.HISTestCode == testCode);
            Assert.IsTrue(radCount > 0, "Diagnostic save must create radiology request");

            Services.SaleInvoice.Cancel(id);
            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
            Services.Department.Delete(Services.Department.Get(diagDeptCode));
        }

        [TestMethod]
        public void SaleInvoice_Mixed_Lab_And_Diagnostic_Lines_Save()
        {
            var patientId = CreatePatient();
            var rateId = 0;
            var labTestId = EnsureTestWithRate(300m, out rateId);

            var diagDeptCode = UniqueCode("IMG", maxLength: 15);
            Services.Department.Add(new Departments
            {
                Code = diagDeptCode,
                Name = "Regression Imaging",
                ProcessingCategory = DepartmentProcessingCategories.Diagnostic
            });
            var specimen = Services.Specimen.Get().Cast<HISSpecimenMaster>().First();
            var diagCode = UniqueCode("US");
            var diagTestId = (int)Services.HisTest.Add(MasterTestDataBuilder.HisTest(diagCode, diagDeptCode, specimen.Code));
            var labRate = MasterTestDataBuilder.StandardRate(labTestId, 300m);
            labRate.TaxPercent = 0;
            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
            rateId = (int)Services.TestRate.Add(labRate);

            var diagRate = MasterTestDataBuilder.StandardRate(diagTestId, 800m);
            diagRate.TaxPercent = 0;
            var diagRateId = (int)Services.TestRate.Add(diagRate);

            var dto = BuildDraft(patientId,
                new SaleInvoiceDetail { TestId = labTestId, Quantity = 1, Rate = 0 },
                new SaleInvoiceDetail { TestId = diagTestId, Quantity = 1, Rate = 0 });

            var id = Services.SaleInvoice.Save(dto);
            var loaded = Services.SaleInvoice.GetById(id);

            Assert.AreEqual(2, loaded.Details.Count());
            Assert.AreEqual(1100m, loaded.Invoice.NetAmount);

            var labLine = loaded.Details.First(d => d.TestId == labTestId);
            var diagLine = loaded.Details.First(d => d.TestId == diagTestId);
            Assert.IsTrue(labLine.RequestDetailId > 0);
            Assert.IsFalse(diagLine.RequestDetailId.HasValue && diagLine.RequestDetailId > 0);

            Services.SaleInvoice.Cancel(id);
            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
            Services.TestRate.Delete(new TestRateMaster { Id = diagRateId });
            Services.Department.Delete(Services.Department.Get(diagDeptCode));
        }

        [TestMethod]
        public void SaleInvoice_No_Lines_Throws()
        {
            var patientId = CreatePatient();
            var dto = BuildDraft(patientId);
            dto.Details = new List<SaleInvoiceDetail>();

            Assert.ThrowsException<ArgumentException>(() => Services.SaleInvoice.Save(dto));
        }

        [TestMethod]
        public void SaleInvoice_Cancelled_Cannot_Be_Edited()
        {
            var patientId = CreatePatient();
            var rateId = 0;
            var testId = EnsureTestWithRate(100m, out rateId);
            var id = Services.SaleInvoice.Save(BuildDraft(patientId,
                new SaleInvoiceDetail { TestId = testId, Quantity = 1, Rate = 0 }));

            Services.SaleInvoice.Cancel(id);
            var loaded = Services.SaleInvoice.GetById(id);
            loaded.Details.First().Quantity = 2;

            Assert.ThrowsException<InvalidOperationException>(() => Services.SaleInvoice.Save(loaded));

            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }

        [TestMethod]
        public void SaleInvoice_Full_Payment_Via_UpdateStatus()
        {
            var patientId = CreatePatient();
            var rateId = 0;
            var testId = EnsureTestWithRate(500m, out rateId);
            var id = Services.SaleInvoice.Save(BuildDraft(patientId,
                new SaleInvoiceDetail { TestId = testId, Quantity = 1, Rate = 0 }));

            Services.SaleInvoice.UpdateStatus(id, (int)InvoiceStatusType.Confirmed, (int)PaymentStatusType.Paid);
            var paid = Services.SaleInvoice.GetById(id);

            Assert.AreEqual((int)PaymentStatusType.Paid, paid.Invoice.PaymentStatus);
            Assert.AreEqual(paid.Invoice.NetAmount, paid.Invoice.PaidAmount);
            Assert.AreEqual(0m, paid.Invoice.DueAmount);

            Services.SaleInvoice.Cancel(id);
            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }

        [TestMethod]
        public void SaleInvoice_Rate_Master_Tax_Is_Not_Auto_Applied()
        {
            // Per CX request: tax must NOT be auto-calculated from the rate master.
            // A rate carrying TaxPercent should still produce zero tax on the invoice
            // unless an explicit line TaxAmount is supplied.
            var patientId = CreatePatient();
            var testId = CreateIsolatedTest();
            var rate = MasterTestDataBuilder.StandardRate(testId, 200m);
            rate.TaxPercent = 12m;
            var rateId = (int)Services.TestRate.Add(rate);

            var id = Services.SaleInvoice.Save(BuildDraft(patientId,
                new SaleInvoiceDetail { TestId = testId, Quantity = 1, Rate = 0 }));

            var loaded = Services.SaleInvoice.GetById(id);
            Assert.AreEqual(0m, loaded.Invoice.TaxAmount);
            Assert.AreEqual(200m, loaded.Invoice.NetAmount);

            Services.SaleInvoice.Cancel(id);
            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }

        [TestMethod]
        public void SaleInvoice_Manual_Invoice_Level_Tax_Is_Preserved()
        {
            // Per CX request: tax is keyed in at the invoice level and must be persisted as-is.
            var patientId = CreatePatient();
            var rateId = 0;
            var testId = EnsureTestWithRate(500m, out rateId);
            var dto = BuildDraft(patientId, new SaleInvoiceDetail { TestId = testId, Quantity = 1, Rate = 0 });
            dto.Invoice.TaxAmount = 90m;

            var id = Services.SaleInvoice.Save(dto);
            var loaded = Services.SaleInvoice.GetById(id);

            Assert.AreEqual(90m, loaded.Invoice.TaxAmount);
            Assert.AreEqual(590m, loaded.Invoice.NetAmount);
            Assert.IsTrue(loaded.Details.All(d => d.TaxAmount == 0m), "Tax is invoice-level; lines carry no tax.");

            Services.SaleInvoice.Cancel(id);
            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }

        [TestMethod]
        public void SaleInvoice_Line_Percentage_Discount_Computes_And_RoundTrips()
        {
            var patientId = CreatePatient();
            var rateId = 0;
            var testId = EnsureTestWithRate(1000m, out rateId);
            var dto = BuildDraft(patientId,
                new SaleInvoiceDetail { TestId = testId, Quantity = 1, Rate = 0, DiscountType = "Percentage", DiscountValue = 10m });

            var id = Services.SaleInvoice.Save(dto);
            var loaded = Services.SaleInvoice.GetById(id);
            var line = loaded.Details.First();

            Assert.AreEqual("Percentage", line.DiscountType);
            Assert.AreEqual(10m, line.DiscountValue);
            Assert.AreEqual(100m, line.DiscountAmount);
            Assert.AreEqual(900m, line.NetAmount);
            Assert.AreEqual(900m, loaded.Invoice.NetAmount);

            Services.SaleInvoice.Cancel(id);
            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }

        [TestMethod]
        public void SaleInvoice_Line_Fixed_Discount_Value_RoundTrips()
        {
            var patientId = CreatePatient();
            var rateId = 0;
            var testId = EnsureTestWithRate(1000m, out rateId);
            var dto = BuildDraft(patientId,
                new SaleInvoiceDetail { TestId = testId, Quantity = 1, Rate = 0, DiscountType = "Fixed Amount", DiscountValue = 150m });

            var id = Services.SaleInvoice.Save(dto);
            var loaded = Services.SaleInvoice.GetById(id);
            var line = loaded.Details.First();

            Assert.AreEqual("Fixed Amount", line.DiscountType);
            Assert.AreEqual(150m, line.DiscountValue);
            Assert.AreEqual(150m, line.DiscountAmount);
            Assert.AreEqual(850m, line.NetAmount);

            Services.SaleInvoice.Cancel(id);
            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }

        [TestMethod]
        public void SaleInvoice_Header_Percentage_Discount_Uses_DiscountValue_And_RoundTrips()
        {
            var patientId = CreatePatient();
            var rateId = 0;
            var testId = EnsureTestWithRate(200m, out rateId);
            var dto = BuildDraft(patientId, new SaleInvoiceDetail { TestId = testId, Quantity = 4, Rate = 0 });
            dto.Invoice.DiscountType = "Percentage";
            dto.Invoice.DiscountValue = 10m;

            var id = Services.SaleInvoice.Save(dto);
            var loaded = Services.SaleInvoice.GetById(id);

            Assert.AreEqual(800m, loaded.Invoice.GrossAmount);
            Assert.AreEqual("Percentage", loaded.Invoice.DiscountType);
            Assert.AreEqual(10m, loaded.Invoice.DiscountValue);
            Assert.AreEqual(80m, loaded.Invoice.DiscountAmount);
            Assert.AreEqual(720m, loaded.Invoice.NetAmount);

            Services.SaleInvoice.Cancel(id);
            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }

        [TestMethod]
        public void SaleInvoice_GetById_Invalid_Returns_Null()
        {
            Assert.IsNull(Services.SaleInvoice.GetById(-1));
        }

        [TestMethod]
        public void SaleInvoice_Confirmed_Edit_Preserves_Invoice_No()
        {
            var patientId = CreatePatient();
            var rateId = 0;
            var testId = EnsureTestWithRate(100m, out rateId);
            var invoiceNo = UniqueCode("INV");
            var dto = BuildDraft(patientId, new SaleInvoiceDetail { TestId = testId, Quantity = 1, Rate = 0 });
            dto.Invoice.InvoiceNo = invoiceNo;
            var id = Services.SaleInvoice.Save(dto);

            var loaded = Services.SaleInvoice.GetById(id);
            loaded.Invoice.InvoiceStatus = (int)InvoiceStatusType.Confirmed;
            loaded.Details.First().Quantity = 3;
            Services.SaleInvoice.Save(loaded);

            var edited = Services.SaleInvoice.GetById(id);
            Assert.AreEqual(invoiceNo, edited.Invoice.InvoiceNo);
            Assert.AreEqual(300m, edited.Invoice.NetAmount);

            Services.SaleInvoice.Cancel(id);
            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }
    }
}
