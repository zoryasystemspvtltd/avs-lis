using LIS.BusinessLogic.Reports;
using LIS.DtoModel.Models;
using LIS.DtoModel.Models.Reports;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LIS.BusinessLogic
{
    /// <summary>
    /// Phase 3 designer operations (Admin). Hides versioning; Save/Activate/Deactivate are the business verbs.
    /// Production declarative print flag remains OFF.
    /// </summary>
    public partial class ReportTemplateManager
    {
        public ReportTemplateModeDto GetMode(string reportType)
        {
            var type = NormalizeReportType(reportType);
            EnsureModeRow(type);
            var row = modeRepo.Get(type);
            return new ReportTemplateModeDto
            {
                ReportType = type,
                Mode = row?.Mode ?? ReportTemplateModes.SystemDefault
            };
        }

        public ReportTemplateModeDto SetMode(string reportType, string mode)
        {
            var type = NormalizeReportType(reportType);
            var normalized = NormalizeMode(mode);
            EnsureModeRow(type);
            var row = modeRepo.Get(type);
            row.Mode = normalized;
            row.ModifiedBy = User();
            row.ModifiedOn = Now();
            modeRepo.Update(row);
            // Switching modes MUST preserve custom templates (do not delete/deactivate).
            return new ReportTemplateModeDto { ReportType = type, Mode = normalized };
        }

        public ReportTemplateDesignerListDto GetDesignerWorkspace(string reportType)
        {
            EnsureSystemDefaults();
            var type = NormalizeReportType(reportType);
            var mode = GetMode(type).Mode;
            var templates = templateRepo.Get(t => t.IsActive && t.ReportType == type).ToList();
            var assignments = assignmentRepo.Get(a => a.IsActive && a.ReportType == type).ToList();

            var systemDefault = templates.FirstOrDefault(t => t.IsSystemDefault);
            var generics = templates
                .Where(t => !t.IsSystemDefault &&
                            string.Equals(t.TemplateCategory, ReportTemplateCategories.CustomGeneric, StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.Name)
                .Select(t => ToDesignerItem(t, assignments, mode))
                .ToList();
            var specifics = templates
                .Where(t => string.Equals(t.TemplateCategory, ReportTemplateCategories.Specific, StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.Name)
                .Select(t => ToDesignerItem(t, assignments, mode))
                .ToList();

            return new ReportTemplateDesignerListDto
            {
                ReportType = type,
                Mode = mode,
                SystemDefault = systemDefault == null ? null : ToDesignerItem(systemDefault, assignments, mode),
                ActiveGeneric = generics.FirstOrDefault(g => g.IsActivated),
                CustomGenerics = generics,
                SpecificTemplates = specifics
            };
        }

        public ReportTemplateDesignerItemDto GetSystemDefaultTemplate(string reportType)
        {
            EnsureSystemDefaults();
            var type = NormalizeReportType(reportType);
            var t = templateRepo.Get(x => x.IsActive && x.ReportType == type && x.IsSystemDefault).FirstOrDefault();
            if (t == null)
            {
                throw new ArgumentException("System Default template not found.");
            }

            // Factory definition for designer view/create-from: conceptual declarative twin (read-only).
            var item = ToDesignerItem(t, assignmentRepo.Get(a => a.ReportType == type).ToList(), GetMode(type).Mode);
            item.DefinitionJson = ReportTemplateCompatibilityDefinitions.BuildForReportType(type);
            item.IsLocked = true;
            item.CanEdit = false;
            return item;
        }

        public ReportTemplateDesignerItemDto CreateCustomTemplate(ReportTemplateDesignerCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Template name is required.");
            }

            var type = NormalizeReportType(request.ReportType);
            var category = NormalizeCategory(request.TemplateCategory);
            if (category == ReportTemplateCategories.SystemDefault)
            {
                throw new ArgumentException("Cannot create System Default templates. They are factory-provided.");
            }

            EnsureNameUnique(type, request.Name.Trim(), null);

            string definition;
            var createFrom = string.IsNullOrWhiteSpace(request.CreateFrom)
                ? ReportTemplateCreateFrom.SystemDefault
                : request.CreateFrom.Trim();

            if (string.Equals(createFrom, ReportTemplateCreateFrom.Blank, StringComparison.OrdinalIgnoreCase))
            {
                definition = BlankDefinition(type);
            }
            else
            {
                definition = ReportTemplateCompatibilityDefinitions.BuildForReportType(type);
            }

            ReportTemplateDefinitionValidator.Validate(definition, type);

            int? targetTestId = null;
            int? targetProfileId = null;
            if (category == ReportTemplateCategories.Specific)
            {
                if (!(request.TargetTestId.HasValue ^ request.TargetProfileId.HasValue))
                {
                    throw new ArgumentException("Test/Profile Specific templates require exactly one Test or Profile target.");
                }

                targetTestId = request.TargetTestId;
                targetProfileId = request.TargetProfileId;
                if (targetTestId.HasValue && targetTestId.Value <= 0)
                {
                    throw new ArgumentException("Invalid Test target.");
                }
                if (targetProfileId.HasValue && targetProfileId.Value <= 0)
                {
                    throw new ArgumentException("Invalid Profile target.");
                }
            }
            else if (request.TargetTestId.HasValue || request.TargetProfileId.HasValue)
            {
                throw new ArgumentException("Custom Generic templates must not specify a Test/Profile target.");
            }

            var now = Now();
            var user = User();
            var template = new ReportTemplate
            {
                Name = request.Name.Trim(),
                ReportType = type,
                Description = category == ReportTemplateCategories.Specific
                    ? "Custom Test/Profile Specific template"
                    : "Custom Generic template",
                BuiltInRendererKey = null,
                IsSystemDefault = false,
                IsActive = true,
                TemplateCategory = category,
                TargetTestId = targetTestId,
                TargetProfileId = targetProfileId,
                CreatedBy = user,
                CreatedOn = now,
                ModifiedBy = user,
                ModifiedOn = now
            };
            templateRepo.Add(template);
            template = templateRepo.Get(t => t.ReportType == type && t.Name == template.Name)
                .OrderByDescending(t => t.Id).First();

            var version = new ReportTemplateVersion
            {
                TemplateId = template.Id,
                VersionNumber = 1,
                Status = ReportTemplateVersionStatuses.Draft,
                DefinitionJson = definition,
                SchemaVersion = ReportTemplateDefinitionValidator.CurrentSchemaVersion,
                CreatedBy = user,
                CreatedOn = now,
                ModifiedBy = user,
                ModifiedOn = now
            };
            versionRepo.Add(version);

            // SAVE does not activate.
            return ToDesignerItem(template, new List<ReportTemplateAssignment>(), GetMode(type).Mode);
        }

        public ReportTemplateDesignerItemDto SaveDesign(ReportTemplateDesignerSaveRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var template = RequireTemplate(request.TemplateId);
            if (template.IsSystemDefault)
            {
                throw new InvalidOperationException("System Default is factory-provided and cannot be edited.");
            }

            if (!string.IsNullOrWhiteSpace(request.Name) &&
                !string.Equals(request.Name.Trim(), template.Name, StringComparison.OrdinalIgnoreCase))
            {
                EnsureNameUnique(template.ReportType, request.Name.Trim(), template.Id);
                template.Name = request.Name.Trim();
            }

            if (string.IsNullOrWhiteSpace(request.DefinitionJson))
            {
                throw new ArgumentException("Definition is required.");
            }

            ReportTemplateDefinitionValidator.Validate(request.DefinitionJson, template.ReportType);

            var draft = versionRepo.Get(v =>
                v.TemplateId == template.Id &&
                v.Status == ReportTemplateVersionStatuses.Draft)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefault();

            if (draft == null)
            {
                // Published exists → create new draft (internal versioning; not user-facing).
                CreateDraftFromPublished(template.Id);
                draft = versionRepo.Get(v =>
                    v.TemplateId == template.Id &&
                    v.Status == ReportTemplateVersionStatuses.Draft)
                    .OrderByDescending(v => v.VersionNumber)
                    .First();
            }

            SaveDraft(new ReportTemplateSaveDraftRequest
            {
                TemplateId = template.Id,
                VersionId = draft.Id,
                DefinitionJson = request.DefinitionJson
            });

            template.ModifiedBy = User();
            template.ModifiedOn = Now();
            templateRepo.Update(template);

            var assignments = assignmentRepo.Get(a => a.ReportType == template.ReportType).ToList();
            return ToDesignerItem(RequireTemplate(template.Id), assignments, GetMode(template.ReportType).Mode);
        }

        public ReportTemplateValidateResultDto ValidateDesign(string reportType, string definitionJson)
        {
            var result = new ReportTemplateValidateResultDto
            {
                Errors = new List<string>(),
                Warnings = new List<string>()
            };

            try
            {
                var type = NormalizeReportType(reportType);
                ReportTemplateDefinitionValidator.Validate(definitionJson, type);

                // Soft warnings: builtin renderer pointer, empty body
                if (!string.IsNullOrWhiteSpace(definitionJson) &&
                    definitionJson.IndexOf("\"renderer\":\"builtin\"", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.Warnings.Add("This definition points at the built-in report format. Custom activation should use a designed declarative layout.");
                }

                result.IsValid = result.Errors.Count == 0;
            }
            catch (ArgumentException ex)
            {
                result.IsValid = false;
                result.Errors.Add(ex.Message);
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add("Template validation failed.");
                logger?.LogException(ex);
            }

            return result;
        }

        public ReportTemplateDesignerItemDto ActivateTemplate(int templateId)
        {
            if (ReportTemplateEngineFeatureFlags.UseDeclarativeRendererForProductionPrint)
            {
                throw new InvalidOperationException("Production declarative print flag must remain false in Phase 3.");
            }

            var template = RequireTemplate(templateId);
            if (template.IsSystemDefault)
            {
                throw new InvalidOperationException("System Default is already the factory active format. Switch Template Mode to System Default instead.");
            }

            var type = template.ReportType;
            var category = template.TemplateCategory ?? ReportTemplateCategories.CustomGeneric;

            // Ensure a Published version exists (activate publishes current draft if needed).
            EnsurePublishedForActivation(template);

            var validation = ValidateDesign(type, GetWorkingDefinition(template.Id));
            if (!validation.IsValid)
            {
                throw new ArgumentException(
                    "Template cannot be activated because it contains validation errors: " +
                    string.Join(" ", validation.Errors ?? new List<string>()));
            }

            var now = Now();
            var user = User();

            if (string.Equals(category, ReportTemplateCategories.CustomGeneric, StringComparison.OrdinalIgnoreCase))
            {
                // Only one active Generic: deactivate other System-scope custom generics for this type.
                foreach (var prior in assignmentRepo.Get(a =>
                    a.IsActive &&
                    a.ReportType == type &&
                    a.ScopeType == ReportTemplateScopeTypes.System).ToList())
                {
                    prior.IsActive = false;
                    prior.ModifiedBy = user;
                    prior.ModifiedOn = now;
                    assignmentRepo.Update(prior);
                }

                UpsertAssignment(new ReportTemplateAssignmentRequest
                {
                    ReportType = type,
                    ScopeType = ReportTemplateScopeTypes.System,
                    TemplateId = template.Id
                });
            }
            else if (string.Equals(category, ReportTemplateCategories.Specific, StringComparison.OrdinalIgnoreCase))
            {
                if (template.TargetTestId.HasValue)
                {
                    foreach (var prior in assignmentRepo.Get(a =>
                        a.IsActive &&
                        a.ReportType == type &&
                        a.ScopeType == ReportTemplateScopeTypes.Test &&
                        a.TestId == template.TargetTestId.Value).ToList())
                    {
                        if (prior.TemplateId != template.Id)
                        {
                            prior.IsActive = false;
                            prior.ModifiedBy = user;
                            prior.ModifiedOn = now;
                            assignmentRepo.Update(prior);
                        }
                    }

                    UpsertAssignment(new ReportTemplateAssignmentRequest
                    {
                        ReportType = type,
                        ScopeType = ReportTemplateScopeTypes.Test,
                        TestId = template.TargetTestId,
                        TemplateId = template.Id
                    });
                }
                else if (template.TargetProfileId.HasValue)
                {
                    foreach (var prior in assignmentRepo.Get(a =>
                        a.IsActive &&
                        a.ReportType == type &&
                        a.ScopeType == ReportTemplateScopeTypesPhase3.Profile &&
                        a.ProfileId == template.TargetProfileId.Value).ToList())
                    {
                        if (prior.TemplateId != template.Id)
                        {
                            prior.IsActive = false;
                            prior.ModifiedBy = user;
                            prior.ModifiedOn = now;
                            assignmentRepo.Update(prior);
                        }
                    }

                    UpsertProfileAssignment(type, template.TargetProfileId.Value, template.Id);
                }
                else
                {
                    throw new ArgumentException("Specific template is missing Test/Profile target.");
                }
            }
            else
            {
                throw new ArgumentException("Unknown template category.");
            }

            // Activation does not flip Template Mode automatically — Admin must be in Custom mode for runtime use.
            var assignments = assignmentRepo.Get(a => a.ReportType == type).ToList();
            return ToDesignerItem(RequireTemplate(template.Id), assignments, GetMode(type).Mode);
        }

        public ReportTemplateDesignerItemDto DeactivateTemplate(int templateId)
        {
            var template = RequireTemplate(templateId);
            if (template.IsSystemDefault)
            {
                throw new InvalidOperationException("System Default cannot be deactivated. Switch Template Mode if needed.");
            }

            var now = Now();
            var user = User();
            foreach (var row in assignmentRepo.Get(a => a.IsActive && a.TemplateId == template.Id).ToList())
            {
                row.IsActive = false;
                row.ModifiedBy = user;
                row.ModifiedOn = now;
                assignmentRepo.Update(row);
            }

            var assignments = assignmentRepo.Get(a => a.ReportType == template.ReportType).ToList();
            return ToDesignerItem(RequireTemplate(template.Id), assignments, GetMode(template.ReportType).Mode);
        }

        public IList<ReportDesignFieldDto> GetDesignFields(string reportType = null)
        {
            return ReportDesignFieldCatalog.List(reportType);
        }

        public IList<ReportTemplateTargetOptionDto> ListTestProfileTargets(string search = null)
        {
            var results = new List<ReportTemplateTargetOptionDto>();
            var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

            try
            {
                var tests = unitOfWork.GetRepoInstance<HisTestMaster>().Search(t => t.IsActive);
                if (term != null)
                {
                    tests = tests.Where(t =>
                        (t.HISTestCode != null && t.HISTestCode.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (t.HISTestCodeDescription != null && t.HISTestCodeDescription.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0));
                }

                foreach (var t in tests.OrderBy(t => t.HISTestCodeDescription).Take(200).ToList())
                {
                    results.Add(new ReportTemplateTargetOptionDto
                    {
                        TargetType = "Test",
                        Id = t.Id,
                        Code = t.HISTestCode,
                        Name = t.HISTestCodeDescription,
                        Label = (t.HISTestCode ?? "") + " — " + (t.HISTestCodeDescription ?? "")
                    });
                }
            }
            catch (Exception ex)
            {
                logger?.LogException(ex);
            }

            try
            {
                var profiles = unitOfWork.GetRepoInstance<TestProfileMaster>().Search(t => t.IsActive);
                if (term != null)
                {
                    profiles = profiles.Where(t =>
                        (t.Code != null && t.Code.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (t.Name != null && t.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0));
                }

                foreach (var p in profiles.OrderBy(p => p.Name).Take(200).ToList())
                {
                    results.Add(new ReportTemplateTargetOptionDto
                    {
                        TargetType = "Profile",
                        Id = p.Id,
                        Code = p.Code,
                        Name = p.Name,
                        Label = "Profile: " + (p.Code ?? "") + " — " + (p.Name ?? "")
                    });
                }
            }
            catch (Exception ex)
            {
                logger?.LogException(ex);
            }

            return results;
        }

        private void UpsertProfileAssignment(string reportType, int profileId, int templateId)
        {
            var existing = assignmentRepo.Get(a =>
                a.ReportType == reportType &&
                a.ScopeType == ReportTemplateScopeTypesPhase3.Profile &&
                a.ProfileId == profileId).FirstOrDefault();

            var now = Now();
            var user = User();
            if (existing == null)
            {
                assignmentRepo.Add(new ReportTemplateAssignment
                {
                    ReportType = reportType,
                    ScopeType = ReportTemplateScopeTypesPhase3.Profile,
                    ProfileId = profileId,
                    TemplateId = templateId,
                    IsActive = true,
                    CreatedBy = user,
                    CreatedOn = now,
                    ModifiedBy = user,
                    ModifiedOn = now
                });
            }
            else
            {
                existing.TemplateId = templateId;
                existing.IsActive = true;
                existing.ModifiedBy = user;
                existing.ModifiedOn = now;
                assignmentRepo.Update(existing);
            }
        }

        private void EnsurePublishedForActivation(ReportTemplate template)
        {
            var published = versionRepo.Get(v =>
                v.TemplateId == template.Id &&
                v.Status == ReportTemplateVersionStatuses.Published).FirstOrDefault();
            if (published != null)
            {
                return;
            }

            var draft = versionRepo.Get(v =>
                v.TemplateId == template.Id &&
                v.Status == ReportTemplateVersionStatuses.Draft)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefault();
            if (draft == null)
            {
                throw new ArgumentException("Template has no saved design to activate.");
            }

            Publish(draft.Id);
        }

        private string GetWorkingDefinition(int templateId)
        {
            var draft = versionRepo.Get(v =>
                v.TemplateId == templateId &&
                v.Status == ReportTemplateVersionStatuses.Draft)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefault();
            if (draft != null)
            {
                return draft.DefinitionJson;
            }

            var published = versionRepo.Get(v =>
                v.TemplateId == templateId &&
                v.Status == ReportTemplateVersionStatuses.Published)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefault();
            return published?.DefinitionJson;
        }

        private ReportTemplateDesignerItemDto ToDesignerItem(ReportTemplate t, IList<ReportTemplateAssignment> assignments, string mode)
        {
            var isActivated = assignments.Any(a => a.IsActive && a.TemplateId == t.Id);
            var locked = t.IsSystemDefault;
            return new ReportTemplateDesignerItemDto
            {
                Id = t.Id,
                Name = t.Name,
                ReportType = t.ReportType,
                TemplateCategory = t.IsSystemDefault
                    ? ReportTemplateCategories.SystemDefault
                    : (t.TemplateCategory ?? ReportTemplateCategories.CustomGeneric),
                IsSystemDefault = t.IsSystemDefault,
                IsLocked = locked,
                IsActivated = isActivated,
                TargetTestId = t.TargetTestId,
                TargetProfileId = t.TargetProfileId,
                TargetLabel = BuildTargetLabel(t),
                DefinitionJson = GetWorkingDefinition(t.Id),
                CanEdit = !locked,
                CanActivate = !locked && !isActivated,
                CanDeactivate = !locked && isActivated,
                ModifiedBy = t.ModifiedBy,
                ModifiedOn = t.ModifiedOn
            };
        }

        private string BuildTargetLabel(ReportTemplate t)
        {
            if (t.TargetTestId.HasValue)
            {
                return "Test #" + t.TargetTestId.Value;
            }
            if (t.TargetProfileId.HasValue)
            {
                return "Profile #" + t.TargetProfileId.Value;
            }
            return null;
        }

        private void EnsureModeRow(string reportType)
        {
            var existing = modeRepo.Get(reportType);
            if (existing != null)
            {
                return;
            }

            modeRepo.Add(new ReportTemplateModeSetting
            {
                ReportType = reportType,
                Mode = ReportTemplateModes.SystemDefault,
                ModifiedBy = User() ?? "system",
                ModifiedOn = Now()
            });
        }

        private void EnsureNameUnique(string reportType, string name, int? excludeId)
        {
            var clash = templateRepo.Get(t =>
                t.IsActive &&
                t.ReportType == reportType &&
                t.Name == name &&
                (!excludeId.HasValue || t.Id != excludeId.Value)).Any();
            if (clash)
            {
                throw new ArgumentException("A template with this name already exists for the report type.");
            }
        }

        private static string NormalizeMode(string mode)
        {
            if (string.Equals(mode, ReportTemplateModes.Custom, StringComparison.OrdinalIgnoreCase))
            {
                return ReportTemplateModes.Custom;
            }
            if (string.Equals(mode, ReportTemplateModes.SystemDefault, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mode, "System Default", StringComparison.OrdinalIgnoreCase))
            {
                return ReportTemplateModes.SystemDefault;
            }
            throw new ArgumentException("Template Mode must be System Default or Custom.");
        }

        private static string NormalizeCategory(string category)
        {
            if (string.Equals(category, ReportTemplateCategories.Specific, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(category, "Test/Profile Specific", StringComparison.OrdinalIgnoreCase))
            {
                return ReportTemplateCategories.Specific;
            }
            if (string.Equals(category, ReportTemplateCategories.CustomGeneric, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(category, "Custom Generic", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(category))
            {
                return ReportTemplateCategories.CustomGeneric;
            }
            throw new ArgumentException("Template Type must be Custom Generic or Test/Profile Specific.");
        }

        private static string BlankDefinition(string reportType)
        {
            var type = string.Equals(reportType, ReportTemplateTypes.Radiology, StringComparison.OrdinalIgnoreCase)
                ? ReportTemplateTypes.Radiology
                : ReportTemplateTypes.Diagnostic;
            return "{\"schemaVersion\":1,\"reportType\":\"" + type +
                   "\",\"renderer\":\"declarative\",\"layoutSource\":\"ReportLayoutConfiguration\"," +
                   "\"page\":{\"size\":\"A4\",\"orientation\":\"Portrait\",\"useGlobalLayoutClearance\":true}," +
                   "\"body\":{\"type\":\"SECTION\",\"id\":\"root\",\"children\":[]}}";
        }
    }
}
