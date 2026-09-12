using LIS.DtoModel.Models;
using LIS.DtoModel.Models.Reports;
using System;
using System.Collections.Generic;

namespace LIS.BusinessLogic.Reports
{
    /// <summary>
    /// Controlled synthetic sample data for Admin template preview. Contains no real PHI.
    /// </summary>
    public static class ReportTemplateSampleDataFactory
    {
        // 1x1 transparent PNG
        private const string TinyPng =
            "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO5X2ZQAAAAASUVORK5CYII=";

        public static DiagnosticTestReportDto CreateDiagnosticSample()
        {
            return new DiagnosticTestReportDto
            {
                Layout = DefaultLayout(ReportTemplateTypes.Diagnostic),
                Header = new DiagnosticTestReportHeader
                {
                    LabNo = "SAMPLE-LAB-001",
                    InvoiceNo = "SAMPLE-INV-001",
                    PatientName = "Sample Patient",
                    PatientId = "SAMPLE-PID",
                    MRNo = "SAMPLE-MR-001",
                    VisitId = "SAMPLE-V-001",
                    Age = 35,
                    Gender = "Female",
                    ReferralDoctor = "Dr. Sample Referrer",
                    Corporate = "Sample Corporate",
                    CollectionDate = new DateTime(2026, 9, 1, 9, 0, 0),
                    ReceivedDate = new DateTime(2026, 9, 1, 9, 30, 0),
                    ReportDate = new DateTime(2026, 9, 1, 14, 0, 0),
                    Status = "Final",
                    ApprovedBy = "sample.doctor",
                    ApprovedByName = "Dr. Sample Approver",
                    ApprovedByQualification = "MD",
                    ApprovedByDesignation = "Consultant Pathologist",
                    ApprovedBySignatureImage = TinyPng,
                    ReviewedBy = "sample.tech",
                    ReviewedByName = "Sample Technician",
                    ReviewedByQualification = "DMLT",
                    ReviewedBySignatureImage = TinyPng,
                    LabName = "Sample Lab",
                    CentreName = "Sample Centre",
                    Tagline = "Sample Tagline",
                    LicenseName = "Sample License",
                    Address = "Sample Address Line",
                    Email = "sample@example.invalid",
                    ContactNumbers = "000-000-0000",
                    PharmacyContact = "000-000-0001",
                    AppointmentContact = "000-000-0002"
                },
                DepartmentGroups = new List<DiagnosticTestReportDepartmentGroup>
                {
                    new DiagnosticTestReportDepartmentGroup
                    {
                        DepartmentName = "BIOCHEMISTRY",
                        Sections = new List<DiagnosticTestReportSection>
                        {
                            new DiagnosticTestReportSection
                            {
                                TestRequestDetailId = 1,
                                TestCode = "GLU",
                                TestName = "Blood Glucose Fasting",
                                Specimen = "Serum",
                                SampleNo = "S-SAMPLE-001",
                                Department = "BIOCHEMISTRY",
                                Comment = "Sample comment / note for preview.",
                                DoctorApprovalComment = "Sample doctor approval comment.",
                                Parameters = new List<DiagnosticTestReportParameter>
                                {
                                    new DiagnosticTestReportParameter
                                    {
                                        ParameterCode = "GLU",
                                        ParameterName = "Glucose Fasting",
                                        ResultValue = "98",
                                        Unit = "mg/dL",
                                        ReferenceRange = "70 - 110",
                                        Flag = null,
                                        IsAbnormal = false,
                                        SectionName = "Chemistry"
                                    },
                                    new DiagnosticTestReportParameter
                                    {
                                        ParameterCode = "CHOL",
                                        ParameterName = "Cholesterol",
                                        ResultValue = "245",
                                        Unit = "mg/dL",
                                        ReferenceRange = "< 200",
                                        Flag = "H",
                                        IsAbnormal = true,
                                        SectionName = "Chemistry"
                                    }
                                }
                            }
                        }
                    }
                },
                ProfileGroups = new List<DiagnosticTestReportProfileGroup>
                {
                    new DiagnosticTestReportProfileGroup
                    {
                        ProfileName = "Sample Lipid Profile",
                        ProfileCode = "LIPID",
                        Sections = new List<DiagnosticTestReportSection>
                        {
                            new DiagnosticTestReportSection
                            {
                                TestRequestDetailId = 2,
                                TestCode = "HDL",
                                TestName = "HDL Cholesterol",
                                Specimen = "Serum",
                                SampleNo = "S-SAMPLE-002",
                                Department = "BIOCHEMISTRY",
                                Comment = "Profile section comment.",
                                DoctorApprovalComment = null,
                                Parameters = new List<DiagnosticTestReportParameter>
                                {
                                    new DiagnosticTestReportParameter
                                    {
                                        ParameterCode = "HDL",
                                        ParameterName = "HDL",
                                        ResultValue = "55",
                                        Unit = "mg/dL",
                                        ReferenceRange = "> 40",
                                        Flag = null,
                                        IsAbnormal = false,
                                        SectionName = "Lipids"
                                    },
                                    new DiagnosticTestReportParameter
                                    {
                                        ParameterCode = "LDL",
                                        ParameterName = "LDL",
                                        ResultValue = "160",
                                        Unit = "mg/dL",
                                        ReferenceRange = "< 100",
                                        Flag = "H",
                                        IsAbnormal = true,
                                        SectionName = "Lipids"
                                    }
                                }
                            }
                        }
                    }
                },
                Sections = new List<DiagnosticTestReportSection>()
            };
        }

