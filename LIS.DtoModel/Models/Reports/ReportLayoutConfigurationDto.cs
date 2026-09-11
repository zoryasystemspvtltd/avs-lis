namespace LIS.DtoModel.Models.Reports
{
    public class ReportLayoutConfigurationDto
    {
        public int Id { get; set; }
        public string ReportType { get; set; }
        public string PageSize { get; set; }
        public string Orientation { get; set; }
        public decimal HeaderHeightMm { get; set; }
        public decimal FooterHeightMm { get; set; }
        public decimal LeftMarginMm { get; set; }
        public decimal RightMarginMm { get; set; }
        public bool DoctorSignatureEnabled { get; set; }
        public string DoctorSignatureHorizontal { get; set; }
        public string DoctorSignatureVertical { get; set; }
        public decimal DoctorSignatureWidthMm { get; set; }
        public decimal DoctorSignatureHeightMm { get; set; }
        public bool TechnicianSignatureEnabled { get; set; }
        public string TechnicianSignatureHorizontal { get; set; }
        public string TechnicianSignatureVertical { get; set; }
        public decimal TechnicianSignatureWidthMm { get; set; }
        public decimal TechnicianSignatureHeightMm { get; set; }
        public bool IsActive { get; set; }
    }

    public static class ReportLayoutReportTypes
    {
        public const string Diagnostic = "Diagnostic";
        public const string Radiology = "Radiology";
    }
}
