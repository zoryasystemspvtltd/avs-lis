# Report Template Engine — Test/Profile Specific PROD Diagnostic

**Date:** 2026-09-18  
**Project:** ZoryaLMS / AVILIS  
**Scope:** Read-only forensic investigation  
**Environment:** PROD Portal `I:\Projects\PROD\AVILIS\PORTAL`, API via portal `/lis`, DB `ZoryaLMS` on `.\SQLEXPRESS`

---

## 1. Problem statement

Admin can open **Setup → Report Template Configuration** in PROD and sees System Default, Template Mode Custom, and Custom Generic (Design / Activate). The Admin reports **no visible section** for:

- Test/Profile Specific templates  
- Test Specific templates  
- Profile Specific templates  

This investigation determines whether that is missing implementation, stale deploy, hidden UI, workflow discoverability, RBAC, or another cause.

---

## 2. Screenshot-observed behavior

Reported PROD Admin screen:

| Observed | Present |
|----------|---------|
| Report Type: Diagnostic | Yes |
| Template Mode: Custom | Yes |
| System Default — Locked | Yes |
| Custom Generic — Active | Yes |
| Generic templates with Design / Activate | Yes |
| Test / Profile Specific section | **Not observed by Admin** |
| Separate Test Specific / Profile Specific sections | **Not observed** |

---

## 3. Approved expected behavior

Source of truth: `docs/Report-Template-Engine-Phase4-Final-Architecture-and-Implementation-Plan.md` §7:

> Custom mode: **Test Specific → Profile Specific → Custom Generic → System Default**.  
> SystemDefault mode: System Default only.

Phase 3 design treats Test/Profile Specific as first-class template categories with targets (`TargetTestId` / `TargetProfileId`) and assignments (`TestId` / `ProfileId`). Configuration is expected on the **Report Template Configuration** Admin workspace (not a separate menu).

---

## 4. Phase 3 persistence verification

| Entity | Implemented | Source evidence |
|--------|-------------|-----------------|
| `ReportTemplate` | Yes | `LIS.DtoModel/Models/Reports/ReportTemplate.cs` — `TemplateCategory`, `TargetTestId`, `TargetProfileId` |
| `ReportTemplateVersion` | Yes | `LIS.DtoModel/Models/Reports/ReportTemplateVersion.cs`; table present in PROD |
| `ReportTemplateAssignment` | Yes | `LIS.DtoModel/Models/Reports/ReportTemplateAssignment.cs` — `ScopeType`, `TestId`, `ProfileId` |
| `ReportTemplateModeSetting` | Yes | `LIS.DtoModel/Models/Reports/ReportTemplateModeSetting.cs` |
| Category constants | Yes | `ReportTemplateCategories` in `ReportTemplateDesignerDtos.cs` (`CustomGeneric`, `Specific`, `SystemDefault`) |
| Schema migration | Yes | `LIS.DataModel/Migrations/202609121800000_ReportTemplateDesignerPhase3.cs` (and API twin) adds `TemplateCategory`, `TargetTestId`, `TargetProfileId` |

**Conclusion:** Test/Profile Specific **persistence was implemented**.

---

## 5. Backend API verification

Controller: `web/Lis.Api/Controllers/Api/ReportTemplateConfigurationController.cs`  
Prefix: `api/ReportTemplateConfiguration`  
Auth: `[QAuthorize(ModuleName = "ReportTemplateConfiguration")]` (+ CanAdd/CanEdit where noted)

| Capability | Endpoint | Method | Auth | Target fields |
|------------|----------|--------|------|---------------|
| Workspace (lists Generic + Specific) | `/workspace?reportType=` | GET | CanView | Response: `customGenerics`, `specificTemplates` |
| Create Generic or Specific | `/designer/create` | POST | CanAdd | `templateCategory`, `targetTestId`, `targetProfileId` |
| Save design | `/designer/save` | POST | CanEdit | (definition; not target) |
| Activate / Deactivate | `/designer/{id}/activate\|deactivate` | POST | CanEdit | Uses template category + targets on activate |
| List Test/Profile targets | `/targets?search=` | GET | CanView | Returns `TargetType` Test\|Profile from `HisTestMaster` / `TestProfileMaster` |
| Resolve precedence | `/resolve` | POST | CanView | Request `testId`, `profileId` |
| Assignments CRUD | `/assignments` | GET/POST | View/Edit | `ReportTemplateAssignmentRequest` (`TestId`, `ProfileId`, …) |

Create request model: `ReportTemplateDesignerCreateRequest`  
(`ReportType`, `Name`, `CreateFrom`, `TemplateCategory`, `TargetTestId`, `TargetProfileId`)

Workspace response model: `ReportTemplateDesignerListDto`  
(`SystemDefault`, `ActiveGeneric`, `CustomGenerics`, `SpecificTemplates`)

**Mutating APIs were not called** during this diagnostic.

---

## 6. Resolver verification

File: `LIS.Businesslogic/ReportTemplateManager.cs` → `Resolve`

Actual Custom-mode order (source):

