using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LIS.Masters.Tests.Masters
{
    [TestClass]
    public class InvoiceCommissionRateValidationTests : IntegrationTestBase
    {
        [TestMethod]
        public void Draft_And_Confirmed_Require_Rate_When_Doctor_Or_Collector_Is_Selected()
        {
            var doctorId = Doctor();
            var collectorId = Collector();

            var draftDoctor = Assert.ThrowsException<ArgumentException>(() =>
                Save(DateTime.Today, doctorId, null, InvoiceStatusType.Draft));
            StringAssert.Contains(draftDoctor.Message, "referral doctor");

            var confirmedDoctor = Assert.ThrowsException<ArgumentException>(() =>
                Save(DateTime.Today, doctorId, null, InvoiceStatusType.Confirmed));
            StringAssert.Contains(confirmedDoctor.Message, "referral doctor");

            var draftCollector = Assert.ThrowsException<ArgumentException>(() =>
                Save(DateTime.Today, null, collectorId, InvoiceStatusType.Draft));
            StringAssert.Contains(draftCollector.Message, "sample collector");

            AddDoctorRate(doctorId, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30));
            var onEnd = Save(new DateTime(2026, 9, 30), doctorId, null, InvoiceStatusType.Draft);
            Assert.AreEqual(doctorId, Services.SaleInvoice.GetById(onEnd).Invoice.ReferralDoctorId);

            Assert.ThrowsException<ArgumentException>(() =>
                Save(new DateTime(2026, 10, 1), doctorId, null, InvoiceStatusType.Draft));
            Assert.ThrowsException<ArgumentException>(() =>
                Save(new DateTime(2026, 8, 31), doctorId, null, InvoiceStatusType.Confirmed));
        }

        [TestMethod]
        public void Invoice_Rate_Rules_Follow_Doctor_Precedence()
        {
            var doctorId = Doctor();
            var collectorId = Collector();
            AddCollectorRate(collectorId, DateTime.Today.AddYears(-1), null);

            var bothWithoutDoctorRate = Assert.ThrowsException<ArgumentException>(() =>
                Save(DateTime.Today, doctorId, collectorId, InvoiceStatusType.Confirmed));
            StringAssert.Contains(bothWithoutDoctorRate.Message, "referral doctor");

            AddDoctorRate(doctorId, DateTime.Today.AddDays(-1), null);
            var both = Save(DateTime.Today, doctorId, collectorId, InvoiceStatusType.Confirmed);
            var loaded = Services.SaleInvoice.GetById(both);
            Assert.AreEqual(doctorId, loaded.Invoice.ReferralDoctorId);
            Assert.AreEqual(collectorId, loaded.Invoice.CollectorId);
            Assert.AreEqual(1000m, loaded.Invoice.NetAmount);
            Assert.AreEqual(0m, loaded.Invoice.PaidAmount);

            var collectorOnly = Save(DateTime.Today, null, collectorId, InvoiceStatusType.Draft);
            Assert.AreEqual(collectorId, Services.SaleInvoice.GetById(collectorOnly).Invoice.CollectorId);

            var neither = Save(DateTime.Today, null, null, InvoiceStatusType.Draft);
            var plain = Services.SaleInvoice.GetById(neither);
            Assert.IsNull(plain.Invoice.ReferralDoctorId);
            Assert.IsNull(plain.Invoice.CollectorId);
        }

        [TestMethod]
        public void Invoice_Update_Can_Add_Rate_Or_Clear_Attribution()
        {
            var doctorId = Doctor();
            var invoiceId = Save(DateTime.Today, null, null, InvoiceStatusType.Draft);

            var missing = Services.SaleInvoice.GetById(invoiceId);
            missing.Invoice.ReferralDoctorId = doctorId;
            Assert.ThrowsException<ArgumentException>(() => Services.SaleInvoice.Save(missing));
            Services.Db.Entry(missing.Invoice).Reload();
            Assert.IsNull(missing.Invoice.ReferralDoctorId);

            AddDoctorRate(doctorId, DateTime.Today.AddDays(-1), null);
            var withDoctor = Services.SaleInvoice.GetById(invoiceId);
            withDoctor.Invoice.ReferralDoctorId = doctorId;
            Services.SaleInvoice.Save(withDoctor);
            Assert.AreEqual(doctorId, Services.SaleInvoice.GetById(invoiceId).Invoice.ReferralDoctorId);

            var cleared = Services.SaleInvoice.GetById(invoiceId);
            cleared.Invoice.ReferralDoctorId = null;
            cleared.Invoice.CollectorId = null;
            Services.SaleInvoice.Save(cleared);
            Assert.IsNull(Services.SaleInvoice.GetById(invoiceId).Invoice.ReferralDoctorId);
        }

        [TestMethod]
        public void Inactive_Rate_Does_Not_Satisfy_Invoice_Date()
        {
            var collectorId = Collector();
            var rateId = AddCollectorRate(collectorId, DateTime.Today.AddDays(-2), DateTime.Today.AddDays(2));
            var rate = Services.CollectorCommissionRate.GetById(rateId);
            rate.IsActive = false;
            Services.CollectorCommissionRate.Update(rate);

            Assert.ThrowsException<ArgumentException>(() =>
                Save(DateTime.Today, null, collectorId, InvoiceStatusType.Draft));
        }

        private int Doctor()
        {
            return (int)Services.ReferralDoctor.Add(MasterTestDataBuilder.ReferralDoctor(UniqueCode("RD")));
        }

        private int Collector()
        {
            return (int)Services.SampleCollector.Add(MasterTestDataBuilder.SampleCollector(UniqueCode("SC")));
        }

        private int AddDoctorRate(int doctorId, DateTime from, DateTime? to)
        {
            return (int)Services.DoctorCommissionRate.Add(new ReferralDoctorCommissionRate
            {
                ReferralDoctorId = doctorId,
                CommissionPercent = 10m,
                EffectiveFrom = from,
                EffectiveTo = to,
                IsActive = true
            });
        }

        private int AddCollectorRate(int collectorId, DateTime from, DateTime? to)
        {
            return (int)Services.CollectorCommissionRate.Add(new SampleCollectorCommissionRate
            {
                CollectorId = collectorId,
                CommissionPercent = 8m,
                EffectiveFrom = from,
                EffectiveTo = to,
                IsActive = true
            });
        }

        private long Save(DateTime invoiceDate, int? doctorId, int? collectorId, InvoiceStatusType status)
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
                    PaymentStatus = (int)PaymentStatusType.Unpaid,
                    PaidAmount = 0,
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
                        Rate = 1000m,
                        IsActive = true
                    }
                }
            });
        }
    }
}
