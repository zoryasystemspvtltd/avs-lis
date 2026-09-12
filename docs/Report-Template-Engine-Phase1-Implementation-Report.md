# Report Template Engine — Phase 1 Implementation Report

**Project:** ZoryaLMS / AVILIS  
**Date:** 2026-09-12  
**Phase:** 1 — Foundation only (no designer, no print replacement)  
**Reference discovery:** `docs/Report-Designer-Current-State-Discovery.md`

---

## 1. Objective

Establish a **safe, versioned, assignable, declarative template foundation** that can later power a drag-and-drop Report Designer **without** changing current Diagnostic/Radiology print output.

---

## 2. Current architecture reference

Unchanged production pipeline:

```
DB → Manager → Report DTO → API JSON → Angular HTML → print CSS → iframe/window.print()
```

Managers: `TestReportManager`, `RadiologyReportManager`  
UI: `test-report`, `radiology-report-print`  
Layout clearance: `ReportLayoutConfiguration` (unchanged)

---

## 3. Architecture decisions

| Decision | Choice | Why |
|----------|--------|-----|
| ReportLayoutConfiguration vs template | **Remain global per ReportType** (clearance/signature geometry) | Avoid duplicate columns; no migration of existing layout rows; templates do not replace stationery config in Phase 1 |
| Built-in default representation | `BuiltInRendererKey` = `Builtin.Diagnostic` / `Builtin.Radiology` + published stub JSON `renderer:"builtin"` | Does **not** fake-convert hard-coded HTML; points at existing renderer |
| Definition format | Versioned JSON `schemaVersion:1` | Extensible for future designer components |
| Binding model | Declarative paths (`Patient.Name`, …) whitelist | No SQL / C# / script execution |
| Assignment hierarchy | Test → Department → System | Deterministic; conflicts throw |
| Production print wiring | **Not connected** | Phase 1 requirement |
| Snapshot/PDF | **Not implemented** | Preserve current regenerate-on-print behavior |

---

## 4. New entities

| Table | Purpose |
|-------|---------|
| `ReportTemplate` | Template header (name, ReportType, BuiltInRendererKey, IsSystemDefault) |
| `ReportTemplateVersion` | Draft / Published / Archived + DefinitionJson |
| `ReportTemplateAssignment` | System / Department / Test active assignment |

Entity files under `LIS.DtoModel/Models/Reports/`.

---

## 5. Relationships

```
ReportTemplate 1──* ReportTemplateVersion
ReportTemplate 1──* ReportTemplateAssignment
Assignment.DepartmentCode → existing Department.Code (logical)
Assignment.TestId → existing HISTestMaster.Id (logical)
```

No duplication of department/test masters.

---

## 6. Template lifecycle

```
Create → Draft v1
Publish → Published (prior Published → Archived)
CreateDraftFromPublished → new Draft vn+1
SaveDraft → Draft only
Archive → Archived
```

Published versions are **immutable** (SaveDraft rejected).

---

## 7. Assignment hierarchy

Resolution order (deterministic):

1. Active **Test** assignment for `(ReportType, TestId)`  
2. Else active **Department** assignment for `(ReportType, DepartmentCode)`  
3. Else active **System** assignment for `ReportType`  
4. Else `IsSystemDefault` template flag  
5. Else `Found=false` (production print still uses built-in HTML)

Conflicts at the same scope → `InvalidOperationException` (no guessing).

---

## 8. Resolver behavior

- Interface: `IReportTemplateResolver` / implemented by `ReportTemplateManager`  
- Input: `ReportType`, optional `DepartmentCode`, optional `TestId`  
- Output: `ReportTemplateResolveResultDto` including `UsesBuiltInRenderer`, published definition (if any)  
- **Does not** accept or return PHI clinical payloads  

---

## 9. API

Prefix: `api/ReportTemplateConfiguration`  
Controller: `ReportTemplateConfigurationController`  
Auth: `QAuthorize(ModuleName = ReportTemplateConfiguration)`

| Method | Route | Permission |
|--------|-------|------------|
| GET | `/` | CanView |
| GET | `/{id}` | CanView |
| POST | `/` | CanAdd |
| GET | `/{id}/versions` | CanView |
| POST | `/draft` | CanEdit |
| POST | `/{id}/draft-from-published` | CanEdit |
| POST | `/versions/{id}/publish` | CanEdit |
| POST | `/versions/{id}/archive` | CanEdit |
| GET/POST | `/assignments` | View / Edit |
| POST | `/assignments/{id}/deactivate` | CanEdit |
| POST | `/resolve` | CanView |
| POST | `/ensure-defaults` | CanEdit |

---

## 10. RBAC

| Item | Value |
|------|-------|
| Module | `ReportTemplateConfiguration` |
| Menu key | `SETUP_REPORT_TEMPLATE_CONFIGURATION` |
| Route | `/report-template-configuration` |
| Order | 57 |
| Initial grant | Administrator only |

Wired in: MenuCatalog, role-permission-catalog, permission.util, left-nav expand, SQL/RBAC migration twin scripts.

