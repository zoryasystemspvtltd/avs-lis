using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models.Reports;
using LIS.Logger;
using System;
using System.Globalization;
using System.Linq;

namespace LIS.BusinessLogic
{
    public class ReportLayoutConfigurationManager : IReportLayoutConfigurationManager
    {
        private const decimal PageHeightMm = 297m;
        private const decimal PageWidthMm = 210m;
        private const decimal MinClearanceMm = 0m;
        private const decimal MaxClearanceMm = 120m;
        private const decimal MinMarginMm = 0m;
        private const decimal MaxMarginMm = 50m;
        private const decimal MinSignatureMm = 5m;
        private const decimal MaxSignatureMm = 80m;

        private readonly ModuleRepo<ReportLayoutConfiguration> repo;
        private readonly IModuleIdentity identity;

        public ReportLayoutConfigurationManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork unitOfWork)
        {
            repo = new ModuleRepo<ReportLayoutConfiguration>(logger, identity, unitOfWork);
            this.identity = identity;
        }

        public ReportLayoutConfigurationDto GetByReportType(string reportType)
        {
            var type = NormalizeReportType(reportType);
            var row = repo.Get(r => r.ReportType == type && r.IsActive)
                .OrderByDescending(r => r.Id)
                .FirstOrDefault();
            return row == null ? GetDefaults(type) : ToDto(row);
        }

        public ReportLayoutConfigurationDto GetDefaults(string reportType)
        {
            var type = NormalizeReportType(reportType);
            if (type == ReportLayoutReportTypes.Radiology)
            {
                return BuildRadiologyDefaults();
            }

            return BuildDiagnosticDefaults();
        }

        public ReportLayoutConfigurationDto Save(ReportLayoutConfigurationDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            var type = NormalizeReportType(dto.ReportType);
            Validate(dto, type);

            var now = DateTime.Now;
            var user = identity?.ActivityMember ?? "system";
            var existing = repo.Get(r => r.ReportType == type)
                .OrderByDescending(r => r.Id)
                .FirstOrDefault();

            if (existing == null)
            {
                existing = new ReportLayoutConfiguration
                {
                    ReportType = type,
                    CreatedOn = now,
                    CreatedBy = user
                };
            }

            ApplyDto(existing, dto, type);
            existing.IsActive = true;
            existing.ModifiedOn = now;
            existing.ModifiedBy = user;

            if (existing.Id > 0)
            {
                repo.Update(existing);
            }
            else
            {
                repo.Add(existing);
            }

            return ToDto(existing);
        }

        public ReportLayoutConfigurationDto ResetToDefault(string reportType)
        {
            var defaults = GetDefaults(reportType);
            defaults.Id = 0;
            return Save(defaults);
        }

