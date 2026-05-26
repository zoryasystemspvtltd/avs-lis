using LIS.DtoModel.Models;
using System;
using System.Globalization;
using System.Linq;

namespace LIS.BusinessLogic
{
    /// <summary>
    /// Shared reference-range / H-L evaluation aligned with TestReportManager.ApplyReferenceRange.
    /// </summary>
    public static class TestResultRangeEvaluator
    {
        public static void Apply(
            string resultValue,
            HISParameterMaster paramMaster,
            PatientDetail patient,
            System.Collections.Generic.IEnumerable<HISParameterRangMaster> allRanges,
            out string referenceRange,
            out string flag,
            out bool isAbnormal)
        {
            referenceRange = string.Empty;
            flag = string.Empty;
            isAbnormal = false;

            HISParameterRangMaster matchedRange = null;
            if (paramMaster != null && allRanges != null)
            {
                matchedRange = allRanges
                    .Where(r => r.HisParameterId == paramMaster.Id)
                    .Where(r => MatchesPatientRange(r, patient))
                    .OrderByDescending(r => r.MinValue > 0 || r.MaxValue > 0)
                    .ThenBy(r => r.Id)
                    .FirstOrDefault();
            }

            if (matchedRange != null)
            {
                if (matchedRange.MinValue > 0 || matchedRange.MaxValue > 0)
                {
                    referenceRange = $"{FormatDecimal(matchedRange.MinValue)} - {FormatDecimal(matchedRange.MaxValue)}";
                }
                else if (!string.IsNullOrWhiteSpace(matchedRange.HISRangeValue))
                {
                    referenceRange = matchedRange.HISRangeValue;
                }
            }

            if (matchedRange != null && TryParseResult(resultValue, out var numeric))
            {
                if (matchedRange.MinValue > 0 && numeric < matchedRange.MinValue)
                {
                    flag = "L";
                    isAbnormal = true;
                }
                else if (matchedRange.MaxValue > 0 && numeric > matchedRange.MaxValue)
                {
                    flag = "H";
                    isAbnormal = true;
                }
            }
        }

        private static bool MatchesPatientRange(HISParameterRangMaster range, PatientDetail patient)
        {
            if (range == null || patient == null)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(range.Gender) &&
                !string.IsNullOrWhiteSpace(patient.Gender) &&
                !GenderMatches(range.Gender, patient.Gender))
            {
                return false;
            }

            if (range.AgeFrom > 0 || range.AgeTo > 0)
            {
                var age = patient.Age;
                if (age < range.AgeFrom || (range.AgeTo > 0 && age > range.AgeTo))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool GenderMatches(string rangeGender, string patientGender)
        {
            var rg = rangeGender.Trim().ToUpperInvariant();
            var pg = patientGender.Trim().ToUpperInvariant();
            if (rg.StartsWith("M") && pg.StartsWith("M")) return true;
            if (rg.StartsWith("F") && pg.StartsWith("F")) return true;
            return rg == pg;
        }

        private static bool TryParseResult(string value, out decimal numeric)
        {
            numeric = 0;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var cleaned = value.Trim().Replace(",", "");
            return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out numeric)
                || decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.CurrentCulture, out numeric);
        }

        private static string FormatDecimal(decimal value)
        {
            return value % 1 == 0 ? value.ToString("0", CultureInfo.InvariantCulture) : value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
