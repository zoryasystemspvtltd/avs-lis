using LIS.BusinessLogic;
using LIS.DtoModel.Models.Reports;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LIS.Masters.Tests.Reports
{
    [TestClass]
    public class ReportLayoutConfigurationManagerTests : IntegrationTestBase
    {
        private ReportLayoutConfigurationManager CreateManager()
        {
            return new ReportLayoutConfigurationManager(Services.Logger, Services.Identity, Services.Uow);
        }

        [TestMethod]
        public void Defaults_Diagnostic_Match_Production_Clearance()
        {
            var defaults = CreateManager().GetDefaults(ReportLayoutReportTypes.Diagnostic);
            Assert.AreEqual(ReportLayoutReportTypes.Diagnostic, defaults.ReportType);
            Assert.AreEqual(50m, defaults.HeaderHeightMm);
            Assert.AreEqual(50m, defaults.FooterHeightMm);
            Assert.AreEqual(12m, defaults.LeftMarginMm);
            Assert.AreEqual(12m, defaults.RightMarginMm);
            Assert.IsTrue(defaults.DoctorSignatureEnabled);
            Assert.AreEqual("Right", defaults.DoctorSignatureHorizontal);
            Assert.AreEqual(50m, defaults.DoctorSignatureWidthMm);
            Assert.AreEqual(14m, defaults.DoctorSignatureHeightMm);
            Assert.IsFalse(defaults.TechnicianSignatureEnabled);
            Assert.AreEqual("A4", defaults.PageSize);
            Assert.AreEqual("Portrait", defaults.Orientation);
        }

        [TestMethod]
        public void TechnicianSignature_Can_Enable_Persist_And_Reset_Diagnostic()
        {
            var manager = CreateManager();
            manager.Save(new ReportLayoutConfigurationDto
            {
                ReportType = "Diagnostic",
                HeaderHeightMm = 50m,
                FooterHeightMm = 50m,
                LeftMarginMm = 12m,
                RightMarginMm = 12m,
                DoctorSignatureEnabled = true,
                DoctorSignatureHorizontal = "Right",
                DoctorSignatureWidthMm = 50m,
                DoctorSignatureHeightMm = 14m,
                TechnicianSignatureEnabled = true,
                TechnicianSignatureHorizontal = "Left",
                TechnicianSignatureWidthMm = 40m,
                TechnicianSignatureHeightMm = 12m
            });

            var loaded = manager.GetByReportType("Diagnostic");
            Assert.IsTrue(loaded.TechnicianSignatureEnabled);
            Assert.AreEqual("Left", loaded.TechnicianSignatureHorizontal);
            Assert.AreEqual(40m, loaded.TechnicianSignatureWidthMm);
            Assert.AreEqual(12m, loaded.TechnicianSignatureHeightMm);

            var reset = manager.ResetToDefault("Diagnostic");
            Assert.IsFalse(reset.TechnicianSignatureEnabled, "Default remains OFF so existing reports do not suddenly show technician signatures.");
            Assert.AreEqual(50m, reset.TechnicianSignatureWidthMm);
            Assert.AreEqual(14m, reset.TechnicianSignatureHeightMm);
        }

        [TestMethod]
        public void TechnicianSignature_Diagnostic_And_Radiology_Remain_Independent()
        {
            var manager = CreateManager();
            manager.Save(new ReportLayoutConfigurationDto
            {
                ReportType = "Diagnostic",
                HeaderHeightMm = 50m,
                FooterHeightMm = 50m,
                LeftMarginMm = 12m,
                RightMarginMm = 12m,
                DoctorSignatureEnabled = true,
                DoctorSignatureHorizontal = "Right",
                DoctorSignatureWidthMm = 50m,
                DoctorSignatureHeightMm = 14m,
                TechnicianSignatureEnabled = true,
                TechnicianSignatureHorizontal = "Right",
                TechnicianSignatureWidthMm = 45m,
                TechnicianSignatureHeightMm = 15m
            });
            manager.Save(new ReportLayoutConfigurationDto
            {
                ReportType = "Radiology",
                HeaderHeightMm = 40m,
                FooterHeightMm = 50m,
                LeftMarginMm = 10m,
                RightMarginMm = 10m,
                DoctorSignatureEnabled = true,
                DoctorSignatureHorizontal = "Left",
                DoctorSignatureWidthMm = 50m,
                DoctorSignatureHeightMm = 14m,
                TechnicianSignatureEnabled = false,
                TechnicianSignatureHorizontal = "Left",
                TechnicianSignatureWidthMm = 50m,
                TechnicianSignatureHeightMm = 14m
            });

            var diagnostic = manager.GetByReportType("Diagnostic");
            var radiology = manager.GetByReportType("Radiology");
            Assert.IsTrue(diagnostic.TechnicianSignatureEnabled);
            Assert.AreEqual("Right", diagnostic.TechnicianSignatureHorizontal);
            Assert.IsFalse(radiology.TechnicianSignatureEnabled);

            manager.ResetToDefault("Diagnostic");
            manager.ResetToDefault("Radiology");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Validation_Rejects_Invalid_Technician_Signature_Size_When_Enabled()
        {
            CreateManager().Save(new ReportLayoutConfigurationDto
            {
                ReportType = "Diagnostic",
                HeaderHeightMm = 50m,
                FooterHeightMm = 50m,
                LeftMarginMm = 12m,
                RightMarginMm = 12m,
                DoctorSignatureEnabled = true,
                DoctorSignatureWidthMm = 50m,
                DoctorSignatureHeightMm = 14m,
                TechnicianSignatureEnabled = true,
                TechnicianSignatureWidthMm = 1m,
                TechnicianSignatureHeightMm = 14m
            });
        }

        [TestMethod]
        public void Defaults_Radiology_Match_Production_Clearance()
        {
            var defaults = CreateManager().GetDefaults(ReportLayoutReportTypes.Radiology);
            Assert.AreEqual(ReportLayoutReportTypes.Radiology, defaults.ReportType);
            Assert.AreEqual(40m, defaults.HeaderHeightMm);
            Assert.AreEqual(50m, defaults.FooterHeightMm);
            Assert.AreEqual(10m, defaults.LeftMarginMm);
            Assert.AreEqual(10m, defaults.RightMarginMm);
            Assert.AreEqual("Left", defaults.DoctorSignatureHorizontal);
            Assert.IsFalse(defaults.TechnicianSignatureEnabled);
        }

        [TestMethod]
        public void GetByReportType_Returns_Active_Or_Defaults()
        {
            var manager = CreateManager();
            var diagnostic = manager.GetByReportType("Diagnostic");
            var radiology = manager.GetByReportType("Radiology");
            Assert.IsNotNull(diagnostic);
            Assert.IsNotNull(radiology);
            Assert.AreEqual("Diagnostic", diagnostic.ReportType);
            Assert.AreEqual("Radiology", radiology.ReportType);
        }

        [TestMethod]
        public void Save_Update_And_Reset_Diagnostic()
        {
            var manager = CreateManager();
            var before = manager.GetByReportType("Diagnostic");
            var originalHeader = before.HeaderHeightMm;

            var updated = manager.Save(new ReportLayoutConfigurationDto
            {
                ReportType = "Diagnostic",
                PageSize = "A4",
                Orientation = "Portrait",
                HeaderHeightMm = 55m,
                FooterHeightMm = 45m,
                LeftMarginMm = 12m,
                RightMarginMm = 12m,
                DoctorSignatureEnabled = true,
                DoctorSignatureHorizontal = "Center",
                DoctorSignatureVertical = "Bottom",
                DoctorSignatureWidthMm = 40m,
                DoctorSignatureHeightMm = 12m,
                TechnicianSignatureEnabled = true,
                TechnicianSignatureHorizontal = "Left",
                TechnicianSignatureVertical = "Bottom",
                TechnicianSignatureWidthMm = 50m,
                TechnicianSignatureHeightMm = 14m,
                IsActive = true
            });

            Assert.AreEqual(55m, updated.HeaderHeightMm);
            Assert.AreEqual(45m, updated.FooterHeightMm);
            Assert.AreEqual("Center", updated.DoctorSignatureHorizontal);
            Assert.IsTrue(updated.TechnicianSignatureEnabled, "Technician signature must persist when enabled.");
            Assert.AreEqual("Left", updated.TechnicianSignatureHorizontal);

            var loaded = manager.GetByReportType("Diagnostic");
            Assert.AreEqual(55m, loaded.HeaderHeightMm);

            var reset = manager.ResetToDefault("Diagnostic");
            Assert.AreEqual(50m, reset.HeaderHeightMm);
            Assert.AreEqual(50m, reset.FooterHeightMm);
            Assert.AreEqual("Right", reset.DoctorSignatureHorizontal);

            // Restore whatever was there before the test if it differed from defaults after reset.
            if (originalHeader != 50m)
            {
                before.Id = 0;
                manager.Save(before);
            }
        }

        [TestMethod]
        public void Save_Update_And_Reset_Radiology()
        {
            var manager = CreateManager();
            manager.Save(new ReportLayoutConfigurationDto
            {
                ReportType = "Radiology",
                HeaderHeightMm = 42m,
                FooterHeightMm = 48m,
                LeftMarginMm = 10m,
                RightMarginMm = 10m,
                DoctorSignatureEnabled = true,
                DoctorSignatureHorizontal = "Right",
                DoctorSignatureWidthMm = 50m,
                DoctorSignatureHeightMm = 14m
            });

            var loaded = manager.GetByReportType("Radiology");
            Assert.AreEqual(42m, loaded.HeaderHeightMm);
            Assert.AreEqual("Right", loaded.DoctorSignatureHorizontal);

            var reset = manager.ResetToDefault("Radiology");
            Assert.AreEqual(40m, reset.HeaderHeightMm);
            Assert.AreEqual(50m, reset.FooterHeightMm);
            Assert.AreEqual("Left", reset.DoctorSignatureHorizontal);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Validation_Rejects_Clearance_Above_Maximum()
        {
            CreateManager().Save(new ReportLayoutConfigurationDto
            {
                ReportType = "Diagnostic",
                HeaderHeightMm = 130m,
                FooterHeightMm = 50m,
                LeftMarginMm = 12m,
                RightMarginMm = 12m,
                DoctorSignatureEnabled = true,
                DoctorSignatureWidthMm = 50m,
                DoctorSignatureHeightMm = 14m
            });
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Validation_Rejects_Insufficient_Content_Height()
        {
            CreateManager().Save(new ReportLayoutConfigurationDto
            {
                ReportType = "Diagnostic",
                HeaderHeightMm = 120m,
                FooterHeightMm = 120m,
                LeftMarginMm = 12m,
                RightMarginMm = 12m,
                DoctorSignatureEnabled = true,
                DoctorSignatureWidthMm = 50m,
                DoctorSignatureHeightMm = 14m
            });
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Validation_Rejects_Insufficient_Content_Width()
        {
            CreateManager().Save(new ReportLayoutConfigurationDto
            {
                ReportType = "Diagnostic",
                HeaderHeightMm = 50m,
                FooterHeightMm = 50m,
                LeftMarginMm = 80m,
                RightMarginMm = 80m,
                DoctorSignatureEnabled = true,
                DoctorSignatureWidthMm = 50m,
                DoctorSignatureHeightMm = 14m
            });
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Validation_Rejects_Negative_Margin()
        {
            CreateManager().Save(new ReportLayoutConfigurationDto
            {
                ReportType = "Diagnostic",
                HeaderHeightMm = 50m,
                FooterHeightMm = 50m,
                LeftMarginMm = -1m,
                RightMarginMm = 12m,
                DoctorSignatureEnabled = true,
                DoctorSignatureWidthMm = 50m,
                DoctorSignatureHeightMm = 14m
            });
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Validation_Rejects_Invalid_Signature_Size_When_Enabled()
        {
            CreateManager().Save(new ReportLayoutConfigurationDto
            {
                ReportType = "Diagnostic",
                HeaderHeightMm = 50m,
                FooterHeightMm = 50m,
                LeftMarginMm = 12m,
                RightMarginMm = 12m,
                DoctorSignatureEnabled = true,
                DoctorSignatureWidthMm = 1m,
                DoctorSignatureHeightMm = 14m
            });
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Validation_Rejects_Unknown_ReportType()
        {
            CreateManager().GetDefaults("Billing");
        }

        [TestMethod]
        public void DoctorSignature_Show_Position_Size_Persist_And_Reload_Diagnostic()
        {
            var manager = CreateManager();
            manager.Save(new ReportLayoutConfigurationDto
            {
                ReportType = "Diagnostic",
                HeaderHeightMm = 50m,
                FooterHeightMm = 50m,
                LeftMarginMm = 12m,
                RightMarginMm = 12m,
                DoctorSignatureEnabled = false,
                DoctorSignatureHorizontal = "Left",
                DoctorSignatureWidthMm = 35m,
                DoctorSignatureHeightMm = 18m
            });

            var loaded = manager.GetByReportType("Diagnostic");
            Assert.IsFalse(loaded.DoctorSignatureEnabled);
            Assert.AreEqual("Left", loaded.DoctorSignatureHorizontal);
            Assert.AreEqual(35m, loaded.DoctorSignatureWidthMm);
            Assert.AreEqual(18m, loaded.DoctorSignatureHeightMm);

            manager.ResetToDefault("Diagnostic");
            var reset = manager.GetByReportType("Diagnostic");
            Assert.IsTrue(reset.DoctorSignatureEnabled);
            Assert.AreEqual("Right", reset.DoctorSignatureHorizontal);
            Assert.AreEqual(50m, reset.DoctorSignatureWidthMm);
            Assert.AreEqual(14m, reset.DoctorSignatureHeightMm);
        }

        [TestMethod]
        public void DoctorSignature_Diagnostic_And_Radiology_Remain_Independent()
        {
            var manager = CreateManager();
            manager.Save(new ReportLayoutConfigurationDto
            {
                ReportType = "Diagnostic",
                HeaderHeightMm = 50m,
                FooterHeightMm = 50m,
                LeftMarginMm = 12m,
                RightMarginMm = 12m,
                DoctorSignatureEnabled = true,
                DoctorSignatureHorizontal = "Center",
                DoctorSignatureWidthMm = 45m,
                DoctorSignatureHeightMm = 16m
            });
            manager.Save(new ReportLayoutConfigurationDto
            {
                ReportType = "Radiology",
                HeaderHeightMm = 40m,
                FooterHeightMm = 50m,
                LeftMarginMm = 10m,
                RightMarginMm = 10m,
                DoctorSignatureEnabled = true,
                DoctorSignatureHorizontal = "Right",
                DoctorSignatureWidthMm = 30m,
                DoctorSignatureHeightMm = 12m
            });

            var diagnostic = manager.GetByReportType("Diagnostic");
            var radiology = manager.GetByReportType("Radiology");
            Assert.AreEqual("Center", diagnostic.DoctorSignatureHorizontal);
            Assert.AreEqual(45m, diagnostic.DoctorSignatureWidthMm);
            Assert.AreEqual("Right", radiology.DoctorSignatureHorizontal);
            Assert.AreEqual(30m, radiology.DoctorSignatureWidthMm);

            manager.ResetToDefault("Diagnostic");
            manager.ResetToDefault("Radiology");
        }
    }
}