**RBAC seed correction:** `RoleMenuPermission` requires non-null `ModuleId`, `IsActive`, and `ApplicationId` (same pattern as Report Layout Configuration). The Phase 1 migration SQL and `Scripts/add-report-template-configuration-rbac.sql` were aligned to that schema so Administrator menu permission seeds correctly.

---

## 11. Security

- Admin-only module (not granted to Doctor/Technician/Receptionist by default)  
- Definition validator rejects `<script>`, `javascript:`, SQL-ish patterns, unknown binding roots, PHI-like payload keys  
- Templates store **layout definition**, not patient/result data  
- No anonymous endpoints  

---

## 12. Validation

`ReportTemplateDefinitionValidator`:

- Requires `schemaVersion == 1`  
- `reportType` must match template  
- `renderer` ∈ { `builtin`, `declarative` }  
- Binding roots whitelist: Patient, Order, Invoice, Visit, Test, Parameter, Doctor, Technician, Accession, Radiology, Section, Header, Layout, Branding  

---

## 13. Compatibility approach

| Area | Phase 1 action |
|------|----------------|
| Diagnostic print HTML | **Unchanged** |
| Radiology print HTML | **Unchanged** |
| Print All / Individual Print | **Unchanged** |
| Payment / Approval gates | **Unchanged** |
| Signatures | **Unchanged** |
| ReportLayoutConfiguration | **Unchanged** (global defaults) |
| Notifications / secure download | **Unchanged** |

---

## 14. Existing report preservation

System defaults are **pointers**:

- `Builtin.Diagnostic` → existing `TestReportComponent` renderer (future Phase 2+ hook)  
- `Builtin.Radiology` → existing `RadiologyReportPrintComponent` renderer  

No HTML conversion was performed. Definition stub documents that stationery clearance remains in `ReportLayoutConfiguration`.

---

## 15. Test coverage (Phase 1)

`LIS.Masters.Tests/Reports/ReportTemplateEnginePhase1Tests.cs` — **8/8 PASS**

| Test | Result |
|------|--------|
| EnsureSystemDefaults_Creates_Builtin_Diagnostic_And_Radiology | PASS |
| Resolver_SystemDefault_When_No_Department_Or_Test | PASS |
| Resolver_Test_Overrides_Department_Overrides_System | PASS |
| Publish_Archives_Prior_Published_And_Draft_Not_Directly_Editable_After_Publish | PASS |
| Conflicting_Department_Assignment_Rejected | PASS |
| Diagnostic_Template_Cannot_Resolve_As_Radiology_Mismatch_On_Assignment | PASS |
| Definition_Rejects_Script_And_Unknown_Binding | PASS |
| Definition_Allows_Safe_Patient_Binding | PASS |

RBAC catalog: `ReportTemplateConfigurationRbacTests` — **4/4 PASS**

---

## 16. Regression results

Executed after Phase 1 build:

| Suite / test | Result |
|--------------|--------|
| PrintSpecific_Allows_Approved_Test_When_Sibling_Pending | PASS |
| PrintSpecific_And_PrintAll_Blocked_When_Invoice_Unpaid | PASS |
| Defaults_Diagnostic_Match_Production_Clearance | PASS |
| Report Layout MenuCatalog tests | PASS |

Existing print output code paths were not modified.

---

## 17. Build results

`LIS.Masters.Tests` Release build: **SUCCESS** (DtoModel → BusinessLogic → Lis.Api → Masters.Tests).

---

## 18. Deployment results

**Not deployed** (per Phase 1 rules). Local DB schema tables present for tests; production/IIS deploy requires explicit approval.

Scripts for teammates:

- `Scripts/add-report-template-engine.sql`  
- `Scripts/add-report-template-configuration-rbac.sql`  

---

## 19. Known limitations (Phase 1)

1. No drag-and-drop designer / canvas / palette  
2. No production print switch-over to resolver  
3. No full declarative renderer  
4. No reusable clinical Report Viewer  
5. No server PDF / snapshot / historical immutability  
6. No template import/export  
7. No modality-specific Radiology layouts  
8. Minimal Admin UI (list + ensure defaults + resolve probe only)  
9. ReportLayoutConfiguration not yet template-scoped (intentionally global)  
10. Empty `IReportRenderer` / `IReportViewer` marker interfaces only  

---

## 20. Phase 2 recommendations (deferred — do not implement now)

1. Visual designer on Angular CDK DragDrop (or approved library after architecture review)  
2. Declarative renderer consuming DTO + published definition  
3. Opt-in print path behind feature flag with built-in fallback  
4. Optional template-level stationery overrides that still respect pre-printed letterhead  
5. Version-aware reprint strategy / optional snapshot policy (product decision required)  
6. Secure download PDF generation only if product requires it  

---

## Files added / touched (summary)

**Added:** entities, DTOs, validator, manager, controller, migrations/scripts, Angular setup page/service, tests, this report.

**Touched carefully:** DbContext DbSets, SimpleInjector, MenuCatalog, role-permission-catalog, permission.util, left-nav, csproj Compile lists, app.module/routing.

**Not touched:** `test-report.component.*`, `radiology-report-print.component.*`, payment/approval/result workflows, signature storage, ReportLayoutConfiguration manager logic.

---

*Phase 1 complete. STOP — do not proceed into full designer implementation without a separate Phase 2 prompt.*
