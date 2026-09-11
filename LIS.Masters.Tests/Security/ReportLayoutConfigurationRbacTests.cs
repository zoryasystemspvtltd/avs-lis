using System.Linq;
using Lis.Api.Providers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LIS.Masters.Tests.Security
{
    /// <summary>
    /// Catalog/RBAC registration checks for Report Layout Configuration menu.
    /// Does not modify layout behaviour — verifies MenuCatalog wiring only.
    /// </summary>
    [TestClass]
    public class ReportLayoutConfigurationRbacTests
    {
        [TestMethod]
        public void MenuCatalog_ContainsReportLayoutConfiguration_UnderMastersSection()
        {
            var item = MenuCatalog.Find("SETUP_REPORT_LAYOUT_CONFIGURATION");
            Assert.IsNotNull(item);
            Assert.AreEqual("ReportLayoutConfiguration", item.ModuleName);
            Assert.AreEqual("Masters", item.Section); // Setup group in left-nav / Role Edit Masters section
            Assert.AreEqual("Report Layout Configuration", item.Label);
            Assert.AreEqual("/report-layout-configuration", item.Route);
            Assert.AreEqual(56, item.Order);
        }

        [TestMethod]
        public void MenuCatalog_LegacyKey_NormalizesToEnterpriseKey()
        {
            Assert.AreEqual(
                "SETUP_REPORT_LAYOUT_CONFIGURATION",
                MenuCatalog.NormalizeKey("setup.reportLayoutConfiguration"));
            Assert.IsTrue(MenuCatalog.IsKnown("setup.reportLayoutConfiguration"));
            Assert.IsTrue(MenuCatalog.IsKnown("SETUP_REPORT_LAYOUT_CONFIGURATION"));
        }

        [TestMethod]
        public void MenuCatalog_NoDuplicateReportLayoutKeys()
        {
            var count = MenuCatalog.All.Count(m =>
                m.MenuKey == "SETUP_REPORT_LAYOUT_CONFIGURATION"
                || m.Route == "/report-layout-configuration");
            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public void MenuCatalog_ReportLayout_UsesDedicatedModule_NotMasters()
        {
            var item = MenuCatalog.Find("SETUP_REPORT_LAYOUT_CONFIGURATION");
            Assert.IsNotNull(item);
            Assert.AreEqual("ReportLayoutConfiguration", item.ModuleName);
            Assert.AreNotEqual("Masters", item.ModuleName);
        }

        [TestMethod]
        public void MenuCatalog_NotificationAndReportLayout_AreAdjacentSetupOrders()
        {
            var notification = MenuCatalog.Find("SETUP_NOTIFICATION_CONFIGURATION");
            var layout = MenuCatalog.Find("SETUP_REPORT_LAYOUT_CONFIGURATION");
            Assert.IsNotNull(notification);
            Assert.IsNotNull(layout);
            Assert.AreEqual(55, notification.Order);
            Assert.AreEqual(56, layout.Order);
            Assert.AreEqual("/notification-configuration", notification.Route);
            Assert.AreEqual("/report-layout-configuration", layout.Route);
        }
    }
}
