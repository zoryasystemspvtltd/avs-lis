using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LIS.Masters.Tests.Masters
{
    [TestClass]
    public class HisTestMasterManagerTests : IntegrationTestBase
    {
        [TestMethod]
        public void HisTest_Create_List_GetById_Update()
        {
            var dept = Services.Department.Get().First();
            var specimen = Services.Specimen.Get().Cast<HISSpecimenMaster>().First();

            var code = UniqueCode("TST");
            var test = MasterTestDataBuilder.HisTest(code, dept.Code, specimen.Code);
            var id = Services.HisTest.Add(test);
            Assert.IsTrue(id > 0);

            var list = Services.HisTest.Get(ListOptionsFactory.ForHisTest());
            Assert.IsTrue(list.Items.Any(t => t.HISTestCode == code));

            var loaded = Services.HisTest.GetTestById(id);
            Assert.AreEqual(code, loaded.HISTestCode);

            loaded.HISTestCodeDescription = "Updated description";
            Services.HisTest.Update(loaded);
            Assert.AreEqual("Updated description", Services.HisTest.GetTestById(id).HISTestCodeDescription);
        }

        [TestMethod]
        public void HisTest_List_With_Invalid_Sort_Does_Not_Throw()
        {
            var option = ListOptionsFactory.Create(sortColumn: "Name");
            var list = Services.HisTest.Get(option);
            Assert.IsNotNull(list);
        }

        [TestMethod]
        public void HisTest_List_Search_By_Text()
        {
            var existing = Services.HisTest.Get(ListOptionsFactory.ForHisTest());
            if (existing.Items == null || !existing.Items.Any())
            {
                Assert.Inconclusive("No HIS tests in database to search");
            }

            var sample = existing.Items.First();
            var search = sample.HISTestCode.Substring(0, System.Math.Min(3, sample.HISTestCode.Length));
            var filtered = Services.HisTest.Get(ListOptionsFactory.Create(sortColumn: "HISTestCode", search: search));
            Assert.IsTrue(filtered.Items.Any(i => i.HISTestCode.Contains(search)));
        }
    }
}
