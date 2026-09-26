using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LIS.Masters.Tests.Masters
{
    [TestClass]
    public class SampleCollectorMasterTests : IntegrationTestBase
    {
        [TestMethod]
        public void SampleCollector_Create_Edit_Deactivate()
        {
            var code = UniqueCode("SC");
            var id = (int)Services.SampleCollector.Add(MasterTestDataBuilder.SampleCollector(code));
            Assert.IsTrue(id > 0);

            var loaded = Services.SampleCollector.GetById(id);
            Assert.AreEqual(code, loaded.Code);
            Assert.AreEqual("8888888888", loaded.Phone);
            Assert.IsTrue(loaded.IsActive);

            loaded.Name = "Updated " + code;
            loaded.Phone = "7777666655";
            Services.SampleCollector.Update(loaded);
            var edited = Services.SampleCollector.GetById(id);
            Assert.AreEqual("Updated " + code, edited.Name);
            Assert.AreEqual("7777666655", edited.Phone);

            Services.SampleCollector.Delete(new SampleCollectorMaster { Id = id });
            Assert.IsFalse(Services.SampleCollector.GetById(id).IsActive);
            Assert.IsFalse(Services.SampleCollector.GetAllActive().Any(c => c.Id == id));
        }

        [TestMethod]
        public void Invoice_Without_Collector_Saves_And_Reloads_Null()
        {
            var invoiceId = SaveInvoice(null, null, null);
            var loaded = Services.SaleInvoice.GetById(invoiceId);
            Assert.IsNull(loaded.Invoice.CollectorId);
            Assert.IsNull(loaded.Invoice.ReferralDoctorId);
            Assert.IsNull(loaded.Invoice.CorporateId);
            AssertCollectedByUnchanged(loaded);
        }

        [TestMethod]
        public void Invoice_With_Collector_Saves_Reloads_And_Clears()
        {
            var collectorId = (int)Services.SampleCollector.Add(MasterTestDataBuilder.SampleCollector(UniqueCode("SC")));
            Services.CollectorCommissionRate.Add(new SampleCollectorCommissionRate
            {
                CollectorId = collectorId,
                CommissionPercent = 10m,
                EffectiveFrom = DateTime.Today.AddDays(-1),
                IsActive = true
            });
            var invoiceId = SaveInvoice(collectorId, null, null);

            var loaded = Services.SaleInvoice.GetById(invoiceId);
            Assert.AreEqual(collectorId, loaded.Invoice.CollectorId);
            Assert.AreEqual(1000m, loaded.Invoice.NetAmount);
            Assert.AreEqual(0m, loaded.Invoice.PaidAmount);
            AssertCollectedByUnchanged(loaded);

            loaded.Invoice.CollectorId = null;
            Services.SaleInvoice.Save(loaded);
            var cleared = Services.SaleInvoice.GetById(invoiceId);
            Assert.IsNull(cleared.Invoice.CollectorId);
        }

        [TestMethod]
        public void Invoice_Allows_ReferralDoctor_And_Collector_Together()
        {
            var doctorId = (int)Services.ReferralDoctor.Add(MasterTestDataBuilder.ReferralDoctor(UniqueCode("RD")));
            var collectorId = (int)Services.SampleCollector.Add(MasterTestDataBuilder.SampleCollector(UniqueCode("SC")));
            var corporateId = (int)Services.Corporate.Add(MasterTestDataBuilder.Corporate(UniqueCode("CO")));
            Services.DoctorCommissionRate.Add(new ReferralDoctorCommissionRate
            {
                ReferralDoctorId = doctorId,
                CommissionPercent = 10m,
                EffectiveFrom = DateTime.Today.AddDays(-1),
                IsActive = true
            });

            var invoiceId = SaveInvoice(collectorId, doctorId, corporateId);
            var loaded = Services.SaleInvoice.GetById(invoiceId);
            Assert.AreEqual(doctorId, loaded.Invoice.ReferralDoctorId);
            Assert.AreEqual(collectorId, loaded.Invoice.CollectorId);
            Assert.AreEqual(corporateId, loaded.Invoice.CorporateId);
            AssertCollectedByUnchanged(loaded);
        }

        [TestMethod]
        public void Invoice_Rejects_Unknown_CollectorId()
        {
            var ex = Assert.ThrowsException<ArgumentException>(() => SaveInvoice(int.MaxValue, null, null));
            StringAssert.Contains(ex.Message, "not found");
        }

        [TestMethod]
        public void SampleCollector_Allows_Duplicate_Code_Like_ReferralDoctor()
        {
            var code = UniqueCode("SC");
            var first = (int)Services.SampleCollector.Add(MasterTestDataBuilder.SampleCollector(code));
            var second = (int)Services.SampleCollector.Add(MasterTestDataBuilder.SampleCollector(code));
            Assert.AreNotEqual(first, second);
            Assert.AreEqual(code, Services.SampleCollector.GetById(second).Code);
        }

        [TestMethod]
        public void SampleCollector_MenuPermission_Matches_ReferralDoctor()
        {
            var missing = Services.Db.Database.SqlQuery<int>(@"
SELECT COUNT(*) FROM RoleMenuPermission r
WHERE r.MenuKey = 'MASTER_REFERRAL_DOCTOR'
AND NOT EXISTS (
    SELECT 1 FROM RoleMenuPermission c
    WHERE c.MenuKey = 'MASTER_SAMPLE_COLLECTOR'
      AND c.RoleId = r.RoleId
      AND c.ModuleId = r.ModuleId
      AND c.CanView = r.CanView
      AND c.CanAdd = r.CanAdd
      AND c.CanEdit = r.CanEdit
      AND c.CanDelete = r.CanDelete
      AND c.IsActive = r.IsActive)").Single();
            Assert.AreEqual(0, missing);

            var granted = Services.Db.Database.SqlQuery<int>(
                "SELECT COUNT(*) FROM RoleMenuPermission WHERE MenuKey = 'MASTER_SAMPLE_COLLECTOR' AND IsActive = 1").Single();
            Assert.IsTrue(granted > 0);
        }

        [TestMethod]
        public void Invoice_Rejects_Newly_Selected_Inactive_Collector()
        {
            var collectorId = (int)Services.SampleCollector.Add(MasterTestDataBuilder.SampleCollector(UniqueCode("SC"), false));
            var ex = Assert.ThrowsException<ArgumentException>(() => SaveInvoice(collectorId, null, null));
            StringAssert.Contains(ex.Message, "inactive");
        }

        private long SaveInvoice(int? collectorId, int? doctorId, int? corporateId)
        {
            var testId = EnsureTestWithStandardRate(1000m, out _);
            var patientId = Services.PatientMaster.Add(MasterTestDataBuilder.Patient(UniqueCode("PT", 8)));
            return Services.SaleInvoice.Save(new SaleInvoiceDto
            {
                Invoice = new SaleInvoice
                {
                    InvoiceNo = UniqueCode("INV"),
                    InvoiceDate = DateTime.Today,
                    PatientId = patientId,
                    InvoiceStatus = (int)InvoiceStatusType.Confirmed,
                    PaymentStatus = (int)PaymentStatusType.Unpaid,
                    PaidAmount = 0,
                    CollectorId = collectorId,
                    ReferralDoctorId = doctorId,
                    CorporateId = corporateId,
                    IsActive = true
                },
                Details = new List<SaleInvoiceDetail>
                {
                    new SaleInvoiceDetail
                    {
                        TestId = testId,
                        Quantity = 1,
                        Rate = 1000m,
                        IsActive = true
                    }
                }
            });
        }

        private void AssertCollectedByUnchanged(SaleInvoiceDto loaded)
        {
            var requestId = loaded.Details.First().RequestDetailId;
            Assert.IsTrue(requestId.HasValue && requestId.Value > 0);
            var request = Services.Db.TestRequestDetails.Find(requestId.Value);
            Assert.IsTrue(string.IsNullOrEmpty(request.CollectedBy));
        }
    }
}
