using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LIS.Masters.Tests.Masters
{
    [TestClass]
    public class LookupMasterCrudTests : IntegrationTestBase
    {
        [TestMethod]
        public void ReferralDoctor_Crud_Search_Pagination_Deactivate()
        {
            var code = UniqueCode("RD");
            var manager = Services.ReferralDoctor;

            var id = (int)manager.Add(MasterTestDataBuilder.ReferralDoctor(code));
            Assert.IsTrue(id > 0);

            var loaded = manager.GetById(id);
            Assert.AreEqual(code, loaded.Code);

            loaded.Name = "Updated " + code;
            manager.Update(loaded);
            Assert.AreEqual("Updated " + code, manager.GetById(id).Name);

            var list = manager.Get(ListOptionsFactory.Create(search: code));
            Assert.IsTrue(list.Items.Any(i => i.Code == code));

            manager.Delete(new ReferralDoctorMaster { Id = id });
            var activeOnly = manager.GetAllActive().Any(i => i.Id == id);
            Assert.IsFalse(activeOnly, "Inactive doctor should be excluded from GetAllActive");
        }

        [TestMethod]
        public void Corporate_Crud_List_Returns_Record()
        {
            var code = UniqueCode("CORP");
            var id = (int)Services.Corporate.Add(MasterTestDataBuilder.Corporate(code));

            var list = Services.Corporate.Get(ListOptionsFactory.Create(search: code));
            Assert.IsTrue(list.TotalRecord > 0);
            Assert.IsTrue(list.Items.Any(i => i.Id == id));

            Services.Corporate.Delete(new CorporateMaster { Id = id });
        }

        [TestMethod]
        public void Unit_Method_SampleType_Container_Create_And_List()
        {
            var unitCode = UniqueCode("U");
            var methodCode = UniqueCode("M");
            var stCode = UniqueCode("ST");
            var contCode = UniqueCode("C");

            var unitId = (int)Services.Unit.Add(MasterTestDataBuilder.Unit(unitCode));
            var methodId = (int)Services.Method.Add(new MethodMaster { Code = methodCode, Name = "Method", IsActive = true });
            var stId = (int)Services.SampleType.Add(new SampleTypeMaster { Code = stCode, Name = "SampleType", IsActive = true });
            var contId = (int)Services.Container.Add(new ContainerMaster { Code = contCode, Name = "Container", IsActive = true });

            Assert.IsTrue(unitId > 0 && methodId > 0 && stId > 0 && contId > 0);

            Assert.IsNotNull(Services.Unit.GetById(unitId));
            Assert.IsNotNull(Services.Method.GetById(methodId));
            Assert.IsNotNull(Services.SampleType.GetById(stId));
            Assert.IsNotNull(Services.Container.GetById(contId));

            Services.Unit.Delete(new UnitMaster { Id = unitId });
            Services.Method.Delete(new MethodMaster { Id = methodId });
            Services.SampleType.Delete(new SampleTypeMaster { Id = stId });
            Services.Container.Delete(new ContainerMaster { Id = contId });
        }

        [TestMethod]
        public void TestGroup_And_TestCategory_Create_List()
        {
            var grpCode = UniqueCode("TG");
            var catCode = UniqueCode("TC");

            var grpId = (int)Services.TestGroup.Add(new TestGroupMaster { Code = grpCode, Name = "Group", IsActive = true });
            var catId = (int)Services.TestCategory.Add(new TestCategoryMaster { Code = catCode, Name = "Category", IsActive = true });

            Assert.IsTrue(Services.TestGroup.Get(ListOptionsFactory.Create()).Items.Any(x => x.Id == grpId));
            Assert.IsTrue(Services.TestCategory.Get(ListOptionsFactory.Create()).Items.Any(x => x.Id == catId));

            Services.TestGroup.Delete(new TestGroupMaster { Id = grpId });
            Services.TestCategory.Delete(new TestCategoryMaster { Id = catId });
        }
    }
}
