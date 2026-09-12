using System.Linq;
using Lis.Api.Providers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LIS.Masters.Tests.Security
{
    [TestClass]
    public class ReportTemplateConfigurationRbacTests
    {
        [TestMethod]
        public void MenuCatalog_ContainsReportTemplateConfiguration()
        {
            var item = MenuCatalog.Find("SETUP_REPORT_TEMPLATE_CONFIGURATION");
            Assert.IsNotNull(item);
            Assert.AreEqual("ReportTemplateConfiguration", item.ModuleName);
            Assert.AreEqual("Masters", item.Section);
            Assert.AreEqual("/report-template-configuration", item.Route);
            Assert.AreEqual(57, item.Order);
        }

        [TestMethod]
        public void MenuCatalog_LegacyKey_Normalizes()
        {
            Assert.AreEqual(
                "SETUP_REPORT_TEMPLATE_CONFIGURATION",
                MenuCatalog.NormalizeKey("setup.reportTemplateConfiguration"));
        }

        [TestMethod]
        public void MenuCatalog_NoDuplicateTemplateKeys()
        {
            var count = MenuCatalog.All.Count(m =>
                m.MenuKey == "SETUP_REPORT_TEMPLATE_CONFIGURATION"
                || m.Route == "/report-template-configuration");
            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public void MenuCatalog_UsesDedicatedModule_NotMasters()
        {
            var item = MenuCatalog.Find("SETUP_REPORT_TEMPLATE_CONFIGURATION");
            Assert.AreEqual("ReportTemplateConfiguration", item.ModuleName);
            Assert.AreNotEqual("Masters", item.ModuleName);
        }
    }
}