1. If `TestId` → active assignment `ScopeType=Test`  
2. Else if `ProfileId` → active assignment `ScopeType=Profile`  
3. Else active System-scope assignment whose template `TemplateCategory=CustomGeneric`  
4. Else System Default  

Matches approved: **Test Specific → Profile Specific → Custom Generic → System Default**.

Designer activate for Specific upserts Test or Profile assignment (`ReportTemplateManager.Designer.cs`).

---

## 7. Angular UI verification

Component: `web/Lis.Web/src/app/setup/report-template-configuration/`

| Concern | Finding |
|---------|---------|
| Custom Generic UI | **Implemented** — heading, Create Generic, table |
| Test / Profile Specific UI | **Implemented** — heading `Test / Profile Specific`, button `Create Specific`, table with Target column |
| Separate “Test Specific” / “Profile Specific” panels | **Not implemented as separate sections** — one combined section; create form chooses Test vs Profile |
| Visibility condition | Section renders when `mode==='Custom' && workspace` (same as Generic). **Table** only if `specificTemplates.length` |
| Target selection | Inside Create panel when `createCategory==='Specific'`; loads `/targets` |
| Routing | Same page `/report-template-configuration`; design `/report-template-configuration/design/:id` |
| Service | `report-template-configuration.service.ts` — `workspace`, `createCustom`, `targets`, activate/deactivate |

Exact HTML evidence (`report-template-configuration.component.html` lines 68–88):

- `<h4>Test / Profile Specific</h4>`  
- `Create Specific` → `openCreate(); createCategory='Specific'`  
- `*ngFor` over `workspace.specificTemplates \|\| workspace.SpecificTemplates`

**Classification vs “not implemented”:** UI **is implemented** on this screen. It is **not** a separate menu/route.

---

## 8. Deployed Portal verification

| Check | Result |
|-------|--------|
| Deployed main bundle | `I:\Projects\PROD\AVILIS\PORTAL\main-es2015.5be29c72310d0fdb4ca5.js` |
| Contains `Test / Profile Specific` | **True** |
| Contains `Create Specific` | **True** |
| Contains `specificTemplates` | **True** |
| Contains `targetTestId` / `targetProfileId` (camelCase client fields) | **True** |
| Match to local `dist\DxI800` hash | **Same hash** |

**Conclusion:** PROD Portal is **not** missing Specific UI code (not a stale-bundle gap for this feature).

---

## 9. RBAC verification

| Check | Result |
|-------|--------|
| Module | `ReportTemplateConfiguration` |
| Admin menu permission | `SETUP_REPORT_TEMPLATE_CONFIGURATION` CanView (Administrator only; unchanged this diagnostic) |
| Admin workspace API | **200**, returns Specific rows |
| Tech/Doctor (prior menu-fix UAT) | No module; workspace **403** |

Specific section is **not** gated by a separate permission. Same module as Generic. **Not an RBAC hide.**

---

## 10. Target model verification

| Concept | How identified | Source |
|---------|----------------|--------|
| Test Specific | `ReportTemplate.TargetTestId` = `HisTestMaster.Id`; assignment `ScopeType=Test`, `TestId` | DTO + manager |
| Profile Specific | `ReportTemplate.TargetProfileId` = `TestProfileMaster.Id`; assignment `ScopeType=Profile`, `ProfileId` | DTO + manager |
| Admin target picker | `GET .../targets` → up to 200 active tests + 200 profiles | `ListTestProfileTargets` |
| New master required? | **No** — reuses existing Test/Profile masters |

PROD targets API (read-only): **400** options (**200** Test + **200** Profile).

---

## 11. PROD data verification (read-only)

### Workspace API (`GET .../workspace?reportType=Diagnostic`)

| Field | Value |
|-------|-------|
| `mode` | `Custom` |
| `customGenerics` count | **54** |
| `specificTemplates` count | **8** |
| `activeGeneric` | present |
| Root JSON keys | `reportType,mode,systemDefault,activeGeneric,customGenerics,specificTemplates` |

All 8 Specific items: `templateCategory=Specific`, **`targetTestId` set**, `targetProfileId` empty, `activated=True`, `targetLabel` present. (No PHI dumped; no DefinitionJson content logged beyond length.)

### Radiology workspace

| Field | Value |
|-------|-------|
| `specificTemplates` | **0** |
| `customGenerics` | 9 |

### DB aggregates (active templates)

| ReportType | TemplateCategory | Count | With TargetTestId | With TargetProfileId |
|------------|------------------|-------|-------------------|----------------------|
| Diagnostic | Specific | 8 | 8 | 0 |
| Diagnostic | CustomGeneric | 54 | 0 | 0 |
| Diagnostic | SystemDefault | 1 | 0 | 0 |
| Radiology | Specific | 0 | — | — |

| Assignment Scope (active) | Notes |
|---------------------------|-------|
| Diagnostic Test | 24 rows |
| Diagnostic System | 1 (Generic) |
| Profile scope | **0** |