        private void Validate(ReportLayoutConfigurationDto dto, string type)
        {
            dto.HeaderHeightMm = Clamp(dto.HeaderHeightMm, MinClearanceMm, MaxClearanceMm, "Header Clearance");
            dto.FooterHeightMm = Clamp(dto.FooterHeightMm, MinClearanceMm, MaxClearanceMm, "Footer Clearance");
            dto.LeftMarginMm = Clamp(dto.LeftMarginMm, MinMarginMm, MaxMarginMm, "Left Margin");
            dto.RightMarginMm = Clamp(dto.RightMarginMm, MinMarginMm, MaxMarginMm, "Right Margin");

            if (dto.DoctorSignatureEnabled)
            {
                dto.DoctorSignatureWidthMm = Clamp(dto.DoctorSignatureWidthMm, MinSignatureMm, MaxSignatureMm, "Doctor Signature Width");
                dto.DoctorSignatureHeightMm = Clamp(dto.DoctorSignatureHeightMm, MinSignatureMm, MaxSignatureMm, "Doctor Signature Height");
            }

            if (dto.TechnicianSignatureEnabled)
            {
                dto.TechnicianSignatureWidthMm = Clamp(dto.TechnicianSignatureWidthMm, MinSignatureMm, MaxSignatureMm, "Technician Signature Width");
                dto.TechnicianSignatureHeightMm = Clamp(dto.TechnicianSignatureHeightMm, MinSignatureMm, MaxSignatureMm, "Technician Signature Height");
            }

            var contentHeight = PageHeightMm - dto.HeaderHeightMm - dto.FooterHeightMm;
            if (contentHeight < 80m)
            {
                throw new InvalidOperationException(
                    "Header Clearance + Footer Clearance leave insufficient content height on A4 (need at least 80 mm).");
            }

            var contentWidth = PageWidthMm - dto.LeftMarginMm - dto.RightMarginMm;
            if (contentWidth < 60m)
            {
                throw new InvalidOperationException(
                    "Left Margin + Right Margin leave insufficient content width on A4 (need at least 60 mm).");
            }

            dto.DoctorSignatureHorizontal = NormalizeHorizontal(dto.DoctorSignatureHorizontal, type == ReportLayoutReportTypes.Radiology ? "Left" : "Right");
            dto.DoctorSignatureVertical = "Bottom";
            dto.TechnicianSignatureHorizontal = NormalizeHorizontal(dto.TechnicianSignatureHorizontal, "Left");
            dto.TechnicianSignatureVertical = "Bottom";
            dto.PageSize = "A4";
            dto.Orientation = "Portrait";
            dto.ReportType = type;
        }

