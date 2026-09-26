using System;

namespace LIS.BusinessLogic
{
    /// <summary>
    /// Shared checks for doctor and collector commission rates. Not a recipient model.
    /// </summary>
    internal static class CommissionRateRules
    {
        public static void ValidatePercent(decimal percent)
        {
            if (percent < 0m || percent > 100m)
            {
                throw new ArgumentException("Commission percentage must be from 0 to 100.");
            }

            if (decimal.Round(percent, 2, MidpointRounding.AwayFromZero) != percent)
            {
                throw new ArgumentException("Commission percentage allows at most 2 decimal places.");
            }
        }

        public static void ValidatePeriod(DateTime effectiveFrom, DateTime? effectiveTo)
        {
            if (effectiveFrom == default(DateTime))
            {
                throw new ArgumentException("Effective from is required.");
            }

            if (effectiveTo.HasValue && effectiveTo.Value != default(DateTime)
                && effectiveFrom.Date > effectiveTo.Value.Date)
            {
                throw new ArgumentException("Effective from must be on or before effective to.");
            }
        }

        public static DateTime? NormalizeEnd(DateTime? effectiveTo)
        {
            if (!effectiveTo.HasValue || effectiveTo.Value == default(DateTime))
            {
                return null;
            }

            return effectiveTo.Value.Date;
        }

        public static bool Overlaps(DateTime from, DateTime? to, DateTime otherFrom, DateTime? otherTo)
        {
            var end = to?.Date ?? DateTime.MaxValue.Date;
            var otherEnd = otherTo?.Date ?? DateTime.MaxValue.Date;
            return from.Date <= otherEnd && otherFrom.Date <= end;
        }
    }
}
