using System;

namespace LIS.DtoModel.Models
{
    public class DiagnosticRadiologyReportDto
    {
        public DiagnosticRadiologyReportHeader Header { get; set; }
        public string ClinicalHistory { get; set; }
        public string Findings { get; set; }
        public string Impression { get; set; }
        public string Recommendation { get; set; }
    }

    public class DiagnosticRadiologyReportHeader
    {
        public string AccessionNo { get; set; }
        public string InvoiceNo { get; set; }
        public string PatientName { get; set; }
        public string PatientId { get; set; }
        public decimal Age { get; set; }
        public string Gender { get; set; }
        public string TestName { get; set; }
        public string Modality { get; set; }
        public string Department { get; set; }
        public string ReportStatus { get; set; }
        public DateTime? ReportDate { get; set; }
        public string AuthorizedBy { get; set; }
        public DateTime? AuthorizedOn { get; set; }
        public string DigitalSignature { get; set; }
    }

    public class RadiologyPrintAccessionOption
    {
        public long RadiologyRequestId { get; set; }
        public string AccessionNo { get; set; }
        public string InvoiceNo { get; set; }
        public string PatientName { get; set; }
        public string TestName { get; set; }
        public string DisplayLabel { get; set; }
    }
}
