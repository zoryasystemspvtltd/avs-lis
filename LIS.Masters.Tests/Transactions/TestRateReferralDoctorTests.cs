using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LIS.Masters.Tests.Transactions
{
    [TestClass]
    public class TestRateReferralDoctorTests : IntegrationTestBase
    {
        [TestMethod]
        public void TestRate_ReferralDoctor_GetEffective_Uses_Doctor_Rate()
        {
            var testId = CreateIsolatedTest();
            var docCode = UniqueCode("RD");
            var docId = (int)Services.ReferralDoctor.Add(MasterTestDataBuilder.ReferralDoctor(docCode));

            var rate = MasterTestDataBuilder.StandardRate(testId, 120m);
            rate.RateType = (int)RateType.ReferralDoctor;
            rate.ReferralDoctorId = docId;
            var rateId = (int)Services.TestRate.Add(rate);

            var effective = Services.TestRate.GetEffectiveRate(testId, (int)RateType.ReferralDoctor, null, docId, null);
            Assert.IsNotNull(effective);
            Assert.AreEqual(docId, effective.ReferralDoctorId);
            Assert.AreEqual(120m, effective.Rate);

            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
            Services.ReferralDoctor.Delete(new ReferralDoctorMaster { Id = docId });
        }
    }
}
