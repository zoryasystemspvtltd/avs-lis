using LIS.BusinessLogic;
using LIS.DtoModel.Interfaces;
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
            // Equipment master is not seeded by tests; reuse an existing active equipment.
            var existing = Services.Equipment.Get().FirstOrDefault(e => e.IsActive);
            if (existing == null)
            {
                Assert.Inconclusive("No active equipment available; add one via the Equipments UI.");
            }

            return existing.Id;
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

            var rangeCodeSearch = Services.HisParameterRange.Get(
                ListOptionsFactory.Create("HISRangeCode", 1, 500, loadedRange.HISRangeCode));
            Assert.IsTrue(rangeCodeSearch.Items.Any(r => r.Id == rangeId));

            var parameterNameSearch = Services.HisParameterRange.Get(
                ListOptionsFactory.Create("HISRangeCode", 1, 500, param.HISParamDescription));
            Assert.IsTrue(parameterNameSearch.Items.Any(r => r.Id == rangeId));

            loadedRange.Gender = "Female";
            Services.HisParameterRange.Update(loadedRange);
            var updatedRange = Services.HisParameterRange.GetById(rangeId);
            Assert.AreEqual("Female", updatedRange.Gender);
            Assert.AreEqual(loadedRange.HISRangeCode, updatedRange.HISRangeCode);

            Services.HisParameterRange.Delete(new HISParameterRangMaster { Id = rangeId });
            Services.HisParameter.Delete(new HISParameterMaster { Id = paramId });
        }

        [TestMethod]
        public void HisParameterRange_Edit_Through_Interface_Preserves_Code_And_Updates()
        {
            var testId = EnsureTestId();
            var test = Services.HisTest.GetTestById(testId);
            var paramId = (int)Services.HisParameter.Add(new HISParameterMaster
            {
                HisTestId = testId,
                HISTestCode = test.HISTestCode,
                HISParamCode = UniqueCode("RNGP"),
                HISParamDescription = "Range parent " + UniqueCode("RNGP")
            });

            var rangeId = (int)Services.HisParameterRange.Add(new HISParameterRangMaster
            {
                HisParameterId = paramId,
                HISRangeValue = "Normal",
                Gender = "Male",
                AgeFrom = 0,
                AgeTo = 99,
                MinValue = 10,
                MaxValue = 20
            });

            var original = Services.HisParameterRange.GetById(rangeId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(original.HISRangeCode));

            var manager = Services.HisParameterRange as IMasterCrudManager<HISParameterRangMaster>;
            Assert.IsNotNull(manager);

            var apiPayload = new HISParameterRangMaster
            {
                Id = rangeId,
                HisParameterId = paramId,
                HISRangeCode = string.Empty,
                HISRangeValue = "High",
                Gender = "Female",
                AgeFrom = 18,
                AgeTo = 65,
                AgeType = "Year",
                MinValue = 21,
                MaxValue = 30
            };

            manager.Update(apiPayload);

            var updated = Services.HisParameterRange.GetById(rangeId);
            Assert.AreEqual(original.HISRangeCode, updated.HISRangeCode);
            Assert.AreEqual("High", updated.HISRangeValue);
            Assert.AreEqual("Female", updated.Gender);
            Assert.AreEqual(18m, updated.AgeFrom);
            Assert.AreEqual(65m, updated.AgeTo);
            Assert.AreEqual("Year", updated.AgeType);
            Assert.AreEqual(21m, updated.MinValue);
            Assert.AreEqual(30m, updated.MaxValue);

            Services.HisParameterRange.Delete(new HISParameterRangMaster { Id = rangeId });
            Services.HisParameter.Delete(new HISParameterMaster { Id = paramId });
        }

        [TestMethod]
        public void HisParameterRange_Edit_After_Load_Does_Not_Attach_Conflict()
        {
            var testId = EnsureTestId();
            var test = Services.HisTest.GetTestById(testId);
            var paramId = (int)Services.HisParameter.Add(new HISParameterMaster
            {
                HisTestId = testId,
                HISTestCode = test.HISTestCode,
                HISParamCode = UniqueCode("RNGE"),
                HISParamDescription = "Range edit " + UniqueCode("RNGE")
            });

            var rangeId = (int)Services.HisParameterRange.Add(new HISParameterRangMaster
            {
                HisParameterId = paramId,
                HISRangeValue = "Low",
                Gender = "Both",
                MinValue = 1,
                MaxValue = 5
            });

            var loaded = Services.HisParameterRange.GetById(rangeId);
            var manager = Services.HisParameterRange as IMasterCrudManager<HISParameterRangMaster>;
            Assert.IsNotNull(manager);

            loaded.HISRangeValue = "Critical Low";
            loaded.MinValue = 0.5m;
            loaded.MaxValue = 2.5m;
            manager.Update(loaded);

            var reloaded = Services.HisParameterRange.GetById(rangeId);
            Assert.AreEqual("Critical Low", reloaded.HISRangeValue);
            Assert.AreEqual(0.5m, reloaded.MinValue);
            Assert.AreEqual(2.5m, reloaded.MaxValue);

            Services.HisParameterRange.Delete(new HISParameterRangMaster { Id = rangeId });
            Services.HisParameter.Delete(new HISParameterMaster { Id = paramId });
        }

        [TestMethod]
        public void HisParameter_Duplicate_Code_Globally_Blocked()
        {
            var testId = EnsureTestId();
            var test = Services.HisTest.GetTestById(testId);
            var paramCode = UniqueCode("DUPP");

            var param = new HISParameterMaster
            {
                HisTestId = testId,
                HISTestCode = test.HISTestCode,
                HISParamCode = paramCode,
                HISParamDescription = "Param " + paramCode
            };
            var paramId = (int)Services.HisParameter.Add(param);
            Assert.IsTrue(paramId > 0);

            var duplicateCode = new HISParameterMaster
            {
                HisTestId = testId,
                HISTestCode = test.HISTestCode,
                HISParamCode = paramCode,
                HISParamDescription = "Duplicate " + paramCode
            };
            var ex = Assert.ThrowsException<InvalidOperationException>(() => Services.HisParameter.Add(duplicateCode));
            Assert.AreEqual(HisParameterMasterManager.ParameterCodeExistsMessage, ex.Message);

            var duplicateCodeCase = new HISParameterMaster
            {
                HisTestId = testId,
                HISTestCode = test.HISTestCode,
                HISParamCode = paramCode.ToLowerInvariant(),
                HISParamDescription = "Another " + paramCode
            };
            ex = Assert.ThrowsException<InvalidOperationException>(() => Services.HisParameter.Add(duplicateCodeCase));
            Assert.AreEqual(HisParameterMasterManager.ParameterCodeExistsMessage, ex.Message);

            Services.HisParameter.Delete(new HISParameterMaster { Id = paramId });
        }

        [TestMethod]
        public void HisParameter_Duplicate_Description_Globally_Blocked()
        {
            var testId = EnsureTestId();
            var test = Services.HisTest.GetTestById(testId);
            var paramCode = UniqueCode("DESC");
            var description = "Haemoglobin " + paramCode;

            var param = new HISParameterMaster
            {
                HisTestId = testId,
                HISTestCode = test.HISTestCode,
                HISParamCode = paramCode,
                HISParamDescription = description
            };
            var paramId = (int)Services.HisParameter.Add(param);
            Assert.IsTrue(paramId > 0);

            var duplicateDescription = new HISParameterMaster
            {
                HisTestId = testId,
                HISTestCode = test.HISTestCode,
                HISParamCode = UniqueCode("NEW"),
                HISParamDescription = description.ToUpperInvariant()
            };
            var ex = Assert.ThrowsException<InvalidOperationException>(() => Services.HisParameter.Add(duplicateDescription));
            Assert.AreEqual(HisParameterMasterManager.ParameterDescriptionExistsMessage, ex.Message);

            Services.HisParameter.Delete(new HISParameterMaster { Id = paramId });
        }

        [TestMethod]
        public void HisParameter_Edit_Without_Change_Succeeds()
        {
            var testId = EnsureTestId();
            var test = Services.HisTest.GetTestById(testId);
            var paramCode = UniqueCode("EDIT");

            var param = new HISParameterMaster
            {
                HisTestId = testId,
                HISTestCode = test.HISTestCode,
                HISParamCode = paramCode,
                HISParamDescription = "Edit " + paramCode,
                HISParamUnit = "mg/dL"
            };
            var paramId = (int)Services.HisParameter.Add(param);
            Assert.IsTrue(paramId > 0);

            var loaded = Services.HisParameter.GetById(paramId);
            loaded.HISParamUnit = "g/dL";
            Services.HisParameter.Update(loaded);

            var reloaded = Services.HisParameter.GetById(paramId);
            Assert.AreEqual("g/dL", reloaded.HISParamUnit);
            Assert.AreEqual(paramCode, reloaded.HISParamCode);

            Services.HisParameter.Delete(new HISParameterMaster { Id = paramId });
        }

        [TestMethod]
        public void HisParameter_Add_Through_Interface_Uses_Validation()
        {
            var testId = EnsureTestId();
            var test = Services.HisTest.GetTestById(testId);
            var paramCode = UniqueCode("IFC");
            var manager = Services.HisParameter as IMasterCrudManager<HISParameterMaster>;
            Assert.IsNotNull(manager);

            var paramId = (int)manager.Add(new HISParameterMaster
            {
                HisTestId = testId,
                HISTestCode = test.HISTestCode,
                HISParamCode = paramCode,
                HISParamDescription = "Interface " + paramCode
            });
            Assert.IsTrue(paramId > 0);

            var duplicate = new HISParameterMaster
            {
                HisTestId = testId,
                HISTestCode = test.HISTestCode,
                HISParamCode = paramCode,
                HISParamDescription = "Dup " + paramCode
            };
            var ex = Assert.ThrowsException<InvalidOperationException>(() => manager.Add(duplicate));
            Assert.AreEqual(HisParameterMasterManager.ParameterCodeExistsMessage, ex.Message);

            Services.HisParameter.Delete(new HISParameterMaster { Id = paramId });
        }

        [TestMethod]
        public void TestMapping_Create_Update_No_Duplicate_For_Same_Equipment()
        {
            var testId = EnsureTestId();
            var test = Services.HisTest.GetTestById(testId);
            var equipId = EnsureEquipmentId();
            var lisCode = UniqueCode("LIS");
            var paramCode = UniqueCode("P");
            var paramId = (int)Services.HisParameter.Add(new HISParameterMaster
            {
                HISParamCode = paramCode,
                HISParamDescription = "Param " + paramCode,
                HisTestId = testId
            });

            var mapping = new TestMappingMaster
            {
                HISParamCode = paramCode,
                HISParamDescription = "Param " + paramCode,
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
                HISParamCode = paramCode,
                LISTestCode = lisCode,
                EquipmentId = equipId,
                IsActive = true
            };
            Assert.ThrowsException<InvalidOperationException>(() => Services.TestMapping.Add(dup));

            Services.TestMapping.Delete(new TestMappingMaster { Id = mapId });
            Services.HisParameter.Delete(new HISParameterMaster { Id = paramId });
        }
    }
}
