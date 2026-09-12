using LIS.DtoModel.Models.Reports;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LIS.BusinessLogic.Reports
{
    /// <summary>
    /// Component palette contract shared by validator, renderer, and future designer.
    /// </summary>
    public static class ReportComponentRegistry
    {
        private static readonly ReportComponentDescriptorDto[] All =
        {
            D(ReportTemplateComponentTypes.Text, "Text", Both, "Shared", "Static label or bound text"),
            D(ReportTemplateComponentTypes.PatientField, "Patient Field", Both, "Patient", "Patient.*"),
            D(ReportTemplateComponentTypes.OrderField, "Order Field", Diag, "Order", "Order.* / Header lab fields"),
            D(ReportTemplateComponentTypes.InvoiceField, "Invoice Field", Both, "Invoice", "Invoice.*"),
            D(ReportTemplateComponentTypes.VisitField, "Visit Field", Both, "Visit", "Visit.*"),
            D(ReportTemplateComponentTypes.TestField, "Test Field", Both, "Test", "Test.*"),
            D(ReportTemplateComponentTypes.SampleField, "Sample Field", Diag, "Sample", "Test.SampleNo / Specimen"),
            D(ReportTemplateComponentTypes.ParameterTable, "Parameter Table", Diag, "Results", "Repeating Parameter rows"),
            D(ReportTemplateComponentTypes.DoctorField, "Doctor Field", Both, "Doctor", "Doctor.*"),
            D(ReportTemplateComponentTypes.TechnicianField, "Technician Field", Diag, "Technician", "Technician.*"),
            D(ReportTemplateComponentTypes.Signature, "Signature", Both, "Signature", "Doctor or Technician image from DTO"),
            D(ReportTemplateComponentTypes.Image, "Image", Both, "Media", "Safe data URI / relative path only"),
            D(ReportTemplateComponentTypes.Line, "Line", Both, "Layout", "Horizontal rule"),
            D(ReportTemplateComponentTypes.Section, "Section", Both, "Layout", "Container / repeating block"),
            D(ReportTemplateComponentTypes.Spacer, "Spacer", Both, "Layout", "Vertical space"),
            D(ReportTemplateComponentTypes.AccessionField, "Accession Field", Rad, "Accession", "Accession.*"),
            D(ReportTemplateComponentTypes.RadiologyField, "Radiology Field", Rad, "Radiology", "Radiology.ClinicalHistory/Findings/Impression/Recommendation"),
            D(ReportTemplateComponentTypes.RepeatingParameterGroup, "Repeating Parameter Group", Diag, "Results",
                "User-friendly Diagnostic repeating block (tests → parameters)"),
            D(ReportTemplateComponentTypes.CommentField, "Comment / Note", Diag, "Comments", "Test.Comment"),
            D(ReportTemplateComponentTypes.DoctorApprovalComment, "Doctor Approval Comment", Diag, "Comments", "Test.DoctorApprovalComment")
        };

        private static string[] Both => new[] { ReportTemplateTypes.Diagnostic, ReportTemplateTypes.Radiology };
        private static string[] Diag => new[] { ReportTemplateTypes.Diagnostic };
        private static string[] Rad => new[] { ReportTemplateTypes.Radiology };

        public static IList<ReportComponentDescriptorDto> List(string reportType = null)
        {
            if (string.IsNullOrWhiteSpace(reportType))
            {
                return All.ToList();
            }

            var type = reportType.Trim();
            return All.Where(c => c.ReportTypes.Any(t => string.Equals(t, type, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        public static bool IsKnownType(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return false;
            }

            return All.Any(c => string.Equals(c.Type, type.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public static bool SupportsReportType(string componentType, string reportType)
        {
            var item = All.FirstOrDefault(c => string.Equals(c.Type, componentType, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                return false;
            }

            return item.ReportTypes.Any(t => string.Equals(t, reportType, StringComparison.OrdinalIgnoreCase));
        }

        private static ReportComponentDescriptorDto D(string type, string name, string[] reportTypes, string category, string notes)
        {
            return new ReportComponentDescriptorDto
            {
                Type = type,
                DisplayName = name,
                ReportTypes = reportTypes,
                Category = category,
                Notes = notes,
                AllowedBindings = new string[0]
            };
        }
    }
}
