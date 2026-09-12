# Report Template Engine — Phase 4 Final Architecture & Implementation Plan

**Project:** ZoryaLMS / AVILIS  
**Date:** 2026-09-12  
**Phase:** 4 — Step 8 (Final Design Consolidation)  
**Status:** PROPOSED — PENDING USER APPROVAL  

**Scope:** Architecture + implementation plan only. **Implementation is NOT authorized by this document.**  
**Flag remains OFF. No deploy. No production activation.**

**Inputs:** Approved Phase 4 Steps 2–7 design docs; Phase 1–3 implementation reports; actual source.  
**Note:** `docs/Report-Template-Engine-Phase4-Step1-Design.md` is **not present in the repository** (**FACT**). Step 1 decisions are treated as restated and locked by the architect’s Step 8 prompt and by Steps 2–7. This is a **documentation gap**, not an implementation-blocking contradiction.

---

## 1. Executive Summary

### Final production architecture (RECOMMENDATION = approved Steps 2–7)

```
Eligibility (unchanged managers)
  → DTO assembly (unchanged)
  → Controller enrichment (signatures / branding / layout) (unchanged)
  → Presentation Adapter (NEW; post-enrichment / pre-response)
       ├─ Flag OFF / System Default / builtin / unsafe / failure
       │     → PresentationMode=Existing → Angular HTML
       └─ Flag ON + Custom Mode + valid active custom + successful integrity-checked render
             → PresentationMode=Declarative + Html/Css
  → Existing Angular print root + iframe / window.print()
```

### Non-negotiable outcomes

| Outcome | Disposition |
|---------|-------------|
| Flag OFF after implementation/deploy | **Existing Angular only** — no customer-visible change |
| System Default / builtin | **Always Existing Angular** until a later equivalence gate |
| Failures | Log once → whole-report Angular → still HTTP 200 (if eligible) |
| Rollback | Disable flag (appSettings) → Angular; templates intact |
| Activation | **Gate 6 only** — not implied by Gates 1–5 |

### Critical source fix (already identified Steps 4–5)

`UseDeclarativeRendererForProductionPrint` is a **compile-time `const false`**. Preview/Activate **throw if true**. Production print **ignores** it.  
Implementation **must** reconcile this **before** any enablement. Do **not** merely set `true`.

### Database

**NO DB CHANGE REQUIRED** for the approved Phase 4 architecture (flag via appSettings; adapter; DTO presentation fields; Angular bind; logging; tests).

---

## 2. Approved Design Decisions

| # | Decision | Source | Tag |
|---|----------|--------|-----|
| 1 | Eligibility stays upstream in managers | Steps 1–2, 7 | Locked |
| 2 | Presentation-only template engine | Steps 1–4 | Locked |
| 3 | Integration after enrichment, before API response | Step 2 | Locked |
| 4 | HTML + existing browser print; no server PDF | Steps 2, 4 | Locked |
| 5 | Whole-report switch; no section mixing | Step 4 | Locked |
| 6 | Existing Angular = mandatory fallback | Steps 2–6 | Locked |
| 7 | System Default / builtin → Angular | Steps 4–5 | Locked |
| 8 | Specific via SaleInvoiceDetail FKs; no HISTestCode→Id sole mapping | Step 3 | Locked |
| 9 | Print All = no per-test Specific | Step 3 | Locked |
| 10 | Radiology: deterministic invoice TestId or omit Specific | Step 3 | Locked |
| 11 | ReportLayoutConfiguration authoritative | Step 4 | Locked |
| 12 | Signatures / page-breaks authoritative as designed | Step 4 | Locked |
| 13 | Single app-wide flag; Mode scopes Diagnostic/Radiology | Step 5 | Locked |
| 14 | Presentation fail → log + Angular; not 400/500 | Step 6 | Locked |
| 15 | Soft-skip unsupported component = integrity failure in production | Step 6 | Locked |
| 16 | Existing RBAC; no parallel auth | Step 7 | Locked |
| 17 | Six activation gates; Gate 6 enables flag | Step 7 | Locked |

No approved decision is reopened herein.

---

## 3. Final Target Architecture

