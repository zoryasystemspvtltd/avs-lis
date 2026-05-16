using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LIS.Masters.Tests.Transactions
{
    [TestClass]
    public class TestProfileMasterManagerTests : IntegrationTestBase
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
        public void TestProfile_SaveWithDetails_Persists_Lines()
        {
            var testId = EnsureTestId();
            var code = UniqueCode("PROF");
            var profile = MasterTestDataBuilder.Profile(code, testId);

            Services.TestProfile.SaveWithDetails(profile, profile.ProfileDetails);
            Assert.IsTrue(profile.Id > 0);

            var loaded = Services.TestProfile.GetWithDetails(profile.Id);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(code, loaded.Code);
            Assert.IsTrue(loaded.ProfileDetails != null && loaded.ProfileDetails.Any());
            Assert.AreEqual(testId, loaded.ProfileDetails.First().TestId);
        }

        [TestMethod]
        public void TestProfile_Update_Replaces_Detail_Lines()
        {
            var testId1 = EnsureTestId();
            var testId2 = EnsureTestId();
            var code = UniqueCode("PROF2");
            var profile = MasterTestDataBuilder.Profile(code, testId1);
            Services.TestProfile.SaveWithDetails(profile, profile.ProfileDetails);

            var updated = Services.TestProfile.GetWithDetails(profile.Id);
            var newLines = new System.Collections.Generic.List<TestProfileDetail>
            {
                new TestProfileDetail { TestId = testId2, Quantity = 2 }
            };
            Services.TestProfile.SaveWithDetails(updated, newLines);

            var reloaded = Services.TestProfile.GetWithDetails(profile.Id);
            Assert.AreEqual(1, reloaded.ProfileDetails.Count());
            Assert.AreEqual(testId2, reloaded.ProfileDetails.First().TestId);
            Assert.AreEqual(2, reloaded.ProfileDetails.First().Quantity);
        }

        [TestMethod]
        public void TestProfile_Deactivate_Excluded_From_GetAllActive()
        {
            var code = UniqueCode("PROF3");
            var testId = EnsureTestId();
            var profile = MasterTestDataBuilder.Profile(code, testId);
            Services.TestProfile.SaveWithDetails(profile, profile.ProfileDetails);

            Services.TestProfile.Delete(new TestProfileMaster { Id = profile.Id });
            Assert.IsFalse(Services.TestProfile.GetAllActive().Any(p => p.Id == profile.Id));
        }
    }
}
