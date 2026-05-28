using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LIS.Masters.Tests.Workflows
{
    /// <summary>
    /// Patient Registration → Sale Invoice → TestRequestDetails (sample workflow).
    /// </summary>
    [TestClass]
    public class PatientInvoiceSampleWorkflowTests : IntegrationTestBase
    {
        private int EnsureTestWithRate(decimal rateAmount, out int rateId)
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

            var rate = MasterTestDataBuilder.StandardRate(test.Id, rateAmount);
            rateId = (int)Services.TestRate.Add(rate);
            return test.Id;
        }

        [TestMethod]
        public void Patient_To_Invoice_Creates_TestRequestDetails_With_Valid_FKs()
        {
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            var patientId = Services.PatientMaster.Add(MasterTestDataBuilder.Patient(suffix));
            Assert.IsTrue(patientId > 0);

            var rateId = 0;
            var testId = EnsureTestWithRate(300m, out rateId);
            var test = Services.HisTest.GetTestById(testId);
            var invoiceNo = UniqueCode("INV");

            var beforeCount = Services.Db.TestRequestDetails
                .Count(r => r.PatientId == patientId && r.HISRequestNo == invoiceNo);

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
                    new SaleInvoiceDetail
                    {
                        TestId = testId,
                        Quantity = 1,
                        Rate = 0,
                        RequestDetailId = 0,
                        SampleNo = invoiceNo
                    }
                }
            };

            var invoiceId = Services.SaleInvoice.Save(dto);
            Assert.IsTrue(invoiceId > 0);

            var loaded = Services.SaleInvoice.GetById(invoiceId);
            Assert.AreEqual(patientId, loaded.Invoice.PatientId);
            Assert.IsTrue(loaded.Invoice.NetAmount > 0, "Net amount should be calculated from effective rate");
            Assert.AreEqual(1, loaded.Details.Count());

            var afterRequests = Services.Db.TestRequestDetails
                .Where(r => r.PatientId == patientId && r.HISRequestNo == invoiceNo)
                .ToList();

            Assert.AreEqual(beforeCount + 1, afterRequests.Count, "Invoice save should create one TestRequestDetail per test line");
            var request = afterRequests.First();
            Assert.AreEqual(test.HISTestCode, request.HISTestCode);
            Assert.AreEqual(ReportStatusType.New, request.ReportStatus, "New requests should appear in technician approval queue");
            Assert.IsFalse(string.IsNullOrEmpty(request.SampleNo));
            Assert.IsTrue(request.SampleCollectionDate > DateTime.MinValue);

            var techList = Services.PatientWorkflow.Get(new ListOptions
            {
                Status = ReportStatusType.New,
                CurrentPage = 1,
                RecordPerPage = 200,
                SearchText = invoiceNo
            });
            Assert.IsNotNull(techList);
            Assert.IsTrue(techList.Items.Any(r => r.Id == request.Id), "Technician approval list should include the new request");

            Services.SaleInvoice.Cancel(invoiceId);
            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }

        [TestMethod]
        public void Invoice_Save_Does_Not_Duplicate_TestRequest_On_Second_Identical_Invoice_No()
        {
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            var patientId = Services.PatientMaster.Add(MasterTestDataBuilder.Patient(suffix));
            var rateId = 0;
            var testId = EnsureTestWithRate(100m, out rateId);
            var invoiceNo = UniqueCode("INV");

            Services.SaleInvoice.Save(BuildMinimalInvoice(invoiceNo, patientId, testId));

            var countAfterFirst = Services.Db.TestRequestDetails
                .Count(r => r.PatientId == patientId && r.HISRequestNo == invoiceNo);

            Assert.ThrowsException<InvalidOperationException>(() =>
                Services.SaleInvoice.Save(BuildMinimalInvoice(invoiceNo, patientId, testId)));
            var countAfterDuplicateAttempt = Services.Db.TestRequestDetails
                .Count(r => r.PatientId == patientId && r.HISRequestNo == invoiceNo);

            Assert.AreEqual(countAfterFirst, countAfterDuplicateAttempt, "Failed duplicate invoice save must not add more requests");

            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }

        private static SaleInvoiceDto BuildMinimalInvoice(string invoiceNo, long patientId, int testId)
        {
            return new SaleInvoiceDto
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
        }
    }
}