```
┌──────────────────────────────── DIAGNOSTIC ────────────────────────────────┐
│ Auth: Reports CanView                                                       │
│ TestReportManager.GetDiagnosticTestReport  (payment/approval/print rules)   │
│ OperationalReportsController enrich (approver/tech/branding/layout)         │
│                              │                                              │
│                              ▼                                              │
│              ReportProductionPresentationAdapter                            │
│                 │                                                           │
│    ┌────────────┼──────────────────────────────┐                            │
│    │ Flag OFF / Mode SystemDefault / builtin   │                            │
│    │ no published declarative / integrity fail │                            │
│    │ exception / unresolved→safe Generic/SD    │                            │
│    ▼                                           ▼                            │
│ Existing Angular                         Declarative Html+Css               │
│ PresentationMode=Existing                PresentationMode=Declarative       │
│                              │                                              │
│                              ▼                                              │
│ test-report.component  →  #diagnostic-test-report-print  → iframe/print     │
└─────────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────── RADIOLOGY ─────────────────────────────────┐
│ Auth: RadiologyReports CanView                                              │
│ RadiologyReportManager.GetRadiologyReportForPrint                           │
│ Enrich authorizer + layout                                                  │
│                              │                                              │
│                              ▼                                              │
│              SAME Presentation Adapter (ReportType=Radiology)               │
│                 │                                                           │
│    Fallback paths identical in principle                                    │
│                              ▼                                              │
│ radiology-report-print → #diagnostic-radiology-report-print → iframe/print  │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Designer path (unchanged authority):** Menu → Route → `QAuthorize(ReportTemplateConfiguration)` → manager — **not** the print path.

---

## 4. Diagnostic Production Flow

### FACT — Current (unchanged core)

| Step | Location |
|------|----------|
| UI | `test-report.component.ts` |
| API | `OperationalReportsController.GetTestReport` |
| Eligibility + DTO | `TestReportManager.GetDiagnosticTestReport` |
| Enrich | `EnrichLabApprover` / `EnrichLabTechnician` / `EnrichReportBranding` / `EnrichReportLayout` |
| HTML | `test-report.component.html` |
| Print | `print()` iframe / `window.print()` |

### RECOMMENDATION — Insert

After enrich, before `return Ok(report)`:

1. Build resolution context (Print Specific vs Print All).  
2. Call presentation adapter with enriched `DiagnosticTestReportDto`.  
3. Attach presentation result fields; never throw for presentation faults.

---

## 5. Radiology Production Flow

### FACT — Current

| Step | Location |
|------|----------|
| UI | `radiology-report-print.component.ts` |
| API | `OperationalReportsController.GetRadiologyReport` |
| Eligibility + DTO | `RadiologyReportManager.GetRadiologyReportForPrint` |
| Enrich | `EnrichRadiologyApprover` / `EnrichRadiologyLayout` |

### RECOMMENDATION — Insert

Same adapter after enrich; ReportType=Radiology; context rules per §6.

---

## 6. Resolution Context

### Diagnostic Print Specific — RECOMMENDATION (Step 3)

```
testRequestDetailId (API query; already used)
  → SaleInvoiceDetail where RequestDetailId matches (active)
  → if deterministic line: Resolve.TestId = line.TestId;
                           Resolve.ProfileId = line.TestProfileId (if any)
  → else: omit TestId/ProfileId (do not use HISTestCode→Id)
