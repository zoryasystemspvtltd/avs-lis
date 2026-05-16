using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LIS.Masters.Tests.Validation
{
    [TestClass]
    public class NegativeValidationTests : IntegrationTestBase
    {
        [TestMethod]
        public void TestRate_GetEffective_InvalidTestId_Returns_Null()
        {
            var effective = Services.TestRate.GetEffectiveRate(-1, (int)RateType.Standard, null, null, null);
            Assert.IsNull(effective);
        }

        [TestMethod]
        public void SaleInvoice_Save_Null_Dto_Throws()
        {
            Assert.ThrowsException<ArgumentException>(() => Services.SaleInvoice.Save(null));
        }

        [TestMethod]
        public void SaleInvoice_GetById_InvalidId_Returns_Null()
        {
            var result = Services.SaleInvoice.GetById(-99999);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ReferralDoctor_GetById_Invalid_Returns_Null()
        {
            Assert.IsNull(Services.ReferralDoctor.GetById(-1));
        }

        [TestMethod]
        public void HisTest_GetTestById_Invalid_Returns_Null()
        {
            Assert.IsNull(Services.HisTest.GetTestById(-1));
        }

        [TestMethod]
        public void TestRate_List_Null_Options_Returns_Null()
        {
            Assert.IsNull(Services.TestRate.Get(null));
        }

        [TestMethod]
        public void Department_Get_InvalidCode_Returns_Null()
        {
            Assert.IsNull(Services.Department.Get("___INVALID___"));
        }
    }
}
