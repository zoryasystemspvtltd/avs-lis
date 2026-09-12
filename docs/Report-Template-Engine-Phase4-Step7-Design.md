# Report Template Engine — Phase 4 Step 7 Design

**Project:** ZoryaLMS / AVILIS  
**Date:** 2026-09-12  
**Phase:** 4 — Design Step 7 (RBAC, Security & Regression / Acceptance Gate)  
**Status:** PROPOSED — PENDING USER APPROVAL  
**Prerequisites:** Step 1–6 APPROVED  

**Scope:** Design / source inspection only. **No implementation. Flag remains OFF.**

---

## 1. Executive Summary

### FACT — Designer is already on the existing RBAC spine

Report Template Configuration is integrated like Report Layout Configuration:

| Layer | Mechanism |
|-------|-----------|
| Menu / catalog | `MenuCatalog` · `SETUP_REPORT_TEMPLATE_CONFIGURATION` → module `ReportTemplateConfiguration` |
| Role UI | `role-permission-catalog.ts` Masters entry (order 57) |
| Angular routes | `AuthGuard` + `PermissionGuard` via `ROUTE_GUARDS` |
| Route rules | `permission.util.ts` · modules `ReportTemplateConfiguration` + menu key |
| API | `ReportTemplateConfigurationController` · `[QAuthorize(ModuleName = "ReportTemplateConfiguration")]` (+ CanAdd/CanEdit) |
| Seed | Migration `202609121601000_ReportTemplateConfigurationRbac` + `Scripts/add-report-template-configuration-rbac.sql` |

### FACT — Production reports use separate modules

| Report | API module | Angular route modules / menu |
|--------|------------|------------------------------|
| Diagnostic print | `Reports` CanView | `Reports` / `DoctorsApprovals` · `REPORT_DIAGNOSTIC` |
| Radiology print | `RadiologyReports` CanView | `RadiologyReports` / `Reports` · `REPORT_RADIOLOGY` |

Having print permission does **not** grant template designer permission (**FACT** of separate module names).

### RECOMMENDATION — Step 7 gate architecture

1. **No parallel authorization** — keep Menu → Route → QAuthorize → Manager.  
2. Presentation adapter runs **only after** authorized eligible print GET success.  
3. Renderer remains presentation-only (allow-listed bindings; no SQL/JS/services).  
4. Acceptance = layered gates; **production flag ON requires explicit Gate #6 approval** after evidence.  
5. Flag OFF ⇒ customer-visible parity with existing production.

---

## 2. Existing RBAC Architecture

### FACT — Authoritative pieces

| Piece | Path / type |
|-------|-------------|
| `QAuthorizeAttribute` | `web/Lis.Api/Providers/QAuthorize.cs` — authenticated; Administrator bypass; else module permission bits (+ optional menu) |
| `MenuCatalog` | `web/Lis.Api/Providers/MenuCatalog.cs` |
| Angular `AuthGuard` | Login + email confirmed |
| Angular `PermissionGuard` | Administrator bypass; else `resolveRoutePermission` / module+menu bits |
| `ROUTE_PERMISSION_RULES` | `web/Lis.Web/src/app/_guards/permission.util.ts` |
| Role edit catalog | `web/Lis.Web/src/app/administration/roles/role-permission-catalog.ts` |

### FACT — Report Layout Configuration (peer pattern)

Same architecture with module `ReportLayoutConfiguration`, route `/report-layout-configuration`, `ReportLayoutConfigurationController` QAuthorize, RBAC migration/SQL, and Angular `report-layout-configuration-rbac.spec.ts`.

### FACT — Report Template Configuration mirrors that pattern

Dedicated module (**not** generic `Masters`), menu key `SETUP_REPORT_TEMPLATE_CONFIGURATION`, order 57. Catalog tests: `LIS.Masters.Tests/Security/ReportTemplateConfigurationRbacTests.cs`.

### RECOMMENDATION

Phase 4 implementation must **preserve** this integration; do not invent a second auth model for designer or production presentation.

