using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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

            var list = Services.HisTest.Get(ListOptionsFactory.Create(sortColumn: "HISTestCode", page: 1, pageSize: 50, search: code));
            Assert.IsTrue(list.Items.Any(t => t.HISTestCode == code));

            var loaded = Services.HisTest.GetTestById(id);
            Assert.AreEqual(code, loaded.HISTestCode);

            var updatedName = "Updated " + code;
            loaded.HISTestCodeDescription = updatedName;
            Services.HisTest.Update(loaded);
            Assert.AreEqual(updatedName, Services.HisTest.GetTestById(id).HISTestCodeDescription);
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

        [TestMethod]
        public void HisTest_Duplicate_TestCode_Throws_On_Create()
        {
            var dept = Services.Department.Get().First();
            var specimen = Services.Specimen.Get().Cast<HISSpecimenMaster>().First();
            var code = UniqueCode("TST");
            var id = Services.HisTest.Add(MasterTestDataBuilder.HisTest(code, dept.Code, specimen.Code));

            var duplicate = MasterTestDataBuilder.HisTest(code, dept.Code, specimen.Code);
            Assert.ThrowsException<InvalidOperationException>(() => Services.HisTest.Add(duplicate));

            Services.HisTest.Delete(Services.HisTest.GetTestById(id));
        }

        [TestMethod]
        public void HisTest_Duplicate_TestName_Throws_On_Create()
        {
            var dept = Services.Department.Get().First();
            var specimen = Services.Specimen.Get().Cast<HISSpecimenMaster>().First();
            var code1 = UniqueCode("TST");
            var code2 = UniqueCode("TST");
            var sharedName = "Shared Test Name " + UniqueCode("NM");
            var test1 = MasterTestDataBuilder.HisTest(code1, dept.Code, specimen.Code);
            test1.HISTestCodeDescription = sharedName;
            var id = Services.HisTest.Add(test1);

            var test2 = MasterTestDataBuilder.HisTest(code2, dept.Code, specimen.Code);
            test2.HISTestCodeDescription = sharedName;
            Assert.ThrowsException<InvalidOperationException>(() => Services.HisTest.Add(test2));

            Services.HisTest.Delete(Services.HisTest.GetTestById(id));
        }

        [TestMethod]
        public void HisTest_List_Search_Is_Case_Insensitive()
        {
            var existing = Services.HisTest.Get(ListOptionsFactory.ForHisTest());
            if (existing.Items == null || !existing.Items.Any())
            {
                Assert.Inconclusive("No HIS tests in database to search");
            }

            var sample = existing.Items.First(i => !string.IsNullOrEmpty(i.HISTestCode));
            var filtered = Services.HisTest.Get(ListOptionsFactory.Create(
                sortColumn: "HISTestCode",
                page: 1,
                pageSize: 10,
                search: sample.HISTestCode.ToLowerInvariant()));
            Assert.IsTrue(filtered.Items.Any(i => i.HISTestCode == sample.HISTestCode));
        }

    }
}
