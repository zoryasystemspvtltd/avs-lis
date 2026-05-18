using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LIS.Masters.Tests.Transactions
{
    [TestClass]
    public class SaleInvoiceEditWorkflowTests : IntegrationTestBase
    {
        private long CreatePatient()
        {
            return Services.PatientMaster.Add(MasterTestDataBuilder.Patient(Guid.NewGuid().ToString("N").Substring(0, 8)));
        }

        private int EnsureTestWithRate(out int rateId)
        {
            var test = Services.HisTest.Get(ListOptionsFactory.ForHisTest()).Items?.FirstOrDefault();
            if (test == null)
            {
                var dept = Services.Department.Get().First();
                var specimen = Services.Specimen.Get().Cast<HISSpecimenMaster>().First();
                var newId = (int)Services.HisTest.Add(MasterTestDataBuilder.HisTest(UniqueCode("TST"), dept.Code, specimen.Code));
                test = Services.HisTest.GetTestById(newId);
            }

            var rate = MasterTestDataBuilder.StandardRate(test.Id, 200m);
            rateId = (int)Services.TestRate.Add(rate);
            return test.Id;
        }

        [TestMethod]
        public void SaleInvoice_Edit_Replaces_Lines_Without_Duplicate_Detail_Rows()
        {
            var patientId = CreatePatient();
            var rateId = 0;
            var testId = EnsureTestWithRate(out rateId);
            var invoiceNo = UniqueCode("INV");

            var dto = new SaleInvoiceDto
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
                Details = new List<SaleInvoiceDetail>
                {
                    new SaleInvoiceDetail { TestId = testId, Quantity = 1, Rate = 0, RequestDetailId = 0 }
                }
            };

            var id = Services.SaleInvoice.Save(dto);
            var first = Services.SaleInvoice.GetById(id);
            var detailId = first.Details.First().Id;

            first.Details.First().Quantity = 3;
            first.Details.First().DiscountAmount = 25;
            Services.SaleInvoice.Save(first);

            var edited = Services.SaleInvoice.GetById(id);
            Assert.AreEqual(1, edited.Details.Count(), "Edit must replace lines, not append duplicates");
            Assert.AreEqual(3, edited.Details.First().Quantity);
            Assert.IsTrue(edited.Invoice.NetAmount > 0);

            var detailRows = Services.Db.SaleInvoiceDetails.Where(d => d.SaleInvoiceId == id && d.IsActive).ToList();
            Assert.AreEqual(1, detailRows.Count);
            Assert.AreNotEqual(detailId, edited.Details.First().Id, "Edit replaces detail rows with new ids");

            Services.SaleInvoice.Cancel(id);
            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }

        [TestMethod]
        public void SaleInvoice_Corporate_Rate_Applied_When_Corporate_Set()
        {
            var patientId = CreatePatient();
            var rateId = 0;
            var testId = EnsureTestWithRate(out rateId);
            var corpCode = UniqueCode("CORP");
            var corpId = (int)Services.Corporate.Add(MasterTestDataBuilder.Corporate(corpCode));

            var corpRate = MasterTestDataBuilder.StandardRate(testId, 175m);
            corpRate.RateType = (int)RateType.Corporate;
            corpRate.CorporateId = corpId;
            var corpRateId = (int)Services.TestRate.Add(corpRate);

            var invoiceNo = UniqueCode("INV");
            var dto = new SaleInvoiceDto
            {
                Invoice = new SaleInvoice
                {
                    InvoiceNo = invoiceNo,
                    InvoiceDate = DateTime.Today,
                    PatientId = patientId,
                    CorporateId = corpId,
                    InvoiceStatus = (int)InvoiceStatusType.Draft,
                    PaymentStatus = (int)PaymentStatusType.Unpaid,
                    IsActive = true
                },
                Details = new List<SaleInvoiceDetail>
                {
                    new SaleInvoiceDetail { TestId = testId, Quantity = 1, Rate = 0, RequestDetailId = 0 }
                }
            };

            var id = Services.SaleInvoice.Save(dto);
            var loaded = Services.SaleInvoice.GetById(id);
            Assert.AreEqual(175m, loaded.Details.First().Rate);

            Services.SaleInvoice.Cancel(id);
            Services.TestRate.Delete(new TestRateMaster { Id = corpRateId });
            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
            Services.Corporate.Delete(new CorporateMaster { Id = corpId });
        }
    }
}