---

## 3. Designer Authorization

### FACT — End-to-end chain

```
Menu (SETUP_REPORT_TEMPLATE_CONFIGURATION / RoleMenuPermission)
  → Route (/report-template-configuration[+ /design/:id]) + AuthGuard + PermissionGuard
  → API api/ReportTemplateConfiguration/* + QAuthorize(ReportTemplateConfiguration)
  → ReportTemplateManager (+ Designer partial)
  → Template operation
```

### FACT — Operation → permission mapping (controller)

| Operation | Route / method | QAuthorize |
|-----------|----------------|-----------|
| List / Get / Workspace / System default | GET | Module (default view) |
| List versions / Assignments / Components / Design fields / Targets / Mode GET | GET | Module |
| Resolve (admin) | POST `resolve` | Module |
| Preview sample / version / definition | POST `preview/*` | Module |
| Validate design | POST `designer/validate` | Module |
| Create / designer create | POST | **CanAdd** |
| Save draft / Save design / Draft from published | POST | **CanEdit** |
| Publish / Archive version | POST | **CanEdit** |
| Upsert assignment / Deactivate assignment | POST | **CanEdit** |
| Ensure defaults | POST | **CanEdit** |
| Mode SET | POST `mode` | **CanEdit** |
| Activate / Deactivate template | POST `designer/{id}/activate\|deactivate` | **CanEdit** |

### FACT — Preview data

`PreviewSample` / `PreviewVersion` / `PreviewDefinition` use `ReportTemplateSampleDataFactory` synthetic DTOs — **not** live clinical PHI from print workflows.

### RECOMMENDATION

Keep every designer mutation on CanAdd/CanEdit as above. Do not expose designer APIs under `Reports` / `RadiologyReports`. Preview must remain Admin-module protected even after production flag semantics are fixed (Step 5).

### UNKNOWN

Whether non-Administrator roles with only CanView on `ReportTemplateConfiguration` are used in customer deployments — product/ops; architecture supports granting view without edit via existing bits.

---

## 4. Unauthorized Access

### FACT — Expected framework behavior

| Caller | UI | API |
|--------|----|-----|
| Unauthenticated | `AuthGuard` → `/login` | `QAuthorize` → unauthorized (not authenticated) |
| Authenticated without module | `PermissionGuard` denies route | `QAuthorize` → forbidden |
| Unauthorized role | Same | Same |
| Administrator | Allowed | Allowed (role short-circuit in `QAuthorize`) |

### FACT — What template APIs can return when authorized

Template metadata, definition JSON, assignments, mode, **sample** preview HTML — configuration/IP sensitive, generally **not** patient PHI when using sample factory.

### RECOMMENDATION — Hard requirements

Unauthorized callers must not receive:

- Template definitions  
- Internal configuration/mode/assignments  
- Preview HTML  
- Live report DTO / PHI  

Production print DTOs remain only on `OperationalReportsController` under `Reports` / `RadiologyReports`.

### UNKNOWN (test gap)

Automated **HTTP** anonymous/non-admin denial tests for `ReportTemplateConfigurationController` were **not found**. Existing security tests are primarily **MenuCatalog** assertions. Phase 4 implementation should add API-level RBAC tests (see §11).

---

## 5. Production Report Authorization Boundary

### FACT — Diagnostic

```
PermissionGuard (REPORT_DIAGNOSTIC / Reports…)
  → GET api/.../TestReport [QAuthorize Reports CanView]
  → TestReportManager.GetDiagnosticTestReport (eligibility)
  → Enrich* in OperationalReportsController
  → Ok(report)
```

### FACT — Radiology

```
PermissionGuard (REPORT_RADIOLOGY / RadiologyReports…)
  → GET RadiologyReport [QAuthorize RadiologyReports CanView]
  → RadiologyReportManager.GetRadiologyReportForPrint (eligibility)
  → Enrich*
  → Ok(report)
```

### RECOMMENDATION — Presentation adapter boundary

