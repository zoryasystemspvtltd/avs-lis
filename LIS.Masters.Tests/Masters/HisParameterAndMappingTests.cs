using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LIS.Masters.Tests.Masters
{
    [TestClass]
    public class HisParameterAndMappingTests : IntegrationTestBase
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

        private int EnsureEquipmentId()
        {
            var key = UniqueCode("EQ", 12);
            return (int)Services.Equipment.Add(new EquipmentMaster
            {
                Name = "UT Equipment",
                Model = "M1",
                AccessKey = key,
                IsActive = true,
                CreatedOn = DateTime.Now
            });
        }

        [TestMethod]
        public void HisParameter_And_Range_Crud_With_Test_FK()
        {
            var testId = EnsureTestId();
            var test = Services.HisTest.GetTestById(testId);
            var paramCode = UniqueCode("HP");

            var param = new HISParameterMaster
            {
                HisTestId = testId,
                HISTestCode = test.HISTestCode,
                HISParamCode = paramCode,
                HISParamDescription = "Param " + paramCode,
                HISParamUnit = "mg/dL"
            };
            var paramId = (int)Services.HisParameter.Add(param);
            Assert.IsTrue(paramId > 0);

            var loaded = Services.HisParameter.GetById(paramId);
            Assert.AreEqual(test.HISTestCode, loaded.HISTestCode);
            Assert.AreEqual("mg/dL", loaded.HISParamUnit);

            loaded.HISParamMethod = "Photometry";
            Services.HisParameter.Update(loaded);
            var reloaded = Services.HisParameter.GetById(paramId);
            Assert.AreEqual("Photometry", reloaded.HISParamMethod);

            var rangeId = (int)Services.HisParameterRange.Add(new HISParameterRangMaster
            {
                HisParameterId = paramId,
                HISRangeValue = "Normal",
                Gender = "Male",
                AgeFrom = 0,
                AgeTo = 120,
                MinValue = 0,
                MaxValue = 100
            });
            Assert.IsTrue(rangeId > 0);

            var loadedRange = Services.HisParameterRange.GetById(rangeId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(loadedRange.HISRangeCode));
            Assert.AreEqual("Male", loadedRange.Gender);

            var duplicate = new HISParameterRangMaster
            {
                HisParameterId = paramId,
                HISRangeCode = loadedRange.HISRangeCode,
                HISRangeValue = "Dup",
                Gender = "Female"
            };
            Assert.ThrowsException<InvalidOperationException>(() => Services.HisParameterRange.Add(duplicate));

            var ranges = Services.HisParameterRange.Get(ListOptionsFactory.Create());
            Assert.IsTrue(ranges.Items.Any(r => r.Id == rangeId));

            loadedRange.Gender = "Female";
            Services.HisParameterRange.Update(loadedRange);
            var updatedRange = Services.HisParameterRange.GetById(rangeId);
            Assert.AreEqual("Female", updatedRange.Gender);
            Assert.AreEqual(loadedRange.HISRangeCode, updatedRange.HISRangeCode);

            Services.HisParameterRange.Delete(new HISParameterRangMaster { Id = rangeId });
            Services.HisParameter.Delete(new HISParameterMaster { Id = paramId });
        }

        [TestMethod]
        public void TestMapping_Create_Update_No_Duplicate_For_Same_Equipment()
        {
            var testId = EnsureTestId();
            var test = Services.HisTest.GetTestById(testId);
            var equipId = EnsureEquipmentId();
            var lisCode = UniqueCode("LIS");

            var mapping = new TestMappingMaster
            {
                HISTestCode = test.HISTestCode,
                HISTestCodeDescription = test.HISTestCodeDescription,
                LISTestCode = lisCode,
                LISTestCodeDescription = "LIS " + lisCode,
                EquipmentId = equipId,
                IsActive = true
            };
            var mapId = (int)Services.TestMapping.Add(mapping);
            Assert.IsTrue(mapId > 0);

            var loaded = Services.TestMapping.GetById(mapId);
            loaded.LISTestCodeDescription = "Updated";
            Services.TestMapping.Update(loaded);
            Assert.AreEqual("Updated", Services.TestMapping.GetById(mapId).LISTestCodeDescription);

            var dup = new TestMappingMaster
            {
                HISTestCode = test.HISTestCode,
                LISTestCode = lisCode,
                EquipmentId = equipId,
                IsActive = true
            };
            Assert.ThrowsException<InvalidOperationException>(() => Services.TestMapping.Add(dup));

            Services.TestMapping.Delete(new TestMappingMaster { Id = mapId });
            Services.Equipment.Delete(new EquipmentMaster { Id = equipId });
        }
    }
}