        private static decimal Clamp(decimal value, decimal min, decimal max, string label)
        {
            if (value < min || value > max)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture,
                        "{0} must be between {1} and {2} mm.", label, min, max));
            }

            return value;
        }

        private static string NormalizeReportType(string reportType)
        {
            if (string.IsNullOrWhiteSpace(reportType))
            {
                throw new InvalidOperationException("Report Type is required.");
            }

            var trimmed = reportType.Trim();
            if (trimmed.Equals(ReportLayoutReportTypes.Diagnostic, StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("Laboratory", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("Lab", StringComparison.OrdinalIgnoreCase))
            {
                return ReportLayoutReportTypes.Diagnostic;
            }

            if (trimmed.Equals(ReportLayoutReportTypes.Radiology, StringComparison.OrdinalIgnoreCase))
            {
                return ReportLayoutReportTypes.Radiology;
            }

            throw new InvalidOperationException("Report Type must be Diagnostic or Radiology.");
        }

        private static string NormalizeHorizontal(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            var v = value.Trim();
            if (v.Equals("Left", StringComparison.OrdinalIgnoreCase))
            {
                return "Left";
            }

            if (v.Equals("Center", StringComparison.OrdinalIgnoreCase)
                || v.Equals("Centre", StringComparison.OrdinalIgnoreCase))
            {
                return "Center";
            }

            if (v.Equals("Right", StringComparison.OrdinalIgnoreCase))
            {
                return "Right";
            }

            return fallback;
        }

        private static void ApplyDto(ReportLayoutConfiguration entity, ReportLayoutConfigurationDto dto, string type)
        {
            entity.ReportType = type;
            entity.PageSize = dto.PageSize;
            entity.Orientation = dto.Orientation;
            entity.HeaderHeightMm = dto.HeaderHeightMm;
            entity.FooterHeightMm = dto.FooterHeightMm;
            entity.LeftMarginMm = dto.LeftMarginMm;
            entity.RightMarginMm = dto.RightMarginMm;
            entity.DoctorSignatureEnabled = dto.DoctorSignatureEnabled;
            entity.DoctorSignatureHorizontal = dto.DoctorSignatureHorizontal;
            entity.DoctorSignatureVertical = dto.DoctorSignatureVertical;
            entity.DoctorSignatureWidthMm = dto.DoctorSignatureWidthMm;
            entity.DoctorSignatureHeightMm = dto.DoctorSignatureHeightMm;
            entity.TechnicianSignatureEnabled = dto.TechnicianSignatureEnabled;
            entity.TechnicianSignatureHorizontal = dto.TechnicianSignatureHorizontal ?? "Left";
            entity.TechnicianSignatureVertical = dto.TechnicianSignatureVertical ?? "Bottom";
            entity.TechnicianSignatureWidthMm = dto.TechnicianSignatureWidthMm > 0 ? dto.TechnicianSignatureWidthMm : 50m;
            entity.TechnicianSignatureHeightMm = dto.TechnicianSignatureHeightMm > 0 ? dto.TechnicianSignatureHeightMm : 14m;
        }

        private static ReportLayoutConfigurationDto BuildDiagnosticDefaults()
        {
            // Matches current production: @page 5cm/5cm + 12mm sides; doctor signature right, 50×14 mm.
            return new ReportLayoutConfigurationDto
            {
                ReportType = ReportLayoutReportTypes.Diagnostic,
                PageSize = "A4",
                Orientation = "Portrait",
                HeaderHeightMm = 50m,
                FooterHeightMm = 50m,
                LeftMarginMm = 12m,
                RightMarginMm = 12m,
                DoctorSignatureEnabled = true,
                DoctorSignatureHorizontal = "Right",
                DoctorSignatureVertical = "Bottom",
                DoctorSignatureWidthMm = 50m,
                DoctorSignatureHeightMm = 14m,
                TechnicianSignatureEnabled = false,
                TechnicianSignatureHorizontal = "Left",
                TechnicianSignatureVertical = "Bottom",
                TechnicianSignatureWidthMm = 50m,
                TechnicianSignatureHeightMm = 14m,
                IsActive = true
            };
        }

        private static ReportLayoutConfigurationDto BuildRadiologyDefaults()
        {
            // Matches current production: ~4cm header, 5cm footer gap, 10mm sides; doctor signature left, 50×14 mm.
            return new ReportLayoutConfigurationDto
            {
                ReportType = ReportLayoutReportTypes.Radiology,
                PageSize = "A4",
                Orientation = "Portrait",
                HeaderHeightMm = 40m,
                FooterHeightMm = 50m,
                LeftMarginMm = 10m,
                RightMarginMm = 10m,
                DoctorSignatureEnabled = true,
                DoctorSignatureHorizontal = "Left",
                DoctorSignatureVertical = "Bottom",
                DoctorSignatureWidthMm = 50m,
                DoctorSignatureHeightMm = 14m,
                TechnicianSignatureEnabled = false,
                TechnicianSignatureHorizontal = "Left",
                TechnicianSignatureVertical = "Bottom",
                TechnicianSignatureWidthMm = 50m,
                TechnicianSignatureHeightMm = 14m,
                IsActive = true
            };
        }

        private static ReportLayoutConfigurationDto ToDto(ReportLayoutConfiguration entity)
        {
            return new ReportLayoutConfigurationDto
            {
                Id = entity.Id,
                ReportType = entity.ReportType,
                PageSize = entity.PageSize,
                Orientation = entity.Orientation,
                HeaderHeightMm = entity.HeaderHeightMm,
                FooterHeightMm = entity.FooterHeightMm,
                LeftMarginMm = entity.LeftMarginMm,
                RightMarginMm = entity.RightMarginMm,
                DoctorSignatureEnabled = entity.DoctorSignatureEnabled,
                DoctorSignatureHorizontal = entity.DoctorSignatureHorizontal,
                DoctorSignatureVertical = entity.DoctorSignatureVertical,
                DoctorSignatureWidthMm = entity.DoctorSignatureWidthMm,
                DoctorSignatureHeightMm = entity.DoctorSignatureHeightMm,
                TechnicianSignatureEnabled = entity.TechnicianSignatureEnabled,
                TechnicianSignatureHorizontal = entity.TechnicianSignatureHorizontal,
                TechnicianSignatureVertical = entity.TechnicianSignatureVertical,
                TechnicianSignatureWidthMm = entity.TechnicianSignatureWidthMm,
                TechnicianSignatureHeightMm = entity.TechnicianSignatureHeightMm,
                IsActive = entity.IsActive
            };
        }
    }
}
