using LIS.DtoModel.Models;
using LIS.DtoModel.Models.Reports;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LIS.BusinessLogic.Reports
{
    /// <summary>
    /// Maps approved binding roots onto an already-assembled report DTO. No SQL / reflection execute.
    /// </summary>
    internal sealed class ReportTemplateBindingContext
    {
        private readonly Dictionary<string, object> _scopes =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public ReportTemplateBindingContext(object reportData)
        {
            if (reportData is DiagnosticTestReportDto diag)
            {
                SeedDiagnostic(diag);
            }
            else if (reportData is DiagnosticRadiologyReportDto rad)
            {
                SeedRadiology(rad);
            }
            else
            {
                throw new ArgumentException("Unsupported report data type for template binding.");
            }
        }

        public ReportTemplateBindingContext Push(string alias, object value)
        {
            var clone = new ReportTemplateBindingContext(this);
            if (!string.IsNullOrWhiteSpace(alias))
            {
                clone._scopes[alias.Trim()] = value;
            }
            return clone;
        }

        private ReportTemplateBindingContext(ReportTemplateBindingContext other)
        {
            foreach (var kv in other._scopes)
            {
                _scopes[kv.Key] = kv.Value;
            }
        }

        public bool TryGetValue(string path, out object value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var parts = path.Trim().Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return false;
            }

            if (!_scopes.TryGetValue(parts[0], out var current) || current == null)
            {
                return false;
            }

            for (var i = 1; i < parts.Length; i++)
            {
                current = ReadProperty(current, parts[i]);
                if (current == null && i < parts.Length - 1)
                {
                    return false;
                }
            }

            value = current;
            return true;
        }

        public string GetString(string path)
        {
            if (!TryGetValue(path, out var value) || value == null)
            {
                return string.Empty;
            }

            if (value is DateTime dt)
            {
                return dt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            }

            if (value is DateTime?)
            {
                var ndt = (DateTime?)value;
                return ndt.HasValue ? ndt.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) : string.Empty;
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        public bool IsNotEmpty(string path)
        {
            var s = GetString(path);
            return !string.IsNullOrWhiteSpace(s);
        }

        public IEnumerable<object> ResolveCollection(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return Enumerable.Empty<object>();
            }

            var key = source.Trim();
            if (TryGetValue(key, out var value) && value is IEnumerable enumerable && !(value is string))
            {
                return enumerable.Cast<object>();
            }

            // Top-level shorthand
            if (_scopes.TryGetValue(key, out var direct) && direct is IEnumerable e2 && !(direct is string))
            {
                return e2.Cast<object>();
            }

            return Enumerable.Empty<object>();
        }

        private void SeedDiagnostic(DiagnosticTestReportDto dto)
        {
            var h = dto.Header ?? new DiagnosticTestReportHeader();
            _scopes["Header"] = h;
            _scopes["Patient"] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Name"] = h.PatientName,
                ["PatientName"] = h.PatientName,
                ["Age"] = h.Age,
                ["Gender"] = h.Gender,
                ["AgeGender"] = h.Age + " / " + (h.Gender ?? string.Empty),
                ["MRNo"] = h.MRNo,
                ["PatientId"] = h.PatientId
            };
            _scopes["Order"] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["LabNo"] = h.LabNo,
                ["ReferralDoctor"] = h.ReferralDoctor,
                ["Corporate"] = h.Corporate,
                ["CollectionDate"] = h.CollectionDate,
                ["ReceivedDate"] = h.ReceivedDate,
                ["ReportDate"] = h.ReportDate,
                ["Status"] = h.Status
            };
            _scopes["Invoice"] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["InvoiceNo"] = h.InvoiceNo
            };
            _scopes["Visit"] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["VisitId"] = h.VisitId
            };
            _scopes["Doctor"] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Name"] = h.ApprovedByName ?? h.ApprovedBy,
                ["ApprovedBy"] = h.ApprovedBy,
                ["Qualification"] = h.ApprovedByQualification,
                ["Designation"] = h.ApprovedByDesignation,
                ["SignatureImage"] = h.ApprovedBySignatureImage,
                ["Signature"] = h.ApprovedBySignatureImage
            };
            _scopes["Technician"] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Name"] = h.ReviewedByName ?? h.ReviewedBy,
                ["ReviewedBy"] = h.ReviewedBy,
                ["Qualification"] = h.ReviewedByQualification,
                ["SignatureImage"] = h.ReviewedBySignatureImage,
                ["Signature"] = h.ReviewedBySignatureImage
            };
            _scopes["Branding"] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["LabName"] = h.LabName,
                ["CentreName"] = h.CentreName,
                ["Tagline"] = h.Tagline,
                ["LogoUrl"] = h.LogoUrl,
                ["LicenseName"] = h.LicenseName,
                ["Address"] = h.Address,
                ["Email"] = h.Email,
                ["ContactNumbers"] = h.ContactNumbers,
                ["PharmacyContact"] = h.PharmacyContact,
                ["AppointmentContact"] = h.AppointmentContact
            };
            _scopes["Layout"] = dto.Layout;
            _scopes["DepartmentGroups"] = dto.DepartmentGroups ?? new List<DiagnosticTestReportDepartmentGroup>();
            _scopes["ProfileGroups"] = dto.ProfileGroups ?? new List<DiagnosticTestReportProfileGroup>();
            _scopes["Sections"] = dto.Sections ?? new List<DiagnosticTestReportSection>();
        }

        private void SeedRadiology(DiagnosticRadiologyReportDto dto)
        {
            var h = dto.Header ?? new DiagnosticRadiologyReportHeader();
            _scopes["Header"] = h;
            _scopes["Patient"] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Name"] = h.PatientName,
                ["PatientName"] = h.PatientName,
                ["Age"] = h.Age,
                ["Gender"] = h.Gender,
                ["AgeGender"] = h.Age + " / " + (h.Gender ?? string.Empty),
                ["MRNo"] = h.MRNo,
                ["PatientId"] = h.PatientId
            };
            _scopes["Invoice"] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["InvoiceNo"] = h.InvoiceNo
            };
            _scopes["Visit"] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["VisitId"] = h.VisitId
            };
            _scopes["Accession"] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["AccessionNo"] = h.AccessionNo
            };
            _scopes["Test"] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["TestName"] = h.TestName,
                ["Name"] = h.TestName
            };
            _scopes["Radiology"] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Modality"] = h.Modality,
                ["Department"] = h.Department,
                ["ReportStatus"] = h.ReportStatus,
                ["ReportDate"] = h.ReportDate,
                ["ClinicalHistory"] = dto.ClinicalHistory,
                ["Findings"] = dto.Findings,
                ["Impression"] = dto.Impression,
                ["Recommendation"] = dto.Recommendation
            };
            _scopes["Doctor"] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Name"] = h.AuthorizedByName ?? h.AuthorizedBy,
                ["AuthorizedBy"] = h.AuthorizedBy,
                ["Designation"] = h.AuthorizedByDesignation,
                ["SignatureImage"] = h.AuthorizedBySignatureImage,
                ["Signature"] = h.AuthorizedBySignatureImage,
                ["DigitalSignature"] = h.DigitalSignature
            };
            _scopes["Layout"] = dto.Layout;
            _scopes["Technician"] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            _scopes["Order"] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            _scopes["Branding"] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            _scopes["DepartmentGroups"] = new List<object>();
            _scopes["ProfileGroups"] = new List<object>();
            _scopes["Sections"] = new List<object>();
        }

        private static object ReadProperty(object target, string name)
        {
            if (target == null)
            {
                return null;
            }

            if (target is IDictionary<string, object> dict)
            {
                return dict.TryGetValue(name, out var v) ? v : FindIgnoreCase(dict, name);
            }

            if (target is IDictionary nonGeneric)
            {
                foreach (DictionaryEntry entry in nonGeneric)
                {
                    if (string.Equals(Convert.ToString(entry.Key), name, StringComparison.OrdinalIgnoreCase))
                    {
                        return entry.Value;
                    }
                }
                return null;
            }

            // Whitelisted DTO property read (no arbitrary method invoke)
            var prop = target.GetType().GetProperty(name,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
            if (prop == null || !prop.CanRead || prop.GetIndexParameters().Length > 0)
            {
                return null;
            }

            return prop.GetValue(target, null);
        }

        private static object FindIgnoreCase(IDictionary<string, object> dict, string name)
        {
            foreach (var kv in dict)
            {
                if (string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase))
                {
                    return kv.Value;
                }
            }
            return null;
        }
    }
}
