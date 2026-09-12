using LIS.DtoModel.Models.Reports;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LIS.BusinessLogic.Reports
{
    /// <summary>
    /// Phase 3 design-field inventory from actual Diagnostic/Radiology DTOs (source-verified).
    /// </summary>
    public static class ReportDesignFieldCatalog
    {
        public static IList<ReportDesignFieldDto> List(string reportType = null)
        {
            var all = DiagnosticFields().Concat(RadiologyFields()).ToList();
            if (string.IsNullOrWhiteSpace(reportType))
            {
                return all;
            }

            var type = reportType.Trim();
            return all.Where(f => string.Equals(f.ReportType, type, StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(f.ReportType, "Both", StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private static IEnumerable<ReportDesignFieldDto> DiagnosticFields()
        {
            var t = ReportTemplateTypes.Diagnostic;

            // Patient / visit / order / invoice (Header)
            yield return F(t, "Patient.Name", "Patient Name", "Patient", true);
            yield return F(t, "Patient.Age", "Age", "Patient", true);
            yield return F(t, "Patient.Gender", "Gender", "Patient", true);
            yield return F(t, "Patient.AgeGender", "Age / Gender", "Patient", true);
            yield return F(t, "Patient.MRNo", "UHID / MR No", "Patient", true);
            yield return F(t, "Patient.PatientId", "Patient Id", "Patient", true);
            yield return F(t, "Visit.VisitId", "Visit ID", "Visit", true);
            yield return F(t, "Order.LabNo", "Lab No", "Order", true);
            yield return F(t, "Invoice.InvoiceNo", "Invoice No", "Invoice", true);
            yield return F(t, "Order.ReferralDoctor", "Referring Doctor", "Order", true);
            yield return F(t, "Order.Corporate", "Corporate", "Order", false, notes: "On Header DTO; mapped in Angular model, not shown in current HTML body");
            yield return F(t, "Order.CollectionDate", "Collected", "Order", true);
            yield return F(t, "Order.ReceivedDate", "Received", "Order", true);
            yield return F(t, "Order.ReportDate", "Reported", "Order", true);
            yield return F(t, "Order.Status", "Status", "Order", true);

            // Branding / lab contact (Header) — available on DTO for design even if stationery-printed today
            yield return F(t, "Branding.CentreName", "Centre", "Branding", true);
            yield return F(t, "Branding.LabName", "Lab Name", "Branding", false);
            yield return F(t, "Branding.Tagline", "Tagline", "Branding", false);
            yield return F(t, "Branding.LogoUrl", "Logo Url", "Branding", false);
            yield return F(t, "Branding.LicenseName", "License Name", "Branding", false);
            yield return F(t, "Branding.Address", "Address", "Branding", false);
            yield return F(t, "Branding.Email", "Email", "Branding", false);
            yield return F(t, "Branding.ContactNumbers", "Contact Numbers", "Branding", false);
            yield return F(t, "Branding.PharmacyContact", "Pharmacy Contact", "Branding", false);
            yield return F(t, "Branding.AppointmentContact", "Appointment Contact", "Branding", false);
            yield return F(t, "Header.Address", "Header Address", "Branding", false, notes: "Same source as Branding.Address via Header DTO");
            yield return F(t, "Header.Email", "Header Email", "Branding", false);
            yield return F(t, "Header.ContactNumbers", "Header Contact Numbers", "Branding", false);
            yield return F(t, "Header.LicenseName", "Header License Name", "Branding", false);
            yield return F(t, "Header.PharmacyContact", "Header Pharmacy Contact", "Branding", false);
            yield return F(t, "Header.AppointmentContact", "Header Appointment Contact", "Branding", false);

            // Collections
            yield return F(t, "DepartmentGroups", "Department Groups", "Collections", true, isCollection: true);
            yield return F(t, "ProfileGroups", "Profile Groups", "Collections", true, isCollection: true,
                notes: "Legacy Angular path when DepartmentGroups empty; also designable via RPG source");
            yield return F(t, "Sections", "Sections (flat)", "Collections", true, isCollection: true);
            yield return F(t, "Profile.ProfileName", "Profile Name", "Profile", true);
            yield return F(t, "Profile.ProfileCode", "Profile Code", "Profile", false);
            yield return F(t, "group.ProfileName", "Profile Name (repeat alias)", "Profile", true);

            // Test / sample / comments
            yield return F(t, "Test.TestName", "Test Name", "Test", true);
            yield return F(t, "Test.TestCode", "Test Code", "Test", false);
            yield return F(t, "Test.SampleNo", "Sample No", "Sample", false);
            yield return F(t, "Test.Specimen", "Specimen", "Sample", false);
            yield return F(t, "Test.Department", "Department", "Test", true);
            yield return F(t, "Test.Comment", "Comment / Note", "Comments", true);
            yield return F(t, "Test.DoctorApprovalComment", "Doctor Approval Comment", "Comments", true);
            yield return F(t, "section.Comment", "Section Comment", "Comments", true);
            yield return F(t, "section.DoctorApprovalComment", "Section Doctor Approval Comment", "Comments", true);

            // Parameters
            yield return F(t, "Parameter.ParameterName", "Parameter Name", "Parameter", true);
            yield return F(t, "Parameter.ParameterCode", "Parameter Code", "Parameter", false);
            yield return F(t, "Parameter.ResultValue", "Result", "Parameter", true);
            yield return F(t, "Parameter.Unit", "Unit", "Parameter", true);
            yield return F(t, "Parameter.ReferenceRange", "Bio. Ref. Interval", "Parameter", true);
            yield return F(t, "Parameter.Flag", "Flag", "Parameter", true);
            yield return F(t, "Parameter.IsAbnormal", "Is Abnormal", "Parameter", true);
            yield return F(t, "Parameter.SectionName", "Parameter Section", "Parameter", true);

            // Signatures
            yield return F(t, "Doctor.Name", "Doctor Name", "Signature", true);
            yield return F(t, "Doctor.Qualification", "Doctor Qualification", "Signature", true);
            yield return F(t, "Doctor.Designation", "Doctor Designation", "Signature", true);
            yield return F(t, "Doctor.SignatureImage", "Doctor Signature Image", "Signature", true);
            yield return F(t, "Technician.Name", "Technician Name", "Signature", true);
            yield return F(t, "Technician.Qualification", "Technician Qualification", "Signature", true);
            yield return F(t, "Technician.SignatureImage", "Technician Signature Image", "Signature", true);
        }

        private static IEnumerable<ReportDesignFieldDto> RadiologyFields()
        {
            var t = ReportTemplateTypes.Radiology;
            yield return F(t, "Patient.Name", "Patient Name", "Patient", true);
            yield return F(t, "Patient.PatientId", "Patient Id", "Patient", true);
            yield return F(t, "Patient.Age", "Age", "Patient", true);
            yield return F(t, "Patient.Gender", "Gender", "Patient", true);
            yield return F(t, "Patient.AgeGender", "Age / Gender", "Patient", true);
            yield return F(t, "Patient.MRNo", "MR No", "Patient", true);
            yield return F(t, "Visit.VisitId", "Visit ID", "Visit", true);
            yield return F(t, "Accession.AccessionNo", "Accession No", "Accession", true);
            yield return F(t, "Invoice.InvoiceNo", "Invoice No", "Invoice", true);
            yield return F(t, "Test.TestName", "Test", "Test", true);
            yield return F(t, "Radiology.Modality", "Modality", "Radiology", true);
            yield return F(t, "Radiology.Department", "Department", "Radiology", true);
            yield return F(t, "Radiology.ReportStatus", "Report Status", "Radiology", true);
            yield return F(t, "Radiology.ReportDate", "Report Date", "Radiology", true);
            yield return F(t, "Radiology.ClinicalHistory", "Clinical History", "Radiology", true);
            yield return F(t, "Radiology.Findings", "Findings", "Radiology", true);
            yield return F(t, "Radiology.Impression", "Impression", "Radiology", true);
            yield return F(t, "Radiology.Recommendation", "Recommendation", "Radiology", true);
            yield return F(t, "Doctor.Name", "Authorizer Name", "Signature", true);
            yield return F(t, "Doctor.AuthorizedBy", "Authorized By (username)", "Signature", true);
            yield return F(t, "Doctor.Designation", "Authorizer Designation", "Signature", true);
            yield return F(t, "Doctor.SignatureImage", "Authorizer Signature Image", "Signature", true);
            yield return F(t, "Doctor.DigitalSignature", "Digital Signature Text", "Signature", true,
                notes: "RadiologyResult.DigitalSignature fallback text");
            yield return F(t, "Header.AuthorizedOn", "Authorized On", "Signature", true,
                notes: "On Header DTO; Angular uses ReportDate for display date");
            yield return F(t, "Header.AuthorizedBy", "Header Authorized By", "Signature", true);
            yield return F(t, "Header.DigitalSignature", "Header Digital Signature", "Signature", true);
        }

        private static ReportDesignFieldDto F(string reportType, string path, string label, string group,
            bool displayed, bool isCollection = false, string notes = null)
        {
            return new ReportDesignFieldDto
            {
                ReportType = reportType,
                Path = path,
                Label = label,
                Group = group,
                DisplayedInCurrentAngular = displayed,
                IsCollection = isCollection,
                Notes = notes
            };
        }
    }
}
