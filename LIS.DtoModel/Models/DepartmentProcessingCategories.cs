namespace LIS.DtoModel.Models
{
    /// <summary>Hardcoded department processing categories — not a separate master.</summary>
    public static class DepartmentProcessingCategories
    {
        public const string Laboratory = "Laboratory";
        public const string Diagnostic = "Diagnostic";

        public static bool IsValid(string value)
        {
            return string.Equals(value, Laboratory, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, Diagnostic, System.StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            if (string.Equals(value, Diagnostic, System.StringComparison.OrdinalIgnoreCase))
            {
                return Diagnostic;
            }

            return Laboratory;
        }

        public static bool IsDiagnostic(string processingCategory)
        {
            return string.Equals(processingCategory, Diagnostic, System.StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsLaboratory(string processingCategory)
        {
            return !IsDiagnostic(processingCategory);
        }
    }
}