| May do | Must not do |
|--------|-------------|
| Choose Existing vs Declarative presentation | Authenticate users |
| Resolve template + render HTML from **already authorized** DTO | Authorize report access |
| Fall back to Angular | Decide payment / approval / ownership |
| Log non-PHI presentation events | Bypass QAuthorize or eligibility |

Adapter runs **only** on eligibility success path (Steps 2–6). Declarative renderer **cannot grant** report access.

---

## 6. Template Data Isolation

### FACT — Controls

| Control | Source |
|---------|--------|
| Binding root allow-list | `ReportTemplateDefinitionValidator.AllowedBindingRoots` |
| Component allow-list + report-type support | `ReportComponentRegistry.SupportsReportType` |
| Unsafe script/SQL/JS patterns | `UnsafePattern` in validator |
| Forbidden PHI-like definition keys | `ForbiddenJsonKeys` |
| Style allow-list | `AllowedStyleKeys` |
| Condition ops allow-list | `AllowedConditionOps` |
| Binding context | `ReportTemplateBindingContext` seeds **only** from supplied Diagnostic/Radiology DTO |
| Render | No DB/service calls in `DeclarativeReportRenderer` |

### FACT — Property read mechanism

After root resolution, `ReportTemplateBindingContext` uses public instance property reflection on objects already in scope (`ReadProperty`). It does **not** execute SQL, JavaScript, or invoke arbitrary services.

### Classification

| Claim | Tag |
|-------|-----|
| No SQL/JS execution from templates | **FACT** (validator + renderer design) |
| No independent PHI retrieval in renderer | **FACT** |
| Only approved DTO instance is bound | **FACT** |
| Completely impossible to read any public property under an allowed root if path is accepted | **RISK / partial** — mitigated by root allow-list + validate-time binding checks; not a free-form CLR sandbox |
| Templates cannot call application services | **FACT** |

### RECOMMENDATION

Preserve allow-lists; production integrity gate (Step 6) for unsupported components; never pass untrusted client HTML as production print content.

---

## 7. Report Type Isolation

### FACT

| Mechanism | Behavior |
|-----------|----------|
| Template.ReportType | Diagnostic \| Radiology |
| `Resolve` | Normalizes request ReportType; queries assignments with that `ReportType` |
| Assignment upsert | Rejects cross-type template (`Diagnostic_Template_Cannot_Resolve_As_Radiology_Mismatch_On_Assignment`) |
| Definition validate | `reportType` must match expected |
| Registry | Component types restricted per report type (e.g. ACCESSION on Diagnostic rejected in Phase 2 tests) |
| Mode setting | Per ReportType PK |

Diagnostic template ≠ Radiology template for resolution purposes.

### RECOMMENDATION

Production adapter always passes the print path’s ReportType into Resolve/Render. Never reuse a Diagnostic definition for Radiology print (or vice versa).

---

## 8. Specific Template Isolation

### FACT — Resolver order (Custom mode)

1. Active Test assignment matching `TestId`  
2. Else active Profile assignment matching `ProfileId`  
3. Else active Custom Generic (System scope, non–SystemDefault)  
4. Else System Default  

(`ReportTemplateManager.Resolve`)

### FACT — Tests

`Resolver_Phase3_Specific_Over_Generic_Over_SystemDefault_When_Custom_Mode` — Specific wins when TestId matches; non-matching TestId falls to Generic.

### RECOMMENDATION — Step 3 locked (do not change)

| Rule | Behavior |
|------|----------|
| Test Specific | Only with deterministic invoice-line `TestId` |
| Profile Specific | Only with deterministic `ProfileId` when present |
| Ambiguous / missing Specific context | **Do not guess** — omit TestId/ProfileId → Generic or System Default |
| Print All | No per-test Specific composition |

Generic must not override a **valid** Specific match when context is correctly supplied (resolver already enforces order).

---

## 9. Activation Security

### FACT

