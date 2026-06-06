using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LIS.Masters.Tests.Transactions
{
    [TestClass]
    public class TestRateInvoicePriorityTests : IntegrationTestBase
    {
        private int EnsureTestId()
        {
            return CreateIsolatedTest();
        }

        [TestMethod]
        public void GetEffectiveRateForInvoice_Uses_InvoiceDate_Not_Today()
        {
            var testId = EnsureTestId();
            var pastEnd = MasterTestDataBuilder.StandardRate(testId, 50m, DateTime.Today.AddDays(-90), DateTime.Today.AddDays(-1));
            var pastId = (int)Services.TestRate.Add(pastEnd);

            var effective = Services.TestRate.GetEffectiveRateForInvoice(testId, DateTime.Today, null, null);
            if (effective != null && effective.Id == pastId)
            {
                Assert.Fail("Expired rate must not apply to current invoice date");
            }

            Services.TestRate.Delete(new TestRateMaster { Id = pastId });
        }

        [TestMethod]
        public void GetEffectiveRateForInvoice_Prefers_Corporate_Over_Standard()
        {
            var testId = EnsureTestId();
            var std = MasterTestDataBuilder.StandardRate(testId, 100m);
            var stdId = (int)Services.TestRate.Add(std);

            var corp = Services.Corporate.GetAllActive().FirstOrDefault();
            if (corp == null)
            {
                corp = new CorporateMaster { Code = UniqueCode("CORP"), Name = "UAT Corp", IsActive = true };
                corp.Id = (int)Services.Corporate.Add(corp);
            }

            var corpRate = MasterTestDataBuilder.StandardRate(testId, 250m);
            corpRate.RateType = (int)RateType.Corporate;
            corpRate.CorporateId = corp.Id;
            var corpId = (int)Services.TestRate.Add(corpRate);

            var effective = Services.TestRate.GetEffectiveRateForInvoice(testId, DateTime.Today, corp.Id, null);
            Assert.IsNotNull(effective);
            Assert.AreEqual(corpId, effective.Id);

            Services.TestRate.Delete(new TestRateMaster { Id = corpId });
            Services.TestRate.Delete(new TestRateMaster { Id = stdId });
        }
    }
}
