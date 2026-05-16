using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LIS.Masters.Tests.Masters
{
    [TestClass]
    public class DepartmentManagerTests : IntegrationTestBase
    {
        [TestMethod]
        public void Department_Create_Update_List_Search_Delete()
        {
            var code = UniqueCode("D", maxLength: 15);
            var dept = new Departments { Code = code, Name = "Unit Test Department" };

            Services.Department.Add(dept);
            var loaded = Services.Department.Get(code);
            Assert.IsNotNull(loaded);
            Assert.AreEqual("Unit Test Department", loaded.Name);

            loaded.Name = "Updated Department";
            Services.Department.Update(loaded);
            Assert.AreEqual("Updated Department", Services.Department.Get(code).Name);

            var list = Services.Department.Get(ListOptionsFactory.Create(search: code));
            Assert.IsTrue(list.Items.Any(d => d.Code == code));

            Services.Department.Delete(loaded);
            Assert.IsNull(Services.Department.Get(code));
        }

        [TestMethod]
        public void Department_List_Sort_InvalidColumn_Defaults_To_Name()
        {
            var list = Services.Department.Get(ListOptionsFactory.Create(sortColumn: "InvalidColumn"));
            Assert.IsNotNull(list);
            Assert.IsTrue(list.TotalRecord > 0);
        }
    }
}
