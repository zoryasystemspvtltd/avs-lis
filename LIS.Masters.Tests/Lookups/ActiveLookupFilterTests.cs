using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LIS.Masters.Tests.Lookups
{
    [TestClass]
    public class ActiveLookupFilterTests : IntegrationTestBase
    {
        [TestMethod]
        public void GetAllActive_Excludes_Inactive_ReferralDoctor_Corporate_Unit()
        {
            var rdCode = UniqueCode("RD");
            var corpCode = UniqueCode("CORP");
            var unitCode = UniqueCode("U");

            var rdId = (int)Services.ReferralDoctor.Add(MasterTestDataBuilder.ReferralDoctor(rdCode));
            var corpId = (int)Services.Corporate.Add(MasterTestDataBuilder.Corporate(corpCode));
            var unitId = (int)Services.Unit.Add(MasterTestDataBuilder.Unit(unitCode));

            Services.ReferralDoctor.Delete(new ReferralDoctorMaster { Id = rdId });
            Services.Corporate.Delete(new CorporateMaster { Id = corpId });
            Services.Unit.Delete(new UnitMaster { Id = unitId });

            Assert.IsFalse(Services.ReferralDoctor.GetAllActive().Any(x => x.Id == rdId));
            Assert.IsFalse(Services.Corporate.GetAllActive().Any(x => x.Id == corpId));
            Assert.IsFalse(Services.Unit.GetAllActive().Any(x => x.Id == unitId));
        }

        [TestMethod]
        public void HisTest_Lookup_List_Returns_Items_With_TestCode()
        {
            var list = Services.HisTest.Get(ListOptionsFactory.ForHisTest());
            Assert.IsNotNull(list);
            if (list.TotalRecord > 0)
            {
                var first = list.Items.First();
                Assert.IsFalse(string.IsNullOrEmpty(first.HISTestCode));
            }
        }

        [TestMethod]
        public void TestRate_GetAllActive_Only_Returns_Active_Rates()
        {
            var test = Services.HisTest.Get(ListOptionsFactory.ForHisTest()).Items.FirstOrDefault();
            if (test == null)
            {
                Assert.Inconclusive("No tests available for rate lookup");
            }

            var rate = MasterTestDataBuilder.StandardRate(test.Id, 99.99m);
            var rateId = (int)Services.TestRate.Add(rate);
            Assert.IsTrue(Services.TestRate.GetAllActive().Any(r => r.Id == rateId));

            Services.TestRate.Delete(new TestRateMaster { Id = rateId });
            Assert.IsFalse(Services.TestRate.GetAllActive().Any(r => r.Id == rateId));
        }
    }
}
