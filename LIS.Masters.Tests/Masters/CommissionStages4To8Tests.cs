using LIS.BusinessLogic;
using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace LIS.Masters.Tests.Masters
{
    [TestClass]
    public class CommissionStages4To8Tests : IntegrationTestBase
    {
        private const string MonthKey = CommissionCalculationService.ImplementationMonthKey;
        private static bool originalCaptured;
        private static string originalMonth;

        [TestInitialize]
        public void SetDefaultImplementationMonth()
        {
            SetMonth("2000-01");
        }

        [TestCleanup]
        public void RestoreImplementationMonth()
        {
            if (originalCaptured)
            {
                SetMonth(originalMonth, false);
            }
        }

        [TestMethod]
        public void Report_Summary_And_Detail_Are_Exclusive()
        {
            var doctorId = Doctor();
            var collectorId = Collector();
            AddDoctorRate(doctorId, DateTime.Today.AddDays(-2), null, 10m);
            AddCollectorRate(collectorId, DateTime.Today.AddDays(-2), null, 8m);
            Save(DateTime.Today, doctorId, collectorId, InvoiceStatusType.Confirmed, 1000m);
            Save(DateTime.Today, doctorId, null, InvoiceStatusType.Confirmed, 1000m);
            var collectorInvoice = Save(DateTime.Today, null, collectorId, InvoiceStatusType.Confirmed, 1000m);

            var summary = Services.CommissionReport.Get(CommissionRecipientType.ReferralDoctor, new ReportFilterOptions
            {
                FromDate = DateTime.Today.AddYears(-1),
                ToDate = DateTime.Today,
                ReferralDoctorId = doctorId,
                CommissionFormat = "Summary",
                CurrentPage = 1,
                RecordPerPage = 25
            });
            Assert.AreEqual("Summary", summary.Format);
            Assert.IsNull(summary.Detail);
            Assert.AreEqual(1, summary.Summary.Count);
            Assert.AreEqual(2, summary.Summary[0].InvoiceCount);
            Assert.AreEqual(200m, summary.Summary[0].Amount);
            Assert.AreEqual(200m, summary.TotalAmount);

            var detail = Services.CommissionReport.Get(CommissionRecipientType.ReferralDoctor, new ReportFilterOptions
            {
                FromDate = DateTime.Today.AddYears(-1),
                ToDate = DateTime.Today,
                ReferralDoctorId = doctorId,
                CommissionFormat = "Detail",
                CurrentPage = 1,
                RecordPerPage = 25
            });
            Assert.AreEqual("Detail", detail.Format);
            Assert.IsNull(detail.Summary);
            Assert.AreEqual(2, detail.Detail.Count);
            Assert.IsTrue(detail.Detail.All(l => l.CommissionBase == 1000m && l.Amount == 100m));
            Assert.IsFalse(detail.Detail.Any(l => string.IsNullOrEmpty(l.InvoiceNo)));

            var collector = Services.CommissionReport.Get(CommissionRecipientType.SampleCollector, new ReportFilterOptions
            {
                FromDate = DateTime.Today.AddYears(-1),
                ToDate = DateTime.Today,
                CollectorId = collectorId,
                CommissionFormat = "Detail",
                RecordPerPage = 0
            });
            Assert.AreEqual(1, collector.Detail.Count);
            Assert.AreEqual(collectorInvoice, collector.Detail[0].SaleInvoiceId);
            Assert.AreEqual(80m, collector.Detail[0].Amount);

            Assert.ThrowsException<ArgumentException>(() =>
                Services.CommissionReport.Get(CommissionRecipientType.ReferralDoctor, new ReportFilterOptions
                {
                    FromDate = DateTime.Today,
                    ToDate = DateTime.Today,
                    ReferralDoctorId = doctorId
                }));
        }

        [TestMethod]
        public void Report_Allows_A_Range_Longer_Than_Two_Months()
        {
            var doctorId = Doctor();
            AddDoctorRate(doctorId, new DateTime(2020, 1, 1), null, 10m);
            var id = Save(DateTime.Today, doctorId, null, InvoiceStatusType.Paid, 1000m);
            var page = Services.CommissionReport.Get(CommissionRecipientType.ReferralDoctor, new ReportFilterOptions
            {
                FromDate = new DateTime(2020, 1, 1),
                ToDate = DateTime.Today,
                ReferralDoctorId = doctorId,
                CommissionFormat = "Detail",
                RecordPerPage = 0
            });
            Assert.IsTrue(page.Detail.Any(l => l.SaleInvoiceId == id));
        }

        [TestMethod]
        public void Payout_First_From_Is_Fixed_And_Preview_Does_Not_Insert()
        {
            SetMonth("2026-09");
            var doctorId = Doctor();
            AddDoctorRate(doctorId, new DateTime(2026, 9, 1), null, 10m);
            var before = Services.CommissionPayout.List((int)CommissionRecipientType.ReferralDoctor, doctorId).Count;

            var wrong = Assert.ThrowsException<ArgumentException>(() =>
                Services.CommissionPayout.Preview(new CommissionPayoutRequest
                {
                    RecipientType = (int)CommissionRecipientType.ReferralDoctor,
                    RecipientId = doctorId,
                    FromDate = new DateTime(2026, 9, 5),
                    ToDate = new DateTime(2026, 9, 15)
                }));
            StringAssert.Contains(wrong.Message, "2026-09-01");

            var preview = Services.CommissionPayout.Preview(new CommissionPayoutRequest
            {
                RecipientType = (int)CommissionRecipientType.ReferralDoctor,
                RecipientId = doctorId,
                FromDate = new DateTime(2026, 9, 1),
                ToDate = new DateTime(2026, 9, 15)
            });
            Assert.AreEqual(0, preview.Lines.Count);
            Assert.AreEqual(before, Services.CommissionPayout.List((int)CommissionRecipientType.ReferralDoctor, doctorId).Count);

            var confirm = Assert.ThrowsException<InvalidOperationException>(() =>
                Services.CommissionPayout.Confirm(new CommissionPayoutRequest
                {
                    RecipientType = (int)CommissionRecipientType.ReferralDoctor,
                    RecipientId = doctorId,
                    FromDate = new DateTime(2026, 9, 1),
                    ToDate = new DateTime(2026, 9, 15)
                }));
            StringAssert.Contains(confirm.Message, "No eligible invoices");
        }

        [TestMethod]
        public void Payout_Confirm_Carry_Forward_Splits_Rate_And_Reprint_Stays_Stored()
        {
            SetMonth("2026-09");
            var doctorId = Doctor();
            AddDoctorRate(doctorId, new DateTime(2026, 9, 1), null, 10m);
            var carryId = Save(new DateTime(2026, 9, 10), doctorId, null, InvoiceStatusType.Confirmed, 0m);
            var currentId = Save(new DateTime(2026, 9, 12), doctorId, null, InvoiceStatusType.Confirmed, 1000m);

            var first = Services.CommissionPayout.Confirm(new CommissionPayoutRequest
            {
                RecipientType = (int)CommissionRecipientType.ReferralDoctor,
                RecipientId = doctorId,
                FromDate = new DateTime(2026, 9, 1),
                ToDate = new DateTime(2026, 9, 15)
            });
            var firstMemo = Services.CommissionPayout.GetMemo(first);
            Assert.IsTrue(firstMemo.Payout.PayoutNo.StartsWith("CP-"));
            Assert.AreEqual(1, firstMemo.Details.Count);
            Assert.AreEqual(currentId, firstMemo.Details[0].SaleInvoiceId);
            Assert.AreEqual(100m, firstMemo.Details[0].Amount);

            var rates = Services.DoctorCommissionRate.GetAllActive()
                .Where(r => r.ReferralDoctorId == doctorId)
                .ToList();
            Assert.IsTrue(rates.Any(r => r.IsLocked && r.EffectiveTo.HasValue && r.EffectiveTo.Value.Date == new DateTime(2026, 9, 15)));
            var open = rates.Single(r => !r.IsLocked);
            Assert.AreEqual(new DateTime(2026, 9, 16), open.EffectiveFrom.Date);
            Services.DoctorCommissionRate.Update(new ReferralDoctorCommissionRate
            {
                Id = open.Id,
                ReferralDoctorId = doctorId,
                CommissionPercent = 12m,
                EffectiveFrom = open.EffectiveFrom,
                EffectiveTo = open.EffectiveTo,
                IsActive = true
            });

            var carry = Services.SaleInvoice.GetById(carryId);
            carry.Invoice.PaidAmount = carry.Invoice.NetAmount;
            carry.Invoice.InvoiceStatus = (int)InvoiceStatusType.Confirmed;
            Services.SaleInvoice.Save(carry);
            var laterId = Save(new DateTime(2026, 9, 20), doctorId, null, InvoiceStatusType.Confirmed, 1000m);

            var schedule = Services.CommissionPayout.GetSchedule((int)CommissionRecipientType.ReferralDoctor, doctorId);
            Assert.IsFalse(schedule.IsFirstPayout);
            Assert.AreEqual(new DateTime(2026, 9, 16), schedule.FromDate.Date);

            Assert.ThrowsException<ArgumentException>(() =>
                Services.CommissionPayout.Preview(new CommissionPayoutRequest
                {
                    RecipientType = (int)CommissionRecipientType.ReferralDoctor,
                    RecipientId = doctorId,
                    FromDate = new DateTime(2026, 9, 17),
                    ToDate = new DateTime(2026, 9, 30)
                }));

            var secondPreview = Services.CommissionPayout.Preview(new CommissionPayoutRequest
            {
                RecipientType = (int)CommissionRecipientType.ReferralDoctor,
                RecipientId = doctorId,
                FromDate = new DateTime(2026, 9, 16),
                ToDate = new DateTime(2026, 9, 30)
            });
            Assert.AreEqual(2, secondPreview.Lines.Count);
            Assert.AreEqual(100m, secondPreview.Lines.Single(l => l.SaleInvoiceId == carryId).Amount);
            Assert.AreEqual(120m, secondPreview.Lines.Single(l => l.SaleInvoiceId == laterId).Amount);
            Assert.IsFalse(secondPreview.Lines.Any(l => l.SaleInvoiceId == currentId));
            Assert.AreEqual(0, Services.CommissionPayout.List((int)CommissionRecipientType.ReferralDoctor, doctorId).Count - 1);

            var second = Services.CommissionPayout.Confirm(new CommissionPayoutRequest
            {
                RecipientType = (int)CommissionRecipientType.ReferralDoctor,
                RecipientId = doctorId,
                FromDate = new DateTime(2026, 9, 16),
                ToDate = new DateTime(2026, 9, 30)
            });

            var october = Services.DoctorCommissionRate.GetAllActive()
                .Single(r => r.ReferralDoctorId == doctorId && !r.IsLocked);
            Services.DoctorCommissionRate.Update(new ReferralDoctorCommissionRate
            {
                Id = october.Id,
                ReferralDoctorId = doctorId,
                CommissionPercent = 15m,
                EffectiveFrom = october.EffectiveFrom,
                EffectiveTo = october.EffectiveTo,
                IsActive = true
            });

            var stored = Services.CommissionPayout.GetMemo(second);
            Assert.AreEqual(220m, stored.Payout.TotalAmount);
            Assert.AreEqual(100m, stored.Details.Single(d => d.SaleInvoiceId == carryId).Amount);
            Assert.AreEqual(10m, stored.Details.Single(d => d.SaleInvoiceId == carryId).CommissionPercent);
            Assert.AreEqual(120m, stored.Details.Single(d => d.SaleInvoiceId == laterId).Amount);
            Assert.AreEqual(12m, stored.Details.Single(d => d.SaleInvoiceId == laterId).CommissionPercent);
        }

        [TestMethod]
        public void Authorization_Seeds_Administrator_Only()
        {
            var otherReportMenus = Services.Db.Database.SqlQuery<int>(@"
SELECT COUNT(*) FROM RoleMenuPermission p
INNER JOIN AspNetRoles r ON r.Id = p.RoleId
WHERE p.MenuKey IN ('REPORT_DOCTOR_COMMISSION', 'REPORT_COLLECTOR_COMMISSION')
  AND r.Name <> 'Administrator'").Single();
            Assert.AreEqual(0, otherReportMenus);

            var adminReports = Services.Db.Database.SqlQuery<int>(@"
SELECT COUNT(*) FROM RoleMenuPermission p
INNER JOIN AspNetRoles r ON r.Id = p.RoleId
WHERE p.MenuKey IN ('REPORT_DOCTOR_COMMISSION', 'REPORT_COLLECTOR_COMMISSION')
  AND r.Name = 'Administrator' AND p.CanView = 1 AND p.IsActive = 1").Single();
            Assert.AreEqual(2, adminReports);

            var otherPayout = Services.Db.Database.SqlQuery<int>(@"
SELECT COUNT(*) FROM RoleModuleMappings m
INNER JOIN UserModules u ON u.Id = m.ModuleId
INNER JOIN AspNetRoles r ON r.Id = m.RoleId
WHERE u.Name = 'CommissionPayout' AND r.Name <> 'Administrator'").Single();
            Assert.AreEqual(0, otherPayout);

            var adminPayout = Services.Db.Database.SqlQuery<int>(@"
SELECT COUNT(*) FROM RoleMenuPermission p
INNER JOIN AspNetRoles r ON r.Id = p.RoleId
INNER JOIN UserModules u ON u.Id = p.ModuleId
WHERE p.MenuKey = 'COMMISSION_PAYOUT' AND u.Name = 'CommissionPayout'
  AND r.Name = 'Administrator' AND p.CanAdd = 1 AND p.CanView = 1").Single();
            Assert.AreEqual(1, adminPayout);
        }

        private int Doctor()
        {
            return (int)Services.ReferralDoctor.Add(MasterTestDataBuilder.ReferralDoctor(UniqueCode("RD")));
        }

        private int Collector()
        {
            return (int)Services.SampleCollector.Add(MasterTestDataBuilder.SampleCollector(UniqueCode("SC")));
        }

        private void AddDoctorRate(int doctorId, DateTime from, DateTime? to, decimal percent)
        {
            Services.DoctorCommissionRate.Add(new ReferralDoctorCommissionRate
            {
                ReferralDoctorId = doctorId,
                CommissionPercent = percent,
                EffectiveFrom = from,
                EffectiveTo = to,
                IsActive = true
            });
        }

        private void AddCollectorRate(int collectorId, DateTime from, DateTime? to, decimal percent)
        {
            Services.CollectorCommissionRate.Add(new SampleCollectorCommissionRate
            {
                CollectorId = collectorId,
                CommissionPercent = percent,
                EffectiveFrom = from,
                EffectiveTo = to,
                IsActive = true
            });
        }

        private long Save(DateTime invoiceDate, int? doctorId, int? collectorId, InvoiceStatusType status, decimal paidAmount)
        {
            var testId = EnsureTestWithStandardRate(1000m, out _);
            var patientId = Services.PatientMaster.Add(MasterTestDataBuilder.Patient(UniqueCode("PT", 8)));
            return Services.SaleInvoice.Save(new SaleInvoiceDto
            {
                Invoice = new SaleInvoice
                {
                    InvoiceNo = UniqueCode("INV"),
                    InvoiceDate = invoiceDate,
                    PatientId = patientId,
                    InvoiceStatus = (int)status,
                    PaidAmount = paidAmount,
                    ReferralDoctorId = doctorId,
                    CollectorId = collectorId,
                    IsActive = true
                },
                Details = new List<SaleInvoiceDetail>
                {
                    new SaleInvoiceDetail { TestId = testId, Quantity = 1, Rate = 1000m, IsActive = true }
                }
            });
        }

        private static void SetMonth(string value, bool capture = true)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (capture && !originalCaptured)
            {
                originalMonth = ConfigurationManager.AppSettings[MonthKey];
                originalCaptured = true;
            }

            if (string.IsNullOrEmpty(value))
            {
                if (config.AppSettings.Settings[MonthKey] != null)
                {
                    config.AppSettings.Settings.Remove(MonthKey);
                }
            }
            else if (config.AppSettings.Settings[MonthKey] == null)
            {
                config.AppSettings.Settings.Add(MonthKey, value);
            }
            else
            {
                config.AppSettings.Settings[MonthKey].Value = value;
            }

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
