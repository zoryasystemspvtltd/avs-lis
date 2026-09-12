using System;
using System.Collections.Generic;
using LIS.DtoModel.Models.Reports;

namespace LIS.DtoModel.Models
{
    public class DiagnosticTestReportDto
    {
        public DiagnosticTestReportHeader Header { get; set; }
        public List<DiagnosticTestReportProfileGroup> ProfileGroups { get; set; }
        /// <summary>Tests grouped by department in Sale Invoice booking order.</summary>
        public List<DiagnosticTestReportDepartmentGroup> DepartmentGroups { get; set; }
        /// <summary>Standalone sections (backward compatible flat list).</summary>
        public List<DiagnosticTestReportSection> Sections { get; set; }
        /// <summary>Optional physical print layout (stationery clearance / signature). Null = client defaults.</summary>
        public ReportLayoutConfigurationDto Layout { get; set; }

        /// <summary>
        /// Phase 4 presentation decision. Null or PresentationMode=Existing → Angular templates.
        /// PresentationMode=Declarative + Html → server-rendered HTML (flag ON + successful Custom only).
        /// </summary>
        public ReportProductionPresentationDto Presentation { get; set; }
    }

    public class DiagnosticTestReportDepartmentGroup
    {
        public string DepartmentName { get; set; }
        public List<DiagnosticTestReportSection> Sections { get; set; }
    }

    public class DiagnosticTestReportProfileGroup
    {
        public string ProfileName { get; set; }
        public string ProfileCode { get; set; }
        public List<DiagnosticTestReportSection> Sections { get; set; }
    }

    public class DiagnosticTestReportHeader
    {
        public string LabNo { get; set; }
        public string InvoiceNo { get; set; }
        public string PatientName { get; set; }
        public string PatientId { get; set; }
        public string MRNo { get; set; }
        public string VisitId { get; set; }
        public decimal Age { get; set; }
        public string Gender { get; set; }
        public string ReferralDoctor { get; set; }
        public string Corporate { get; set; }
        public DateTime? CollectionDate { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public DateTime? ReportDate { get; set; }
        public string Status { get; set; }
        public string ApprovedBy { get; set; }
        public string ApprovedByName { get; set; }
        public string ApprovedByQualification { get; set; }
        public string ApprovedByDesignation { get; set; }
        public string ApprovedBySignatureImage { get; set; }
        /// <summary>Technician who approved the result (<c>TestResult.ReviewedBy</c> username).</summary>
        public string ReviewedBy { get; set; }
        public string ReviewedByName { get; set; }
        public string ReviewedByQualification { get; set; }
        /// <summary>Inline data-URI from the same user signature store used for Doctor/Technician upload.</summary>
        public string ReviewedBySignatureImage { get; set; }
        /// <summary>
        /// Deprecated for print: doctor notes are now emitted per test section
        /// (<see cref="DiagnosticTestReportSection.DoctorApprovalComment"/>).
        /// Kept for API backward compatibility; always null on new reports.
        /// </summary>
        public string DoctorApprovalComment { get; set; }

        // Laboratory branding / footer (from configurable app settings + client application)
        public string LabName { get; set; }
        public string Tagline { get; set; }
        public string CentreName { get; set; }
        public string LogoUrl { get; set; }
        public string LicenseName { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string ContactNumbers { get; set; }
        public string PharmacyContact { get; set; }
        public string AppointmentContact { get; set; }
    }

    public class DiagnosticTestReportSection
    {
        public string TestCode { get; set; }
        public string TestName { get; set; }
        public string Specimen { get; set; }
        public string SampleNo { get; set; }
        public string Department { get; set; }
        /// <summary>Authoritative per-test identity for print selection (<c>TestRequestDetail.Id</c>).</summary>
        public long TestRequestDetailId { get; set; }
        /// <summary>Aggregated Comment/Note from Parameter Master (HTML/text).</summary>
        public string Comment { get; set; }
        /// <summary>Doctor authorization note for this test/specimen only (when present).</summary>
        public string DoctorApprovalComment { get; set; }
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
        /// <summary>Optional parameter-group / section heading (e.g. Physical Examination).</summary>
        public string SectionName { get; set; }
    }

    public class TestReportLabNoOption
    {
        public string LabNo { get; set; }
        public string InvoiceNo { get; set; }
        public string PatientName { get; set; }
        public string DisplayLabel { get; set; }
    }

    /// <summary>
    /// Print eligibility for an order: Print All requires every test printable;
    /// individual options expose which tests may print independently (payment already validated).
    /// </summary>
    public class TestReportPrintOptionsDto
    {
        public string LabNo { get; set; }
        public string InvoiceNo { get; set; }
        public bool CanPrintAll { get; set; }
        public List<TestReportPrintableTestOption> Tests { get; set; }
    }

    public class TestReportPrintableTestOption
    {
        public long TestRequestDetailId { get; set; }
        public string TestCode { get; set; }
        public string TestName { get; set; }
        public string Label { get; set; }
        public bool IsPrintable { get; set; }
    }
}
