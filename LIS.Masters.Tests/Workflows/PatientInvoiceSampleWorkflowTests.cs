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
            return EnsureTestWithStandardRate(rateAmount, out rateId);
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

        [TestMethod]
        public void Invoice_Same_Specimen_Reuses_Specimen_Barcode()
        {
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            var patientId = Services.PatientMaster.Add(MasterTestDataBuilder.Patient(suffix));
            var specimen = Services.Specimen.Get().Cast<HISSpecimenMaster>().First();
            var dept = Services.Department.Get().Cast<Departments>().First();
            var testCode1 = UniqueCode("T1");
            var testCode2 = UniqueCode("T2");
            var testId1 = (int)Services.HisTest.Add(MasterTestDataBuilder.HisTest(testCode1, dept.Code, specimen.Code));
            var testId2 = (int)Services.HisTest.Add(MasterTestDataBuilder.HisTest(testCode2, dept.Code, specimen.Code));
            var rateId1 = (int)Services.TestRate.Add(MasterTestDataBuilder.StandardRate(testId1, 150m));
            var rateId2 = (int)Services.TestRate.Add(MasterTestDataBuilder.StandardRate(testId2, 175m));
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
                    new SaleInvoiceDetail { TestId = testId1, Quantity = 1, Rate = 0, RequestDetailId = 0 },
                    new SaleInvoiceDetail { TestId = testId2, Quantity = 1, Rate = 0, RequestDetailId = 0 }
                }
            };

            Services.SaleInvoice.Save(dto);

            var requests = Services.Db.TestRequestDetails
                .Where(r => r.PatientId == patientId && r.HISRequestNo == invoiceNo)
                .ToList();

            Assert.AreEqual(2, requests.Count);
            Assert.IsFalse(string.IsNullOrWhiteSpace(requests[0].SampleNo));
            Assert.AreEqual(requests[0].SampleNo, requests[1].SampleNo);
            Assert.AreEqual(invoiceNo, requests[0].HISRequestNo);
            Assert.AreEqual(invoiceNo, requests[0].HISRequestId);
            Assert.AreEqual(
                LIS.BusinessLogic.Helper.Helper.BuildSampleNo(invoiceNo, specimen.Code),
                requests[0].SampleNo);
            Assert.IsFalse(requests[0].SampleNo.Contains("INV"), "SampleNo must not contain INV");
            Assert.IsFalse(requests[0].SampleNo.Contains("-"), "SampleNo must not contain hyphens");

            Services.TestRate.Delete(new TestRateMaster { Id = rateId1 });
            Services.TestRate.Delete(new TestRateMaster { Id = rateId2 });
        }

        [TestMethod]
        public void Invoice_Multiple_Tests_And_Profile_With_Different_Specimens()
        {
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            var patientId = Services.PatientMaster.Add(MasterTestDataBuilder.Patient(suffix));
            var dept = Services.Department.Get().Cast<Departments>().First();

            var specimens = Services.Specimen.Get().Where(s => s.IsActive).ToList();
            var specA = specimens.First();
            var specB = specimens.Skip(1).FirstOrDefault();
            if (specB == null)
            {
                var newSpecCode = UniqueCode("SPB", 20);
                Services.Specimen.Add(new HISSpecimenMaster
                {
                    Code = newSpecCode,
                    Name = "Specimen B",
                    IsActive = true,
                    CreatedOn = DateTime.Now
                });
                specB = Services.Specimen.Get().First(s => s.Code == newSpecCode);
            }

            // Standalone tests with different specimens
            var standaloneSerumId = (int)Services.HisTest.Add(MasterTestDataBuilder.HisTest(UniqueCode("SA"), dept.Code, specA.Code));
            var standaloneEdtaId = (int)Services.HisTest.Add(MasterTestDataBuilder.HisTest(UniqueCode("SB"), dept.Code, specB.Code));
            var rateA = (int)Services.TestRate.Add(MasterTestDataBuilder.StandardRate(standaloneSerumId, 120m));
            var rateB = (int)Services.TestRate.Add(MasterTestDataBuilder.StandardRate(standaloneEdtaId, 130m));

            // Profile tests with different specimens
            var profSerumId = (int)Services.HisTest.Add(MasterTestDataBuilder.HisTest(UniqueCode("PA"), dept.Code, specA.Code));
            var profEdtaId = (int)Services.HisTest.Add(MasterTestDataBuilder.HisTest(UniqueCode("PB"), dept.Code, specB.Code));
            var profile = MasterTestDataBuilder.Profile(UniqueCode("PROF"), profSerumId, profEdtaId);
            profile.PackageRate = 500m;
            Services.TestProfile.SaveWithDetails(profile, profile.ProfileDetails);

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
                    new SaleInvoiceDetail { TestId = standaloneSerumId, Quantity = 1, Rate = 0, RequestDetailId = 0 },
                    new SaleInvoiceDetail { TestId = standaloneEdtaId, Quantity = 1, Rate = 0, RequestDetailId = 0 },
                    new SaleInvoiceDetail { TestProfileId = profile.Id, Quantity = 1, Rate = 0, RequestDetailId = 0 }
                }
            };

            var invoiceId = Services.SaleInvoice.Save(dto);
            Assert.IsTrue(invoiceId > 0);

            var requests = Services.Db.TestRequestDetails
                .Where(r => r.PatientId == patientId && r.HISRequestNo == invoiceNo)
                .ToList();

            // 2 standalone + 2 profile tests = 4 requests
            Assert.AreEqual(4, requests.Count, "Should create one request per lab test across lines and profile");

            var sampleBySpecimen = requests
                .Where(r => !string.IsNullOrWhiteSpace(r.SpecimenCode))
                .GroupBy(r => r.SpecimenCode)
                .ToDictionary(g => g.Key, g => g.Select(x => x.SampleNo).Distinct().ToList());

            Assert.IsTrue(sampleBySpecimen.Count >= 2, "Should have requests for at least two specimen types");
            foreach (var kv in sampleBySpecimen)
            {
                Assert.AreEqual(1, kv.Value.Count, $"Specimen {kv.Key} should reuse one barcode across tests");
                Assert.IsFalse(string.IsNullOrWhiteSpace(kv.Value[0]), $"Specimen {kv.Key} must have SampleNo");
            }

            foreach (var r in requests)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(r.SampleNo), "Every request must have a sample number");
                Assert.AreEqual(ReportStatusType.New, r.ReportStatus);
            }

            Services.SaleInvoice.Cancel(invoiceId);
            Services.TestRate.Delete(new TestRateMaster { Id = rateA });
            Services.TestRate.Delete(new TestRateMaster { Id = rateB });
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
