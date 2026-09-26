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
    public class CommissionCalculationTests : IntegrationTestBase
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
            if (!originalCaptured)
            {
                return;
            }

            SetMonth(originalMonth, capture: false);
        }

        [TestMethod]
        public void Missing_And_Invalid_ImplementationMonth_Fail_Closed()
        {
            SetMonth(null);
            var missing = Assert.ThrowsException<InvalidOperationException>(() =>
                Services.Commission.Calculate(new CommissionCalculationRequest
                {
                    FromDate = DateTime.Today,
                    ToDate = DateTime.Today
                }));
            StringAssert.Contains(missing.Message, "Commission:ImplementationMonth");

            SetMonth("September 2026");
            Assert.ThrowsException<InvalidOperationException>(() =>
                Services.Commission.Calculate(new CommissionCalculationRequest
                {
                    FromDate = DateTime.Today,
                    ToDate = DateTime.Today
                }));

            Assert.ThrowsException<ArgumentException>(() =>
                Services.Commission.Calculate(new CommissionCalculationRequest
                {
                    FromDate = DateTime.Today,
                    ToDate = DateTime.Today.AddDays(-1)
                }));
        }

        [TestMethod]
        public void Eligible_Confirmed_And_InvoiceStatus_Paid_Are_Calculated()
        {
            var doctorId = Doctor(out var doctorName);
            AddDoctorRate(doctorId, DateTime.Today.AddDays(-1), null, 10m);
            var confirmedId = Save(DateTime.Today, doctorId, null, InvoiceStatusType.Confirmed, 1000m, 0m, 0m);
            var paidStatusId = Save(DateTime.Today, doctorId, null, InvoiceStatusType.Paid, 1000m, 0m, 0m);

            var result = Calculate(doctorId, CommissionRecipientType.ReferralDoctor, DateTime.Today, DateTime.Today);
            Assert.AreEqual(2, result.Lines.Count);
            Assert.AreEqual(200m, result.TotalAmount);
            var confirmed = result.Lines.Single(l => l.SaleInvoiceId == confirmedId);
            Assert.AreEqual(1000m, confirmed.CommissionBase);
            Assert.AreEqual(10m, confirmed.CommissionPercent);
            Assert.AreEqual(100m, confirmed.Amount);
            Assert.AreEqual(doctorName, confirmed.RecipientName);
            Assert.AreEqual(CommissionRecipientType.ReferralDoctor, confirmed.RecipientType);
            Assert.IsTrue(result.Lines.Any(l => l.SaleInvoiceId == paidStatusId && l.Amount == 100m));

            var stored = Services.SaleInvoice.GetById(confirmedId).Invoice;
            Assert.AreEqual(1000m, stored.NetAmount);
            Assert.AreEqual(1000m, stored.PaidAmount);
        }

        [TestMethod]
        public void Draft_Partial_Unpaid_And_Cancelled_Are_Omitted()
        {
            var doctorId = Doctor(out _);
            AddDoctorRate(doctorId, DateTime.Today.AddYears(-1), null, 10m);
            Save(DateTime.Today, doctorId, null, InvoiceStatusType.Draft, 1000m, 0m, 0m);
            Save(DateTime.Today, doctorId, null, InvoiceStatusType.Confirmed, 400m, 0m, 0m);
            Save(DateTime.Today, doctorId, null, InvoiceStatusType.Confirmed, 0m, 0m, 0m);
            var cancelledId = Save(DateTime.Today, doctorId, null, InvoiceStatusType.Confirmed, 1000m, 0m, 0m);
            Services.SaleInvoice.Cancel(cancelledId);

            var result = Calculate(doctorId, CommissionRecipientType.ReferralDoctor, DateTime.Today, DateTime.Today);
            Assert.AreEqual(0, result.Lines.Count);
            Assert.AreEqual(0m, result.TotalAmount);
        }

        [TestMethod]
        public void Doctor_Precedence_Skips_Collector_When_Both_Are_Selected()
        {
            var doctorId = Doctor(out _);
            var collectorId = Collector();
            AddDoctorRate(doctorId, DateTime.Today.AddDays(-1), null, 10m);
            AddCollectorRate(collectorId, DateTime.Today.AddDays(-1), null, 8m);
            var both = Save(DateTime.Today, doctorId, collectorId, InvoiceStatusType.Confirmed, 1000m, 0m, 0m);
            var collectorOnly = Save(DateTime.Today, null, collectorId, InvoiceStatusType.Confirmed, 1000m, 0m, 0m);

            var doctorResult = Calculate(doctorId, CommissionRecipientType.ReferralDoctor, DateTime.Today, DateTime.Today);
            Assert.AreEqual(1, doctorResult.Lines.Count);
            Assert.AreEqual(both, doctorResult.Lines[0].SaleInvoiceId);
            Assert.AreEqual(100m, doctorResult.Lines[0].Amount);

            var collectorResult = Calculate(collectorId, CommissionRecipientType.SampleCollector, DateTime.Today, DateTime.Today);
            Assert.AreEqual(1, collectorResult.Lines.Count);
            Assert.AreEqual(collectorOnly, collectorResult.Lines[0].SaleInvoiceId);
            Assert.AreEqual(80m, collectorResult.Lines[0].Amount);
            Assert.AreEqual(CommissionRecipientType.SampleCollector, collectorResult.Lines[0].RecipientType);
        }

        [TestMethod]
        public void Base_Is_Net_Minus_Tax_After_Header_Discount()
        {
            var doctorId = Doctor(out _);
            AddDoctorRate(doctorId, DateTime.Today.AddDays(-1), null, 10m);
            var withTax = Save(DateTime.Today, doctorId, null, InvoiceStatusType.Confirmed, 1050m, 50m, 0m);
            var withDiscount = Save(DateTime.Today, doctorId, null, InvoiceStatusType.Confirmed, 900m, 0m, 100m);

            var result = Calculate(doctorId, CommissionRecipientType.ReferralDoctor, DateTime.Today, DateTime.Today);
            var taxLine = result.Lines.Single(l => l.SaleInvoiceId == withTax);
            var discountLine = result.Lines.Single(l => l.SaleInvoiceId == withDiscount);
            Assert.AreEqual(1000m, taxLine.CommissionBase);
            Assert.AreEqual(100m, taxLine.Amount);
            Assert.AreEqual(900m, discountLine.CommissionBase);
            Assert.AreEqual(90m, discountLine.Amount);
            Assert.AreEqual(190m, result.TotalAmount);
        }

        [TestMethod]
        public void Rounding_Is_Per_Invoice_Then_Summed()
        {
            var doctorId = Doctor(out _);
            AddDoctorRate(doctorId, DateTime.Today.AddDays(-1), null, 12.5m);
            Save(DateTime.Today, doctorId, null, InvoiceStatusType.Confirmed, 1m, 0m, 0m, 1m);
            Save(DateTime.Today, doctorId, null, InvoiceStatusType.Confirmed, 1m, 0m, 0m, 1m);

            var result = Calculate(doctorId, CommissionRecipientType.ReferralDoctor, DateTime.Today, DateTime.Today);
            Assert.AreEqual(2, result.Lines.Count);
            Assert.IsTrue(result.Lines.All(l => l.Amount == 0.12m));
            Assert.AreEqual(0.24m, result.TotalAmount);
        }

        [TestMethod]
        public void Before_Implementation_Month_And_Outside_Period_Are_Omitted()
        {
            var doctorId = Doctor(out _);
            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            AddDoctorRate(doctorId, monthStart.AddDays(-10), null, 10m);
            var laterDate = monthStart.AddDays(1);
            var before = Save(monthStart.AddDays(-1), doctorId, null, InvoiceStatusType.Confirmed, 1000m, 0m, 0m);
            var onStart = Save(monthStart, doctorId, null, InvoiceStatusType.Confirmed, 1000m, 0m, 0m);
            var later = Save(laterDate, doctorId, null, InvoiceStatusType.Paid, 1000m, 0m, 0m);

            SetMonth(monthStart.ToString("yyyy-MM"));
            var inMonth = Calculate(doctorId, CommissionRecipientType.ReferralDoctor, monthStart.AddDays(-10), laterDate);
            Assert.IsFalse(inMonth.Lines.Any(l => l.SaleInvoiceId == before));
            Assert.IsTrue(inMonth.Lines.Any(l => l.SaleInvoiceId == onStart));
            Assert.IsTrue(inMonth.Lines.Any(l => l.SaleInvoiceId == later));

            SetMonth("2000-01");
            var onlyLater = Calculate(doctorId, CommissionRecipientType.ReferralDoctor, laterDate, laterDate);
            Assert.AreEqual(1, onlyLater.Lines.Count);
            Assert.AreEqual(later, onlyLater.Lines[0].SaleInvoiceId);
        }

        [TestMethod]
        public void Inactive_Rate_And_Duplicate_Coverage_Fail_Closed()
        {
            var doctorId = Doctor(out _);
            var rateId = AddDoctorRate(doctorId, DateTime.Today.AddDays(-2), DateTime.Today.AddDays(2), 10m);
            var invoiceId = Save(DateTime.Today, doctorId, null, InvoiceStatusType.Confirmed, 1000m, 0m, 0m);
            var invoiceNo = Services.SaleInvoice.GetById(invoiceId).Invoice.InvoiceNo;

            var rate = Services.DoctorCommissionRate.GetById(rateId);
            rate.IsActive = false;
            Services.DoctorCommissionRate.Update(rate);

            var inactive = Assert.ThrowsException<InvalidOperationException>(() =>
                Calculate(doctorId, CommissionRecipientType.ReferralDoctor, DateTime.Today, DateTime.Today));
            StringAssert.Contains(inactive.Message, invoiceNo);

            rate.IsActive = true;
            Services.DoctorCommissionRate.Update(rate);
            Services.Db.ReferralDoctorCommissionRate.Add(new ReferralDoctorCommissionRate
            {
                ReferralDoctorId = doctorId,
                CommissionPercent = 11m,
                EffectiveFrom = DateTime.Today.AddDays(-2),
                EffectiveTo = DateTime.Today.AddDays(2),
                IsActive = true,
                IsLocked = false,
                CreatedOn = DateTime.Now,
                ModifiedOn = DateTime.Now
            });
            Services.Db.SaveChanges();

            var duplicate = Assert.ThrowsException<InvalidOperationException>(() =>
                Calculate(doctorId, CommissionRecipientType.ReferralDoctor, DateTime.Today, DateTime.Today));
            StringAssert.Contains(duplicate.Message, "More than one commission rate");
        }

        [TestMethod]
        public void Locked_Rate_Still_Calculates()
        {
            var doctorId = Doctor(out _);
            AddDoctorRate(doctorId, DateTime.Today.AddDays(-3), null, 10m);
            Services.DoctorCommissionRate.ApplyClosedBoundary(doctorId, DateTime.Today);
            var invoiceId = Save(DateTime.Today, doctorId, null, InvoiceStatusType.Confirmed, 1000m, 0m, 0m);

            var result = Calculate(doctorId, CommissionRecipientType.ReferralDoctor, DateTime.Today, DateTime.Today);
            var line = result.Lines.Single(l => l.SaleInvoiceId == invoiceId);
            Assert.AreEqual(100m, line.Amount);
        }

        private CommissionCalculationResult Calculate(int recipientId, CommissionRecipientType type, DateTime from, DateTime to)
        {
            return Services.Commission.Calculate(new CommissionCalculationRequest
            {
                FromDate = from,
                ToDate = to,
                RecipientType = type,
                RecipientId = recipientId
            });
        }

        private int Doctor(out string name)
        {
            var code = UniqueCode("RD");
            var id = (int)Services.ReferralDoctor.Add(MasterTestDataBuilder.ReferralDoctor(code));
            name = Services.ReferralDoctor.GetById(id).Name;
            return id;
        }

        private int Collector()
        {
            return (int)Services.SampleCollector.Add(MasterTestDataBuilder.SampleCollector(UniqueCode("SC")));
        }

        private int AddDoctorRate(int doctorId, DateTime from, DateTime? to, decimal percent)
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

        private long Save(
            DateTime invoiceDate,
            int? doctorId,
            int? collectorId,
            InvoiceStatusType status,
            decimal paidAmount,
            decimal taxAmount,
            decimal discountValue,
            decimal rate = 1000m)
        {
            var testId = EnsureTestWithStandardRate(rate, out _);
            var patientId = Services.PatientMaster.Add(MasterTestDataBuilder.Patient(UniqueCode("PT", 8)));
            return Services.SaleInvoice.Save(new SaleInvoiceDto
            {
                Invoice = new SaleInvoice
                {
                    InvoiceNo = UniqueCode("INV"),
                    InvoiceDate = invoiceDate,
                    PatientId = patientId,
                    InvoiceStatus = (int)status,
                    PaymentStatus = (int)PaymentStatusType.Unpaid,
                    PaidAmount = paidAmount,
                    TaxAmount = taxAmount,
                    DiscountType = discountValue > 0 ? "Fixed Amount" : null,
                    DiscountValue = discountValue,
                    ReferralDoctorId = doctorId,
                    CollectorId = collectorId,
                    CorporateId = null,
                    IsActive = true
                },
                Details = new List<SaleInvoiceDetail>
                {
                    new SaleInvoiceDetail
                    {
                        TestId = testId,
                        Quantity = 1,
                        Rate = rate,
                        IsActive = true
                    }
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
