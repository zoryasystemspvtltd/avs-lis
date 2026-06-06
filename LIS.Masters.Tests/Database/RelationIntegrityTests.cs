using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LIS.Masters.Tests.Database
{
    [TestClass]
    public class RelationIntegrityTests : IntegrationTestBase
    {
        [TestMethod]
        public void SaleInvoice_Details_Reference_Valid_Patient_And_Test()
        {
            var patientId = Services.PatientMaster.Add(MasterTestDataBuilder.Patient(UniqueCode("REL")));
            var dept = Services.Department.Get().First();
            var specimen = Services.Specimen.Get().Cast<HISSpecimenMaster>().First();
            var testId = (int)Services.HisTest.Add(MasterTestDataBuilder.HisTest(UniqueCode("TST"), dept.Code, specimen.Code));
            var rateId = (int)Services.TestRate.Add(MasterTestDataBuilder.StandardRate(testId, 50m));

            var invoiceNo = UniqueCode("INV");
            var id = Services.SaleInvoice.Save(new SaleInvoiceDto
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
            });

            var header = Services.Db.SaleInvoices.Find(id);
            var patient = Services.Db.PatientDetails.Find(patientId);
            var details = Services.Db.SaleInvoiceDetails.Where(d => d.SaleInvoiceId == id).ToList();
            var test = Services.Db.HisTestMaster.Find(testId);

            Assert.IsNotNull(header);
            Assert.IsNotNull(patient);
            Assert.IsTrue(details.Any());
            Assert.IsNotNull(test);
            Assert.AreEqual(patientId, header.PatientId);
            Assert.IsTrue(details.All(d => d.TestId == testId));

            var requests = Services.Db.TestRequestDetails
                .Where(r => r.PatientId == patientId && r.HISRequestNo == invoiceNo)
                .ToList();
            Assert.IsTrue(requests.Any());
            Assert.IsTrue(requests.All(r => Services.Db.PatientDetails.Any(p => p.Id == r.PatientId)));

            Services.SaleInvoice.Cancel(id);
            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }

        [TestMethod]
        public void TestProfile_Details_Link_To_Valid_Tests()
        {
            var dept = Services.Department.Get().First();
            var specimen = Services.Specimen.Get().Cast<HISSpecimenMaster>().First();
            var testId = (int)Services.HisTest.Add(MasterTestDataBuilder.HisTest(UniqueCode("TST"), dept.Code, specimen.Code));
            var code = UniqueCode("PROF");
            var profile = MasterTestDataBuilder.Profile(code, testId);
            Services.TestProfile.SaveWithDetails(profile, profile.ProfileDetails);

            var lines = Services.Db.TestProfileDetail.Where(d => d.TestProfileId == profile.Id).ToList();
            Assert.IsTrue(lines.Any());
            Assert.IsTrue(lines.All(l => Services.Db.HisTestMaster.Any(t => t.Id == l.TestId)));
        }
    }
}
