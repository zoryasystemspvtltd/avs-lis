using LIS.DtoModel.Models;
using System;

namespace LIS.Masters.Tests.Infrastructure
{
    internal static class MasterTestDataBuilder
    {
        public static ReferralDoctorMaster ReferralDoctor(string code, bool active = true)
        {
            return new ReferralDoctorMaster
            {
                Code = code,
                Name = $"Doctor {code}",
                Phone = "9999999999",
                IsActive = active
            };
        }

        public static CorporateMaster Corporate(string code, bool active = true)
        {
            return new CorporateMaster
            {
                Code = code,
                Name = $"Corp {code}",
                DefaultDiscountPercent = 5,
                IsActive = active
            };
        }

        public static UnitMaster Unit(string code, bool active = true)
        {
            return new UnitMaster { Code = code, Name = $"Unit {code}", IsActive = active };
        }

        public static HisTestMaster HisTest(string code, string deptCode, string specimenCode, bool active = true)
        {
            return new HisTestMaster
            {
                HISTestCode = code,
                HISTestCodeDescription = $"Test {code}",
                DepartmentCode = deptCode,
                HISSpecimenCode = specimenCode,
                HISSpecimenName = specimenCode,
                IsActive = active,
                CreatedOn = DateTime.Now
            };
        }

        public static TestRateMaster StandardRate(int testId, decimal rate, DateTime? start = null, DateTime? end = null)
        {
            var s = start ?? DateTime.Today.AddDays(-30);
            var e = end ?? DateTime.Today.AddDays(365);
            return new TestRateMaster
            {
                TestId = testId,
                RateType = (int)RateType.Standard,
                Rate = rate,
                EmergencyRate = rate * 1.5m,
                DiscountPercent = 0,
                TaxPercent = 5,
                EffectiveStart = s,
                EffectiveEnd = e,
                IsActive = true
            };
        }

        public static TestProfileMaster Profile(string code, params int[] testIds)
        {
            var profile = new TestProfileMaster
            {
                Code = code,
                Name = $"Profile {code}",
                PackageRate = 0,
                IsActive = true,
                ProfileDetails = new System.Collections.Generic.List<TestProfileDetail>()
            };

            foreach (var testId in testIds)
            {
                profile.ProfileDetails.Add(new TestProfileDetail { TestId = testId, Quantity = 1 });
            }

            return profile;
        }
    }
}
