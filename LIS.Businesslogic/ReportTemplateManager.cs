using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models.Reports;
using LIS.BusinessLogic.Reports;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LIS.BusinessLogic
{
    public partial class ReportTemplateManager : IReportTemplateManager, IReportTemplateResolver, IReportViewer
    {
        private readonly ModuleRepo<ReportTemplate> templateRepo;
        private readonly ModuleRepo<ReportTemplateVersion> versionRepo;
        private readonly ModuleRepo<ReportTemplateAssignment> assignmentRepo;
        private readonly ModuleRepo<ReportTemplateModeSetting> modeRepo;
        private readonly IModuleIdentity identity;
        private readonly IReportRenderer renderer;
        private readonly GenericUnitOfWork unitOfWork;
        private readonly ILogger logger;

        /// <summary>
        /// Single public constructor required by SimpleInjector (one-constructor rule).
        /// Tests and DI must supply <see cref="IReportRenderer"/> explicitly.
        /// </summary>
        public ReportTemplateManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork unitOfWork, IReportRenderer renderer)
        {
            this.logger = logger;
            this.unitOfWork = unitOfWork;
            templateRepo = new ModuleRepo<ReportTemplate>(logger, identity, unitOfWork);
            versionRepo = new ModuleRepo<ReportTemplateVersion>(logger, identity, unitOfWork);
            assignmentRepo = new ModuleRepo<ReportTemplateAssignment>(logger, identity, unitOfWork);
            modeRepo = new ModuleRepo<ReportTemplateModeSetting>(logger, identity, unitOfWork);
            this.identity = identity;
            this.renderer = renderer ?? new DeclarativeReportRenderer();
        }

        public void EnsureSystemDefaults()
        {
            EnsureBuiltin(ReportTemplateTypes.Diagnostic, ReportTemplateBuiltInKeys.Diagnostic, "Built-in Diagnostic Report");
            EnsureBuiltin(ReportTemplateTypes.Radiology, ReportTemplateBuiltInKeys.Radiology, "Built-in Radiology Report");
        }

        public IList<ReportTemplateDto> List(string reportType = null)
        {
            EnsureSystemDefaults();
            var q = templateRepo.Get(t => t.IsActive);
            if (!string.IsNullOrWhiteSpace(reportType))
            {
                var type = NormalizeReportType(reportType);
                q = q.Where(t => t.ReportType == type);
            }

            return q.OrderBy(t => t.ReportType).ThenBy(t => t.Name).ToList().Select(ToTemplateDto).ToList();
        }

        public ReportTemplateDto Get(int templateId)
        {
            var t = templateRepo.Get(templateId);
            if (t == null || !t.IsActive)
            {
                throw new ArgumentException("Template not found.");
            }

            return ToTemplateDto(t);
        }

        public ReportTemplateDto Create(ReportTemplateCreateRequest request)
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
            var now = Now();
            var user = User();

            var builtIn = string.IsNullOrWhiteSpace(request.BuiltInRendererKey)
                ? null
                : request.BuiltInRendererKey.Trim();

            string definition;
            if (!string.IsNullOrWhiteSpace(request.DefinitionJson))
            {
                definition = request.DefinitionJson;
            }
            else if (builtIn != null)
            {
                definition = Reports.ReportTemplateDefinitionValidator.BuildBuiltinStub(type, builtIn);
            }
            else
            {
                definition = Reports.ReportTemplateDefinitionValidator.BuildBuiltinStub(type, null)
                    .Replace("\"renderer\":\"builtin\"", "\"renderer\":\"declarative\"");
            }

            Reports.ReportTemplateDefinitionValidator.Validate(definition, type);

            if (request.IsSystemDefault)
            {
                ClearSystemDefaultFlag(type);
            }

            var template = new ReportTemplate
            {
                Name = request.Name.Trim(),
                ReportType = type,
                Description = request.Description?.Trim(),
                BuiltInRendererKey = builtIn,
                IsSystemDefault = request.IsSystemDefault,
                IsActive = true,
                CreatedBy = user,
                CreatedOn = now,
                ModifiedBy = user,
                ModifiedOn = now
            };
            var newId = (int)templateRepo.Add(template);
            if (template.Id <= 0)
            {
                template.Id = newId;
            }

            var version = new ReportTemplateVersion
            {
                TemplateId = template.Id,
                VersionNumber = 1,
                Status = ReportTemplateVersionStatuses.Draft,
                DefinitionJson = definition,
                SchemaVersion = Reports.ReportTemplateDefinitionValidator.CurrentSchemaVersion,
                CreatedBy = user,
                CreatedOn = now,
                ModifiedBy = user,
                ModifiedOn = now
            };
            versionRepo.Add(version);

            return ToTemplateDto(template);
        }

        public ReportTemplateVersionDto GetVersion(int versionId)
        {
            var v = versionRepo.Get(versionId);
            if (v == null)
            {
                throw new ArgumentException("Template version not found.");
            }

            return ToVersionDto(v);
        }

        public IList<ReportTemplateVersionDto> ListVersions(int templateId)
        {
            return versionRepo.Get(v => v.TemplateId == templateId)
                .OrderByDescending(v => v.VersionNumber)
                .ToList()
                .Select(ToVersionDto)
                .ToList();
        }

        public ReportTemplateVersionDto SaveDraft(ReportTemplateSaveDraftRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var template = RequireTemplate(request.TemplateId);
            ReportTemplateVersion version;

            if (request.VersionId.HasValue && request.VersionId.Value > 0)
            {
                version = versionRepo.Get(request.VersionId.Value);
                if (version == null || version.TemplateId != template.Id)
                {
                    throw new ArgumentException("Draft version not found for template.");
                }

                if (!string.Equals(version.Status, ReportTemplateVersionStatuses.Draft, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Only Draft versions can be edited. Create a new draft from the published version.");
                }
            }
            else
            {
                version = versionRepo.Get(v => v.TemplateId == template.Id && v.Status == ReportTemplateVersionStatuses.Draft)
                    .OrderByDescending(v => v.VersionNumber)
                    .FirstOrDefault();
                if (version == null)
                {
                    throw new InvalidOperationException("No draft version exists. Create a draft from the published version first.");
                }
            }

            Reports.ReportTemplateDefinitionValidator.Validate(request.DefinitionJson, template.ReportType);
            version.DefinitionJson = request.DefinitionJson;
            version.SchemaVersion = Reports.ReportTemplateDefinitionValidator.CurrentSchemaVersion;
            version.ModifiedBy = User();
            version.ModifiedOn = Now();
            versionRepo.Update(version);

            template.ModifiedBy = User();
            template.ModifiedOn = Now();
            templateRepo.Update(template);

            return ToVersionDto(version);
        }

        public ReportTemplateVersionDto CreateDraftFromPublished(int templateId)
        {
            var template = RequireTemplate(templateId);
            var published = versionRepo.Get(v => v.TemplateId == templateId && v.Status == ReportTemplateVersionStatuses.Published)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefault();
            if (published == null)
            {
                throw new InvalidOperationException("No published version exists to create a draft from.");
            }

            var existingDraft = versionRepo.Get(v => v.TemplateId == templateId && v.Status == ReportTemplateVersionStatuses.Draft)
                .FirstOrDefault();
            if (existingDraft != null)
            {
                throw new InvalidOperationException("A draft version already exists for this template.");
            }

            var next = versionRepo.Get(v => v.TemplateId == templateId).Select(v => v.VersionNumber).DefaultIfEmpty(0).Max() + 1;
            var now = Now();
            var user = User();
            var draft = new ReportTemplateVersion
            {
                TemplateId = templateId,
                VersionNumber = next,
                Status = ReportTemplateVersionStatuses.Draft,
                DefinitionJson = published.DefinitionJson,
                SchemaVersion = published.SchemaVersion,
                CreatedBy = user,
                CreatedOn = now,
                ModifiedBy = user,
                ModifiedOn = now
            };
            versionRepo.Add(draft);
            return ToVersionDto(draft);
        }

        public ReportTemplateVersionDto Publish(int versionId)
        {
            var version = versionRepo.Get(versionId);
            if (version == null)
            {
                throw new ArgumentException("Template version not found.");
            }

            if (!string.Equals(version.Status, ReportTemplateVersionStatuses.Draft, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only Draft versions can be published.");
            }

            var template = RequireTemplate(version.TemplateId);
            Reports.ReportTemplateDefinitionValidator.Validate(version.DefinitionJson, template.ReportType);

            var now = Now();
            var user = User();

            foreach (var prior in versionRepo.Get(v =>
                v.TemplateId == version.TemplateId &&
                v.Status == ReportTemplateVersionStatuses.Published &&
                v.Id != version.Id).ToList())
            {
                prior.Status = ReportTemplateVersionStatuses.Archived;
                prior.ArchivedBy = user;
                prior.ArchivedOn = now;
                prior.ModifiedBy = user;
                prior.ModifiedOn = now;
                versionRepo.Update(prior);
            }

            version.Status = ReportTemplateVersionStatuses.Published;
            version.PublishedBy = user;
            version.PublishedOn = now;
            version.ModifiedBy = user;
            version.ModifiedOn = now;
            versionRepo.Update(version);

            template.ModifiedBy = user;
            template.ModifiedOn = now;
            templateRepo.Update(template);

            return ToVersionDto(version);
        }

        public ReportTemplateVersionDto Archive(int versionId)
        {
            var version = versionRepo.Get(versionId);
            if (version == null)
            {
                throw new ArgumentException("Template version not found.");
            }

            if (string.Equals(version.Status, ReportTemplateVersionStatuses.Archived, StringComparison.OrdinalIgnoreCase))
            {
                return ToVersionDto(version);
            }

            var now = Now();
            var user = User();
            version.Status = ReportTemplateVersionStatuses.Archived;
            version.ArchivedBy = user;
            version.ArchivedOn = now;
            version.ModifiedBy = user;
            version.ModifiedOn = now;
            versionRepo.Update(version);
            return ToVersionDto(version);
        }

        public ReportTemplateAssignmentDto UpsertAssignment(ReportTemplateAssignmentRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var type = NormalizeReportType(request.ReportType);
            var scope = NormalizeScope(request.ScopeType);
            var template = RequireTemplate(request.TemplateId);
            if (!string.Equals(template.ReportType, type, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Assignment ReportType must match the template ReportType.");
            }

            string dept = null;
            int? testId = null;
            if (scope == ReportTemplateScopeTypes.Department)
            {
                if (string.IsNullOrWhiteSpace(request.DepartmentCode))
                {
                    throw new ArgumentException("DepartmentCode is required for Department scope.");
                }

                dept = request.DepartmentCode.Trim();
            }
            else if (scope == ReportTemplateScopeTypes.Test)
            {
                if (!request.TestId.HasValue || request.TestId.Value <= 0)
                {
                    throw new ArgumentException("TestId is required for Test scope.");
                }

                testId = request.TestId.Value;
            }
            else
            {
                // System
                dept = null;
                testId = null;
            }

            // Conflict detection at same scope key
            var conflicts = assignmentRepo.Get(a =>
                a.IsActive &&
                a.ReportType == type &&
                a.ScopeType == scope).ToList();

            if (scope == ReportTemplateScopeTypes.System)
            {
                conflicts = conflicts.Where(a => a.TemplateId != template.Id).ToList();
            }
            else if (scope == ReportTemplateScopeTypes.Department)
            {
                conflicts = conflicts.Where(a =>
                    string.Equals(a.DepartmentCode, dept, StringComparison.OrdinalIgnoreCase) &&
                    a.TemplateId != template.Id).ToList();
            }
            else
            {
                conflicts = conflicts.Where(a => a.TestId == testId && a.TemplateId != template.Id).ToList();
            }

            if (conflicts.Any())
            {
                throw new InvalidOperationException(
                    "Conflicting active template assignment already exists for this scope. Deactivate it before assigning another.");
            }

            var existing = assignmentRepo.Get(a =>
                a.ReportType == type &&
                a.ScopeType == scope &&
                a.TemplateId == template.Id).ToList();

            ReportTemplateAssignment row = null;
            if (scope == ReportTemplateScopeTypes.System)
            {
                row = existing.FirstOrDefault(a => string.IsNullOrWhiteSpace(a.DepartmentCode) && !a.TestId.HasValue)
                      ?? assignmentRepo.Get(a => a.ReportType == type && a.ScopeType == scope && a.IsActive).FirstOrDefault();
            }
            else if (scope == ReportTemplateScopeTypes.Department)
            {
                row = assignmentRepo.Get(a =>
                    a.ReportType == type &&
                    a.ScopeType == scope &&
                    a.DepartmentCode == dept).FirstOrDefault();
            }
            else
            {
                row = assignmentRepo.Get(a =>
                    a.ReportType == type &&
                    a.ScopeType == scope &&
                    a.TestId == testId).FirstOrDefault();
            }

            var now = Now();
            var user = User();
            if (row == null)
            {
                row = new ReportTemplateAssignment
                {
                    ReportType = type,
                    ScopeType = scope,
                    DepartmentCode = dept,
                    TestId = testId,
                    TemplateId = template.Id,
                    IsActive = true,
                    CreatedBy = user,
                    CreatedOn = now,
                    ModifiedBy = user,
                    ModifiedOn = now
                };
                assignmentRepo.Add(row);
            }
            else
            {
                // For system scope, replace active system assignment target
                if (scope == ReportTemplateScopeTypes.System)
                {
                    foreach (var other in assignmentRepo.Get(a =>
                        a.IsActive && a.ReportType == type && a.ScopeType == scope && a.Id != row.Id).ToList())
                    {
                        other.IsActive = false;
                        other.ModifiedBy = user;
                        other.ModifiedOn = now;
                        assignmentRepo.Update(other);
                    }
                }

                row.TemplateId = template.Id;
                row.DepartmentCode = dept;
                row.TestId = testId;
                row.IsActive = true;
                row.ModifiedBy = user;
                row.ModifiedOn = now;
                assignmentRepo.Update(row);
            }

            return ToAssignmentDto(row, template.Name);
        }

        public IList<ReportTemplateAssignmentDto> ListAssignments(string reportType = null)
        {
            var q = assignmentRepo.Get(a => a.IsActive);
            if (!string.IsNullOrWhiteSpace(reportType))
            {
                var type = NormalizeReportType(reportType);
                q = q.Where(a => a.ReportType == type);
            }

            var list = q.OrderBy(a => a.ReportType).ThenBy(a => a.ScopeType).ToList();
            var templates = templateRepo.Get().ToDictionary(t => t.Id, t => t.Name);
            return list.Select(a => ToAssignmentDto(a, templates.ContainsKey(a.TemplateId) ? templates[a.TemplateId] : null)).ToList();
        }

        public void DeactivateAssignment(int assignmentId)
        {
            var row = assignmentRepo.Get(assignmentId);
            if (row == null)
            {
                throw new ArgumentException("Assignment not found.");
            }

            row.IsActive = false;
            row.ModifiedBy = User();
            row.ModifiedOn = Now();
            assignmentRepo.Update(row);
        }

        public ReportTemplateResolveResultDto Resolve(ReportTemplateResolveRequest request)
        {
            EnsureSystemDefaults();
            if (request == null || string.IsNullOrWhiteSpace(request.ReportType))
            {
                throw new ArgumentException("ReportType is required for template resolution.");
            }

            var type = NormalizeReportType(request.ReportType);
            var mode = GetMode(type).Mode;

            // Phase 3: System Default mode always uses factory System Default (customs remain saved).
            if (string.Equals(mode, ReportTemplateModes.SystemDefault, StringComparison.OrdinalIgnoreCase))
            {
                return ResolveSystemDefaultOnly(type);
            }

            // Custom mode: Test/Profile Specific → Active Custom Generic → System Default.
            // Department scope is legacy Phase 1 and is skipped in Phase 3 Custom mode.

            if (request.TestId.HasValue && request.TestId.Value > 0)
            {
                var testAssign = assignmentRepo.Get(a =>
                    a.IsActive &&
                    a.ReportType == type &&
                    a.ScopeType == ReportTemplateScopeTypes.Test &&
                    a.TestId == request.TestId.Value).ToList();

                if (testAssign.Count > 1)
                {
                    throw new InvalidOperationException("Another active template already exists for this Test/Profile.");
                }

                if (testAssign.Count == 1)
                {
                    return BuildResolveResult(testAssign[0], ReportTemplateScopeTypes.Test);
                }
            }

            if (request.ProfileId.HasValue && request.ProfileId.Value > 0)
            {
                var profileAssign = assignmentRepo.Get(a =>
                    a.IsActive &&
                    a.ReportType == type &&
                    a.ScopeType == ReportTemplateScopeTypesPhase3.Profile &&
                    a.ProfileId == request.ProfileId.Value).ToList();

                if (profileAssign.Count > 1)
                {
                    throw new InvalidOperationException("Another active template already exists for this Test/Profile.");
                }

                if (profileAssign.Count == 1)
                {
                    return BuildResolveResult(profileAssign[0], ReportTemplateScopeTypesPhase3.Profile);
                }
            }

            var systemAssign = assignmentRepo.Get(a =>
                a.IsActive &&
                a.ReportType == type &&
                a.ScopeType == ReportTemplateScopeTypes.System).ToList();

            if (systemAssign.Count > 1)
            {
                throw new InvalidOperationException("Another Custom Generic template is already active for this report type.");
            }

            if (systemAssign.Count == 1)
            {
                var tpl = templateRepo.Get(systemAssign[0].TemplateId);
                if (tpl != null && tpl.IsActive && !tpl.IsSystemDefault &&
                    string.Equals(tpl.TemplateCategory, ReportTemplateCategories.CustomGeneric, StringComparison.OrdinalIgnoreCase))
                {
                    return BuildResolveResult(systemAssign[0], "CustomGeneric");
                }
            }

            return ResolveSystemDefaultOnly(type);
        }

        private ReportTemplateResolveResultDto ResolveSystemDefaultOnly(string type)
        {
            var systemDefault = templateRepo.Get(t =>
                t.IsActive && t.ReportType == type && t.IsSystemDefault).OrderBy(t => t.Id).FirstOrDefault();
            if (systemDefault != null)
            {
                return BuildResolveFromTemplate(systemDefault, ReportTemplateModes.SystemDefault);
            }

            return new ReportTemplateResolveResultDto
            {
                Found = false,
                ReportType = type,
                ResolutionSource = "None",
                Message = "No template resolved. Existing hard-coded report remains the production renderer."
            };
        }

        public IList<ReportComponentDescriptorDto> GetComponentRegistry(string reportType = null)
        {
            return ReportComponentRegistry.List(reportType);
        }

        public ReportRenderResultDto PreviewSample(string reportType)
        {
            // Admin preview remains available regardless of production print flag (Phase 4).
            var type = NormalizeReportType(reportType);
            var definition = ReportTemplateCompatibilityDefinitions.BuildForReportType(type);
            return renderer.Render(new ReportRenderRequestDto
            {
                ReportType = type,
                DefinitionJson = definition,
                ReportData = ReportTemplateSampleDataFactory.CreateSample(type),
                AllowDraftDefinition = true
            });
        }

        public ReportRenderResultDto PreviewVersion(int versionId)
        {
            // Admin preview remains available regardless of production print flag (Phase 4).
            var version = versionRepo.Get(versionId);
            if (version == null)
            {
                throw new ArgumentException("Template version not found.");
            }

            if (version.Status == ReportTemplateVersionStatuses.Archived)
            {
                throw new ArgumentException("Archived template versions are not available for preview.");
            }

            // Draft and Published allowed for Admin preview only (API is QAuthorize-protected).
            if (version.Status != ReportTemplateVersionStatuses.Draft &&
                version.Status != ReportTemplateVersionStatuses.Published)
            {
                throw new ArgumentException("Only Draft or Published versions can be previewed.");
            }

            var template = RequireTemplate(version.TemplateId);
            var definition = version.DefinitionJson;
            var usesBuiltin = definition != null &&
                definition.IndexOf("\"renderer\":\"builtin\"", StringComparison.OrdinalIgnoreCase) >= 0;

            if (usesBuiltin || !string.IsNullOrWhiteSpace(template.BuiltInRendererKey) &&
                (definition == null || definition.IndexOf("\"renderer\":\"declarative\"", StringComparison.OrdinalIgnoreCase) < 0))
            {
                // Built-in pointer: show conceptual declarative twin for Admin comparison, labeled clearly.
                var conceptual = ReportTemplateCompatibilityDefinitions.BuildForReportType(template.ReportType);
                var result = renderer.Render(new ReportRenderRequestDto
                {
                    ReportType = template.ReportType,
                    DefinitionJson = conceptual,
                    ReportData = ReportTemplateSampleDataFactory.CreateSample(template.ReportType),
                    AllowDraftDefinition = true
                });
                result.UsesBuiltInRenderer = true;
                result.Message = "Version targets built-in Angular renderer. Showing conceptual declarative twin with sample data for Admin comparison only. Production print is unchanged.";
                return result;
            }

            return renderer.Render(new ReportRenderRequestDto
            {
                ReportType = template.ReportType,
                DefinitionJson = definition,
                ReportData = ReportTemplateSampleDataFactory.CreateSample(template.ReportType),
                AllowDraftDefinition = version.Status == ReportTemplateVersionStatuses.Draft
            });
        }

        public ReportRenderResultDto PreviewDefinition(string reportType, string definitionJson)
        {
            // Admin preview remains available regardless of production print flag (Phase 4).
            var type = NormalizeReportType(reportType);
            return renderer.Render(new ReportRenderRequestDto
            {
                ReportType = type,
                DefinitionJson = definitionJson,
                ReportData = ReportTemplateSampleDataFactory.CreateSample(type),
                AllowDraftDefinition = true
            });
        }

        private ReportTemplateResolveResultDto BuildResolveResult(ReportTemplateAssignment assignment, string source)
        {
            var template = templateRepo.Get(assignment.TemplateId);
            if (template == null || !template.IsActive)
            {
                throw new InvalidOperationException("Assigned template is missing or inactive.");
            }

            if (!string.Equals(template.ReportType, assignment.ReportType, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Template ReportType mismatch for assignment.");
            }

            return BuildResolveFromTemplate(template, source);
        }

        private ReportTemplateResolveResultDto BuildResolveFromTemplate(ReportTemplate template, string source)
        {
            var published = versionRepo.Get(v =>
                v.TemplateId == template.Id &&
                v.Status == ReportTemplateVersionStatuses.Published)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefault();

            var usesBuiltin = !string.IsNullOrWhiteSpace(template.BuiltInRendererKey);
            return new ReportTemplateResolveResultDto
            {
                Found = true,
                ResolutionSource = source,
                TemplateId = template.Id,
                TemplateName = template.Name,
                ReportType = template.ReportType,
                BuiltInRendererKey = template.BuiltInRendererKey,
                UsesBuiltInRenderer = usesBuiltin,
                VersionId = published?.Id,
                VersionNumber = published?.VersionNumber,
                DefinitionJson = published?.DefinitionJson,
                SchemaVersion = published?.SchemaVersion,
                Message = published == null
                    ? "Template found but no Published version; production print still uses built-in renderer."
                    : null
            };
        }

        private void EnsureBuiltin(string reportType, string builtInKey, string name)
        {
            var existing = templateRepo.Get(t =>
                t.ReportType == reportType &&
                t.BuiltInRendererKey == builtInKey).FirstOrDefault();

            var now = Now();
            var user = User() ?? "system";

            if (existing == null)
            {
                ClearSystemDefaultFlag(reportType);
                existing = new ReportTemplate
                {
                    Name = name,
                    ReportType = reportType,
                    Description = "Phase 1 system default pointing at the existing hard-coded Angular report renderer. Not a converted HTML template.",
                    BuiltInRendererKey = builtInKey,
                    IsSystemDefault = true,
                    IsActive = true,
                    CreatedBy = user,
                    CreatedOn = now,
                    ModifiedBy = user,
                    ModifiedOn = now
                };
                templateRepo.Add(existing);
                if (existing.Id <= 0)
                {
                    // Reload if identity not stamped on entity
                    existing = templateRepo.Get(t => t.ReportType == reportType && t.BuiltInRendererKey == builtInKey).First();
                }

                var definition = Reports.ReportTemplateDefinitionValidator.BuildBuiltinStub(reportType, builtInKey);
                var version = new ReportTemplateVersion
                {
                    TemplateId = existing.Id,
                    VersionNumber = 1,
                    Status = ReportTemplateVersionStatuses.Published,
                    DefinitionJson = definition,
                    SchemaVersion = Reports.ReportTemplateDefinitionValidator.CurrentSchemaVersion,
                    CreatedBy = user,
                    CreatedOn = now,
                    ModifiedBy = user,
                    ModifiedOn = now,
                    PublishedBy = user,
                    PublishedOn = now
                };
                versionRepo.Add(version);
            }
            else if (!existing.IsSystemDefault || !existing.IsActive)
            {
                existing.IsSystemDefault = true;
                existing.IsActive = true;
                existing.ModifiedBy = user;
                existing.ModifiedOn = now;
                templateRepo.Update(existing);
            }

            var sysAssign = assignmentRepo.Get(a =>
                a.ReportType == reportType &&
                a.ScopeType == ReportTemplateScopeTypes.System &&
                a.IsActive).ToList();

            if (sysAssign.Count == 0)
            {
                assignmentRepo.Add(new ReportTemplateAssignment
                {
                    ReportType = reportType,
                    ScopeType = ReportTemplateScopeTypes.System,
                    TemplateId = existing.Id,
                    IsActive = true,
                    CreatedBy = user,
                    CreatedOn = now,
                    ModifiedBy = user,
                    ModifiedOn = now
                });
            }
            else if (sysAssign.Count == 1 && sysAssign[0].TemplateId != existing.Id && existing.IsSystemDefault)
            {
                // Keep existing active system assignment if already configured; do not overwrite.
            }
            else if (sysAssign.Count > 1)
            {
                // Leave conflict for resolver to surface — do not auto-heal ambiguously.
            }
        }

        private void ClearSystemDefaultFlag(string reportType)
        {
            foreach (var t in templateRepo.Get(x => x.ReportType == reportType && x.IsSystemDefault).ToList())
            {
                t.IsSystemDefault = false;
                t.ModifiedBy = User();
                t.ModifiedOn = Now();
                templateRepo.Update(t);
            }
        }

        private ReportTemplate RequireTemplate(int id)
        {
            var t = templateRepo.Get(id);
            if (t == null || !t.IsActive)
            {
                throw new ArgumentException("Template not found.");
            }

            return t;
        }

        private static string NormalizeReportType(string reportType)
        {
            if (string.IsNullOrWhiteSpace(reportType))
            {
                throw new ArgumentException("ReportType is required.");
            }

            var t = reportType.Trim();
            if (string.Equals(t, ReportTemplateTypes.Radiology, StringComparison.OrdinalIgnoreCase))
            {
                return ReportTemplateTypes.Radiology;
            }

            if (string.Equals(t, ReportTemplateTypes.Diagnostic, StringComparison.OrdinalIgnoreCase))
            {
                return ReportTemplateTypes.Diagnostic;
            }

            throw new ArgumentException("ReportType must be Diagnostic or Radiology.");
        }

        private static string NormalizeScope(string scope)
        {
            if (string.IsNullOrWhiteSpace(scope))
            {
                throw new ArgumentException("ScopeType is required.");
            }

            var s = scope.Trim();
            if (string.Equals(s, ReportTemplateScopeTypes.System, StringComparison.OrdinalIgnoreCase))
            {
                return ReportTemplateScopeTypes.System;
            }

            if (string.Equals(s, ReportTemplateScopeTypes.Department, StringComparison.OrdinalIgnoreCase))
            {
                return ReportTemplateScopeTypes.Department;
            }

            if (string.Equals(s, ReportTemplateScopeTypes.Test, StringComparison.OrdinalIgnoreCase))
            {
                return ReportTemplateScopeTypes.Test;
            }

            throw new ArgumentException("ScopeType must be System, Department, or Test.");
        }

        private string User() => identity?.ActivityMember ?? "system";
        private static DateTime Now() => DateTime.Now;

        private ReportTemplateDto ToTemplateDto(ReportTemplate t)
        {
            var versions = versionRepo.Get(v => v.TemplateId == t.Id).ToList();
            return new ReportTemplateDto
            {
                Id = t.Id,
                Name = t.Name,
                ReportType = t.ReportType,
                Description = t.Description,
                BuiltInRendererKey = t.BuiltInRendererKey,
                IsSystemDefault = t.IsSystemDefault,
                IsActive = t.IsActive,
                PublishedVersionNumber = versions
                    .Where(v => v.Status == ReportTemplateVersionStatuses.Published)
                    .Select(v => (int?)v.VersionNumber)
                    .DefaultIfEmpty(null)
                    .Max(),
                DraftVersionNumber = versions
                    .Where(v => v.Status == ReportTemplateVersionStatuses.Draft)
                    .Select(v => (int?)v.VersionNumber)
                    .DefaultIfEmpty(null)
                    .Max(),
                CreatedBy = t.CreatedBy,
                CreatedOn = t.CreatedOn,
                ModifiedBy = t.ModifiedBy,
                ModifiedOn = t.ModifiedOn
            };
        }

        private static ReportTemplateVersionDto ToVersionDto(ReportTemplateVersion v)
        {
            return new ReportTemplateVersionDto
            {
                Id = v.Id,
                TemplateId = v.TemplateId,
                VersionNumber = v.VersionNumber,
                Status = v.Status,
                DefinitionJson = v.DefinitionJson,
                SchemaVersion = v.SchemaVersion,
                CreatedBy = v.CreatedBy,
                CreatedOn = v.CreatedOn,
                ModifiedBy = v.ModifiedBy,
                ModifiedOn = v.ModifiedOn,
                PublishedBy = v.PublishedBy,
                PublishedOn = v.PublishedOn,
                ArchivedBy = v.ArchivedBy,
                ArchivedOn = v.ArchivedOn
            };
        }

        private static ReportTemplateAssignmentDto ToAssignmentDto(ReportTemplateAssignment a, string templateName)
        {
            return new ReportTemplateAssignmentDto
            {
                Id = a.Id,
                ReportType = a.ReportType,
                ScopeType = a.ScopeType,
                DepartmentCode = a.DepartmentCode,
                TestId = a.TestId,
                TemplateId = a.TemplateId,
                TemplateName = templateName,
                IsActive = a.IsActive
            };
        }
    }
}