```

### Diagnostic Print All — RECOMMENDATION

```
Resolve(ReportType=Diagnostic) only — no TestId/ProfileId
```

### Radiology — RECOMMENDATION

Prefer deterministic invoice-line `TestId` for the accession’s test; if not deterministic → omit Specific.

### FACT — Reuse

`TestReportManager.BuildInvoiceTestOrderLookup` already uses `SaleInvoiceDetail.RequestDetailId` for print ordering — identity linkage exists in production print workflow.

---

## 7. Template Resolution

### FACT — `ReportTemplateManager.Resolve`

Custom mode: Test Specific → Profile Specific → Custom Generic → System Default.  
SystemDefault mode: System Default only.  
Assignments filtered by `ReportType`.

### RECOMMENDATION — Production interpretation

| Resolve outcome | Production presentation |
|-----------------|-------------------------|
| System Default / `UsesBuiltInRenderer` / builtin definition | Existing Angular |
| Custom declarative + published DefinitionJson | Attempt Render |
| Found but no Published definition | Existing Angular |
| Found=false | Existing Angular |

---

## 8. Declarative Rendering

### FACT — Reuse

| Piece | Location |
|-------|----------|
| Renderer | `DeclarativeReportRenderer.Render` |
| Validator | `ReportTemplateDefinitionValidator.Validate` |
| Binding | `ReportTemplateBindingContext` |
| Layout | From DTO `Layout` / `ReportLayoutConfiguration` |

### RECOMMENDATION — Production success criteria

All required:

- Flag ON  
- Custom eligible (not System Default/builtin)  
- `Success==true`  
- `UsesBuiltInRenderer==false`  
- Non-empty Html  
- No unsupported-component soft-skip warnings (integrity)  
- Exception-free  

Else → Existing Angular.

---

## 9. Legacy Renderer Fallback

### RECOMMENDATION — Triggers → `PresentationMode=Existing`

Flag OFF; System Default; Mode SystemDefault; no active custom; unresolved Specific (safe path still may Generic); validation fail; soft-skip warnings; empty/malformed Html; render exception; any uncertainty.

Client: render existing Angular templates into the same print root. Print transport unchanged.

---

## 10. Feature Flag Architecture

### FACT — Today

| Item | Detail |
|------|--------|
| Definition | `ReportTemplateEngineFeatureFlags.UseDeclarativeRendererForProductionPrint = false` (`const`) in `ReportTemplateEnginePhase2.cs` |
| Production print | Does not read flag |
| PreviewSample / PreviewVersion / PreviewDefinition | Throw if true |
| ActivateTemplate | Throw if true |
| Config pattern in product | `ConfigurationManager.AppSettings` (`Config.GetConfigValue`, `OperationalReportsController.ReadSetting`, `Report:*` keys in `Web.config`) |

### Source contradiction (does **not** stop the plan — already approved to fix)

| Conflict | Classification |
|----------|----------------|
| Flag `true` intended to enable production declarative, but Preview/Activate throw when true | **FACT** defect vs desired Step 5 semantics |
| **Minimum correction** | Remove/invert those throws; production adapter is the sole consumer for print enablement; preview/Activate remain usable when flag ON |

### RECOMMENDATION — Minimum safe operational mechanism (no DB)

| Aspect | Design |
|--------|--------|
| **Where it lives** | `Web.config` `<appSettings>` key, e.g. `ReportTemplate:UseDeclarativeRendererForProductionPrint` |
| **How read** | Replace `const` with a fail-closed reader on `ReportTemplateEngineFeatureFlags` (e.g. property/`IsEnabled()` using `ConfigurationManager.AppSettings`) — same pattern as existing Report branding settings |
| **Default** | Missing / empty / anything not explicitly true-like → **false** |
| **Fail-closed** | Read exceptions → false → Existing Angular |
| **Who enables** | Ops / release engineer with server config ACL — **not** designer UI, **not** report users |
| **How disable** | Set key to `false` or remove key |
| **Restart** | IIS app pool recycle / app restart required for `Web.config` changes (**typical .NET Framework FACT**) |
| **Rollback** | Disable key → subsequent prints Angular; **no DB rollback**; templates intact |
| **Second flag** | **Not required** — use `ReportTemplateModeSetting` per ReportType |
| **Deploy default** | Key present as `false` or omitted |

### Explicit

- Do **not** assume DB-backed flag.  
- Do **not** enable in designer.  
- Do **not** ship deploy with key `true` unless Gate 6 approved.

### Tests impact

Phase 2/3 tests asserting `const == false` must assert **reader returns false** under default config (**RECOMMENDATION**).

---

## 11. Failure Handling

### RECOMMENDATION (Step 6)

```
try { resolve; maybe render; integrity }
catch { Existing }
integrity fail { Existing }
```

- Catch **only** in presentation adapter.  
- Do **not** convert to `TestReportValidationException` / 400 / 500.  
- Eligibility exceptions unchanged in managers/controller.

---

## 12. Observability

### RECOMMENDATION

Single logging boundary in adapter via existing `LIS.Logger.ILogger` / log4net:

| Event | Level |
|-------|-------|
| Attempt (flag ON + custom candidate) | Info |
| Success | Info |
| Fallback (with reason code) | Warning/Error by category |
| Slow (after measured baseline) | Warning |

Safe fields only: ReportType, path, TemplateId/VersionId, ResolutionSource, reason codes, ResolveMs/RenderMs, PresentationTraceId.  
**Never** PHI, Html, JSON, clinical text.

No new logging framework. No circuit breaker. No DB audit table.

---

## 13. Performance

### RECOMMENDATION (Step 6)

Measure ResolveMs and RenderMs inside adapter. Baseline scenarios before Gate 6. No invented SLA numbers in code.

Prefer: assembled DTO + one Resolve + one Render. No clinical DB inside renderer (**FACT** today).

---

## 14. RBAC

### FACT — Already integrated

Designer: `SETUP_REPORT_TEMPLATE_CONFIGURATION` → `ReportTemplateConfiguration` → routes + `QAuthorize`.  
Print: `Reports` / `RadiologyReports`.

### RECOMMENDATION — Implementation must

- Keep designer on existing RBAC (no parallel auth).  
- Add HTTP-level authorization tests + Angular RBAC catalog/spec coverage (Step 7 gaps).  
- Adapter performs **no** authentication/authorization.

---

## 15. Security

### FACT / preserve

Validator allow-lists; sample-only Admin preview; HtmlEncode; no SQL/JS execution; ReportType isolation; Specific order.

### RECOMMENDATION

Server-only production Html; client trusts via same `DomSanitizer.bypassSecurityTrustHtml` pattern as `report-template-viewer.component.ts`. No client-authored production HTML.

---

## 16. Test Strategy

**Do not claim results until executed during implementation.**

### Legacy / Flag OFF

| Area | Cases | Expect |
|------|-------|--------|
| Diagnostic | Print All/Specific; paid/unpaid; approved; sibling-pending Specific; visual parity | Angular / eligibility unchanged |
| Radiology | paid/unpaid; authorized/unauthorized; findings rules; visual parity | Angular / eligibility unchanged |

### Custom Generic / Specific

Valid/invalid/empty Generic (Diag+Rad); Test/Profile Specific; unresolved/ambiguous identity; radiology deterministic vs not; payment/sibling gates unchanged.

### Fallback → Angular

Flag OFF; System Default; no template; invalid; unsupported binding/component; soft-skip; exception; malformed/empty Html.

### RBAC

Admin menu; non-admin denial; anonymous API; non-admin API; Admin API; unauthorized Activate/Mode.

### Output / Security / Regression

A4, margins, clearance, signatures, param tables, RPG, radiology narrative, page breaks, browser print; script/SQL/HTML rejection; cross-type; target isolation; PHI-free logs; unrelated workflows untouched.

---

## 17. File-Level Implementation Plan

Speculative files omitted. Only items justified by approved design + source.

### A. Backend — modify

| File | Why | Changes | Unchanged |
|------|-----|---------|-----------|
| `LIS.DtoModel/Models/Reports/ReportTemplateEnginePhase2.cs` | Flag reconciliation | Replace `const` with fail-closed appSettings reader API | Component type constants, render DTOs structure (extend carefully) |
| `LIS.Businesslogic/ReportTemplateManager.cs` | Preview must work when flag ON | Remove throw-if-flag-true on Preview* | Resolve/CRUD core |
| `LIS.Businesslogic/ReportTemplateManager.Designer.cs` | Activate must work when flag ON | Remove throw-if-flag-true on Activate | Mode/Activate assignment rules |
| `web/Lis.Api/Controllers/Api/OperationalReportsController.cs` | Integration boundary | After enrich, call adapter; attach presentation; catch none for presentation (adapter internal) | Eligibility mapping 400/500; enrich methods’ business meaning |
| `web/Lis.Api/App_Start/SimpleInjectorConfig.cs` | DI | Register presentation adapter | Existing registrations |
| `LIS.DtoModel/Models/Reports/DiagnosticTestReportDto.cs` | Carry presentation to client | Add optional presentation payload properties (minimal) | Existing clinical fields |
| `LIS.DtoModel/Models/Reports/DiagnosticRadiologyReportDto.cs` | Same | Same | Existing clinical fields |
| `web/Lis.Api/Web.config` | Operational flag | Add appSettings key default `false` | Other settings |

### A2. Backend — add

| File | Why |
|------|-----|
| `LIS.Businesslogic/Reports/ReportProductionPresentationAdapter.cs` (name flexible) | Sole presentation decision + Resolve/Render/integrity/logging/timing |
| `LIS.DtoModel/Interfaces/IReportProductionPresentationAdapter.cs` (or equivalent) | Testable boundary |
| `LIS.DtoModel/Models/Reports/ReportPresentationDto.cs` (or nest in Phase2 file) | PresentationMode, Html, Css, FallbackReason, TraceId — non-PHI |

### A3. Context helper

| Approach | RECOMMENDATION |
|----------|----------------|
| SaleInvoiceDetail → TestId/ProfileId | Prefer **private methods inside adapter** (or thin helper in BusinessLogic) using existing UoW/repos — **do not** alter `GetDiagnosticTestReport` eligibility |

### B. Angular — modify

| File | Why | Changes | Unchanged |
|------|-----|---------|-----------|
| `test-report.component.ts` / `.html` | Bind declarative vs legacy | If Declarative+Html → trusted HTML in print root; else existing template | Filter panel, eligibility UX, `print()` transport |
| `test-report.component.ts` CSS/`getPrintStyles` | Avoid double bands | Scope styles so declarative path doesn’t double-apply conflicting chrome | Legacy path CSS behavior when Existing |
| `radiology-report-print.component.ts` / `.html` | Same | Same pattern | Accession UX; print transport |
| `report-template-configuration.component.ts` | Messaging accuracy | Update “flag remains OFF” text if needed after reader exists | Mode/Activate UX |
| Optional: `report-template-*-rbac.spec.ts` | Step 7 gap | Catalog/route rule coverage | — |

### C. Reuse as-is (Phase 2/3)

`DeclarativeReportRenderer`, `ReportTemplateDefinitionValidator`, `ReportTemplateBindingContext`, `ReportComponentRegistry`, `ReportTemplateCompatibilityDefinitions`, `ReportTemplateSampleDataFactory`, `ReportTemplateManager.Resolve` / Mode / Activate, designer Angular, `ReportLayoutConfigurationManager`, existing print iframe logic, `QAuthorize` / MenuCatalog.

### D. Tests — add/modify

| Area | Files |
|------|-------|
| Flag reader fail-closed + default OFF | Extend Phase 2/3 flag tests |
| Adapter matrix (OFF, SystemDefault, Generic, Specific, failures) | New `ReportTemplateEnginePhase4*Tests.cs` |
| Soft-skip → fallback | New |
| Diagnostic context mapping (invoice line) | New (no guess HISTestCode) |
| HTTP RBAC template API | New security tests |
| Angular RBAC spec | New/extend |
| Existing Phase 1–3 regressions | Run; modify only if flag API shape changes |

### E. Configuration

`web/Lis.Api/Web.config` (+ deployed `Lis.Api.dll.config` / transform if used in release pipeline) — key default false.  
**No** Angular environment flag required for master switch (server authoritative).

### F–G. Migration / deployment SQL

**NO DB CHANGE REQUIRED** → no new migration, no new deployment SQL for Phase 4 presentation enablement.  
Existing Report Template Configuration RBAC SQL/migration already exists from Phase 3 — verify present on target; do not recreate unless missing (ops verification, not redesign).

---

## 18. Implementation Sequence

**Do not execute until architect authorizes implementation.**

1. **Flag/configuration reconciliation** — appSettings reader; default false; remove Preview/Activate throws; update flag tests.  
2. **Presentation DTOs + interface** — minimal payload types.  
3. **Presentation adapter** — decision matrix, integrity gate, logging, timing; no controller wiring yet.  
4. **Resolution-context helpers** — Diagnostic Specific/All; Radiology TestId rules.  
5. **Diagnostic integration** — `GetTestReport` post-enrich call.  
6. **Radiology integration** — `GetRadiologyReport` post-enrich call.  
7. **Angular bind + print root** — Diagnostic then Radiology; DomSanitizer pattern; no print pipeline change.  
8. **Failure/observability verification** — ensure no 500 on presentation faults.  
9. **RBAC HTTP + Angular tests**.  
10. **Rendering / fallback / regression tests**.  
11. **Build** — BusinessLogic, Api, Angular prod.  
12. **Deployment verification** — flag OFF; smoke legacy print.  
13. **Migration/SQL** — skip if still no DB change; else follow mandatory migration rule.  
14. **Stop** — Gate 6 separate.

---

## 19. Database / Migration Plan

**NO DB CHANGE REQUIRED** for approved Phase 4 implementation.

If implementation discovery forces a schema change (unexpected):

1. Stop and obtain architect approval if outside this plan.  
2. Create/update EF migration + deployment SQL to final model.  
3. Test on target path; include in final implementation report.

Flag must **not** be DB-backed under this plan.

---

## 20. Deployment Plan

| Step | Action |
|------|--------|
| Build | Backend Release + `Lis.Web` production build |
| Config | Ensure `ReportTemplate:UseDeclarativeRendererForProductionPrint` = `false` or absent |
| Deploy | Api + Web artifacts per site SOP |
| IIS | App pool recycle as required for config/binaries |
| DB | No Phase 4 schema migrate unless gap found |
| Smoke | Flag OFF Diagnostic + Radiology print; designer open for Admin |
| **Flag** | Remains OFF unless Gate 6 |

---

## 21. Rollback Plan

### Preferred (presentation)

```
Set appSettings flag false (or remove) → recycle app pool
→ subsequent reports use Existing Angular
```

No template deletion; no data rollback; no workflow rollback.

### Soft-stop

`ReportTemplateModeSetting.Mode = SystemDefault` for a ReportType (Admin CanEdit) while flag remains ON.

### Full app rollback

Only if binary/deploy defect — standard prior-release redeploy; DB unchanged for this feature.

---

## 22. Production Activation Gate

| Gate | Meaning |
|------|---------|
| 1 | Implementation complete |
| 2 | Automated tests complete |
| 3 | Build complete |
| 4 | Deployment verification (flag OFF) |
| 5 | Browser/E2E validation |
| 6 | **Explicit production activation approval** |

Only Gate 6 authorizes setting the appSettings flag to enable declarative production printing.  
Gates 1–5 **do not** auto-enable.

---

## 23. Risks / Unknowns

| Item | Tag | Mitigation |
|------|-----|------------|
| Step 1 design file missing in repo | **FACT** | Decisions locked via Steps 2–7 + this plan |
| Page-break parity System Default vs Angular | **UNKNOWN** | Keep System Default on Angular |
| Soft-skip → incomplete HTML | **FACT** | Integrity failure → Angular |
| Web.config recycle lag / multi-server sync | **UNKNOWN** / ops | Document per-site |
| Double CSS if Angular print styles wrap declarative HTML | **RISK** | Scope CSS to mode |
| Preview/Activate throw until fixed | **FACT** | Sequence step 1 first |
| Exact Radiology invoice↔accession TestId determinism edge cases | **UNKNOWN** | Do not guess; omit Specific |

### Contradictions requiring STOP?

**None identified that make the approved architecture impossible.**  
The flag throw-when-true issue has an approved **minimum correction** (reconcile semantics; appSettings reader). No alternate architecture invented.

---

## 24. Explicit Non-Goals

- Implementing in this Step 8  
- Enabling production flag  
- Deploying  
- Declarative System Default parity project  
- Second Diagnostic/Radiology feature flag  
- Server PDF / new print engine  
- Partial section mixing  
- Parallel RBAC  
- DB-backed flag  
- Fixing unrelated defects  
- Gate 6 activation  

---

## 25. Final Traceability Matrix

| Concern | Design step | Primary source symbols |
|---------|-------------|------------------------|
| Eligibility Diagnostic | 2, 7 | `TestReportManager.GetDiagnosticTestReport` |
| Eligibility Radiology | 2, 7 | `RadiologyReportManager.GetRadiologyReportForPrint` |
| Enrichment | 2, 4 | `OperationalReportsController.Enrich*` |
| Presentation boundary | 2, 4, 8 | Adapter after enrich / before `Ok` |
| Specific context | 3 | `SaleInvoiceDetail.RequestDetailId` → TestId/ProfileId |
| Resolve | 3, 5 | `ReportTemplateManager.Resolve` |
| Render | 4 | `DeclarativeReportRenderer.Render` |
| Layout | 4 | `ReportLayoutConfiguration` / DTO.Layout |
| Flag | 5, 8 | `ReportTemplateEngineFeatureFlags` + `Web.config` appSettings |
| Mode soft-stop | 5 | `ReportTemplateModeSetting` / `SetMode` |
| Failure/log | 6 | Adapter + `ILogger` |
| RBAC designer | 7 | `ReportTemplateConfigurationController`, MenuCatalog, guards |
| RBAC print | 7 | `Reports` / `RadiologyReports` QAuthorize |
| Print transport | 4 | `test-report` / `radiology-report-print` `print()` |
| Activation | 7, 8 | Gates 1–6 |

---

## Change Integrity (Step 8 deliverable)

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

**Only this document was created:**  
`docs/Report-Template-Engine-Phase4-Final-Architecture-and-Implementation-Plan.md`

---

STEP 8 STATUS: PROPOSED — PENDING USER APPROVAL
