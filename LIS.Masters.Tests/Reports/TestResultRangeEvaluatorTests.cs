using LIS.BusinessLogic;
using LIS.DtoModel.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace LIS.Masters.Tests.Reports
{
    [TestClass]
    public class TestResultRangeEvaluatorTests
    {
        [TestMethod]
        public void Both_Gender_Range_Matches_Male_Patient_And_Prints_Applicable_Interval_Only()
        {
            var patient = new PatientDetail { Gender = "Male", Age = 35 };
            var param = new HISParameterMaster { Id = 10, HISParamCode = "HB" };
            var ranges = new List<HISParameterRangMaster>
            {
                new HISParameterRangMaster
                {
                    Id = 1,
                    HisParameterId = 10,
                    Gender = "Both",
                    AgeFrom = 18,
                    AgeTo = 100,
                    AgeType = "Year",
                    MinValue = 35m,
                    MaxValue = 45m,
                    HISRangeValue = "35.00 - 45.00"
                }
            };

            TestResultRangeEvaluator.Apply("40", param, patient, ranges,
                out var referenceRange, out var flag, out var isAbnormal);

            Assert.AreEqual("35.00 - 45.00", referenceRange);
            Assert.IsFalse(referenceRange.IndexOf("Both", System.StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.IsFalse(referenceRange.IndexOf("Year", System.StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.AreEqual(string.Empty, flag);
            Assert.IsFalse(isAbnormal);
        }

        [TestMethod]
        public void Both_Gender_Range_Matches_Female_Patient()
        {
            var patient = new PatientDetail { Gender = "Female", Age = 28 };
            var param = new HISParameterMaster { Id = 11 };
            var ranges = new List<HISParameterRangMaster>
            {
                new HISParameterRangMaster
                {
                    Id = 2,
                    HisParameterId = 11,
                    Gender = "Both",
                    AgeFrom = 18,
                    AgeTo = 100,
                    MinValue = 12m,
                    MaxValue = 15m,
                    HISRangeValue = "12.00 - 15.00"
                }
            };

            TestResultRangeEvaluator.Apply("13.5", param, patient, ranges,
                out var referenceRange, out _, out _);

            Assert.AreEqual("12.00 - 15.00", referenceRange);
        }

        [TestMethod]
        public void Gender_Specific_Range_Selected_Over_Other_Gender()
        {
            var patient = new PatientDetail { Gender = "Male", Age = 40 };
            var param = new HISParameterMaster { Id = 12 };
            var ranges = new List<HISParameterRangMaster>
            {
                new HISParameterRangMaster
                {
                    Id = 3,
                    HisParameterId = 12,
                    Gender = "Female",
                    AgeFrom = 18,
                    AgeTo = 100,
                    MinValue = 12m,
                    MaxValue = 15m,
                    HISRangeValue = "12 - 15"
                },
                new HISParameterRangMaster
                {
                    Id = 4,
                    HisParameterId = 12,
                    Gender = "Male",
                    AgeFrom = 18,
                    AgeTo = 100,
                    MinValue = 13m,
                    MaxValue = 17m,
                    HISRangeValue = "13.00 - 17.00"
                }
            };

            TestResultRangeEvaluator.Apply("14", param, patient, ranges,
                out var referenceRange, out _, out _);

            Assert.AreEqual("13.00 - 17.00", referenceRange);
        }

        [TestMethod]
        public void High_Result_Sets_H_Flag()
        {
            var patient = new PatientDetail { Gender = "Both", Age = 30 };
            var param = new HISParameterMaster { Id = 13 };
            var ranges = new List<HISParameterRangMaster>
            {
                new HISParameterRangMaster
                {
                    Id = 5,
                    HisParameterId = 13,
                    Gender = "Both",
                    AgeFrom = 0,
                    AgeTo = 120,
                    MinValue = 10m,
                    MaxValue = 20m,
                    HISRangeValue = "10 - 20"
                }
            };

            TestResultRangeEvaluator.Apply("25", param, patient, ranges,
                out var referenceRange, out var flag, out var isAbnormal);

            Assert.AreEqual("10 - 20", referenceRange);
            Assert.AreEqual("H", flag);
            Assert.IsTrue(isAbnormal);
        }

        [TestMethod]
        public void Falls_Back_To_MinMax_When_RangeValue_Empty()
        {
            var patient = new PatientDetail { Gender = "Male", Age = 50 };
            var param = new HISParameterMaster { Id = 14 };
            var ranges = new List<HISParameterRangMaster>
            {
                new HISParameterRangMaster
                {
                    Id = 6,
                    HisParameterId = 14,
                    Gender = "Both",
                    AgeFrom = 18,
                    AgeTo = 100,
                    MinValue = 35m,
                    MaxValue = 45m,
                    HISRangeValue = null
                }
            };

            TestResultRangeEvaluator.Apply("40", param, patient, ranges,
                out var referenceRange, out _, out _);

            Assert.AreEqual("35 - 45", referenceRange);
        }
    }
}
