using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LIS.Masters.Tests.Transactions
{
    [TestClass]
    public class TestRateMasterManagerTests : IntegrationTestBase
    {
        private int EnsureTestId()
        {
            return CreateIsolatedTest();
        }

        [TestMethod]
        public void TestRate_Standard_GetEffective_Returns_Current_Rate()
        {
            var testId = EnsureTestId();
            var rate = MasterTestDataBuilder.StandardRate(testId, 150m);
            var rateId = (int)Services.TestRate.Add(rate);

            var effective = Services.TestRate.GetEffectiveRate(testId, (int)RateType.Standard, null, null, null);
            Assert.IsNotNull(effective);
            Assert.AreEqual(testId, effective.TestId);
            Assert.IsTrue(effective.Rate > 0);
            Assert.IsFalse(string.IsNullOrEmpty(effective.TestCode) && string.IsNullOrEmpty(effective.TestName),
                "Effective rate should include test code or name from enrichment");

            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }

        [TestMethod]
        public void TestRate_Emergency_GetEffective_Uses_EmergencyRate_When_Set()
        {
            var testId = EnsureTestId();
            var rate = MasterTestDataBuilder.StandardRate(testId, 100m);
            rate.RateType = (int)RateType.Emergency;
            rate.EmergencyRate = 175m;
            var rateId = (int)Services.TestRate.Add(rate);

            var effective = Services.TestRate.GetEffectiveRate(testId, (int)RateType.Emergency, null, null, null);
            Assert.IsNotNull(effective);
            Assert.AreEqual(175m, effective.Rate);

            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }

        [TestMethod]
        public void TestRate_Expired_Rate_Not_Returned_By_GetEffective()
        {
            var testId = EnsureTestId();
            var expired = MasterTestDataBuilder.StandardRate(testId, 50m, DateTime.Today.AddDays(-60), DateTime.Today.AddDays(-1));
            var rateId = (int)Services.TestRate.Add(expired);

            var effective = Services.TestRate.GetEffectiveRate(testId, (int)RateType.Standard, null, null, null);
            if (effective != null && effective.Id == rateId)
            {
                Assert.Fail("Expired rate should not be selected as effective");
            }

            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }

        [TestMethod]
        public void TestRate_Corporate_Requires_CorporateId()
        {
            var testId = EnsureTestId();
            var corpCode = UniqueCode("CORP");
            var corpId = (int)Services.Corporate.Add(MasterTestDataBuilder.Corporate(corpCode));

            var rate = MasterTestDataBuilder.StandardRate(testId, 200m);
            rate.RateType = (int)RateType.Corporate;
            rate.CorporateId = corpId;
            var rateId = (int)Services.TestRate.Add(rate);

            var effective = Services.TestRate.GetEffectiveRate(testId, (int)RateType.Corporate, corpId, null, null);
            Assert.IsNotNull(effective);
            Assert.AreEqual(corpId, effective.CorporateId);

            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
            Services.Corporate.Delete(new CorporateMaster { Id = corpId });
        }

        [TestMethod]
        public void TestRate_List_Sort_InvalidColumn_Does_Not_Throw()
        {
            var list = Services.TestRate.Get(ListOptionsFactory.Create(sortColumn: "Name"));
            Assert.IsNotNull(list);
        }

        [TestMethod]
        public void TestRate_GetByTestId_Returns_Active_Only()
        {
            var testId = EnsureTestId();
            var rateId = (int)Services.TestRate.Add(MasterTestDataBuilder.StandardRate(testId, 88m));

            var rates = Services.TestRate.GetByTestId(testId);
            Assert.IsTrue(rates.Any(r => r.Id == rateId));

            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
            rates = Services.TestRate.GetByTestId(testId);
            Assert.IsFalse(rates.Any(r => r.Id == rateId));
        }
    }
}