| Verb | Who can call (API) | Business notes |
|------|--------------------|----------------|
| Save / SaveDesign | CanEdit on ReportTemplateConfiguration | Does not Activate (Phase 3 test) |
| Publish | CanEdit (version publish) | Also invoked inside Activate via `EnsurePublishedForActivation` |
| Activate | CanEdit | Validates definition; creates assignment; throws if production flag true (Phase 3 quirk) |
| Deactivate | CanEdit | Clears assignments |
| Archive | CanEdit on version archive | |
| Mode change | CanEdit | Preserves customs |

### FACT

Ordinary report modules (`Reports` / `RadiologyReports`) do **not** authorize these endpoints.

### RECOMMENDATION

Preserve separation: print users cannot manage templates merely because they can print. Role grants for template module remain Admin/setup responsibility.

---

## 10. Feature Flag Security

### FACT

| Attribute | Value |
|-----------|--------|
| Name | `UseDeclarativeRendererForProductionPrint` |
| Location | `ReportTemplateEngineFeatureFlags` in `ReportTemplateEnginePhase2.cs` |
| Type | `const bool = false` |
| User-facing control | **None** |
| Designer UI | Message that flag remains OFF — no toggle |
| Ordinary users | Cannot affect it |

### RECOMMENDATION — Eventual operational control (design only; do not implement)

- Keep master switch **out of** designer and report UIs.  
- Prefer release/config ownership (const today; optional later `appSettings` per Step 5) with server ACL.  
- Do **not** introduce a second flag.  
- Do **not** change the flag in this step.

### FACT — Config infrastructure

`Web.config` `appSettings` exists for branding keys; **not** currently used for this flag. Safe support is possible later with fail-closed parsing (Step 5) — **RECOMMENDATION**, not present today.

---

## 11. Security Test Coverage

### FACT — Existing (not re-executed in this design step)

| Area | Evidence in source |
|------|--------------------|
| MenuCatalog for template module | `ReportTemplateConfigurationRbacTests` |
| Flag OFF | Phase 2/3 feature flag tests |
| Script / unknown binding / JS URL / SQL-like binding / bad condition ops | Phase 1/2 validator tests |
| Cross report-type assignment | Phase 1 mismatch test |
| Specific over Generic | Phase 1 resolver test |
| Activate blocked on invalid | Phase 3 remediation test |
| Layout vs template RBAC catalog (Angular) | Layout has `report-layout-configuration-rbac.spec.ts`; **no** parallel Angular spec file found for template |

### RECOMMENDATION — Add for Phase 4 implementation (do not claim pass now)

| Test | Purpose |
|------|---------|
| Anonymous → template API | 401/unauthorized |
| Authenticated non-admin without module → template API | Forbidden |
| Admin / granted role → allowed | Positive |
| Reports-only user → Activate/Save denied | Separation |
| Reports CanView still loads print | Access preservation |
| Binding allow-list / script-SQL rejection | Regression |
| Cross-ReportType Resolve/Render isolation | Regression |
| Specific target isolation + no-guess context | Step 3 |
| Presentation fallback does not 500 | Step 6 |
| Flag OFF print path unchanged | Compatibility |

### Classification

Existing unit/integration tests exist for many engine/security facets (**FACT** that tests are present). **This Step 7 design does not claim they were run or passed** for a production activation decision.

---

## 12. Regression Test Matrix

### A. Existing behavior with flag OFF — RECOMMENDATION matrix

**Diagnostic**

| Case | Expect |
|------|--------|
| Print All | Angular; eligibility unchanged |
| Print Specific | Angular; sibling-pending rules unchanged |
| Payment gate | 400 / blocked |
| Approval gate | 400 / blocked |
| Sibling-pending Specific | Existing manager behavior |

**Radiology**

| Case | Expect |
|------|--------|
| Authorized/Released print | Angular when eligible |
| Payment gate | Blocked |
| Findings/impression required | Blocked if missing |

### B. Declarative with controlled activation (flag ON + Mode Custom + valid Active) — RECOMMENDATION

