using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LIS.Masters.Tests.Transactions
{
    [TestClass]
    public class TestRateEffectiveDateExtendedTests : IntegrationTestBase
    {
        private int EnsureTestId()
        {
            var existing = Services.HisTest.Get(ListOptionsFactory.ForHisTest()).Items?.FirstOrDefault();
            if (existing != null)
            {
                return existing.Id;
            }

            var dept = Services.Department.Get().First();
            var specimen = Services.Specimen.Get().Cast<HISSpecimenMaster>().First();
            return (int)Services.HisTest.Add(MasterTestDataBuilder.HisTest(UniqueCode("TST"), dept.Code, specimen.Code));
        }

        [TestMethod]
        public void TestRate_Future_Rate_Not_Returned_By_GetEffective()
        {
            var testId = EnsureTestId();
            var future = MasterTestDataBuilder.StandardRate(testId, 999m, DateTime.Today.AddDays(10), DateTime.Today.AddDays(400));
            var rateId = (int)Services.TestRate.Add(future);

            var effective = Services.TestRate.GetEffectiveRate(testId, (int)RateType.Standard, null, null, null);
            if (effective != null && effective.Id == rateId)
            {
                Assert.Fail("Future-dated rate must not be selected as effective");
            }

            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }

        [TestMethod]
        public void TestRate_Overlapping_Current_Selects_Latest_EffectiveStart()
        {
            var dept = Services.Department.Get().First();
            var specimen = Services.Specimen.Get().Cast<HISSpecimenMaster>().First();
            var testId = (int)Services.HisTest.Add(MasterTestDataBuilder.HisTest(UniqueCode("TST"), dept.Code, specimen.Code));

            var older = MasterTestDataBuilder.StandardRate(testId, 80m, DateTime.Today.AddDays(-60), DateTime.Today.AddDays(365));
            var newer = MasterTestDataBuilder.StandardRate(testId, 120m, DateTime.Today.AddDays(-1), DateTime.Today.AddDays(365));
            var olderId = (int)Services.TestRate.Add(older);
            var newerId = (int)Services.TestRate.Add(newer);

            var effective = Services.TestRate.GetEffectiveRate(testId, (int)RateType.Standard, null, null, null);
            Assert.IsNotNull(effective);
            Assert.IsTrue(effective.Id == newerId || effective.Rate == 120m,
                "When multiple rates overlap, latest EffectiveStart should win");

            Services.TestRate.Delete(new TestRateMaster { Id = olderId });
            Services.TestRate.Delete(new TestRateMaster { Id = newerId });
        }

        [TestMethod]
        public void TestRate_Profile_GetEffective_Uses_ProfileId()
        {
            var testId = EnsureTestId();
            var code = UniqueCode("PROF");
            var profile = MasterTestDataBuilder.Profile(code, testId);
            Services.TestProfile.SaveWithDetails(profile, profile.ProfileDetails);

            var rate = MasterTestDataBuilder.StandardRate(testId, 450m);
            rate.RateType = (int)RateType.Profile;
            rate.TestProfileId = profile.Id;
            var rateId = (int)Services.TestRate.Add(rate);

            var effective = Services.TestRate.GetEffectiveRate(testId, (int)RateType.Profile, null, null, profile.Id);
            Assert.IsNotNull(effective);
            Assert.AreEqual(profile.Id, effective.TestProfileId);
            Assert.AreEqual(450m, effective.Rate);

            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
        }
    }
}
