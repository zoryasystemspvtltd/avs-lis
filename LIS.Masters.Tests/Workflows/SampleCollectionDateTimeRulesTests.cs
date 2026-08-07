using LIS.BusinessLogic.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Configuration;

namespace LIS.Masters.Tests.Workflows
{
    [TestClass]
    public class SampleCollectionDateTimeRulesTests
    {
        [TestInitialize]
        public void SetUp()
        {
            SetAppSetting(OperationalDateTime.FacilityTimeZoneIdKey, "India Standard Time");
            SetAppSetting(OperationalDateTime.AllowedClockDriftMinutesKey, "5");
        }

        [TestMethod]
        public void Current_Timestamp_Within_Drift_Passes()
        {
            var now = new DateTime(2026, 8, 7, 7, 25, 0);
            var invoice = new DateTime(2026, 8, 7, 7, 22, 0);

            SampleCollectionDateRules.Validate(now, invoice, now, 5);
        }

        [TestMethod]
        public void Collection_Three_Minutes_After_Invoice_Passes()
        {
            var invoice = new DateTime(2026, 8, 7, 7, 22, 0);
            var collection = new DateTime(2026, 8, 7, 7, 25, 0);
            var facilityNow = new DateTime(2026, 8, 7, 7, 25, 0);

            SampleCollectionDateRules.Validate(collection, invoice, facilityNow, 5);
        }

        [TestMethod]
        public void Collection_Before_Invoice_Fails()
        {
            var invoice = new DateTime(2026, 8, 7, 7, 22, 0);
            var collection = new DateTime(2026, 8, 6, 11, 0, 0);
            var facilityNow = new DateTime(2026, 8, 7, 7, 25, 0);

            var ex = Assert.ThrowsException<ArgumentException>(() =>
                SampleCollectionDateRules.Validate(collection, invoice, facilityNow, 5));
            Assert.AreEqual(SampleCollectionDateRules.BeforeInvoiceMessage, ex.Message);
        }

        [TestMethod]
        public void Genuine_Future_Beyond_Drift_Fails()
        {
            var invoice = new DateTime(2026, 8, 7, 7, 22, 0);
            var facilityNow = new DateTime(2026, 8, 7, 7, 25, 0);
            var tomorrow = facilityNow.AddDays(1);

            var ex = Assert.ThrowsException<ArgumentException>(() =>
                SampleCollectionDateRules.Validate(tomorrow, invoice, facilityNow, 5));
            Assert.AreEqual(SampleCollectionDateRules.FutureMessage, ex.Message);
        }

        [TestMethod]
        public void Collection_Within_Configured_Clock_Drift_Passes()
        {
            var invoice = new DateTime(2026, 8, 7, 7, 22, 0);
            var facilityNow = new DateTime(2026, 8, 7, 7, 25, 0);
            var collection = facilityNow.AddMinutes(4);

            SampleCollectionDateRules.Validate(collection, invoice, facilityNow, 5);
        }

        [TestMethod]
        public void Collection_Beyond_Configured_Clock_Drift_Fails()
        {
            var invoice = new DateTime(2026, 8, 7, 7, 22, 0);
            var facilityNow = new DateTime(2026, 8, 7, 7, 25, 0);
            var collection = facilityNow.AddMinutes(6);

            var ex = Assert.ThrowsException<ArgumentException>(() =>
                SampleCollectionDateRules.Validate(collection, invoice, facilityNow, 5));
            Assert.AreEqual(SampleCollectionDateRules.FutureMessage, ex.Message);
        }

        [TestMethod]
        public void Utc_Wire_Value_Is_Converted_To_Facility_Wall_Clock()
        {
            // 07:25 IST = 01:55 UTC
            var utcWire = new DateTime(2026, 8, 7, 1, 55, 0, DateTimeKind.Utc);
            var wall = OperationalDateTime.ToFacilityWallClock(utcWire);

            Assert.AreEqual(DateTimeKind.Unspecified, wall.Kind);
            Assert.AreEqual(new DateTime(2026, 8, 7, 7, 25, 0), wall);
        }

        [TestMethod]
        public void Unspecified_Local_Serialization_Remains_Wall_Clock()
        {
            var unspecified = new DateTime(2026, 8, 7, 7, 25, 0, DateTimeKind.Unspecified);
            var wall = OperationalDateTime.ToFacilityWallClock(unspecified);

            Assert.AreEqual(DateTimeKind.Unspecified, wall.Kind);
            Assert.AreEqual(unspecified, wall);
        }

        [TestMethod]
        public void False_Future_Against_Utc_Server_Clock_Is_Avoided_When_Using_Facility_Now()
        {
            // Regression of observed production failure mode:
            // client wall-clock 07:25 (Unspecified) vs server DateTime.Now = 01:55 (UTC host).
            var collection = new DateTime(2026, 8, 7, 7, 25, 0, DateTimeKind.Unspecified);
            var invoice = new DateTime(2026, 8, 7, 7, 22, 0, DateTimeKind.Unspecified);
            var facilityNow = new DateTime(2026, 8, 7, 7, 25, 0, DateTimeKind.Unspecified);
            var utcHostNowMistakenAsNow = new DateTime(2026, 8, 7, 1, 55, 0);

            Assert.IsTrue(collection > utcHostNowMistakenAsNow.AddMinutes(1),
                "Precondition: naive server-Now comparison would falsely reject.");

            SampleCollectionDateRules.Validate(collection, invoice, facilityNow, 5);
        }

        [TestMethod]
        public void AllowedClockDrift_Reads_From_Config()
        {
            SetAppSetting(OperationalDateTime.AllowedClockDriftMinutesKey, "7");
            Assert.AreEqual(7, OperationalDateTime.GetAllowedClockDriftMinutes());
        }

        [TestMethod]
        public void Past_Timestamp_After_Invoice_Passes()
        {
            var invoice = new DateTime(2026, 8, 7, 7, 0, 0);
            var collection = new DateTime(2026, 8, 7, 7, 10, 0);
            var facilityNow = new DateTime(2026, 8, 7, 8, 0, 0);

            SampleCollectionDateRules.Validate(collection, invoice, facilityNow, 5);
        }

        private static void SetAppSetting(string key, string value)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (config.AppSettings.Settings[key] == null)
            {
                config.AppSettings.Settings.Add(key, value);
            }
            else
            {
                config.AppSettings.Settings[key].Value = value;
            }

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
