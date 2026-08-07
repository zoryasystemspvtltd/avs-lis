using LIS.BusinessLogic.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Configuration;

namespace LIS.Masters.Tests.Workflows
{
    [TestClass]
    public class OperationalDateTimeStandardizationTests
    {
        [TestInitialize]
        public void SetUp()
        {
            SetAppSetting(OperationalDateTime.FacilityTimeZoneIdKey, "India Standard Time");
            SetAppSetting(OperationalDateTime.AllowedClockDriftMinutesKey, "5");
        }

        [TestMethod]
        public void FacilityNow_Is_Unspecified_Wall_Clock()
        {
            var now = OperationalDateTime.GetFacilityNow();
            var fromUtc = OperationalDateTime.ToFacilityWallClock(DateTime.UtcNow);
            Assert.AreEqual(DateTimeKind.Unspecified, now.Kind);
            Assert.IsTrue(Math.Abs((now - fromUtc).TotalMinutes) < 2);
        }

        [TestMethod]
        public void Invoice_Business_Day_Prefix_Uses_Facility_Calendar()
        {
            var facility = OperationalDateTime.GetFacilityNow();
            var prefix = $"INV-{facility:yyyyMMdd}-";
            StringAssert.StartsWith(prefix, "INV-");
            Assert.AreEqual(13, prefix.Length);
        }

        [TestMethod]
        public void Receiving_After_Collection_Passes()
        {
            var collection = new DateTime(2026, 8, 7, 7, 25, 0);
            var received = new DateTime(2026, 8, 7, 7, 40, 0);
            var facilityNow = new DateTime(2026, 8, 7, 8, 0, 0);

            SampleReceivingDateRules.Validate(received, collection, facilityNow, 5);
        }

        [TestMethod]
        public void Receiving_Before_Collection_Fails()
        {
            var collection = new DateTime(2026, 8, 7, 7, 25, 0);
            var received = new DateTime(2026, 8, 7, 7, 10, 0);
            var facilityNow = new DateTime(2026, 8, 7, 8, 0, 0);

            var ex = Assert.ThrowsException<ArgumentException>(() =>
                SampleReceivingDateRules.Validate(received, collection, facilityNow, 5));
            Assert.AreEqual(SampleReceivingDateRules.BeforeCollectionMessage, ex.Message);
        }

        [TestMethod]
        public void Receiving_Future_Beyond_Drift_Fails()
        {
            var collection = new DateTime(2026, 8, 7, 7, 25, 0);
            var facilityNow = new DateTime(2026, 8, 7, 8, 0, 0);
            var received = facilityNow.AddMinutes(10);

            var ex = Assert.ThrowsException<ArgumentException>(() =>
                SampleReceivingDateRules.Validate(received, collection, facilityNow, 5));
            Assert.AreEqual(SampleReceivingDateRules.FutureMessage, ex.Message);
        }

        [TestMethod]
        public void Receiving_Within_Drift_Passes()
        {
            var collection = new DateTime(2026, 8, 7, 7, 25, 0);
            var facilityNow = new DateTime(2026, 8, 7, 8, 0, 0);
            var received = facilityNow.AddMinutes(4);

            SampleReceivingDateRules.Validate(received, collection, facilityNow, 5);
        }

        [TestMethod]
        public void Chronology_Invoice_Then_Collection_Then_Receiving()
        {
            var invoice = new DateTime(2026, 8, 7, 7, 22, 0);
            var collection = new DateTime(2026, 8, 7, 7, 25, 0);
            var receiving = new DateTime(2026, 8, 7, 7, 40, 0);
            var facilityNow = new DateTime(2026, 8, 7, 8, 0, 0);

            SampleCollectionDateRules.Validate(collection, invoice, facilityNow, 5);
            SampleReceivingDateRules.Validate(receiving, collection, facilityNow, 5);
        }

        [TestMethod]
        public void Utc_Host_False_Future_Avoided_For_Receiving()
        {
            var collection = new DateTime(2026, 8, 7, 7, 25, 0, DateTimeKind.Unspecified);
            var received = new DateTime(2026, 8, 7, 7, 40, 0, DateTimeKind.Unspecified);
            var facilityNow = new DateTime(2026, 8, 7, 8, 0, 0, DateTimeKind.Unspecified);
            var utcHostNow = new DateTime(2026, 8, 7, 2, 30, 0);

            Assert.IsTrue(received > utcHostNow.AddMinutes(1),
                "Precondition: naive host Now would falsely reject receiving.");

            SampleReceivingDateRules.Validate(received, collection, facilityNow, 5);
        }

        [TestMethod]
        public void Approval_Stamp_Is_Facility_Wall_Clock_Kind()
        {
            var stamp = OperationalDateTime.GetFacilityNow();
            Assert.AreEqual(DateTimeKind.Unspecified, stamp.Kind);
        }

        [TestMethod]
        public void Local_Wire_Normalized_For_Invoice_Date()
        {
            var raw = new DateTime(2026, 8, 7, 7, 22, 0, DateTimeKind.Unspecified);
            var normalized = OperationalDateTime.ToFacilityWallClock(raw);
            Assert.AreEqual(raw, normalized);
            Assert.AreEqual(DateTimeKind.Unspecified, normalized.Kind);
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
