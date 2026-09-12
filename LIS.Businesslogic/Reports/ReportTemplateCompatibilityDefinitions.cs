using LIS.DtoModel.Models.Reports;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LIS.BusinessLogic.Reports
{
    /// <summary>
    /// Conceptual declarative twins of the current hard-coded Angular reports.
    /// Not pixel-perfect; used for Phase 2 renderer validation / Admin preview only.
    /// Does not replace production Angular HTML.
    /// </summary>
    public static class ReportTemplateCompatibilityDefinitions
    {
        public static string BuildDiagnosticConceptual()
        {
            var root = new JObject
            {
                ["schemaVersion"] = ReportTemplateDefinitionValidator.CurrentSchemaVersion,
                ["reportType"] = ReportTemplateTypes.Diagnostic,
                ["renderer"] = "declarative",
                ["layoutSource"] = "ReportLayoutConfiguration",
                ["page"] = new JObject
                {
                    ["size"] = "A4",
                    ["orientation"] = "Portrait",
                    ["useGlobalLayoutClearance"] = true
                },
                ["body"] = new JObject
                {
                    ["type"] = ReportTemplateComponentTypes.Section,
                    ["id"] = "root",
                    ["children"] = new JArray
                    {
                        PatientPanel(),
                        DepartmentRepeat(),
                        SignatureBlock("Technician", "Technician.Name", "Technician.SignatureImage", "Technician.Qualification"),
                        SignatureBlock("Doctor", "Doctor.Name", "Doctor.SignatureImage", "Doctor.Designation")
                    }
                }
            };
            return root.ToString(Formatting.None);
        }

        public static string BuildRadiologyConceptual()
        {
            var root = new JObject
            {
                ["schemaVersion"] = ReportTemplateDefinitionValidator.CurrentSchemaVersion,
                ["reportType"] = ReportTemplateTypes.Radiology,
                ["renderer"] = "declarative",
                ["layoutSource"] = "ReportLayoutConfiguration",
                ["page"] = new JObject
                {
                    ["size"] = "A4",
                    ["orientation"] = "Portrait",
                    ["useGlobalLayoutClearance"] = true
                },
                ["body"] = new JObject
                {
                    ["type"] = ReportTemplateComponentTypes.Section,
                    ["id"] = "root",
                    ["children"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = ReportTemplateComponentTypes.Section,
                            ["id"] = "patient",
                            ["className"] = "report-patient-panel",
                            ["children"] = new JArray
                            {
                                Field(ReportTemplateComponentTypes.PatientField, "Patient Name", "Patient.Name"),
                                Field(ReportTemplateComponentTypes.PatientField, "Age / Gender", "Patient.AgeGender"),
                                Field(ReportTemplateComponentTypes.PatientField, "Patient Id", "Patient.PatientId"),
                                Field(ReportTemplateComponentTypes.PatientField, "UHID / MR No", "Patient.MRNo"),
                                Field(ReportTemplateComponentTypes.VisitField, "Visit ID", "Visit.VisitId"),
                                Field(ReportTemplateComponentTypes.AccessionField, "Accession No", "Accession.AccessionNo"),
                                Field(ReportTemplateComponentTypes.InvoiceField, "Invoice No", "Invoice.InvoiceNo"),
                                Field(ReportTemplateComponentTypes.TestField, "Test", "Test.TestName"),
                                Field(ReportTemplateComponentTypes.RadiologyField, "Modality", "Radiology.Modality"),
                                Field(ReportTemplateComponentTypes.RadiologyField, "Department", "Radiology.Department"),
                                Field(ReportTemplateComponentTypes.RadiologyField, "Report Status", "Radiology.ReportStatus"),
                                Field(ReportTemplateComponentTypes.RadiologyField, "Report Date", "Radiology.ReportDate")
                            }
                        },
                        Narrative("Clinical History", "Radiology.ClinicalHistory"),
                        Narrative("Findings", "Radiology.Findings"),
                        Narrative("Impression", "Radiology.Impression"),
                        new JObject
                        {
                            ["type"] = ReportTemplateComponentTypes.Section,
                            ["id"] = "recommendation",
                            ["visibleWhen"] = new JObject
                            {
                                ["op"] = ReportTemplateConditionOps.NotEmpty,
                                ["binding"] = "Radiology.Recommendation"
                            },
                            ["children"] = new JArray
                            {
                                Narrative("Recommendation", "Radiology.Recommendation")
                            }
                        },
                        SignatureBlock("Doctor", "Doctor.Name", "Doctor.SignatureImage", "Doctor.Designation")
                    }
                }
            };
            return root.ToString(Formatting.None);
        }

        public static string BuildForReportType(string reportType)
        {
            return string.Equals(reportType, ReportTemplateTypes.Radiology, System.StringComparison.OrdinalIgnoreCase)
                ? BuildRadiologyConceptual()
                : BuildDiagnosticConceptual();
        }

        private static JObject PatientPanel()
        {
            return new JObject
            {
                ["type"] = ReportTemplateComponentTypes.Section,
                ["id"] = "patient",
                ["className"] = "report-patient-panel",
                ["children"] = new JArray
                {
                    Field(ReportTemplateComponentTypes.PatientField, "Patient Name", "Patient.Name"),
                    Field(ReportTemplateComponentTypes.PatientField, "Age / Gender", "Patient.AgeGender"),
                    Field(ReportTemplateComponentTypes.PatientField, "UHID / MR No", "Patient.MRNo"),
                    Field(ReportTemplateComponentTypes.VisitField, "Visit ID", "Visit.VisitId"),
                    Field(ReportTemplateComponentTypes.OrderField, "Referring Doctor", "Order.ReferralDoctor"),
                    Field(ReportTemplateComponentTypes.OrderField, "Centre", "Branding.CentreName"),
                    Field(ReportTemplateComponentTypes.OrderField, "Lab No", "Order.LabNo"),
                    Field(ReportTemplateComponentTypes.InvoiceField, "Invoice No", "Invoice.InvoiceNo"),
                    Field(ReportTemplateComponentTypes.OrderField, "Collected", "Order.CollectionDate"),
                    Field(ReportTemplateComponentTypes.OrderField, "Received", "Order.ReceivedDate"),
                    Field(ReportTemplateComponentTypes.OrderField, "Reported", "Order.ReportDate"),
                    Field(ReportTemplateComponentTypes.OrderField, "Status", "Order.Status")
                }
            };
        }

        private static JObject DepartmentRepeat()
        {
            return new JObject
            {
                ["type"] = ReportTemplateComponentTypes.Section,
                ["id"] = "departments",
                ["repeat"] = new JObject
                {
                    ["source"] = ReportTemplateRepeatSources.DepartmentGroups,
                    ["alias"] = "dept"
                },
                ["children"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = ReportTemplateComponentTypes.Text,
                        ["id"] = "dept-header",
                        ["className"] = "report-department-header",
                        ["prefix"] = "DEPARTMENT OF ",
                        ["binding"] = "dept.DepartmentName",
                        ["transform"] = "uppercase"
                    },
                    new JObject
                    {
                        ["type"] = ReportTemplateComponentTypes.Section,
                        ["id"] = "tests",
                        ["repeat"] = new JObject
                        {
                            ["source"] = "dept.Sections",
                            ["alias"] = "section"
                        },
                        ["children"] = new JArray
                        {
                            new JObject
                            {
                                ["type"] = ReportTemplateComponentTypes.TestField,
                                ["id"] = "test-name",
                                ["className"] = "test-block-header",
                                ["binding"] = "section.TestName",
                                ["strong"] = true
                            },
                            new JObject
                            {
                                ["type"] = ReportTemplateComponentTypes.ParameterTable,
                                ["id"] = "params",
                                ["repeat"] = new JObject { ["source"] = "section.Parameters" },
                                ["columns"] = new JArray
                                {
                                    Col("Parameter.ParameterName", "Test Name / Parameter", 40),
                                    Col("Parameter.ResultValue", "Result", 20, true),
                                    Col("Parameter.Unit", "Unit", 15),
                                    Col("Parameter.ReferenceRange", "Bio. Ref. Interval", 25)
                                }
                            },
                            new JObject
                            {
                                ["type"] = ReportTemplateComponentTypes.Section,
                                ["id"] = "comment",
                                ["visibleWhen"] = new JObject
                                {
                                    ["op"] = ReportTemplateConditionOps.NotEmpty,
                                    ["binding"] = "section.Comment"
                                },
                                ["children"] = new JArray
                                {
                                    Field(ReportTemplateComponentTypes.Text, "Comment / Note:", "section.Comment")
                                }
                            },
                            new JObject
                            {
                                ["type"] = ReportTemplateComponentTypes.Section,
                                ["id"] = "doc-comment",
                                ["visibleWhen"] = new JObject
                                {
                                    ["op"] = ReportTemplateConditionOps.NotEmpty,
                                    ["binding"] = "section.DoctorApprovalComment"
                                },
                                ["children"] = new JArray
                                {
                                    Field(ReportTemplateComponentTypes.Text, "Doctor Approval Comment:", "section.DoctorApprovalComment")
                                }
                            }
                        }
                    }
                }
            };
        }

        private static JObject SignatureBlock(string source, string nameBinding, string imageBinding, string designationBinding)
        {
            return new JObject
            {
                ["type"] = ReportTemplateComponentTypes.Signature,
                ["id"] = "sig-" + source.ToLowerInvariant(),
                ["source"] = source,
                ["nameBinding"] = nameBinding,
                ["imageBinding"] = imageBinding,
                ["metaBinding"] = designationBinding,
                ["visibleWhen"] = new JObject
                {
                    ["op"] = ReportTemplateConditionOps.NotEmpty,
                    ["binding"] = nameBinding
                }
            };
        }

        private static JObject Narrative(string label, string binding)
        {
            return new JObject
            {
                ["type"] = ReportTemplateComponentTypes.RadiologyField,
                ["label"] = label,
                ["binding"] = binding,
                ["multiline"] = true
            };
        }

        private static JObject Field(string type, string label, string binding)
        {
            return new JObject
            {
                ["type"] = type,
                ["label"] = label,
                ["binding"] = binding
            };
        }

        private static JObject Col(string binding, string label, int widthPct, bool showFlag = false)
        {
            return new JObject
            {
                ["binding"] = binding,
                ["label"] = label,
                ["widthPct"] = widthPct,
                ["showFlag"] = showFlag
            };
        }
    }
}
