using LIS.DtoModel.Models;
using LIS.DtoModel.Models.Reports;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace LIS.BusinessLogic.Reports
{
    /// <summary>
    /// Safe declarative HTML renderer. Consumes assembled DTOs only. Never used for production print while feature flag is false.
    /// Positioning model: section/block document flow (print-safe). Signature alignment via CSS classes, not free-form canvas.
    /// </summary>
    public sealed class DeclarativeReportRenderer : IReportRenderer
    {
        public ReportRenderResultDto Render(ReportRenderRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var warnings = new List<string>();
            var reportType = NormalizeReportType(request.ReportType);

            ReportTemplateDefinitionValidator.Validate(request.DefinitionJson, reportType);

            var root = JObject.Parse(request.DefinitionJson);
            var renderer = ((string)(root["renderer"] ?? root["Renderer"]) ?? "declarative").Trim();
            if (string.Equals(renderer, "builtin", StringComparison.OrdinalIgnoreCase))
            {
                return new ReportRenderResultDto
                {
                    Success = true,
                    ReportType = reportType,
                    UsesBuiltInRenderer = true,
                    Html = null,
                    Css = null,
                    Message = "Definition targets built-in Angular renderer. Declarative HTML is not produced.",
                    Warnings = warnings
                };
            }

            if (request.ReportData == null)
            {
                throw new ArgumentException("Report data is required for declarative rendering.");
            }

            var layout = request.LayoutOverride
                ?? ExtractLayout(request.ReportData)
                ?? ReportTemplateSampleDataFactory.DefaultLayout(reportType);

            ApplyPageHints(root["page"] as JObject, layout);

            var ctx = new ReportTemplateBindingContext(request.ReportData);
            var body = root["body"] as JObject;
            var html = new StringBuilder();
            html.Append("<div class=\"rte-report\" data-report-type=\"").Append(Encode(reportType)).Append("\">");
            html.Append("<header class=\"rte-header-band\" aria-hidden=\"true\"></header>");
            html.Append("<div class=\"rte-body\">");

            if (body != null)
            {
                RenderComponent(body, ctx, html, warnings, layout);
            }
            else
            {
                var components = root["components"] as JArray;
                if (components != null)
                {
                    foreach (var c in components.OfType<JObject>())
                    {
                        RenderComponent(c, ctx, html, warnings, layout);
                    }
                }
            }

            html.Append("</div>");
            html.Append("<div class=\"rte-footer-band\" aria-hidden=\"true\"></div>");
            html.Append("</div>");

            return new ReportRenderResultDto
            {
                Success = true,
                ReportType = reportType,
                UsesBuiltInRenderer = false,
                Html = html.ToString(),
                Css = BuildCss(layout),
                Message = "Rendered with declarative template engine (preview only).",
                Warnings = warnings,
                PageSize = layout.PageSize ?? "A4",
                Orientation = layout.Orientation ?? "Portrait",
                HeaderHeightMm = layout.HeaderHeightMm,
                FooterHeightMm = layout.FooterHeightMm,
                LeftMarginMm = layout.LeftMarginMm,
                RightMarginMm = layout.RightMarginMm
            };
        }

        private static void RenderComponent(JObject component, ReportTemplateBindingContext ctx, StringBuilder html,
            IList<string> warnings, ReportLayoutConfigurationDto layout)
        {
            if (component == null)
            {
                return;
            }

            if (IsStyleHidden(component))
            {
                return;
            }

            if (!EvaluateVisibleWhen(component["visibleWhen"] as JObject, ctx))
            {
                return;
            }

            var type = ((string)(component["type"] ?? component["Type"]) ?? string.Empty).Trim();

            // PARAMETER_TABLE owns its row iteration — do not expand via generic repeat cloning.
            if (string.Equals(type, ReportTemplateComponentTypes.ParameterTable, StringComparison.OrdinalIgnoreCase))
            {
                RenderParameterTable(component, ctx, html);
                return;
            }

            if (string.Equals(type, ReportTemplateComponentTypes.RepeatingParameterGroup, StringComparison.OrdinalIgnoreCase))
            {
                RenderRepeatingParameterGroup(component, ctx, html, warnings, layout);
                return;
            }

            if (string.Equals(type, ReportTemplateComponentTypes.CommentField, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(type, ReportTemplateComponentTypes.DoctorApprovalComment, StringComparison.OrdinalIgnoreCase))
            {
                RenderCommentLike(component, ctx, html, type);
                return;
            }

            var repeat = component["repeat"] as JObject;
            if (repeat != null)
            {
                var source = ((string)(repeat["source"] ?? repeat["Source"]) ?? string.Empty).Trim();
                var alias = ((string)(repeat["alias"] ?? repeat["Alias"]) ?? "item").Trim();
                var items = ctx.ResolveCollection(source).ToList();
                if (items.Count == 0)
                {
                    return;
                }

                foreach (var item in items)
                {
                    var childCtx = PushRepeatItem(ctx, alias, item);
                    RenderComponentOnce(component, childCtx, html, warnings, layout);
                }
                return;
            }

            RenderComponentOnce(component, ctx, html, warnings, layout);
        }

        private static ReportTemplateBindingContext PushRepeatItem(ReportTemplateBindingContext ctx, string alias, object item)
        {
            var next = ctx.Push(alias, item);

            if (item is DiagnosticTestReportDepartmentGroup dept)
            {
                next = next.Push("Department", dept.DepartmentName);
            }
            else if (item is DiagnosticTestReportSection section)
            {
                next = next.Push("Test", new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["TestName"] = section.TestName,
                    ["TestCode"] = section.TestCode,
                    ["SampleNo"] = section.SampleNo,
                    ["Specimen"] = section.Specimen,
                    ["Department"] = section.Department,
                    ["Comment"] = section.Comment,
                    ["DoctorApprovalComment"] = section.DoctorApprovalComment
                });
                next = next.Push("section", section);
            }
            else if (item is DiagnosticTestReportParameter param)
            {
                next = next.Push("Parameter", new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ParameterName"] = param.ParameterName,
                    ["ParameterCode"] = param.ParameterCode,
                    ["ResultValue"] = param.ResultValue,
                    ["Unit"] = param.Unit,
                    ["ReferenceRange"] = param.ReferenceRange,
                    ["Flag"] = param.Flag,
                    ["IsAbnormal"] = param.IsAbnormal,
                    ["SectionName"] = param.SectionName
                });
            }
            else if (item is DiagnosticTestReportProfileGroup group)
            {
                next = next.Push("group", group);
                next = next.Push("Profile", new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ProfileName"] = group.ProfileName,
                    ["Name"] = group.ProfileName,
                    ["ProfileCode"] = group.ProfileCode,
                    ["Code"] = group.ProfileCode
                });
            }

            return next;
        }

        private static void RenderComponentOnce(JObject component, ReportTemplateBindingContext ctx, StringBuilder html,
            IList<string> warnings, ReportLayoutConfigurationDto layout)
        {
            var type = ((string)(component["type"] ?? component["Type"]) ?? string.Empty).Trim().ToUpperInvariant();
            switch (type)
            {
                case "SECTION":
                    RenderSection(component, ctx, html, warnings, layout);
                    break;
                case "REPEATING_PARAMETER_GROUP":
                    RenderRepeatingParameterGroup(component, ctx, html, warnings, layout);
                    break;
                case "COMMENT_FIELD":
                case "DOCTOR_APPROVAL_COMMENT":
                    RenderCommentLike(component, ctx, html, type);
                    break;
                case "TEXT":
                case "PATIENT_FIELD":
                case "ORDER_FIELD":
                case "INVOICE_FIELD":
                case "VISIT_FIELD":
                case "TEST_FIELD":
                case "SAMPLE_FIELD":
                case "DOCTOR_FIELD":
                case "TECHNICIAN_FIELD":
                case "ACCESSION_FIELD":
                case "RADIOLOGY_FIELD":
                    RenderField(component, ctx, html, type);
                    break;
                case "PARAMETER_TABLE":
                    RenderParameterTable(component, ctx, html);
                    break;
                case "SIGNATURE":
                    RenderSignature(component, ctx, html, layout);
                    break;
                case "IMAGE":
                    RenderImage(component, ctx, html);
                    break;
                case "LINE":
                    html.Append("<hr class=\"rte-line\"").Append(StyleAttr(component)).Append(" />");
                    break;
                case "SPACER":
                    var h = component.Value<decimal?>("heightMm")
                            ?? ParseMm(ReadStyle(component, "height"))
                            ?? 4m;
                    html.Append("<div class=\"rte-spacer\" style=\"height:")
                        .Append(h.ToString(CultureInfo.InvariantCulture)).Append("mm;\"></div>");
                    break;
                default:
                    warnings.Add("Skipped unsupported component at render time: " + type);
                    break;
            }
        }

        private static void RenderRepeatingParameterGroup(JObject component, ReportTemplateBindingContext ctx,
            StringBuilder html, IList<string> warnings, ReportLayoutConfigurationDto layout)
        {
            // Expand to department/profile → section → parameter table when children omitted (designer convenience).
            var children = component["children"] as JArray;
            if (children == null || children.Count == 0)
            {
                var source = ResolveRpgSource(component);
                if (string.Equals(source, ReportTemplateRepeatSources.ProfileGroups, StringComparison.OrdinalIgnoreCase))
                {
                    RenderComponent(BuildProfileGroupExpansion(component), ctx, html, warnings, layout);
                }
                else if (string.Equals(source, "Both", StringComparison.OrdinalIgnoreCase))
                {
                    RenderComponent(BuildDepartmentGroupExpansion(component), ctx, html, warnings, layout);
                    RenderComponent(BuildProfileGroupExpansion(component), ctx, html, warnings, layout);
                }
                else
                {
                    RenderComponent(BuildDepartmentGroupExpansion(component), ctx, html, warnings, layout);
                }
                return;
            }

            var asSection = new JObject(component)
            {
                ["type"] = ReportTemplateComponentTypes.Section
            };
            if (asSection["repeat"] == null)
            {
                asSection["repeat"] = new JObject
                {
                    ["source"] = ReportTemplateRepeatSources.DepartmentGroups,
                    ["alias"] = "dept"
                };
            }
            RenderComponent(asSection, ctx, html, warnings, layout);
        }

        private static string ResolveRpgSource(JObject component)
        {
            var repeat = component["repeat"] as JObject;
            var source = ((string)(repeat?["source"] ?? repeat?["Source"]) ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(source)
                ? ReportTemplateRepeatSources.DepartmentGroups
                : source;
        }

        private static JObject BuildDepartmentGroupExpansion(JObject component)
        {
            return new JObject
            {
                ["type"] = ReportTemplateComponentTypes.Section,
                ["id"] = (string)(component["id"] ?? "rpg-dept"),
                ["style"] = component["style"],
                ["visibleWhen"] = component["visibleWhen"],
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
                        ["className"] = "report-department-header",
                        ["prefix"] = "DEPARTMENT OF ",
                        ["binding"] = "dept.DepartmentName",
                        ["transform"] = "uppercase"
                    },
                    BuildSectionParameterChildren("dept.Sections", component)
                }
            };
        }

        private static JObject BuildProfileGroupExpansion(JObject component)
        {
            return new JObject
            {
                ["type"] = ReportTemplateComponentTypes.Section,
                ["id"] = ((string)(component["id"] ?? "rpg")) + "-profile",
                ["style"] = component["style"],
                ["visibleWhen"] = component["visibleWhen"],
                ["repeat"] = new JObject
                {
                    ["source"] = ReportTemplateRepeatSources.ProfileGroups,
                    ["alias"] = "group"
                },
                ["children"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = ReportTemplateComponentTypes.Text,
                        ["className"] = "report-profile-header",
                        ["binding"] = "group.ProfileName",
                        ["strong"] = true
                    },
                    BuildSectionParameterChildren("group.Sections", component)
                }
            };
        }

        private static JObject BuildSectionParameterChildren(string sectionsSource, JObject component)
        {
            return new JObject
            {
                ["type"] = ReportTemplateComponentTypes.Section,
                ["repeat"] = new JObject { ["source"] = sectionsSource, ["alias"] = "section" },
                ["children"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = ReportTemplateComponentTypes.TestField,
                        ["binding"] = "section.TestName",
                        ["strong"] = true,
                        ["className"] = "test-block-header"
                    },
                    new JObject
                    {
                        ["type"] = ReportTemplateComponentTypes.ParameterTable,
                        ["repeat"] = new JObject { ["source"] = "section.Parameters" },
                        ["columns"] = component["columns"] ?? DefaultParameterColumns()
                    },
                    new JObject
                    {
                        ["type"] = ReportTemplateComponentTypes.CommentField,
                        ["binding"] = "section.Comment",
                        ["label"] = "Comment / Note:",
                        ["visibleWhen"] = new JObject
                        {
                            ["op"] = ReportTemplateConditionOps.NotEmpty,
                            ["binding"] = "section.Comment"
                        }
                    },
                    new JObject
                    {
                        ["type"] = ReportTemplateComponentTypes.DoctorApprovalComment,
                        ["binding"] = "section.DoctorApprovalComment",
                        ["label"] = "Doctor Approval Comment:",
                        ["visibleWhen"] = new JObject
                        {
                            ["op"] = ReportTemplateConditionOps.NotEmpty,
                            ["binding"] = "section.DoctorApprovalComment"
                        }
                    }
                }
            };
        }

        private static JArray DefaultParameterColumns()
        {
            return new JArray
            {
                new JObject { ["binding"] = "Parameter.ParameterName", ["label"] = "Test Name / Parameter", ["widthPct"] = 40 },
                new JObject { ["binding"] = "Parameter.ResultValue", ["label"] = "Result", ["widthPct"] = 20, ["showFlag"] = true },
                new JObject { ["binding"] = "Parameter.Unit", ["label"] = "Unit", ["widthPct"] = 15 },
                new JObject { ["binding"] = "Parameter.ReferenceRange", ["label"] = "Bio. Ref. Interval", ["widthPct"] = 25 }
            };
        }

        private static void RenderCommentLike(JObject component, ReportTemplateBindingContext ctx, StringBuilder html, string type)
        {
            if (IsStyleHidden(component))
            {
                return;
            }

            if (!EvaluateVisibleWhen(component["visibleWhen"] as JObject, ctx))
            {
                return;
            }

            var binding = (string)(component["binding"] ?? component["Binding"]);
            if (string.IsNullOrWhiteSpace(binding))
            {
                binding = string.Equals(type, "DOCTOR_APPROVAL_COMMENT", StringComparison.OrdinalIgnoreCase)
                    ? "section.DoctorApprovalComment"
                    : "section.Comment";
            }

            var label = (string)(component["label"] ?? component["Label"])
                ?? (string.Equals(type, "DOCTOR_APPROVAL_COMMENT", StringComparison.OrdinalIgnoreCase)
                    ? "Doctor Approval Comment:"
                    : "Comment / Note:");

            var value = ctx.GetString(binding);
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            html.Append("<div class=\"rte-comment\"").Append(StyleAttr(component)).Append("><div class=\"rte-label\">")
                .Append(Encode(label))
                .Append("</div><div class=\"rte-value\">")
                .Append(Encode(value))
                .Append("</div></div>");
        }

        private static void RenderSection(JObject component, ReportTemplateBindingContext ctx, StringBuilder html,
            IList<string> warnings, ReportLayoutConfigurationDto layout)
        {
            var id = Encode((string)(component["id"] ?? component["Id"]) ?? string.Empty);
            var className = Encode((string)(component["className"] ?? component["ClassName"]) ?? "rte-section");
            html.Append("<section class=\"").Append(className).Append("\"");
            if (!string.IsNullOrWhiteSpace(id))
            {
                html.Append(" data-id=\"").Append(id).Append("\"");
            }
            html.Append(StyleAttr(component));
            html.Append(">");

            var children = component["children"] as JArray;
            if (children != null)
            {
                foreach (var child in children.OfType<JObject>())
                {
                    RenderComponent(child, ctx, html, warnings, layout);
                }
            }

            html.Append("</section>");
        }

        private static void RenderField(JObject component, ReportTemplateBindingContext ctx, StringBuilder html, string type)
        {
            var label = (string)(component["label"] ?? component["Label"]);
            var binding = (string)(component["binding"] ?? component["Binding"]);
            var prefix = (string)(component["prefix"] ?? component["Prefix"]) ?? string.Empty;
            var text = (string)(component["text"] ?? component["Text"]) ?? string.Empty;
            var value = string.IsNullOrWhiteSpace(binding) ? text : ctx.GetString(binding);
            var transform = ((string)(component["transform"] ?? component["Transform"]) ?? string.Empty).Trim();
            if (string.Equals(transform, "uppercase", StringComparison.OrdinalIgnoreCase))
            {
                value = (value ?? string.Empty).ToUpperInvariant();
            }

            var display = prefix + value;
            var className = Encode((string)(component["className"] ?? component["ClassName"]) ?? "rte-field");
            var multiline = component.Value<bool?>("multiline") == true;
            var strong = component.Value<bool?>("strong") == true
                         || string.Equals(ReadStyle(component, "fontWeight"), "bold", StringComparison.OrdinalIgnoreCase);
            var styleAttr = StyleAttr(component);

            if (!string.IsNullOrWhiteSpace(label))
            {
                html.Append("<div class=\"").Append(className).Append(" rte-labeled-field\"").Append(styleAttr).Append(">");
                html.Append("<span class=\"rte-label\">").Append(Encode(label)).Append("</span>");
                if (multiline)
                {
                    html.Append("<div class=\"rte-value\">").Append(Encode(display)).Append("</div>");
                }
                else
                {
                    html.Append("<span class=\"rte-value\">").Append(Encode(display)).Append("</span>");
                }
                html.Append("</div>");
            }
            else
            {
                html.Append("<div class=\"").Append(className).Append("\"").Append(styleAttr).Append(">");
                if (strong)
                {
                    html.Append("<strong>").Append(Encode(display)).Append("</strong>");
                }
                else
                {
                    html.Append(Encode(display));
                }
                html.Append("</div>");
            }
        }

        private static void RenderParameterTable(JObject component, ReportTemplateBindingContext ctx, StringBuilder html)
        {
            if (IsStyleHidden(component))
            {
                return;
            }

            var repeat = component["repeat"] as JObject;
            var source = repeat != null
                ? ((string)(repeat["source"] ?? repeat["Source"]) ?? "Parameters").Trim()
                : "Parameters";

            var columns = (component["columns"] as JArray) ?? new JArray();
            var rows = ctx.ResolveCollection(source).OfType<DiagnosticTestReportParameter>().ToList();

            html.Append("<table class=\"rte-parameter-table\"").Append(StyleAttr(component)).Append("><thead><tr>");
            foreach (var col in columns.OfType<JObject>())
            {
                var label = (string)(col["label"] ?? col["Label"]) ?? string.Empty;
                var width = col.Value<int?>("widthPct");
                var align = ((string)(col["align"] ?? col["Align"]) ?? string.Empty).Trim().ToLowerInvariant();
                html.Append("<th");
                var thStyle = new List<string>();
                if (width.HasValue)
                {
                    thStyle.Add("width:" + width.Value + "%");
                }
                if (align == "left" || align == "center" || align == "right")
                {
                    thStyle.Add("text-align:" + align);
                }
                if (thStyle.Count > 0)
                {
                    html.Append(" style=\"").Append(string.Join(";", thStyle)).Append("\"");
                }
                html.Append(">").Append(Encode(label)).Append("</th>");
            }
            html.Append("</tr></thead><tbody>");

            string lastSection = null;
            foreach (var param in rows)
            {
                if (!string.IsNullOrWhiteSpace(param.SectionName) &&
                    !string.Equals(param.SectionName, lastSection, StringComparison.OrdinalIgnoreCase))
                {
                    lastSection = param.SectionName;
                    html.Append("<tr class=\"rte-param-section\"><td colspan=\"")
                        .Append(columns.Count)
                        .Append("\">")
                        .Append(Encode(param.SectionName.ToUpperInvariant()))
                        .Append("</td></tr>");
                }

                var rowCtx = PushRepeatItem(ctx, "Parameter", param);
                var abnormal = param.IsAbnormal ? " rte-abnormal" : string.Empty;
                html.Append("<tr class=\"rte-param-row").Append(abnormal).Append("\">");
                foreach (var col in columns.OfType<JObject>())
                {
                    var binding = (string)(col["binding"] ?? col["Binding"]);
                    var cell = rowCtx.GetString(binding);
                    var showFlag = col.Value<bool?>("showFlag") == true;
                    var align = ((string)(col["align"] ?? col["Align"]) ?? string.Empty).Trim().ToLowerInvariant();
                    html.Append("<td");
                    if (align == "left" || align == "center" || align == "right")
                    {
                        html.Append(" style=\"text-align:").Append(align).Append("\"");
                    }
                    html.Append(">");
                    html.Append(Encode(cell));
                    if (showFlag && !string.IsNullOrWhiteSpace(param.Flag))
                    {
                        html.Append(" <span class=\"rte-flag\">").Append(Encode(param.Flag)).Append("</span>");
                    }
                    html.Append("</td>");
                }
                html.Append("</tr>");
            }

            html.Append("</tbody></table>");
        }

        private static void RenderSignature(JObject component, ReportTemplateBindingContext ctx, StringBuilder html,
            ReportLayoutConfigurationDto layout)
        {
            if (IsStyleHidden(component))
            {
                return;
            }

            var source = ((string)(component["source"] ?? component["Source"]) ?? "Doctor").Trim();
            var nameBinding = (string)(component["nameBinding"] ?? component["NameBinding"])
                ?? (string.Equals(source, "Technician", StringComparison.OrdinalIgnoreCase) ? "Technician.Name" : "Doctor.Name");
            var imageBinding = (string)(component["imageBinding"] ?? component["ImageBinding"])
                ?? (string.Equals(source, "Technician", StringComparison.OrdinalIgnoreCase) ? "Technician.SignatureImage" : "Doctor.SignatureImage");
            var metaBinding = (string)(component["metaBinding"] ?? component["MetaBinding"]);

            if (string.Equals(source, "Doctor", StringComparison.OrdinalIgnoreCase) && layout != null && !layout.DoctorSignatureEnabled)
            {
                return;
            }
            if (string.Equals(source, "Technician", StringComparison.OrdinalIgnoreCase) && layout != null && !layout.TechnicianSignatureEnabled)
            {
                return;
            }

            var name = ctx.GetString(nameBinding);
            var image = ctx.GetString(imageBinding);
            var meta = string.IsNullOrWhiteSpace(metaBinding) ? string.Empty : ctx.GetString(metaBinding);

            var width = string.Equals(source, "Technician", StringComparison.OrdinalIgnoreCase)
                ? (layout?.TechnicianSignatureWidthMm ?? 40)
                : (layout?.DoctorSignatureWidthMm ?? 40);
            var height = string.Equals(source, "Technician", StringComparison.OrdinalIgnoreCase)
                ? (layout?.TechnicianSignatureHeightMm ?? 20)
                : (layout?.DoctorSignatureHeightMm ?? 20);

            // Component style width/height (mm) override layout defaults when present.
            var styleWidth = ParseMm(ReadStyle(component, "width"));
            var styleHeight = ParseMm(ReadStyle(component, "height"));
            if (styleWidth.HasValue)
            {
                width = styleWidth.Value;
            }
            if (styleHeight.HasValue)
            {
                height = styleHeight.Value;
            }

            var align = string.Equals(source, "Technician", StringComparison.OrdinalIgnoreCase)
                ? (layout?.TechnicianSignatureHorizontal ?? "Left")
                : (layout?.DoctorSignatureHorizontal ?? "Right");
            var styleAlign = ReadStyle(component, "textAlign");
            if (!string.IsNullOrWhiteSpace(styleAlign))
            {
                align = styleAlign;
            }

            html.Append("<div class=\"rte-signature rte-sig-").Append(Encode(source.ToLowerInvariant()))
                .Append(" rte-align-").Append(Encode((align ?? "Right").ToLowerInvariant())).Append("\"")
                .Append(StyleAttr(component, omitKeys: new[] { "width", "height", "textAlign", "visibility" }))
                .Append(">");

            if (IsSafeDataImage(image))
            {
                html.Append("<img class=\"rte-signature-img\" alt=\"\" src=\"").Append(EncodeAttr(image))
                    .Append("\" style=\"width:")
                    .Append(width.ToString(CultureInfo.InvariantCulture))
                    .Append("mm;height:")
                    .Append(height.ToString(CultureInfo.InvariantCulture))
                    .Append("mm;\" />");
            }
            else if (!string.IsNullOrWhiteSpace(name))
            {
                html.Append("<div class=\"rte-sign-line\">_________________________</div>");
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                html.Append("<div class=\"rte-sig-name\">").Append(Encode(name)).Append("</div>");
            }
            if (!string.IsNullOrWhiteSpace(meta))
            {
                html.Append("<div class=\"rte-sig-meta\">").Append(Encode(meta)).Append("</div>");
            }
            html.Append("</div>");
        }

        private static void RenderImage(JObject component, ReportTemplateBindingContext ctx, StringBuilder html)
        {
            var src = (string)(component["src"] ?? component["Src"]);
            var binding = (string)(component["binding"] ?? component["Binding"]);
            if (string.IsNullOrWhiteSpace(src) && !string.IsNullOrWhiteSpace(binding))
            {
                src = ctx.GetString(binding);
            }

            if (!IsSafeDataImage(src) && !IsSafeRelativeImage(src))
            {
                return;
            }

            html.Append("<img class=\"rte-image\" alt=\"\" src=\"").Append(EncodeAttr(src)).Append("\" />");
        }

        private static bool EvaluateVisibleWhen(JObject condition, ReportTemplateBindingContext ctx)
        {
            if (condition == null)
            {
                return true;
            }

            var op = ((string)(condition["op"] ?? condition["Op"]) ?? string.Empty).Trim();
            var binding = (string)(condition["binding"] ?? condition["Binding"]);
            var expected = condition["value"] ?? condition["Value"];

            switch (op)
            {
                case ReportTemplateConditionOps.Exists:
                    return ctx.TryGetValue(binding, out var v) && v != null;
                case ReportTemplateConditionOps.NotEmpty:
                    return ctx.IsNotEmpty(binding);
                case ReportTemplateConditionOps.EqualsValue:
                    return string.Equals(ctx.GetString(binding), expected?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
                case ReportTemplateConditionOps.NotEquals:
                    return !string.Equals(ctx.GetString(binding), expected?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
                default:
                    return true;
            }
        }

        private static string BuildCss(ReportLayoutConfigurationDto layout)
        {
            var page = layout?.PageSize ?? "A4";
            var orient = layout?.Orientation ?? "Portrait";
            var header = (layout?.HeaderHeightMm ?? 50).ToString(CultureInfo.InvariantCulture);
            var footer = (layout?.FooterHeightMm ?? 50).ToString(CultureInfo.InvariantCulture);
            var left = (layout?.LeftMarginMm ?? 10).ToString(CultureInfo.InvariantCulture);
            var right = (layout?.RightMarginMm ?? 10).ToString(CultureInfo.InvariantCulture);

            return $@"
.rte-report {{ box-sizing:border-box; font-family: Arial, Helvetica, sans-serif; font-size: 11pt; color:#000; }}
.rte-header-band {{ height:{header}mm; }}
.rte-footer-band {{ height:{footer}mm; }}
.rte-body {{ margin-left:{left}mm; margin-right:{right}mm; }}
.rte-labeled-field {{ display:flex; gap:8px; margin:2px 0; }}
.rte-label {{ min-width:140px; font-weight:600; }}
.rte-parameter-table {{ width:100%; border-collapse:collapse; margin:8px 0 12px; }}
.rte-parameter-table th, .rte-parameter-table td {{ border-bottom:1px solid #ccc; padding:4px 6px; text-align:left; }}
.rte-param-section td {{ font-weight:700; background:#f5f5f5; }}
.rte-abnormal .rte-flag, .rte-abnormal td:nth-child(2) {{ font-weight:700; }}
.rte-signature {{ margin-top:18px; max-width:45%; }}
.rte-align-right {{ margin-left:auto; text-align:right; }}
.rte-align-left {{ margin-right:auto; text-align:left; }}
.rte-align-center {{ margin-left:auto; margin-right:auto; text-align:center; }}
.rte-signature-img {{ object-fit:contain; }}
.rte-department-header, .report-department-header {{ font-weight:700; margin:12px 0 6px; border-bottom:1px solid #000; }}
.rte-profile-header, .report-profile-header {{ font-weight:700; margin:12px 0 6px; }}
.rte-line {{ border:none; border-top:1px solid #999; margin:8px 0; }}
@media print {{
  @page {{ size: {page} {orient.ToLowerInvariant()}; }}
  .rte-report {{ page-break-inside:auto; }}
  .rte-parameter-table tr {{ page-break-inside:avoid; }}
}}
".Trim();
        }

        private static bool IsStyleHidden(JObject component)
        {
            var visibility = ReadStyle(component, "visibility");
            return string.Equals(visibility, "hidden", StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadStyle(JObject component, string key)
        {
            var style = component?["style"] as JObject ?? component?["Style"] as JObject;
            if (style == null || string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            return (string)(style[key] ?? style[char.ToUpperInvariant(key[0]) + key.Substring(1)]);
        }

        private static string StyleAttr(JObject component, string[] omitKeys = null)
        {
            var css = BuildSafeInlineStyle(component, omitKeys);
            if (string.IsNullOrWhiteSpace(css))
            {
                return string.Empty;
            }

            return " style=\"" + EncodeAttr(css) + "\"";
        }

        private static string BuildSafeInlineStyle(JObject component, string[] omitKeys = null)
        {
            var style = component?["style"] as JObject ?? component?["Style"] as JObject;
            if (style == null)
            {
                return string.Empty;
            }

            var omit = new HashSet<string>(omitKeys ?? new string[0], StringComparer.OrdinalIgnoreCase)
            {
                // Visibility is handled by skipping render, not CSS.
                "visibility"
            };

            var parts = new List<string>();
            foreach (var prop in style.Properties())
            {
                if (omit.Contains(prop.Name))
                {
                    continue;
                }

                var cssName = ToCssPropertyName(prop.Name);
                if (cssName == null)
                {
                    continue;
                }

                var raw = prop.Value?.ToString() ?? string.Empty;
                var safe = SanitizeStyleValue(prop.Name, raw);
                if (safe == null)
                {
                    continue;
                }

                parts.Add(cssName + ":" + safe);
            }

            return string.Join(";", parts);
        }

        private static string ToCssPropertyName(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            switch (key.Trim().ToLowerInvariant())
            {
                case "fontsize": return "font-size";
                case "fontweight": return "font-weight";
                case "fontstyle": return "font-style";
                case "fontfamily": return "font-family";
                case "textalign": return "text-align";
                case "color": return "color";
                case "width": return "width";
                case "height": return "height";
                case "margintop": return "margin-top";
                case "marginbottom": return "margin-bottom";
                case "margin": return "margin";
                case "padding": return "padding";
                case "border": return "border";
                case "borderbottom": return "border-bottom";
                case "bordertop": return "border-top";
                case "borderleft": return "border-left";
                case "borderright": return "border-right";
                case "whitespace": return "white-space";
                default: return null;
            }
        }

        private static readonly HashSet<string> AllowedFontFamilies =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Arial", "Helvetica", "Times New Roman", "Courier New", "sans-serif", "serif", "monospace"
            };

        private static string SanitizeStyleValue(string key, string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var value = raw.Trim();
            if (value.IndexOf('<') >= 0 || value.IndexOf('>') >= 0 || value.IndexOf('"') >= 0 ||
                value.IndexOf('\'') >= 0 || value.IndexOf("url(", StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf("expression", StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf("javascript", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return null;
            }

            switch (key.Trim().ToLowerInvariant())
            {
                case "fontfamily":
                    return AllowedFontFamilies.Contains(value) ? value : null;
                case "fontweight":
                    return (value == "normal" || value == "bold" || value == "400" || value == "700") ? value : null;
                case "fontstyle":
                    return (value == "normal" || value == "italic") ? value : null;
                case "textalign":
                    return (value == "left" || value == "center" || value == "right") ? value : null;
                case "visibility":
                    return (value == "visible" || value == "hidden") ? value : null;
                case "whitespace":
                    return (value == "normal" || value == "nowrap" || value == "pre-wrap") ? value : null;
                case "color":
                    return Regex.IsMatch(value, @"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$") ? value : null;
                case "fontsize":
                case "width":
                case "height":
                case "margintop":
                case "marginbottom":
                case "margin":
                case "padding":
                    return Regex.IsMatch(value, @"^\d+(\.\d+)?(px|mm|pt|%|em)?$") ? value : null;
                case "border":
                case "borderbottom":
                case "bordertop":
                case "borderleft":
                case "borderright":
                    if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase))
                    {
                        return "none";
                    }
                    return Regex.IsMatch(value, @"^\d+(\.\d+)?(px|mm)\s+solid\s+#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")
                        ? value
                        : null;
                default:
                    return null;
            }
        }

        private static decimal? ParseMm(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var v = value.Trim();
            if (v.EndsWith("mm", StringComparison.OrdinalIgnoreCase))
            {
                v = v.Substring(0, v.Length - 2).Trim();
            }

            if (decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out var mm) && mm >= 0 && mm <= 400)
            {
                return mm;
            }

            return null;
        }

        private static ReportLayoutConfigurationDto ExtractLayout(object data)
        {
            if (data is DiagnosticTestReportDto d)
            {
                return d.Layout;
            }
            if (data is DiagnosticRadiologyReportDto r)
            {
                return r.Layout;
            }
            return null;
        }

        private static void ApplyPageHints(JObject page, ReportLayoutConfigurationDto layout)
        {
            if (page == null || layout == null)
            {
                return;
            }

            // Global ReportLayoutConfiguration remains authoritative; page hints only fill blanks.
            if (string.IsNullOrWhiteSpace(layout.PageSize))
            {
                layout.PageSize = (string)(page["size"] ?? page["Size"]) ?? "A4";
            }
            if (string.IsNullOrWhiteSpace(layout.Orientation))
            {
                layout.Orientation = (string)(page["orientation"] ?? page["Orientation"]) ?? "Portrait";
            }
        }

        private static string NormalizeReportType(string reportType)
        {
            return string.Equals(reportType, ReportTemplateTypes.Radiology, StringComparison.OrdinalIgnoreCase)
                ? ReportTemplateTypes.Radiology
                : ReportTemplateTypes.Diagnostic;
        }

        private static string Encode(string value)
        {
            return WebUtility.HtmlEncode(value ?? string.Empty);
        }

        private static string EncodeAttr(string value)
        {
            return WebUtility.HtmlEncode(value ?? string.Empty);
        }

        private static bool IsSafeDataImage(string src)
        {
            if (string.IsNullOrWhiteSpace(src))
            {
                return false;
            }
            var s = src.Trim();
            return s.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)
                   && s.IndexOf("base64,", StringComparison.OrdinalIgnoreCase) > 0
                   && !s.Contains("<")
                   && s.IndexOf("javascript", StringComparison.OrdinalIgnoreCase) < 0;
        }

        private static bool IsSafeRelativeImage(string src)
        {
            if (string.IsNullOrWhiteSpace(src))
            {
                return false;
            }
            var s = src.Trim();
            if (s.StartsWith("http", StringComparison.OrdinalIgnoreCase) || s.StartsWith("//"))
            {
                return false;
            }
            return s.StartsWith("/") || s.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase);
        }
    }
}
