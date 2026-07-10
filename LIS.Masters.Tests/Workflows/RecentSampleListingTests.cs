using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LIS.Masters.Tests.Workflows
{
    [TestClass]
    public class RecentSampleListingTests : IntegrationTestBase
    {
        private long CreateReceivedRequest(out string invoiceNo, out long requestId)
        {
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            var patientId = Services.PatientMaster.Add(MasterTestDataBuilder.Patient(suffix));
            Assert.IsTrue(patientId > 0);

            var rateId = 0;
            var testId = EnsureTestWithStandardRate(250m, out rateId);
            invoiceNo = UniqueCode("RSMP");

            var invoiceId = Services.SaleInvoice.Save(new SaleInvoiceDto
            {
                Invoice = new SaleInvoice
                {
                    InvoiceNo = invoiceNo,
                    InvoiceDate = DateTime.Today,
                    PatientId = patientId,
                    InvoiceStatus = (int)InvoiceStatusType.Confirmed,
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
                        RequestDetailId = 0
                    }
                }
            });
            Assert.IsTrue(invoiceId > 0);

            requestId = Services.SaleInvoice.GetById(invoiceId).Details.First().RequestDetailId.Value;

            Services.SampleCollection.CollectSample(new SampleCollectionAction
            {
                TestRequestId = requestId,
                CollectionDateTime = DateTime.Now.AddMinutes(-5)
            });

            Services.SampleReceiving.ReceiveSample(new SampleReceivingAction
            {
                TestRequestId = requestId,
                ReceivedDateTime = DateTime.Now
            });

            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
            return patientId;
        }

        private bool IsInRecentSampleList(long requestId, string invoiceNo)
        {
            var list = Services.PatientWorkflow.Get(new ListOptions
            {
                CurrentPage = 1,
                RecordPerPage = 500,
                ReceivedOnly = true,
                SearchText = invoiceNo
            });

            return list.Items.Any(r => r.Id == requestId);
        }

        [TestMethod]
        public void Recent_Sample_List_Includes_Received_New_And_SentToEquipment_Only()
        {
            CreateReceivedRequest(out var invoiceNo, out var requestId);

            Assert.IsTrue(IsInRecentSampleList(requestId, invoiceNo), "Received New sample should appear in Recent Samples");

            Services.TestRequest.UpdateStatus(requestId, ReportStatusType.SentToEquipment);
            Assert.IsTrue(IsInRecentSampleList(requestId, invoiceNo), "Received SentToEquipment sample should appear in Recent Samples");

            Services.TestRequest.UpdateStatus(requestId, ReportStatusType.ReportGenerated);
            Assert.IsFalse(IsInRecentSampleList(requestId, invoiceNo), "Report Generated sample should not appear in Recent Samples");

            Services.TestRequest.UpdateStatus(requestId, ReportStatusType.TechnicianApproved);
            Assert.IsFalse(IsInRecentSampleList(requestId, invoiceNo), "Technician Approved sample should not appear in Recent Samples");
        }
    }
}
