using LIS.DtoModel.Models.Reports;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LIS.BusinessLogic.Reports
{
    /// <summary>
    /// Validates declarative template JSON. Rejects PHI payload shapes, scripts, SQL, unknown bindings/components.
    /// </summary>
    public static class ReportTemplateDefinitionValidator
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaxNestingDepth = 12;
        public const int MaxComponents = 500;
        public const decimal MaxDimensionMm = 400m;

        private static readonly HashSet<string> AllowedRenderers =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "builtin", "declarative" };

        private static readonly HashSet<string> AllowedBindingRoots =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Patient", "Order", "Invoice", "Visit", "Test", "Parameter", "Doctor", "Technician",
                "Accession", "Radiology", "Section", "Header", "Layout", "Branding", "Profile",
                // Repeat aliases used in compatibility templates
                "dept", "section", "group", "item", "row"
            };

        private static readonly HashSet<string> AllowedConditionOps =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ReportTemplateConditionOps.Exists,
                ReportTemplateConditionOps.NotEmpty,
                ReportTemplateConditionOps.EqualsValue,
                ReportTemplateConditionOps.NotEquals
            };

        private static readonly HashSet<string> AllowedRepeatSources =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ReportTemplateRepeatSources.DepartmentGroups,
                ReportTemplateRepeatSources.ProfileGroups,
                ReportTemplateRepeatSources.Sections,
                ReportTemplateRepeatSources.Parameters,
                "Both",
                "dept.Sections",
                "group.Sections",
                "section.Parameters",
                "section.parameters"
            };

        private static readonly Regex UnsafePattern = new Regex(
            @"<\s*(script|iframe|object|embed)\b|javascript\s*:|on\w+\s*=|\b(select|insert|update|delete|drop|exec|execute|union)\b\s+|\b(eval|function\s*\(|=>)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly HashSet<string> ForbiddenJsonKeys =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "patientName", "resultValue", "phone", "mrNo", "hisPatientId", "phi", "password", "access_token"
            };

        private static readonly HashSet<string> AllowedStyleKeys =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "fontSize", "fontWeight", "fontStyle", "fontFamily", "textAlign", "color",
                "width", "height", "marginTop", "marginBottom", "margin", "padding",
                "border", "borderBottom", "borderTop", "borderLeft", "borderRight",
                "whiteSpace", "visibility"
            };

        public static void Validate(string definitionJson, string expectedReportType)
        {
            if (string.IsNullOrWhiteSpace(definitionJson))
            {
                throw new ArgumentException("Template definition JSON is required.");
            }

            if (UnsafePattern.IsMatch(definitionJson))
            {
                throw new ArgumentException("Template definition contains unsafe or executable content.");
            }

            JObject root;
            try
            {
                root = JObject.Parse(definitionJson);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("Template definition is not valid JSON: " + ex.Message);
            }

            var schemaVersion = root.Value<int?>("schemaVersion") ?? root.Value<int?>("SchemaVersion");
            if (schemaVersion != CurrentSchemaVersion)
            {
                throw new ArgumentException("Unsupported template schemaVersion. Expected " + CurrentSchemaVersion + ".");
            }

            var reportType = (string)(root["reportType"] ?? root["ReportType"]);
            if (string.IsNullOrWhiteSpace(reportType))
            {
                throw new ArgumentException("Template definition must include reportType.");
            }

            reportType = reportType.Trim();
            if (!string.Equals(reportType, expectedReportType, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Template definition reportType does not match template ReportType.");
            }

            var renderer = (string)(root["renderer"] ?? root["Renderer"]) ?? "declarative";
            if (!AllowedRenderers.Contains(renderer.Trim()))
            {
                throw new ArgumentException("Template renderer must be 'builtin' or 'declarative'.");
            }

            RejectForbiddenKeys(root);
            ValidateBindings(root);
            ValidatePage(root["page"] as JObject ?? root["Page"] as JObject);

            if (string.Equals(renderer.Trim(), "declarative", StringComparison.OrdinalIgnoreCase))
            {
                var body = root["body"] as JObject ?? root["Body"] as JObject;
                var components = root["components"] as JArray ?? root["Components"] as JArray;
                if (body == null && (components == null || components.Count == 0))
                {
                    // Empty declarative body is allowed (Phase 1 stubs); renderer will emit empty content.
                }
                else
                {
                    var counter = 0;
                    if (body != null)
                    {
                        ValidateComponent(body, reportType, 0, ref counter);
                    }
                    else if (components != null)
                    {
                        foreach (var c in components.OfType<JObject>())
                        {
                            ValidateComponent(c, reportType, 0, ref counter);
                        }
                    }
                }
            }
        }

        public static string BuildBuiltinStub(string reportType, string builtInKey)
        {
            var type = string.Equals(reportType, ReportTemplateTypes.Radiology, StringComparison.OrdinalIgnoreCase)
                ? ReportTemplateTypes.Radiology
                : ReportTemplateTypes.Diagnostic;

            var obj = new JObject
            {
                ["schemaVersion"] = CurrentSchemaVersion,
                ["reportType"] = type,
                ["renderer"] = "builtin",
                ["builtInKey"] = builtInKey ?? (type == ReportTemplateTypes.Radiology
                    ? ReportTemplateBuiltInKeys.Radiology
                    : ReportTemplateBuiltInKeys.Diagnostic),
                ["layoutSource"] = "ReportLayoutConfiguration",
                ["page"] = new JObject
                {
                    ["size"] = "A4",
                    ["orientation"] = "Portrait",
                    ["useGlobalLayoutClearance"] = true,
                    ["note"] = "Stationery clearance remains in ReportLayoutConfiguration (global per report type)."
                },
                ["components"] = new JArray(),
                ["bindings"] = new JArray()
            };
            return obj.ToString(Formatting.None);
        }

        private static void ValidatePage(JObject page)
        {
            if (page == null)
            {
                return;
            }

            foreach (var key in new[] { "headerClearanceMm", "footerClearanceMm", "leftMarginMm", "rightMarginMm", "widthMm", "heightMm" })
            {
                var token = page[key];
                if (token == null)
                {
                    continue;
                }

                if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer)
                {
                    var val = token.Value<decimal>();
                    if (val < 0 || val > MaxDimensionMm)
                    {
                        throw new ArgumentException("Invalid page dimension: " + key);
                    }
                }
            }
        }

        private static void ValidateComponent(JObject component, string reportType, int depth, ref int counter)
        {
            if (component == null)
            {
                return;
            }

            if (depth > MaxNestingDepth)
            {
                throw new ArgumentException("Template component nesting exceeds maximum depth of " + MaxNestingDepth + ".");
            }

            counter++;
            if (counter > MaxComponents)
            {
                throw new ArgumentException("Template exceeds maximum component count of " + MaxComponents + ".");
            }

            var type = ((string)(component["type"] ?? component["Type"]) ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentException("Each component must include type.");
            }

            if (!ReportComponentRegistry.IsKnownType(type))
            {
                throw new ArgumentException("Unknown component type: " + type);
            }

            if (!ReportComponentRegistry.SupportsReportType(type, reportType))
            {
                throw new ArgumentException("Component type " + type + " is not valid for report type " + reportType + ".");
            }

            // Reject arbitrary HTML blobs
            if (component["html"] != null || component["Html"] != null || component["innerHtml"] != null)
            {
                throw new ArgumentException("Arbitrary HTML content is not allowed in template components.");
            }

            ValidateDimensions(component);
            ValidateStyle(component["style"] as JObject ?? component["Style"] as JObject);
            ValidateVisibleWhen(component["visibleWhen"] as JObject ?? component["VisibleWhen"] as JObject);
            ValidateRepeat(component["repeat"] as JObject ?? component["Repeat"] as JObject);

            if (string.Equals(type, ReportTemplateComponentTypes.ParameterTable, StringComparison.OrdinalIgnoreCase))
            {
                var columns = component["columns"] as JArray ?? component["Columns"] as JArray;
                if (columns == null || columns.Count == 0)
                {
                    throw new ArgumentException("PARAMETER_TABLE requires columns.");
                }

                foreach (var col in columns.OfType<JObject>())
                {
                    ValidateBindingPath((string)(col["binding"] ?? col["Binding"]));
                }
            }

            if (string.Equals(type, ReportTemplateComponentTypes.Image, StringComparison.OrdinalIgnoreCase))
            {
                var src = (string)(component["src"] ?? component["Src"]);
                if (!string.IsNullOrWhiteSpace(src) && !IsSafeImageSrc(src))
                {
                    throw new ArgumentException("IMAGE src must be a data:image URI or relative path without script.");
                }
            }

            var children = component["children"] as JArray ?? component["Children"] as JArray;
            if (children != null)
            {
                foreach (var child in children.OfType<JObject>())
                {
                    ValidateComponent(child, reportType, depth + 1, ref counter);
                }
            }
        }

        private static void ValidateDimensions(JObject component)
        {
            foreach (var key in new[] { "width", "height", "widthMm", "heightMm", "x", "y" })
            {
                var token = component[key];
                if (token == null)
                {
                    continue;
                }

                if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
                {
                    var val = token.Value<decimal>();
                    if (val < 0 || val > MaxDimensionMm)
                    {
                        throw new ArgumentException("Invalid component dimension: " + key);
                    }
                }
            }
        }

        private static void ValidateStyle(JObject style)
        {
            if (style == null)
            {
                return;
            }

            foreach (var prop in style.Properties())
            {
                if (!AllowedStyleKeys.Contains(prop.Name))
                {
                    throw new ArgumentException("Unsupported style property: " + prop.Name);
                }

                var text = prop.Value?.ToString() ?? string.Empty;
                if (UnsafePattern.IsMatch(text) || text.IndexOf("expression", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    text.IndexOf("url(", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    throw new ArgumentException("Unsafe style value rejected: " + prop.Name);
                }
            }
        }

        private static void ValidateVisibleWhen(JObject condition)
        {
            if (condition == null)
            {
                return;
            }

            var op = ((string)(condition["op"] ?? condition["Op"]) ?? string.Empty).Trim();
            if (!AllowedConditionOps.Contains(op))
            {
                throw new ArgumentException("Unsupported condition op: " + op);
            }

            ValidateBindingPath((string)(condition["binding"] ?? condition["Binding"]));
            var value = condition["value"] ?? condition["Value"];
            if (value != null && value.Type == JTokenType.String && UnsafePattern.IsMatch(value.ToString()))
            {
                throw new ArgumentException("Unsafe condition value rejected.");
            }
        }

        private static void ValidateRepeat(JObject repeat)
        {
            if (repeat == null)
            {
                return;
            }

            var source = ((string)(repeat["source"] ?? repeat["Source"]) ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentException("repeat.source is required.");
            }

            if (!AllowedRepeatSources.Contains(source) && !IsAliasedRepeatSource(source))
            {
                throw new ArgumentException("Unsupported repeat source: " + source);
            }

            var alias = ((string)(repeat["alias"] ?? repeat["Alias"]) ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(alias) && !Regex.IsMatch(alias, @"^[A-Za-z][A-Za-z0-9_]*$"))
            {
                throw new ArgumentException("Invalid repeat alias.");
            }
        }

        private static bool IsAliasedRepeatSource(string source)
        {
            // alias.Property form e.g. dept.Sections
            var parts = source.Split('.');
            if (parts.Length != 2)
            {
                return false;
            }

            return AllowedBindingRoots.Contains(parts[0]) &&
                   (parts[1].Equals("Sections", StringComparison.OrdinalIgnoreCase) ||
                    parts[1].Equals("Parameters", StringComparison.OrdinalIgnoreCase) ||
                    parts[1].Equals("ProfileGroups", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsSafeImageSrc(string src)
        {
            var s = src.Trim();
            if (UnsafePattern.IsMatch(s))
            {
                return false;
            }

            if (s.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                return !s.Contains("<") && s.IndexOf("base64,", StringComparison.OrdinalIgnoreCase) > 0;
            }

            if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                s.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                s.StartsWith("//", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return s.StartsWith("/") || s.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase);
        }

        private static void RejectForbiddenKeys(JToken token)
        {
            if (token is JObject obj)
            {
                foreach (var prop in obj.Properties())
                {
                    if (ForbiddenJsonKeys.Contains(prop.Name))
                    {
                        throw new ArgumentException("Template definition must not store clinical/PHI field payloads: " + prop.Name);
                    }
                    RejectForbiddenKeys(prop.Value);
                }
            }
            else if (token is JArray arr)
            {
                foreach (var item in arr)
                {
                    RejectForbiddenKeys(item);
                }
            }
        }

        private static void ValidateBindings(JToken token)
        {
            if (token is JObject obj)
            {
                foreach (var prop in obj.Properties())
                {
                    if (prop.Name.Equals("binding", StringComparison.OrdinalIgnoreCase) ||
                        prop.Name.Equals("dataPath", StringComparison.OrdinalIgnoreCase) ||
                        prop.Name.Equals("path", StringComparison.OrdinalIgnoreCase) ||
                        prop.Name.Equals("nameBinding", StringComparison.OrdinalIgnoreCase) ||
                        prop.Name.Equals("imageBinding", StringComparison.OrdinalIgnoreCase) ||
                        prop.Name.Equals("metaBinding", StringComparison.OrdinalIgnoreCase))
                    {
                        ValidateBindingPath(prop.Value?.ToString());
                    }
                    ValidateBindings(prop.Value);
                }
            }
            else if (token is JArray arr)
            {
                foreach (var item in arr)
                {
                    ValidateBindings(item);
                }
            }
        }

        private static void ValidateBindingPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var trimmed = path.Trim();
            if (UnsafePattern.IsMatch(trimmed) || trimmed.Contains("'") || trimmed.Contains(";") || trimmed.Contains("("))
            {
                throw new ArgumentException("Unsafe binding path rejected: " + trimmed);
            }

            var root = trimmed.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(root) || !AllowedBindingRoots.Contains(root))
            {
                throw new ArgumentException("Unknown or disallowed binding root: " + trimmed);
            }

            foreach (var part in trimmed.Split('.'))
            {
                if (!Regex.IsMatch(part, @"^[A-Za-z][A-Za-z0-9_]*$"))
                {
                    throw new ArgumentException("Invalid binding path segment: " + trimmed);
                }
            }
        }
    }
}
