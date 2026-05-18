using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LIS.Masters.Tests.Masters
{
    [TestClass]
    public class EquipmentManagerTests : IntegrationTestBase
    {
        [TestMethod]
        public void Equipment_Create_Get_Deactivate()
        {
            var key = UniqueCode("EQ", 12);
            var id = (int)Services.Equipment.Add(new EquipmentMaster
            {
                Name = "Analyzer " + key,
                Model = "X1",
                AccessKey = key,
                IsActive = true,
                CreatedOn = DateTime.Now
            });

            var loaded = Services.Equipment.Get(id);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(key, loaded.AccessKey);

            Services.Equipment.Delete(new EquipmentMaster { Id = id });
            var after = Services.Equipment.Get(id);
            Assert.IsFalse(after.IsActive);
        }

        [TestMethod]
        public void Equipment_List_Returns_Records()
        {
            var list = Services.Equipment.Get();
            Assert.IsNotNull(list);
        }
    }
}
