using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LIS.Masters.Tests.Workflows
{
    [TestClass]
    public class SampleCollectionInvoiceStatusTests : IntegrationTestBase
    {
        private long CreatePatientAndInvoice(
            string invoiceNo,
            int testId,
            InvoiceStatusType invoiceStatus,
            PaymentStatusType paymentStatus,
            out long requestId)
        {
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            var patientId = Services.PatientMaster.Add(MasterTestDataBuilder.Patient(suffix));
            Assert.IsTrue(patientId > 0);

            var invoiceId = Services.SaleInvoice.Save(new SaleInvoiceDto
            {
                Invoice = new SaleInvoice
                {
                    InvoiceNo = invoiceNo,
                    InvoiceDate = DateTime.Today,
                    PatientId = patientId,
                    InvoiceStatus = (int)invoiceStatus,
                    PaymentStatus = (int)paymentStatus,
                    IsActive = true
                },
                Details = new List<SaleInvoiceDetail>
                {
                    new SaleInvoiceDetail
                    {
                        TestId = testId,
                        Quantity = 1,
                        Rate = 0,
                        RequestDetailId = 0
                    }
                }
            });
            Assert.IsTrue(invoiceId > 0);

            var loaded = Services.SaleInvoice.GetById(invoiceId);
            var detail = loaded.Details.First();
            Assert.IsTrue(detail.RequestDetailId.HasValue && detail.RequestDetailId.Value > 0);
            requestId = detail.RequestDetailId.Value;
            return patientId;
        }

        private bool IsInCollectionQueue(long requestId, string invoiceNo)
        {
            var queue = Services.SampleCollection.GetPendingQueue(new SampleWorkflowSearchOptions
            {
                CurrentPage = 1,
                RecordPerPage = 500,
                OrderNumber = invoiceNo
            });
            return queue.Items.Any(r => r.Id == requestId);
        }

        private bool IsInReceivingQueue(long requestId, string orderNumber)
        {
            var queue = Services.SampleReceiving.GetReceivingQueue(new SampleWorkflowSearchOptions
            {
                CurrentPage = 1,
                RecordPerPage = 500,
                OrderNumber = orderNumber
            });
            return queue.Items.Any(r => r.Id == requestId);
        }

        [TestMethod]
        public void Draft_Invoice_Not_In_Collection_Or_Receiving_Queue()
        {
            var rateId = 0;
            var testId = EnsureTestWithStandardRate(250m, out rateId);
            var invoiceNo = UniqueCode("DRF");
            CreatePatientAndInvoice(invoiceNo, testId, InvoiceStatusType.Draft, PaymentStatusType.Unpaid, out var requestId);

            Assert.IsFalse(IsInCollectionQueue(requestId, invoiceNo));

            var ex = Assert.ThrowsException<InvalidOperationException>(() =>
                Services.SampleCollection.CollectSample(new SampleCollectionAction
                {
                    TestRequestId = requestId,
                    CollectionDateTime = DateTime.Now
                }));
            Assert.IsTrue(ex.Message.IndexOf("confirmed", StringComparison.OrdinalIgnoreCase) >= 0);

            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }

        [TestMethod]
        public void Confirmed_Unpaid_Appears_In_Collection_Queue()
        {
            var rateId = 0;
            var testId = EnsureTestWithStandardRate(250m, out rateId);
            var invoiceNo = UniqueCode("CNF");
            CreatePatientAndInvoice(invoiceNo, testId, InvoiceStatusType.Confirmed, PaymentStatusType.Unpaid, out var requestId);

            Assert.IsTrue(IsInCollectionQueue(requestId, invoiceNo));

            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }

        [TestMethod]
        public void Confirmed_PartialPayment_Appears_In_Collection_Queue()
        {
            var rateId = 0;
            var testId = EnsureTestWithStandardRate(250m, out rateId);
            var invoiceNo = UniqueCode("PRT");
            CreatePatientAndInvoice(invoiceNo, testId, InvoiceStatusType.Confirmed, PaymentStatusType.Partial, out var requestId);

            Assert.IsTrue(IsInCollectionQueue(requestId, invoiceNo));

            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }

        [TestMethod]
        public void Confirmed_Unpaid_Collect_Then_Appears_In_Receiving_Queue()
        {
            var rateId = 0;
            var testId = EnsureTestWithStandardRate(250m, out rateId);
            var invoiceNo = UniqueCode("RCV");
            CreatePatientAndInvoice(invoiceNo, testId, InvoiceStatusType.Confirmed, PaymentStatusType.Unpaid, out var requestId);

            Services.SampleCollection.CollectSample(new SampleCollectionAction
            {
                TestRequestId = requestId,
                CollectionDateTime = DateTime.Now,
                Remarks = "UT collect"
            });

            Assert.IsFalse(IsInCollectionQueue(requestId, invoiceNo));
            Assert.IsTrue(IsInReceivingQueue(requestId, invoiceNo));

            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }

        [TestMethod]
        public void Confirmed_Unpaid_Reject_At_Receiving_Returns_To_Collection_Queue()
        {
            var rateId = 0;
            var testId = EnsureTestWithStandardRate(250m, out rateId);
            var invoiceNo = UniqueCode("RRJ");
            CreatePatientAndInvoice(invoiceNo, testId, InvoiceStatusType.Confirmed, PaymentStatusType.Unpaid, out var requestId);

            Services.SampleCollection.CollectSample(new SampleCollectionAction
            {
                TestRequestId = requestId,
                CollectionDateTime = DateTime.Now,
                Remarks = "UT collect before receiving reject"
            });

            Assert.IsTrue(IsInReceivingQueue(requestId, invoiceNo));
            Assert.IsFalse(IsInCollectionQueue(requestId, invoiceNo));

            Services.SampleReceiving.RejectSample(new SampleRejectionAction
            {
                TestRequestId = requestId,
                RejectionReasonCode = "RJ01",
                Remarks = "UT receiving reject"
            });

            Assert.IsFalse(IsInReceivingQueue(requestId, invoiceNo));
            Assert.IsTrue(IsInCollectionQueue(requestId, invoiceNo));

            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }

        [TestMethod]
        public void Paid_Invoice_Still_Appears_In_Collection_Queue()
        {
            var rateId = 0;
            var testId = EnsureTestWithStandardRate(250m, out rateId);
            var invoiceNo = UniqueCode("PAD");
            CreatePatientAndInvoice(invoiceNo, testId, InvoiceStatusType.Paid, PaymentStatusType.Paid, out var requestId);

            Assert.IsTrue(IsInCollectionQueue(requestId, invoiceNo));

            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }
    }
}
