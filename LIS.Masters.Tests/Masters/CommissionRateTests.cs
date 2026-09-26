using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LIS.Masters.Tests.Masters
{
    [TestClass]
    public class CommissionRateTests : IntegrationTestBase
    {
        [TestMethod]
        public void DoctorRate_Accepts_Valid_Percentages_And_Open_End()
        {
            var doctorId = Doctor();
            var zero = AddDoctor(doctorId, 0m, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
            var full = AddDoctor(doctorId, 100m, new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));
            var open = AddDoctor(doctorId, 10.50m, new DateTime(2026, 3, 1), null);
            Assert.AreEqual(0m, Services.DoctorCommissionRate.GetById(zero).CommissionPercent);
            Assert.AreEqual(100m, Services.DoctorCommissionRate.GetById(full).CommissionPercent);
            Assert.IsNull(Services.DoctorCommissionRate.GetById(open).EffectiveTo);
            Assert.IsFalse(Services.DoctorCommissionRate.GetById(open).IsLocked);
        }

        [TestMethod]
        public void DoctorRate_Rejects_Invalid_Percentages()
        {
            var doctorId = Doctor();
            Assert.ThrowsException<ArgumentException>(() => AddDoctor(doctorId, -1m, new DateTime(2026, 4, 1), new DateTime(2026, 4, 30)));
            Assert.ThrowsException<ArgumentException>(() => AddDoctor(doctorId, 100.01m, new DateTime(2026, 4, 1), new DateTime(2026, 4, 30)));
            Assert.ThrowsException<ArgumentException>(() => AddDoctor(doctorId, 5.123m, new DateTime(2026, 4, 1), new DateTime(2026, 4, 30)));
        }

        [TestMethod]
        public void DoctorRate_Rejects_From_After_To_And_Overlap_Allows_Adjacent()
        {
            var doctorId = Doctor();
            Assert.ThrowsException<ArgumentException>(() =>
                AddDoctor(doctorId, 5m, new DateTime(2026, 6, 15), new DateTime(2026, 6, 1)));

            AddDoctor(doctorId, 10m, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30));
            Assert.ThrowsException<InvalidOperationException>(() =>
                AddDoctor(doctorId, 12m, new DateTime(2026, 9, 15), new DateTime(2026, 10, 31)));

            var next = AddDoctor(doctorId, 12m, new DateTime(2026, 10, 1), new DateTime(2026, 12, 31));
            Assert.IsTrue(next > 0);

            AddDoctor(doctorId, 8m, new DateTime(2027, 1, 1), null);
            Assert.ThrowsException<InvalidOperationException>(() =>
                AddDoctor(doctorId, 9m, new DateTime(2027, 6, 1), null));
        }

        [TestMethod]
        public void Rates_For_Different_Recipients_Do_Not_Overlap()
        {
            var first = Doctor();
            var second = Doctor();
            var collectorId = (int)Services.SampleCollector.Add(MasterTestDataBuilder.SampleCollector(UniqueCode("SC")));
            AddDoctor(first, 10m, new DateTime(2026, 5, 1), new DateTime(2026, 5, 31));
            AddDoctor(second, 10m, new DateTime(2026, 5, 1), new DateTime(2026, 5, 31));
            var collectorRate = (int)Services.CollectorCommissionRate.Add(new SampleCollectorCommissionRate
            {
                CollectorId = collectorId,
                CommissionPercent = 10m,
                EffectiveFrom = new DateTime(2026, 5, 1),
                EffectiveTo = new DateTime(2026, 5, 31),
                IsActive = true
            });
            Assert.IsTrue(collectorRate > 0);
        }

        [TestMethod]
        public void DoctorRate_Edit_Before_Lock_And_Lock_Splits_History()
        {
            var doctorId = Doctor();
            var id = AddDoctor(doctorId, 10m, new DateTime(2026, 9, 1), new DateTime(2026, 12, 31));
            var editable = Services.DoctorCommissionRate.GetById(id);
            editable.CommissionPercent = 11m;
            Services.DoctorCommissionRate.Update(editable);
            Assert.AreEqual(11m, Services.DoctorCommissionRate.GetById(id).CommissionPercent);

            editable.CommissionPercent = 10m;
            Services.DoctorCommissionRate.Update(editable);
            Services.DoctorCommissionRate.ApplyClosedBoundary(doctorId, new DateTime(2026, 9, 30));

            var rows = Services.DoctorCommissionRate.GetAllActive().Where(r => r.ReferralDoctorId == doctorId).OrderBy(r => r.EffectiveFrom).ToList();
            Assert.AreEqual(2, rows.Count);
            Assert.IsTrue(rows[0].IsLocked);
            Assert.AreEqual(new DateTime(2026, 9, 30), rows[0].EffectiveTo);
            Assert.AreEqual(10m, rows[0].CommissionPercent);
            Assert.IsFalse(rows[1].IsLocked);
            Assert.AreEqual(new DateTime(2026, 10, 1), rows[1].EffectiveFrom);
            Assert.AreEqual(new DateTime(2026, 12, 31), rows[1].EffectiveTo);

            var lockedEdit = new ReferralDoctorCommissionRate
            {
                Id = rows[0].Id,
                ReferralDoctorId = rows[0].ReferralDoctorId,
                CommissionPercent = 15m,
                EffectiveFrom = rows[0].EffectiveFrom,
                EffectiveTo = rows[0].EffectiveTo,
                IsActive = true
            };
            Assert.ThrowsException<InvalidOperationException>(() => Services.DoctorCommissionRate.Update(lockedEdit));
            Assert.ThrowsException<InvalidOperationException>(() =>
                Services.DoctorCommissionRate.Delete(new ReferralDoctorCommissionRate { Id = rows[0].Id }));

            var openEdit = new ReferralDoctorCommissionRate
            {
                Id = rows[1].Id,
                ReferralDoctorId = rows[1].ReferralDoctorId,
                CommissionPercent = 12m,
                EffectiveFrom = rows[1].EffectiveFrom,
                EffectiveTo = rows[1].EffectiveTo,
                IsActive = true
            };
            Services.DoctorCommissionRate.Update(openEdit);
            Services.DoctorCommissionRate.ApplyClosedBoundary(doctorId, new DateTime(2026, 10, 31));

            rows = Services.DoctorCommissionRate.GetAllActive().Where(r => r.ReferralDoctorId == doctorId).OrderBy(r => r.EffectiveFrom).ToList();
            Assert.AreEqual(3, rows.Count);
            Assert.AreEqual(10m, rows[0].CommissionPercent);
            Assert.IsTrue(rows[0].IsLocked);
            Assert.AreEqual(12m, rows[1].CommissionPercent);
            Assert.AreEqual(new DateTime(2026, 10, 31), rows[1].EffectiveTo);
            Assert.IsTrue(rows[1].IsLocked);
            Assert.AreEqual(12m, rows[2].CommissionPercent);
            Assert.AreEqual(new DateTime(2026, 11, 1), rows[2].EffectiveFrom);
            Assert.IsFalse(rows[2].IsLocked);
        }

        [TestMethod]
        public void CommissionRate_MenuPermission_Matches_Parent_Master()
        {
            var missingDoctor = Services.Db.Database.SqlQuery<int>(@"
SELECT COUNT(*) FROM RoleMenuPermission r
WHERE r.MenuKey = 'MASTER_REFERRAL_DOCTOR'
AND NOT EXISTS (
    SELECT 1 FROM RoleMenuPermission c
    WHERE c.MenuKey = 'MASTER_DOCTOR_COMMISSION_RATE'
      AND c.RoleId = r.RoleId AND c.ModuleId = r.ModuleId
      AND c.CanView = r.CanView AND c.CanAdd = r.CanAdd AND c.CanEdit = r.CanEdit
      AND c.CanDelete = r.CanDelete AND c.IsActive = r.IsActive)").Single();
            Assert.AreEqual(0, missingDoctor);

            var missingCollector = Services.Db.Database.SqlQuery<int>(@"
SELECT COUNT(*) FROM RoleMenuPermission r
WHERE r.MenuKey = 'MASTER_SAMPLE_COLLECTOR'
AND NOT EXISTS (
    SELECT 1 FROM RoleMenuPermission c
    WHERE c.MenuKey = 'MASTER_COLLECTOR_COMMISSION_RATE'
      AND c.RoleId = r.RoleId AND c.ModuleId = r.ModuleId
      AND c.CanView = r.CanView AND c.CanAdd = r.CanAdd AND c.CanEdit = r.CanEdit
      AND c.CanDelete = r.CanDelete AND c.IsActive = r.IsActive)").Single();
            Assert.AreEqual(0, missingCollector);
        }

        private int Doctor()
        {
            return (int)Services.ReferralDoctor.Add(MasterTestDataBuilder.ReferralDoctor(UniqueCode("RD")));
        }

        private int AddDoctor(int doctorId, decimal percent, DateTime from, DateTime? to)
        {
            return (int)Services.DoctorCommissionRate.Add(new ReferralDoctorCommissionRate
            {
                ReferralDoctorId = doctorId,
                CommissionPercent = percent,
                EffectiveFrom = from,
                EffectiveTo = to,
                IsActive = true
            });
        }
    }
}
