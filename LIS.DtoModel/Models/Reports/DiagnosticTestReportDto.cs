using System;
using System.Collections.Generic;

namespace LIS.DtoModel.Models
{
    public class DiagnosticTestReportDto
    {
        public DiagnosticTestReportHeader Header { get; set; }
        public List<DiagnosticTestReportSection> Sections { get; set; }
    }

    public class DiagnosticTestReportHeader
    {
        public string LabNo { get; set; }
        public string InvoiceNo { get; set; }
        public string PatientName { get; set; }
        public string PatientId { get; set; }
        public decimal Age { get; set; }
        public string Gender { get; set; }
        public string ReferralDoctor { get; set; }
        public string Corporate { get; set; }
        public DateTime? CollectionDate { get; set; }
        public DateTime? ReportDate { get; set; }
        public string ApprovedBy { get; set; }
    }

    public class DiagnosticTestReportSection
    {
        public string TestCode { get; set; }
        public string TestName { get; set; }
        public string Specimen { get; set; }
        public string SampleNo { get; set; }
        public string Department { get; set; }
        public List<DiagnosticTestReportParameter> Parameters { get; set; }
    }

    public class DiagnosticTestReportParameter
    {
        public string ParameterCode { get; set; }
        public string ParameterName { get; set; }
        public string ResultValue { get; set; }
        public string Unit { get; set; }
        public string ReferenceRange { get; set; }
        public string Flag { get; set; }
        public bool IsAbnormal { get; set; }
    }

    public class TestReportLabNoOption
    {
        public string LabNo { get; set; }
        public string InvoiceNo { get; set; }
        public string PatientName { get; set; }
        public string DisplayLabel { get; set; }
    }
}
