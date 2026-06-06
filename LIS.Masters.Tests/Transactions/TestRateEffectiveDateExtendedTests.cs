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
            return CreateIsolatedTest();
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
        public void TestRate_Overlapping_Period_Blocked_On_Save()
        {
            var dept = Services.Department.Get().First();
            var specimen = Services.Specimen.Get().Cast<HISSpecimenMaster>().First();
            var testId = (int)Services.HisTest.Add(MasterTestDataBuilder.HisTest(UniqueCode("TST"), dept.Code, specimen.Code));

            var existing = MasterTestDataBuilder.StandardRate(testId, 80m, DateTime.Today.AddDays(-60), DateTime.Today.AddDays(365));
            var existingId = (int)Services.TestRate.Add(existing);

            var overlap = MasterTestDataBuilder.StandardRate(testId, 120m, DateTime.Today.AddDays(30), DateTime.Today.AddDays(90));
            Assert.ThrowsException<InvalidOperationException>(() => Services.TestRate.Add(overlap));

            var nonOverlap = MasterTestDataBuilder.StandardRate(testId, 150m, DateTime.Today.AddDays(400), DateTime.Today.AddDays(500));
            var nonOverlapId = (int)Services.TestRate.Add(nonOverlap);
            Assert.IsTrue(nonOverlapId > 0);

            Services.TestRate.Delete(new TestRateMaster { Id = nonOverlapId });
            Services.TestRate.Delete(new TestRateMaster { Id = existingId });
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
