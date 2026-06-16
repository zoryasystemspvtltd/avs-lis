using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LIS.Masters.Tests.Transactions
{
    [TestClass]
    public class TestProfileHierarchyTests : IntegrationTestBase
    {
        [TestMethod]
        public void TestProfile_GetWithHierarchy_Includes_Parameters()
        {
            var testId = EnsureTestId();
            var code = UniqueCode("HPROF");
            var profile = MasterTestDataBuilder.Profile(code, testId);
            Services.TestProfile.SaveWithDetails(profile, profile.ProfileDetails);

            var hierarchy = Services.TestProfile.GetWithHierarchy(profile.Id);
            Assert.IsNotNull(hierarchy);
            Assert.IsTrue(hierarchy.Tests != null && hierarchy.Tests.Any());
            Assert.AreEqual(testId, hierarchy.Tests.First().TestId);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestProfile_Duplicate_Test_In_Profile_Blocked()
        {
            var testId = EnsureTestId();
            var profile = MasterTestDataBuilder.Profile(UniqueCode("HDUP"), testId);
            profile.ProfileDetails.Add(new TestProfileDetail { TestId = testId, Quantity = 1 });
            Services.TestProfile.SaveWithDetails(profile, profile.ProfileDetails);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestProfile_Empty_Lines_Blocked()
        {
            var profile = new TestProfileMaster
            {
                Code = UniqueCode("HEMPTY"),
                Name = "Empty Profile",
                PackageRate = 100,
                IsActive = true
            };
            Services.TestProfile.SaveWithDetails(profile, new TestProfileDetail[0]);
        }

        private int EnsureTestId()
        {
            var existing = Services.HisTest.Get(ListOptionsFactory.ForHisTest()).Items?.FirstOrDefault(t => t.IsActive);
            if (existing != null)
            {
                return existing.Id;
            }

            var dept = Services.Department.Get().First();
            var specimen = Services.Specimen.Get().Cast<HISSpecimenMaster>().First();
            return (int)Services.HisTest.Add(MasterTestDataBuilder.HisTest(UniqueCode("TST"), dept.Code, specimen.Code));
        }
    }
}
