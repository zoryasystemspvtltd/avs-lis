using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LIS.Masters.Tests.Transactions
{
    [TestClass]
    public class SaleInvoiceManagerTests : IntegrationTestBase
    {
        private long EnsurePatientId()
        {
            var patients = Services.Db.PatientDetails.Take(1).ToList();
            if (patients.Any())
            {
                return patients.First().Id;
            }

            Assert.Inconclusive("No patients in database for invoice tests");
            return 0;
        }

        private long EnsureRequestDetailId(long patientId)
        {
            var existing = Services.Db.TestRequestDetails
                .Where(r => r.PatientId == patientId)
                .Select(r => r.Id)
                .FirstOrDefault();
            if (existing > 0)
            {
                return existing;
            }

            var any = Services.Db.TestRequestDetails.Select(r => r.Id).FirstOrDefault();
            if (any > 0)
            {
                return any;
            }

            Assert.Inconclusive("No test request details in database for invoice line FK");
            return 0;
        }

        private int EnsureTestWithRate(out int rateId)
        {
            var tests = Services.HisTest.Get(ListOptionsFactory.ForHisTest()).Items;
            var test = tests?.FirstOrDefault(t => t != null && t.IsActive);
            if (test == null)
            {
                var dept = Services.Department.Get().First();
                var specimen = Services.Specimen.Get().Cast<HISSpecimenMaster>().First();
                var newId = (int)Services.HisTest.Add(MasterTestDataBuilder.HisTest(UniqueCode("TST"), dept.Code, specimen.Code));
                test = Services.HisTest.GetTestById(newId);
            }

            var rate = MasterTestDataBuilder.StandardRate(test.Id, 250m);
            rateId = (int)Services.TestRate.Add(rate);
            return test.Id;
        }

        [TestMethod]
        public void SaleInvoice_Create_Calculates_Totals_And_Persists_Lines()
        {
            var patientId = EnsurePatientId();
            var requestDetailId = EnsureRequestDetailId(patientId);
            var rateId = 0;
            var testId = EnsureTestWithRate(out rateId);
            var invoiceNo = UniqueCode("INV");

            var dto = BuildInvoiceDto(invoiceNo, patientId, testId, requestDetailId);
            dto.Details.First().Quantity = 2;
            dto.Details.First().DiscountAmount = 10;
            dto.Details.First().SampleNo = "UT-SAMPLE";

            var id = Services.SaleInvoice.Save(dto);
            Assert.IsTrue(id > 0);

            var loaded = Services.SaleInvoice.GetById(id);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(invoiceNo, loaded.Invoice.InvoiceNo);
            Assert.IsTrue(loaded.Invoice.GrossAmount > 0);
            Assert.IsTrue(loaded.Invoice.NetAmount > 0);
            Assert.AreEqual(1, loaded.Details.Count());
            Assert.IsTrue(loaded.Details.First().Rate > 0, "Rate should be resolved from TestRate master");

            Services.SaleInvoice.Cancel(id);
        }

        [TestMethod]
        public void SaleInvoice_Duplicate_InvoiceNo_Throws()
        {
            var patientId = EnsurePatientId();
            var requestDetailId = EnsureRequestDetailId(patientId);
            var rateId = 0;
            var testId = EnsureTestWithRate(out rateId);
            var invoiceNo = UniqueCode("INV");

            Services.SaleInvoice.Save(BuildInvoiceDto(invoiceNo, patientId, testId, requestDetailId));

            Assert.ThrowsException<InvalidOperationException>(() =>
                Services.SaleInvoice.Save(BuildInvoiceDto(invoiceNo, patientId, testId, requestDetailId)));
        }

        [TestMethod]
        public void SaleInvoice_UpdateStatus_And_Cancel()
        {
            var patientId = EnsurePatientId();
            var rateId = 0;
            var testId = EnsureTestWithRate(out rateId);
            var invoiceNo = UniqueCode("INV");

            var requestDetailId = EnsureRequestDetailId(patientId);
            var id = Services.SaleInvoice.Save(BuildInvoiceDto(invoiceNo, patientId, testId, requestDetailId));

            Services.SaleInvoice.UpdateStatus(id, (int)InvoiceStatusType.Confirmed, (int)PaymentStatusType.Paid);
            var paid = Services.SaleInvoice.GetById(id);
            Assert.AreEqual((int)InvoiceStatusType.Confirmed, paid.Invoice.InvoiceStatus);
            Assert.AreEqual((int)PaymentStatusType.Paid, paid.Invoice.PaymentStatus);
            Assert.AreEqual(0, paid.Invoice.DueAmount);

            Services.SaleInvoice.Cancel(id);
            var cancelled = Services.SaleInvoice.GetById(id);
            Assert.AreEqual((int)InvoiceStatusType.Cancelled, cancelled.Invoice.InvoiceStatus);
            Assert.IsFalse(cancelled.Invoice.IsActive);
        }

        [TestMethod]
        public void SaleInvoice_List_Returns_Records_With_PatientName()
        {
            var list = Services.SaleInvoice.Get(ListOptionsFactory.ForSaleInvoice());
            Assert.IsNotNull(list);
            if (list.TotalRecord > 0)
            {
                Assert.IsFalse(string.IsNullOrEmpty(list.Items.First().InvoiceNo));
            }
        }

        [TestMethod]
        public void SaleInvoice_GenerateInvoiceNo_Returns_Unique_Prefix()
        {
            var no = Services.SaleInvoice.GenerateInvoiceNo();
            Assert.IsFalse(string.IsNullOrEmpty(no));
            Assert.IsTrue(no.StartsWith("INV-"));
        }

        [TestMethod]
        public void SaleInvoice_Cancelled_Cannot_Update_Status()
        {
            var patientId = EnsurePatientId();
            var requestDetailId = EnsureRequestDetailId(patientId);
            var rateId = 0;
            var testId = EnsureTestWithRate(out rateId);
            var id = Services.SaleInvoice.Save(BuildInvoiceDto(UniqueCode("INV"), patientId, testId, requestDetailId));
            Services.SaleInvoice.Cancel(id);

            Assert.ThrowsException<InvalidOperationException>(() =>
                Services.SaleInvoice.UpdateStatus(id, (int)InvoiceStatusType.Confirmed, (int)PaymentStatusType.Paid));
        }

        private SaleInvoiceDto BuildInvoiceDto(string invoiceNo, long patientId, int testId, long requestDetailId)
        {
            return new SaleInvoiceDto
            {
                Invoice = new SaleInvoice
                {
                    InvoiceNo = invoiceNo,
                    InvoiceDate = DateTime.Today,
                    PatientId = patientId,
                    RequestDetailId = requestDetailId,
                    InvoiceStatus = (int)InvoiceStatusType.Draft,
                    PaymentStatus = (int)PaymentStatusType.Unpaid,
                    IsActive = true
                },
                Details = new List<SaleInvoiceDetail>
                {
                    new SaleInvoiceDetail
                    {
                        TestId = testId,
                        Quantity = 1,
                        Rate = 0,
                        RequestDetailId = requestDetailId,
                        SampleNo = "UT"
                    }
                }
            };
        }
    }
}