**Diagnostic:** Generic; Specific; repeating groups; parameter tables; signatures; ReportLayoutConfiguration clearance.  
**Radiology:** Generic; Specific where Step 3 allows; narratives; authorizer signature; layout.

Whole-report only; browser print unchanged.

### C. Fallback → Existing Angular — RECOMMENDATION

No template; System Default; unresolved Specific context; invalid template; unsupported component (integrity fail); renderer exception; malformed/empty output.

### D. Security — RECOMMENDATION

RBAC designer vs print; template isolation; report-type isolation; Specific isolation; no PHI in presentation logs.

---

## 13. Output Integrity Tests

### RECOMMENDATION — Validation checklist (manual/UAT + automated where feasible)

| Topic | Expect |
|-------|--------|
| A4 / orientation | From ReportLayoutConfiguration |
| Margins / header/footer mm | Clearance bands only; preprinted stationery unchanged |
| No new letterhead art | Locked |
| Signatures | From enriched DTO; placement only |
| Page breaks | Angular authoritative for System Default; Custom uses declarative CSS without new pagination engine |
| Diagnostic sections / parameters | Present when template defines them; else Angular on fallback |
| Radiology narrative | Findings/impression/recommendation when declarative; else Angular |
| Whole-report | No section mixing |

Do not invent a competing pagination system.

---

## 14. Backward Compatibility Gate

### RECOMMENDATION — Required evidence

| Evidence | Pass criteria |
|----------|---------------|
| Flag OFF in build | Const false / tests assert false |
| Diagnostic/Radiology print UAT | Matches pre-Phase-4 behavior |
| Eligibility messages | Unchanged |
| Existing Angular templates | Still used |
| Saved custom templates | Preserved; no auto-Activate |
| Mode SystemDefault default | No customer-visible format change |

```
Feature flag OFF = Existing production behavior
```

---

## 15. Performance Gate

### RECOMMENDATION — Align with Step 6

Compare representative scenarios (simple / multi-test / parameter-heavy / Generic / Specific / Radiology / Radiology custom) measuring:

- ResolveMs  
- RenderMs  
- PresentationTotalMs  

Secondary: full HTTP time (includes unchanged eligibility/DTO).

**Do not invent arbitrary thresholds.** Architect/ops accept after measured baselines show no unacceptable degradation **and** correctness gates pass.

---

## 16. Production Activation Gate

### Explicit stages (RECOMMENDATION)

| # | Gate | Meaning |
|---|------|---------|
| 1 | Code implementation complete | Adapter + flag semantics fix + Angular bind + logging |
| 2 | Automated tests complete | Matrix §11–12 automated subset green |
| 3 | Build complete | Backend + Angular production build |
| 4 | Deployment verification | Target env deploy; flag still OFF unless #6 |
| 5 | Browser/E2E validation | Print/fallback/RBAC/layout UAT |
| 6 | **Production activation approved** | Explicit architect/ops decision to enable flag |

**Passing #1–#5 does NOT automatically mean #6.**  
Production flag remains OFF until #6 is explicitly approved.

---

## 17. Acceptance Evidence

### RECOMMENDATION — Record before Gate #6

| Artifact | Notes |
|----------|--------|
| Automated test results | Engine + RBAC + fallback + flag OFF |
| Build result | Success logs/artifacts |
| Deployment result | Env verified; flag OFF until #6 |
| Browser print validation | Representative Diagnostic/Radiology |
| Fallback validation | Forced failure classes → Angular |
| Security/RBAC validation | Designer denied for print-only users; print still works |
| Performance baseline | Step 6 scenarios |
| Defect bar | **No unresolved Critical / High / Medium** findings against Phase 4 production path |

Do **not** declare actual results in this design document.

---

## 18. Rollback Acceptance

### FACT / RECOMMENDATION

Disabling the production flag returns subsequent reports to the existing Angular renderer (Steps 4–5).

Must **not** require:

