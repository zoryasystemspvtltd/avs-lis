using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LIS.Masters.Tests.Masters
{
    [TestClass]
    public class InvoiceCommissionRateValidationTests : IntegrationTestBase
    {
        [TestMethod]
        public void Draft_And_Confirm_Succeed_When_Doctor_Has_No_Rate()
        {
            var doctorId = Doctor();
            var draft = Save(DateTime.Today, doctorId, null, InvoiceStatusType.Draft);
            AssertAttribution(draft, doctorId, null, InvoiceStatusType.Draft);

            var confirmed = Save(DateTime.Today, doctorId, null, InvoiceStatusType.Confirmed);
            AssertAttribution(confirmed, doctorId, null, InvoiceStatusType.Confirmed);
        }

        [TestMethod]
        public void Draft_Succeeds_When_Collector_Has_No_Rate()
        {
            var collectorId = Collector();
            var id = Save(DateTime.Today, null, collectorId, InvoiceStatusType.Draft);
            AssertAttribution(id, null, collectorId, InvoiceStatusType.Draft);
        }

        [TestMethod]
        public void Confirm_Succeeds_When_Collector_Has_No_Rate()
        {
            var collectorId = Collector();
            var id = Save(DateTime.Today, null, collectorId, InvoiceStatusType.Confirmed);
            AssertAttribution(id, null, collectorId, InvoiceStatusType.Confirmed);
        }

        [TestMethod]
        public void Confirm_Succeeds_When_Both_Have_No_Rate()
        {
            var doctorId = Doctor();
            var collectorId = Collector();
            var id = Save(DateTime.Today, doctorId, collectorId, InvoiceStatusType.Confirmed);
            AssertAttribution(id, doctorId, collectorId, InvoiceStatusType.Confirmed);
        }

        [TestMethod]
        public void Confirm_Succeeds_When_Only_Collector_Rate_Exists()
        {
            var doctorId = Doctor();
            var collectorId = Collector();
            AddCollectorRate(collectorId, DateTime.Today.AddYears(-1), null);

            var id = Save(DateTime.Today, doctorId, collectorId, InvoiceStatusType.Confirmed);
            AssertAttribution(id, doctorId, collectorId, InvoiceStatusType.Confirmed);
        }

        [TestMethod]
        public void Draft_Succeeds_When_Only_Inactive_Doctor_Rate_Exists()
        {
            var doctorId = Doctor();
            var rateId = AddDoctorRate(doctorId, DateTime.Today.AddDays(-2), DateTime.Today.AddDays(2));
            var rate = Services.DoctorCommissionRate.GetById(rateId);
            rate.IsActive = false;
            Services.DoctorCommissionRate.Update(rate);

            var id = Save(DateTime.Today, doctorId, null, InvoiceStatusType.Draft);
            AssertAttribution(id, doctorId, null, InvoiceStatusType.Draft);
        }

        [TestMethod]
        public void Draft_Succeeds_When_Only_Inactive_Collector_Rate_Exists()
        {
            var collectorId = Collector();
            var rateId = AddCollectorRate(collectorId, DateTime.Today.AddDays(-2), DateTime.Today.AddDays(2));
            var rate = Services.CollectorCommissionRate.GetById(rateId);
            rate.IsActive = false;
            Services.CollectorCommissionRate.Update(rate);

            var id = Save(DateTime.Today, null, collectorId, InvoiceStatusType.Draft);
            AssertAttribution(id, null, collectorId, InvoiceStatusType.Draft);
        }

        [TestMethod]
        public void Draft_And_Confirm_Succeed_When_Rate_Starts_After_Invoice_Date()
        {
            var doctorId = Doctor();
            AddDoctorRate(doctorId, DateTime.Today.AddDays(1), DateTime.Today.AddDays(10));

            var draft = Save(DateTime.Today, doctorId, null, InvoiceStatusType.Draft);
            AssertAttribution(draft, doctorId, null, InvoiceStatusType.Draft);

            var confirmed = Save(DateTime.Today, doctorId, null, InvoiceStatusType.Confirmed);
            AssertAttribution(confirmed, doctorId, null, InvoiceStatusType.Confirmed);
        }

        [TestMethod]
        public void Update_Can_Add_Doctor_Or_Collector_Without_A_Rate()
        {
            var doctorId = Doctor();
            var collectorId = Collector();
            var invoiceId = Save(DateTime.Today, null, null, InvoiceStatusType.Draft);

            var withDoctor = Services.SaleInvoice.GetById(invoiceId);
            withDoctor.Invoice.ReferralDoctorId = doctorId;
            Services.SaleInvoice.Save(withDoctor);
            AssertAttribution(invoiceId, doctorId, null, InvoiceStatusType.Draft);

            var withCollector = Services.SaleInvoice.GetById(invoiceId);
            withCollector.Invoice.ReferralDoctorId = null;
            withCollector.Invoice.CollectorId = collectorId;
            Services.SaleInvoice.Save(withCollector);
            AssertAttribution(invoiceId, null, collectorId, InvoiceStatusType.Draft);
        }

        [TestMethod]
        public void Cancelled_Invoice_Update_Is_Still_Rejected()
        {
            var invoiceId = Save(DateTime.Today, null, null, InvoiceStatusType.Draft);
            Services.SaleInvoice.Cancel(invoiceId);

            var cancelled = Services.SaleInvoice.GetById(invoiceId);
            cancelled.Invoice.Notes = "qa-edit";
            var ex = Assert.ThrowsException<InvalidOperationException>(() => Services.SaleInvoice.Save(cancelled));
            StringAssert.Contains(ex.Message, "Cancelled invoice cannot be edited");
        }

        private void AssertAttribution(long invoiceId, int? doctorId, int? collectorId, InvoiceStatusType status)
        {
            var invoice = Services.SaleInvoice.GetById(invoiceId).Invoice;
            Assert.AreEqual(doctorId, invoice.ReferralDoctorId);
            Assert.AreEqual(collectorId, invoice.CollectorId);
            Assert.AreEqual((int)status, invoice.InvoiceStatus);
            Assert.AreEqual(1000m, invoice.GrossAmount);
            Assert.AreEqual(0m, invoice.DiscountAmount);
            Assert.AreEqual(0m, invoice.TaxAmount);
            Assert.AreEqual(1000m, invoice.NetAmount);
            Assert.AreEqual(0m, invoice.PaidAmount);
            Assert.IsFalse(typeof(SaleInvoice).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Any(p => p.Name.IndexOf("Commission", StringComparison.OrdinalIgnoreCase) >= 0));
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
