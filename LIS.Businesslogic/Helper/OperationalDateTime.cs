using System;
using System.Configuration;

namespace LIS.BusinessLogic.Helper
{
    /// <summary>
    /// Enterprise operational date/time standard for ZoryaLMS.
    /// Operational timestamps (invoice, collection, receiving, approvals) are facility wall-clock times.
    /// They must not be interpreted as UTC unless an explicit offset/Z is present on the wire.
    /// </summary>
    public static class OperationalDateTime
    {
        public const string FacilityTimeZoneIdKey = "OperationalDateTime:FacilityTimeZoneId";
        public const string AllowedClockDriftMinutesKey = "OperationalDateTime:AllowedClockDriftMinutes";
        public const int DefaultAllowedClockDriftMinutes = 5;

        public static TimeZoneInfo GetFacilityTimeZone()
        {
            var id = Config.GetConfigValue(FacilityTimeZoneIdKey);
            if (string.IsNullOrWhiteSpace(id))
            {
                return TimeZoneInfo.Local;
            }

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id.Trim());
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.Local;
            }
            catch (InvalidTimeZoneException)
            {
                return TimeZoneInfo.Local;
            }
        }

        /// <summary>
        /// Current facility wall-clock time (Unspecified Kind).
        /// Uses configured facility timezone so validation is stable when IIS host TZ differs.
        /// </summary>
        public static DateTime GetFacilityNow()
        {
            var facilityTz = GetFacilityTimeZone();
            var facilityLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, facilityTz);
            return DateTime.SpecifyKind(facilityLocal, DateTimeKind.Unspecified);
        }

        public static int GetAllowedClockDriftMinutes()
        {
            var raw = Config.GetConfigValue(AllowedClockDriftMinutesKey);
            int minutes;
            if (int.TryParse(raw, out minutes) && minutes >= 0)
            {
                return minutes;
            }

            return DefaultAllowedClockDriftMinutes;
        }

        /// <summary>
        /// Normalize an inbound operational timestamp to facility wall-clock (Unspecified).
        /// Unspecified values are treated as already facility wall-clock (Angular datetime-local).
        /// Utc/Local values are converted into the facility timezone.
        /// </summary>
        public static DateTime ToFacilityWallClock(DateTime value)
        {
            if (value == default(DateTime))
            {
                return value;
            }

            var facilityTz = GetFacilityTimeZone();

            if (value.Kind == DateTimeKind.Utc)
            {
                var converted = TimeZoneInfo.ConvertTimeFromUtc(value, facilityTz);
                return DateTime.SpecifyKind(converted, DateTimeKind.Unspecified);
            }

            if (value.Kind == DateTimeKind.Local)
            {
                var asUtc = value.ToUniversalTime();
                var converted = TimeZoneInfo.ConvertTimeFromUtc(asUtc, facilityTz);
                return DateTime.SpecifyKind(converted, DateTimeKind.Unspecified);
            }

            return DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
        }
    }
}
