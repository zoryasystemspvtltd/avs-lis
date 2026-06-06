using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LIS.Masters.Tests.Masters
{
    /// <summary>
    /// Strict revalidation of duplicate/overlap rules requested in QA pass.
    /// </summary>
    [TestClass]
    public class MasterDuplicateRevalidationTests : IntegrationTestBase
    {
        private const string OverlapMessage =
            "An active Test Rate already exists for the selected test and overlapping effective period.";

        private int CreateTest()
        {
            var dept = Services.Department.Get().First();
            var specimen = Services.Specimen.Get().Cast<HISSpecimenMaster>().First();
            return (int)Services.HisTest.Add(MasterTestDataBuilder.HisTest(UniqueCode("TST"), dept.Code, specimen.Code));
        }

        private static TestRateMaster Rate(int testId, DateTime start, DateTime end, int rateType = (int)RateType.Standard,
            int? corporateId = null, int? doctorId = null, int? profileId = null)
        {
            var rate = MasterTestDataBuilder.StandardRate(testId, 100m, start, end);
            rate.RateType = rateType;
            rate.CorporateId = corporateId;
            rate.ReferralDoctorId = doctorId;
            rate.TestProfileId = profileId;
            return rate;
        }

        private static void AssertOverlapBlocked(Action action)
        {
            var ex = Assert.ThrowsException<InvalidOperationException>(action);
            Assert.AreEqual(OverlapMessage, ex.Message);
        }

        [TestMethod]
        public void TestRate_Overlap_JanDec2026_vs_JunAug2026_Blocked()
        {
            var testId = CreateTest();
            var y = 2026;
            var id1 = (int)Services.TestRate.Add(Rate(testId, new DateTime(y, 1, 1), new DateTime(y, 12, 31)));

            AssertOverlapBlocked(() => Services.TestRate.Add(Rate(testId, new DateTime(y, 6, 1), new DateTime(y, 8, 31))));

            Services.TestRate.Delete(new TestRateMaster { Id = id1 });
        }

        [TestMethod]
        public void TestRate_Overlap_JanDec_vs_DecDec_Blocked()
        {
            var testId = CreateTest();
            var y = 2027;
            var id1 = (int)Services.TestRate.Add(Rate(testId, new DateTime(y, 1, 1), new DateTime(y, 12, 31)));

            AssertOverlapBlocked(() => Services.TestRate.Add(Rate(testId, new DateTime(y, 12, 1), new DateTime(y, 12, 31))));

            Services.TestRate.Delete(new TestRateMaster { Id = id1 });
        }

        [TestMethod]
        public void TestRate_Overlap_JanDec_vs_JanJan_Blocked()
        {
            var testId = CreateTest();
            var y = 2028;
            var id1 = (int)Services.TestRate.Add(Rate(testId, new DateTime(y, 1, 1), new DateTime(y, 12, 31)));

            AssertOverlapBlocked(() => Services.TestRate.Add(Rate(testId, new DateTime(y, 1, 1), new DateTime(y, 1, 31))));

            Services.TestRate.Delete(new TestRateMaster { Id = id1 });
        }

        [TestMethod]
        public void TestRate_Adjacent_Q1_and_Q2_Allowed()
        {
            var testId = CreateTest();
            var y = 2029;
            var id1 = (int)Services.TestRate.Add(Rate(testId, new DateTime(y, 1, 1), new DateTime(y, 3, 31)));
            var id2 = (int)Services.TestRate.Add(Rate(testId, new DateTime(y, 4, 1), new DateTime(y, 6, 30)));
            Assert.IsTrue(id2 > 0);

            Services.TestRate.Delete(new TestRateMaster { Id = id1 });
            Services.TestRate.Delete(new TestRateMaster { Id = id2 });
        }

        [TestMethod]
        public void TestRate_Different_Corporate_Allows_Overlap()
        {
            var testId = CreateTest();
            var corp1 = (int)Services.Corporate.Add(MasterTestDataBuilder.Corporate(UniqueCode("C1")));
            var corp2 = (int)Services.Corporate.Add(MasterTestDataBuilder.Corporate(UniqueCode("C2")));
            var y = 2030;
            var start = new DateTime(y, 1, 1);
            var end = new DateTime(y, 12, 31);

            var id1 = (int)Services.TestRate.Add(Rate(testId, start, end, (int)RateType.Corporate, corp1));
            var id2 = (int)Services.TestRate.Add(Rate(testId, start, end, (int)RateType.Corporate, corp2));
            Assert.IsTrue(id2 > 0);

            Services.TestRate.Delete(new TestRateMaster { Id = id1 });
            Services.TestRate.Delete(new TestRateMaster { Id = id2 });
            Services.Corporate.Delete(new CorporateMaster { Id = corp1 });
            Services.Corporate.Delete(new CorporateMaster { Id = corp2 });
        }

        [TestMethod]
        public void TestRate_Different_ReferralDoctor_Allows_Overlap()
        {
            var testId = CreateTest();
            var d1 = (int)Services.ReferralDoctor.Add(MasterTestDataBuilder.ReferralDoctor(UniqueCode("D1")));
            var d2 = (int)Services.ReferralDoctor.Add(MasterTestDataBuilder.ReferralDoctor(UniqueCode("D2")));
            var y = 2031;
            var start = new DateTime(y, 1, 1);
            var end = new DateTime(y, 12, 31);

            var id1 = (int)Services.TestRate.Add(Rate(testId, start, end, (int)RateType.ReferralDoctor, null, d1));
            var id2 = (int)Services.TestRate.Add(Rate(testId, start, end, (int)RateType.ReferralDoctor, null, d2));
            Assert.IsTrue(id2 > 0);

            Services.TestRate.Delete(new TestRateMaster { Id = id1 });
            Services.TestRate.Delete(new TestRateMaster { Id = id2 });
            Services.ReferralDoctor.Delete(new ReferralDoctorMaster { Id = d1 });
            Services.ReferralDoctor.Delete(new ReferralDoctorMaster { Id = d2 });
        }

        [TestMethod]
        public void TestMapping_Duplicate_Equipment_Histest_Lis_Blocked()
        {
            var testId = CreateTest();
            var test = Services.HisTest.GetTestById(testId);
            var equipId = (int)Services.Equipment.Add(new EquipmentMaster
            {
                Name = "EQ " + UniqueCode("EQ"),
                Model = "M",
                AccessKey = UniqueCode("AK"),
                IsActive = true
            });
            var lis = UniqueCode("LIS");

            var mapId = (int)Services.TestMapping.Add(new TestMappingMaster
            {
                HISTestCode = test.HISTestCode,
                LISTestCode = lis,
                EquipmentId = equipId,
                IsActive = true
            });

            var dup = new TestMappingMaster
            {
                HISTestCode = test.HISTestCode,
                LISTestCode = lis,
                EquipmentId = equipId,
                IsActive = true
            };
            var ex = Assert.ThrowsException<InvalidOperationException>(() => Services.TestMapping.Add(dup));
            Assert.AreEqual("Test Mapping already exists.", ex.Message);

            Services.TestMapping.Delete(new TestMappingMaster { Id = mapId });
            Services.Equipment.Delete(new EquipmentMaster { Id = equipId });
        }

        [TestMethod]
        public void TestMapping_Same_Equipment_Histest_Different_Lis_Allowed()
        {
            var testId = CreateTest();
            var test = Services.HisTest.GetTestById(testId);
            var equipId = (int)Services.Equipment.Add(new EquipmentMaster
            {
                Name = "EQ " + UniqueCode("EQ"),
                Model = "M2",
                AccessKey = UniqueCode("AK"),
                IsActive = true
            });

            var id1 = (int)Services.TestMapping.Add(new TestMappingMaster
            {
                HISTestCode = test.HISTestCode,
                LISTestCode = UniqueCode("LIS1"),
                EquipmentId = equipId,
                IsActive = true
            });
            var id2 = (int)Services.TestMapping.Add(new TestMappingMaster
            {
                HISTestCode = test.HISTestCode,
                LISTestCode = UniqueCode("LIS2"),
                EquipmentId = equipId,
                IsActive = true
            });
            Assert.IsTrue(id2 > 0);

            Services.TestMapping.Delete(new TestMappingMaster { Id = id1 });
            Services.TestMapping.Delete(new TestMappingMaster { Id = id2 });
            Services.Equipment.Delete(new EquipmentMaster { Id = equipId });
        }

        [TestMethod]
        public void Specimen_Lookup_Includes_SERUM()
        {
            var list = Services.Specimen.Get(new ListOptions
            {
                RecordPerPage = 500,
                CurrentPage = 1,
                SortColumnName = "Name",
                SortDirection = true
            });

            Assert.IsTrue(list.Items.Any(s =>
                s.IsActive &&
                string.Equals(s.Code, "SERUM", StringComparison.OrdinalIgnoreCase)));
        }

        [TestMethod]
        public void Patient_Duplicate_Name_Phone_Blocked_Different_Phone_Allowed()
        {
            var suffix = UniqueCode("P");
            var name = "Reval Patient " + suffix;
            var p1 = MasterTestDataBuilder.Patient(suffix);
            p1.Name = name;
            p1.Phone = "9000111222";
            var id1 = Services.PatientMaster.Add(p1);

            var dup = MasterTestDataBuilder.Patient("x");
            dup.Name = name;
            dup.Phone = "900-011-1222";
            Assert.ThrowsException<InvalidOperationException>(() => Services.PatientMaster.Add(dup));

            var unique = MasterTestDataBuilder.Patient("y");
            unique.Name = name;
            unique.Phone = "9000111333";
            var id2 = Services.PatientMaster.Add(unique);
            Assert.IsTrue(id2 > 0);

            Services.PatientMaster.Delete(new PatientDetail { Id = id1 });
            Services.PatientMaster.Delete(new PatientDetail { Id = id2 });
        }
    }
}
