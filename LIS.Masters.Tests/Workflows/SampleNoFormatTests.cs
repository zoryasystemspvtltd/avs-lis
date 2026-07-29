using LIS.BusinessLogic.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LIS.Masters.Tests.Workflows
{
    [TestClass]
    public class SampleNoFormatTests
    {
        [TestMethod]
        public void Normalize_Removes_Inv_Prefix_And_Hyphens()
        {
            Assert.AreEqual("202607290001", Helper.NormalizeSampleNoBase("INV-20260729-0001"));
            Assert.AreEqual("202607290125", Helper.NormalizeSampleNoBase("INV-20260729-0125"));
        }

        [TestMethod]
        public void BuildSampleNo_Appends_Specimen_Without_Hyphen()
        {
            Assert.AreEqual("202607290001SERUM", Helper.BuildSampleNo("INV-20260729-0001", "SERUM"));
            Assert.AreEqual("202607290125EDTA", Helper.BuildSampleNo("INV-20260729-0125", "EDTA"));
        }

        [TestMethod]
        public void BuildSampleNo_Contains_No_Inv_And_No_Hyphen()
        {
            var sampleNo = Helper.BuildSampleNo("INV-20260729-0001", "SERUM");
            Assert.IsFalse(sampleNo.Contains("INV"));
            Assert.IsFalse(sampleNo.Contains("-"));
            Assert.IsTrue(sampleNo.EndsWith("SERUM"));
            Assert.IsTrue(sampleNo.Length < 20);
        }

        [TestMethod]
        public void BuildSampleNo_Handles_Fallback_InvId_Format()
        {
            Assert.AreEqual("123SERUM", Helper.BuildSampleNo("INV123", "SERUM"));
        }

        [TestMethod]
        public void Normalize_Empty_Returns_Empty()
        {
            Assert.AreEqual(string.Empty, Helper.NormalizeSampleNoBase(null));
            Assert.AreEqual(string.Empty, Helper.NormalizeSampleNoBase(""));
            Assert.AreEqual(string.Empty, Helper.NormalizeSampleNoBase("   "));
        }
    }
}
