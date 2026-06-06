using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace LIS.Masters.Tests.Transactions
{
    [TestClass]
    public class SaleInvoicePatientValidationTests : IntegrationTestBase
    {
        [TestMethod]
        public void Save_Without_Patient_Throws()
        {
            var dto = new SaleInvoiceDto
            {
                Invoice = new SaleInvoice
                {
                    InvoiceNo = UniqueCode("INV"),
                    InvoiceDate = DateTime.Today,
                    PatientId = 0,
                    InvoiceStatus = (int)InvoiceStatusType.Draft
                },
                Details = new List<SaleInvoiceDetail>
                {
                    new SaleInvoiceDetail { TestId = 1, Quantity = 1, Rate = 100m }
                }
            };

            try
            {
                Services.SaleInvoice.Save(dto);
                Assert.Fail("Expected patient validation failure");
            }
            catch (ArgumentException ex)
            {
                Assert.IsTrue(ex.Message.IndexOf("Patient", StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }
    }
}
