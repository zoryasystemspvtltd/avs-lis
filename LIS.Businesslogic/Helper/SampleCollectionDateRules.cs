using System;

namespace LIS.BusinessLogic.Helper
{
    /// <summary>
    /// Sample collection date/time business rules (facility wall-clock standard).
    /// </summary>
    public static class SampleCollectionDateRules
    {
        public const string BeforeInvoiceMessage =
            "Collection date/time cannot be earlier than Invoice date/time.";

        public const string FutureMessage =
            "Collection time cannot be in the future.";

        public static void Validate(
            DateTime collectionDateTime,
            DateTime? invoiceDateTime,
            DateTime facilityNow,
            int allowedClockDriftMinutes)
        {
            var collection = OperationalDateTime.ToFacilityWallClock(collectionDateTime);
            var now = DateTime.SpecifyKind(facilityNow, DateTimeKind.Unspecified);
            var drift = Math.Max(0, allowedClockDriftMinutes);

            if (invoiceDateTime.HasValue && invoiceDateTime.Value != default(DateTime))
            {
                var invoice = OperationalDateTime.ToFacilityWallClock(invoiceDateTime.Value);
                if (collection < invoice)
                {
                    throw new ArgumentException(BeforeInvoiceMessage);
                }
            }

            if (collection > now.AddMinutes(drift))
            {
                throw new ArgumentException(FutureMessage);
            }
        }
    }
}
