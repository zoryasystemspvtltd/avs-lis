using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LIS.Masters.Tests.Masters
{
    [TestClass]
    public class SpecimenManagerTests : IntegrationTestBase
    {
        [TestMethod]
        public void Specimen_Create_List_GetById_Update()
        {
            var code = UniqueCode("SP");
            var specimen = new HISSpecimenMaster
            {
                Code = code,
                Name = "Unit Test Specimen",
                IsActive = true
            };

            Services.Specimen.Add(specimen);
            var all = Services.Specimen.Get().Cast<HISSpecimenMaster>().ToList();
            Assert.IsTrue(all.Any(s => s.Code == code));

            var byId = Services.Specimen.Get(all.First(s => s.Code == code).Id);
            Assert.AreEqual(code, byId.Code);

            byId.Name = "Updated Specimen";
            Services.Specimen.Update(byId);
            Assert.AreEqual("Updated Specimen", Services.Specimen.Get(byId.Id).Name);
        }

        [TestMethod]
        public void Specimen_List_With_ApiOption_Returns_Paged_Result()
        {
            var list = Services.Specimen.Get(ListOptionsFactory.Create(sortColumn: "Name", pageSize: 10));
            Assert.IsNotNull(list);
            Assert.IsTrue(list.TotalRecord >= 0);
        }
    }
}
