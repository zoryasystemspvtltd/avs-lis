using System;

namespace LIS.BusinessLogic.Helper
{
    /// <summary>
    /// Sample receiving date/time business rules (facility wall-clock standard).
    /// </summary>
    public static class SampleReceivingDateRules
    {
        public const string BeforeCollectionMessage =
            "Receiving time cannot be before collection time.";

        public const string FutureMessage =
            "Receiving time cannot be in the future.";

        public static void Validate(
            DateTime receivedDateTime,
            DateTime? collectionDateTime,
            DateTime facilityNow,
            int allowedClockDriftMinutes)
        {
            var received = OperationalDateTime.ToFacilityWallClock(receivedDateTime);
            var now = DateTime.SpecifyKind(facilityNow, DateTimeKind.Unspecified);
            var drift = Math.Max(0, allowedClockDriftMinutes);

            if (collectionDateTime.HasValue && collectionDateTime.Value != default(DateTime))
            {
                var collection = OperationalDateTime.ToFacilityWallClock(collectionDateTime.Value);
                if (received < collection)
                {
                    throw new ArgumentException(BeforeCollectionMessage);
                }
            }

            if (received > now.AddMinutes(drift))
            {
                throw new ArgumentException(FutureMessage);
            }
        }
    }
}