        public static DiagnosticRadiologyReportDto CreateRadiologySample()
        {
            return new DiagnosticRadiologyReportDto
            {
                Layout = DefaultLayout(ReportTemplateTypes.Radiology),
                Header = new DiagnosticRadiologyReportHeader
                {
                    AccessionNo = "SAMPLE-ACC-001",
                    InvoiceNo = "SAMPLE-INV-R01",
                    PatientName = "Sample Patient",
                    PatientId = "SAMPLE-PID",
                    MRNo = "SAMPLE-MR-001",
                    VisitId = "SAMPLE-V-001",
                    Age = 42,
                    Gender = "Male",
                    TestName = "Chest X-Ray PA",
                    Modality = "XR",
                    Department = "RADIOLOGY",
                    ReportStatus = "Authorized",
                    ReportDate = new DateTime(2026, 9, 1, 16, 0, 0),
                    AuthorizedBy = "sample.radiologist",
                    AuthorizedOn = new DateTime(2026, 9, 1, 16, 5, 0),
                    AuthorizedByName = "Dr. Sample Radiologist",
                    AuthorizedByDesignation = "Consultant Radiologist",
                    AuthorizedBySignatureImage = TinyPng
                },
                ClinicalHistory = "Sample clinical history for preview.",
                Findings = "Sample findings narrative for preview.",
                Impression = "Sample impression for preview.",
                Recommendation = "Sample recommendation for preview."
            };
        }

        public static object CreateSample(string reportType)
        {
            return string.Equals(reportType, ReportTemplateTypes.Radiology, StringComparison.OrdinalIgnoreCase)
                ? (object)CreateRadiologySample()
                : CreateDiagnosticSample();
        }

        public static ReportLayoutConfigurationDto DefaultLayout(string reportType)
        {
            return new ReportLayoutConfigurationDto
            {
                ReportType = reportType,
                PageSize = "A4",
                Orientation = "Portrait",
                HeaderHeightMm = 50,
                FooterHeightMm = 50,
                LeftMarginMm = 10,
                RightMarginMm = 10,
                DoctorSignatureEnabled = true,
                DoctorSignatureHorizontal = "Right",
                DoctorSignatureVertical = "Bottom",
                DoctorSignatureWidthMm = 40,
                DoctorSignatureHeightMm = 20,
                TechnicianSignatureEnabled = true,
                TechnicianSignatureHorizontal = "Left",
                TechnicianSignatureVertical = "Bottom",
                TechnicianSignatureWidthMm = 40,
                TechnicianSignatureHeightMm = 20,
                IsActive = true
            };
        }
    }
}