**Conclusion:** PROD **has Test Specific data** and API returns it. **No Profile Specific templates** exist yet in PROD.

---

## 12. Root cause

### Primary classification: **8. Other (G)**

More precise statement:

> **Test/Profile Specific is implemented end-to-end (DB + API + Angular + currently deployed Portal). On Diagnostic Custom mode the workspace returns 8 Specific templates. The UI section is on the same page, directly below the Custom Generic block. The Admin observation that it is “not visible” is not explained by missing backend, missing UI source, stale Portal omit, or RBAC.**

Most consistent explanations for the screenshot/observation (evidence-ranked):

1. **Discoverability / viewport** — Custom Generic lists **54** rows before the Specific heading; a screenshot focused on System Default + Generic easily **omits** the Specific block below.  
2. **Expectation mismatch** — Product expectation of **separate** “Test Specific” and “Profile Specific” panels; implemented UI is a **single** “Test / Profile Specific” section with Test|Profile chosen at create time.  
3. **Profile Specific unused in PROD** — zero Profile-targeted templates; Profile path exists but has no rows to demonstrate.  
4. **Less likely:** browser cache of a pre–Phase-3 bundle — contradicted by deployed bundle strings matching source and by Admin already using the post–menu-fix Portal that serves this same `main-es2015.5be29c72310d0fdb4ca5.js`.

### Explicit rejection of other buckets

| Option | Verdict |
|--------|---------|
| A. Backend/DB exist, Portal UI does not expose | **Rejected** — UI exposes Specific |
| B. Source exists, not in deployed Portal | **Rejected** — deployed JS contains Specific UI |
| C. Hidden by UI logic | **Partial only** — empty table hidden via `*ngIf`; Diagnostic has 8 rows so table should render. Heading/button are not `*ngIf`-hidden |
| D. Target workflow not visible until create | **Contributing UX factor** — targets appear only in Create panel after Create Specific / Template Type Specific |
| E. Never implemented | **Rejected** |
| F. RBAC | **Rejected** |
| 5. Backend Specific missing | **Rejected** |
| 6. DB target model missing | **Rejected** |

---

## 13. Evidence (index)

| # | Evidence |
|---|----------|
| E1 | Architecture precedence §7 of Phase4 Final Architecture doc |
| E2 | `ReportTemplate.TargetTestId` / `TargetProfileId` |
| E3 | `ReportTemplateManager.Resolve` order Test → Profile → Generic → SystemDefault |
| E4 | `GetDesignerWorkspace` fills `SpecificTemplates` |
| E5 | Angular HTML “Test / Profile Specific” + Create Specific + target selects |
| E6 | Deployed `main-es2015.5be29c72310d0fdb4ca5.js` contains those strings |
| E7 | PROD workspace Diagnostic: `specificTemplates.length === 8` |
| E8 | PROD DB: 8 Specific + TargetTestId; 0 TargetProfileId |
| E9 | `/targets` returns Test + Profile from existing masters |

---

## 14. Whether implementation is complete

| Area | Complete? |
|------|-----------|
| Persistence + migration columns | Yes |
| Create/activate/resolve Test Specific | Yes |
| Create/activate/resolve Profile Specific | Yes (code path); **no PROD Profile Specific rows yet** |
| Admin UI combined Specific section | Yes (source + deployed) |
| Separate Test vs Profile section chrome | No (by design of current UI — combined) |
| Always-visible target browser outside create | No — create-time only |

**Overall:** Functionality is **not missing**. Admin Specific configuration **should appear** on the same page under Custom mode after the Generic list.

---

## 15. Recommended next action

**Do not implement new Specific features yet.** Confirm observation:

1. Hard refresh Portal (`Ctrl+F5`) as Admin.  
2. Report Type **Diagnostic**, Mode **Custom**.  
3. **Scroll below** the Custom Generic table (54 rows).  
4. Confirm heading **“Test / Profile Specific”**, button **Create Specific**, and a table of **8** rows (Target column).  
5. Optionally open DevTools → Network → `workspace?reportType=Diagnostic` → confirm `specificTemplates` length 8.  
6. If DOM still lacks the heading after scroll, capture HTML snippet + `main-es2015.*.js` hash from Network — that would reopen cache/CDN issues.

Optional **later** UX (only after product decision; **not done now**):

- Promote Specific section above Generic, or add sticky sub-nav.  
- Split headings “Test Specific” / “Profile Specific”.  
- Empty-state text when `specificTemplates.length === 0` (e.g. Radiology).

---

## 16. Explicit statement

**NO CODE / DB / CONFIG / DEPLOYMENT CHANGES WERE MADE DURING THIS DIAGNOSTIC.**

Gate 6 / `ReportTemplate:UseDeclarativeRendererForProductionPrint` was not read for mutation and was not changed. No templates were created, activated, or altered. No RBAC changes. No Portal/API redeploy.

---

## STOP

Investigation shows Test/Profile Specific is **present in implementation and PROD data/API/UI bundle**. Stopping per instructions — no automatic “completion” or UI redesign until product review of these findings.