- Database rollback  
- Template deletion  
- Clinical data loss  
- Workflow changes  

Optional soft-stop: Mode → SystemDefault (preserves templates).

---

## 19. Database Implications

Security and acceptance design reuse existing:

- `UserModules` / `RoleModuleMappings` / `RoleMenuPermission` (already seeded for ReportTemplateConfiguration)  
- Template tables from Phase 1–3  

**NO NEW DB CHANGE REQUIRED** for Step 7.

If a future implementation adds DB (e.g. audit table), that implementation must include migration + deployment SQL — not authorized here.

---

## 20. RBAC Requirements

### RECOMMENDATION / locked requirement for Phase 4 implementation

The Report Template Configuration designer **must** remain integrated into the **existing** RBAC/menu authorization architecture:

```
MenuCatalog + Role permissions
  → Angular AuthGuard + PermissionGuard
  → QAuthorize(ReportTemplateConfiguration)
  → Manager operations
```

| Requirement | Status |
|-------------|--------|
| No parallel authorization | Mandatory |
| Dedicated module (not piggyback Reports) | **FACT** already |
| Administrator seed + grantable in Role edit | **FACT** scripts/catalog |
| Production print modules unchanged | Mandatory |
| Presentation cannot grant access | Mandatory |

---

## 21. Risks / Unknowns

| Item | Tag | Mitigation |
|------|-----|------------|
| No HTTP anonymous/non-admin API tests for template controller | **FACT** gap | Add in Phase 4 implementation |
| No Angular `report-template-*-rbac.spec.ts` peer to layout | **FACT** gap | Add catalog/route rule spec |
| QAuthorize on template APIs omits `MenuKey` | **FACT** | Module bits authoritative; menu enforced on Angular routes |
| Binding reflection under allowed roots | **RISK** | Keep root allow-list; avoid expanding roots |
| Preview throw-when-flag-true | **FACT** | Fix before Gate #6 |
| System Default ≠ Angular proven | **UNKNOWN** | Keep System Default on Angular |
| Customer role matrices for template module | **UNKNOWN** | Ops grant deliberately |

---

## 22. Recommended Step 7 Architecture

```
                    ┌─────────────────────────────┐
                    │ Existing RBAC (authoritative)│
                    └──────────────┬──────────────┘
           ┌───────────────────────┼───────────────────────┐
           ▼                                               ▼
 ReportTemplateConfiguration                         Reports /
 (designer Menu→Route→API)                    RadiologyReports (print)
           │                                               │
           ▼                                               ▼
 Template CRUD / Mode / Activate              Eligibility → Enrich
 Preview (sample data only)                            │
                                                       ▼
                                          Presentation adapter
                                          (no auth / no eligibility)
                                                       │
                                      ┌────────────────┴────────────┐
                                      ▼                             ▼
                               Declarative OK                 Existing Angular
                                      └────────────┬────────────────┘
                                                   ▼
                                        Browser print (unchanged)

Acceptance: Gates 1–5 evidence → Gate 6 explicit flag enable
Rollback: flag OFF (templates preserved)
```

---

## 23. Explicit Non-Goals

- Implementing adapter, tests, or RBAC changes  
- Enabling the feature flag  
- New authorization systems or modules  
- DB migrations/SQL for Step 7  
- Declaring live test/build/deploy results  
- Approving production activation  
- Changing Step 3 resolution rules  
- Server PDF / new print pipeline  
- Beginning Step 8 or implementation  

---

## Change Integrity (Step 7 deliverable)

| Category | Count |
|----------|-------|
| Source changes | **0** |
| Angular changes | **0** |
| DTO changes | **0** |
| DB changes | **0** |
| SQL/migration changes | **0** |
| Configuration changes | **0** |
| Feature flag changes | **0** |
| Test changes | **0** |
| Deployment changes | **0** |

**Only this design document was created:**  
`docs/Report-Template-Engine-Phase4-Step7-Design.md`

---

STEP 7 STATUS: PROPOSED — PENDING USER APPROVAL
