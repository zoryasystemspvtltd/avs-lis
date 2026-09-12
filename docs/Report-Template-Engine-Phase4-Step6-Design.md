# Report Template Engine — Phase 4 Step 6 Design

**Project:** ZoryaLMS / AVILIS  
**Date:** 2026-09-12  
**Phase:** 4 — Design Step 6 (Failure Handling, Observability & Performance)  
**Status:** PROPOSED — PENDING USER APPROVAL  
**Prerequisites:** Step 1–5 APPROVED  

**Scope:** Design / source inspection only. **No implementation. Flag remains OFF.**

---

## 1. Executive Summary

### FACT — Today’s production error model

| Layer | Behavior |
|-------|----------|
| Eligibility / DTO (`TestReportManager` / `RadiologyReportManager`) | Throws `TestReportValidationException` (business) or other exceptions |
| `OperationalReportsController.GetTestReport` / `GetRadiologyReport` | Validation → **400** + `LogError`; unexpected → **500** + `LogException` + generic message |
| Angular print UIs | `alertService.error` on HTTP failure; print only after successful load |
| Declarative renderer | Used for **Admin preview only**; not on production print path |
| Production presentation adapter | **Not implemented** (Steps 2–5 design only) |

### RECOMMENDATION — Step 6 core architecture

```
Eligibility success + enrichment
        ↓
Presentation adapter (single catch / single diagnostic boundary)
        ↓
  ┌─────┴─────┐
  │ success   │ failure / ineligible for declarative
  ▼           ▼
Declarative   Existing Angular (whole-report)
HTML in API   PresentationMode=Existing
        ↓
Same browser iframe / window.print path
```

**Hard rules:**

1. Presentation failure **must not** become HTTP 400/500 eligibility failure.  
2. Report user sees **normal print** via existing Angular when declarative fails.  
3. One primary diagnostic log event per presentation decision (avoid duplicate layers).  
4. Use existing `LIS.Logger.ILogger` / log4net — **no new logging framework**.  
5. No PHI in logs; no report HTML/JSON.  
6. Measure Resolve vs Render separately from clinical DTO time.  
7. No circuit breaker / async timeout service unless later evidence demands it — ops disable flag.

---

## 2. Existing Error / Exception Handling

### FACT — Diagnostic print API

| Step | Source | Behavior |
|------|--------|----------|
| Load | `TestReportManager.GetDiagnosticTestReport` | Payment, approval, readiness → `TestReportValidationException` |
| Enrich | `OperationalReportsController` · `EnrichLabApprover` / `EnrichLabTechnician` / `EnrichReportBranding` / `EnrichReportLayout` | Runs only after manager success |
| Catch | `GetTestReport` | `TestReportValidationException` → `logger.LogError` + **400**; other → `logger.LogException` + **500** `"Unable to load test report."` |

### FACT — Radiology print API

| Step | Source | Behavior |
|------|--------|----------|
| Load | `RadiologyReportManager.GetRadiologyReportForPrint` | Authorized/Released, payment, findings/impression → `TestReportValidationException` |
| Enrich | `EnrichRadiologyApprover` / `EnrichRadiologyLayout` | After success |
| Catch | `GetRadiologyReport` | Same 400 / 500 pattern as Diagnostic |

### FACT — Angular client

- `test-report.component.ts` / `radiology-report-print.component.ts`: subscribe error → `alertService.error(readError(...))`.  
- Print (`print()`) runs against already-loaded report DOM; iframe/`window.print` is a **client transport fallback**, not a presentation-engine fallback.

### FACT — Declarative engine

| Path | Source | Behavior |
|------|--------|----------|
| Validate | `ReportTemplateDefinitionValidator.Validate` | Throws `ArgumentException` on invalid/unsafe definition |
| Render entry | `DeclarativeReportRenderer.Render` | Null request / missing data → throw; builtin → `Success` + `UsesBuiltInRenderer`, no HTML |
| Unknown component | `RenderComponent` default | `warnings.Add("Skipped unsupported...")` — **does not throw** |
| Designer validate | `ReportTemplateManager.ValidateDesign` | Catches `ArgumentException` → `Errors`; unexpected → `LogException` + generic error string |

### FACT — Admin designer API

`ReportTemplateConfigurationController` methods catch `Exception` → `LogException` (and typically error HTTP). Separate from production print.

### FACT — No production presentation catch today

Because the adapter is not wired, there is **no** current “declarative fail → Angular” path on print GETs.

### RECOMMENDATION — Catch boundary (future implementation)

Catch **inside the presentation adapter only** (post-enrichment). Re-throw **nothing** for presentation failures; return `PresentationMode=Existing`.  
Do **not** wrap eligibility/manager calls in the adapter.  
Do **not** let adapter exceptions escape to the controller’s generic 500 handler.

---

## 3. Existing Logging Infrastructure

### FACT — Framework

| Item | Traceability |
|------|--------------|
| Interface | `LIS.Logger/ILogger.cs.cs` · `ILogger` |
| Implementation | `LIS.Logger/Logger.cs` · log4net wrapper |
| Levels | `LogTrace`, `LogDebug`, `LogInfo`, `LogWarning`, `LogError`, `LogFatal`, `LogException` |
| API usage | Controllers inject `ILogger`; managers often receive `ILogger` |
| Config | `web/Lis.Api/Web.config` · `<log4net>` rolling file appenders |
| Pattern | `%date %level - %message%newline` |

### FACT — What is / is not present

| Capability | Present? |
|------------|----------|
| File-based INFO / ERROR logs | **Yes** |
| Structured JSON telemetry / APM | **No** evidence in report path |
| Built-in request correlation ID in log pattern | **No** (`%message` only; `GlobalContext` sets `host`) |
| Report-print timing helpers | **No** |
| Stopwatch reuse elsewhere | **Yes** — e.g. `NotificationProcessor` (notifications domain, not reports) |
| Notification `CorrelationId` | **Yes** on notification audits — **not** wired to report print |

### RECOMMENDATION

Reuse `ILogger` + existing log4net appenders. Emit **formatted non-PHI message strings** (same style as current `LogInfo` / `LogWarning` / `LogError` usage). Do not add Serilog/AppInsights/etc. in Phase 4.

### UNKNOWN

Whether ops already scrape `log\*.txt` / `exceptions.txt` with alerting — organizational, not encoded in report code.

---

## 4. Failure Taxonomy

| ID | Category | Expected? | Log level (RECOMMENDATION) | Fallback | User-visible | Ops significance |
|----|----------|-----------|----------------------------|----------|--------------|------------------|
| **A** | Normal fallback | **Yes** (common) | `LogInfo` (or omit high-volume “flag OFF”) | Existing Angular | Normal report | Low — expected path |
| **B** | Template/configuration failure | Unexpected if Activated | `LogWarning` or `LogError` | Existing Angular | Normal report | Medium–High — fix template |
| **C** | Renderer execution failure | Unexpected | `LogError` (+ exception type via `LogException` **once** if useful, sanitized message) | Existing Angular | Normal report | High |
| **D** | Output/layout compatibility failure | Unexpected | `LogWarning` / `LogError` | Existing Angular | Normal report | Medium — UAT/page-break |
| **E** | Performance degradation | Situational | `LogWarning` when duration exceeds **site-established** baseline (after measurement) | Still complete print (declarative or Angular per result) | Normal report (possibly slower) | Medium — tune/disable |

### Classification

Taxonomy itself is **RECOMMENDATION**. Mapping of concrete source behaviors into A–E below mixes **FACT** (what code does) with **RECOMMENDATION** (how production adapter should classify).

---

## 5. Normal Fallback Behavior

### FACT / designed triggers (Steps 4–5)

| Trigger | Classification |
|---------|----------------|
| Flag OFF | **A** — expected |
| System Default / builtin | **A** — locked |
| Mode Custom but no active custom | **A** — Resolve → System Default → Angular |
| Unresolved Specific context (safe omit TestId/ProfileId) | **A** — may still hit Generic (**FACT** Resolve) or System Default |
| `Found=false` / no published definition | **A** or **B** if assignment exists without Published version |

### RECOMMENDATION — Observability for A

| Event | Level | Volume control |
|-------|-------|----------------|
| Flag OFF | Prefer **no per-print log** (noise) OR rare `LogDebug` | High volume |
| Chose Existing due to SystemDefault/builtin | Optional `LogInfo` with reason code when flag ON | Medium |
| UnresolvedSpecificSafePath → Generic/SystemDefault | `LogInfo` when flag ON | Low–medium |

These are **not errors**. Do not `LogException`. Do not surface to the user.

---

## 6. Template Failure Handling

### FACT — Validation behavior

- `ReportTemplateDefinitionValidator.Validate` throws `ArgumentException` for empty JSON, unsafe content, bad schema, unknown components/bindings/styles (at validate time), nesting limits, etc.  
- `ActivateTemplate` requires `ValidateDesign` success before assignment (**FACT** Phase 3).  
- Runtime `Render` re-validates via `Validate` at start (**FACT**).

### FACT — Soft unsupported at render

Unknown component type at render → warning + skip (**FACT** `DeclarativeReportRenderer.RenderComponent`). This can yield **incomplete** but `Success=true` HTML.

### RECOMMENDATION — Production path for B

```
Declarative attempt
  → Validate / detect unsupported critical definition issues
  → safe failure (no declarative Html attached)
  → diagnostic event (category B, reason code)
  → PresentationMode=Existing
  → existing Angular print
```

**Do not** allow invalid or integrity-failed declarative output onto the print path.

### RECOMMENDATION — Integrity gate before Declarative success

Treat as failure → Angular if any of:

- Validation throw  
- `Success != true`  
- `UsesBuiltInRenderer == true` (production must use Angular, not twin)  
- Html null/whitespace  
- Malformed wrapper (e.g. missing expected root class — implementation detail)  
- **RECOMMENDATION:** non-empty `Warnings` containing `Skipped unsupported component` → treat as **B/D failure** for production (prefer Angular over silently incomplete Custom print)

### UNKNOWN

Whether soft-skipped components are always unacceptable for every Custom template — prefer fail-closed to Angular for production until Admin tooling guarantees Activate-time validation covers all types.

---

## 7. Renderer Failure Handling

### FACT

`DeclarativeReportRenderer.Render` can throw (`ArgumentNullException`, `ArgumentException`, binding unsupported type, JSON issues after validate edge cases). It does **not** return `Success=false` for exceptions — callers must catch.

### RECOMMENDATION — Exact catch boundary

```
try {
  resolve (+ time)
  if not declarative-eligible → Existing (A)
  render (+ time)
  integrity check
  if ok → Declarative
  else → Existing (B/D) + log
}
catch (Exception) {
  Existing (C) + single diagnostic log
  // do not rethrow
}
```

Location: **presentation adapter** (shared Diagnostic/Radiology), invoked **after** enrichment inside the successful try of `GetTestReport` / `GetRadiologyReport` (or equivalent), **before** `return Ok(...)`.

### Explicit

Do **not** move this catch into `TestReportManager` / `RadiologyReportManager` eligibility logic.

### RECOMMENDATION — Null / unexpected result

Null adapter result or null render DTO → Existing + log category C.

---

## 8. User-Facing Error Behavior

### RECOMMENDATION

| Situation | User sees |
|-----------|-----------|
| Eligibility failure | Existing **400** business message (unchanged) |
| Unexpected DTO/enrichment failure | Existing **500** generic load message (unchanged) |
| Declarative presentation failure | **No technical error** — report loads; Angular HTML; print works |
| Angular render/print client failure | Existing client alerts / browser print behavior |

### FACT supporting “no user exception for presentation”

Controller already returns `Ok(report)` on success path; attaching `PresentationMode=Existing` without throwing preserves that contract (Step 2 design).

### RECOMMENDATION

Do not put declarative exception text into API error bodies for print GETs. Optional non-PHI `FallbackReason` on success payload for Admin diagnostics is acceptable (Step 2) but **must not** include stack traces or definition snippets with risk.

---

## 9. Duplicate Logging Prevention

### FACT — Current call hierarchy risk

If adapter logged `LogException` **and** rethrew, controller would `LogException` again → duplicate (**FACT** pattern on other endpoints).

### RECOMMENDATION — Single boundary

| Layer | Logs presentation outcomes? |
|-------|----------------------------|
| `DeclarativeReportRenderer` | **No** (keep pure; no ILogger today — **FACT**) |
| `ReportTemplateManager.Resolve` | **No** extra production decision logs |
| **Presentation adapter** | **Yes — sole owner** of attempt/success/fallback/timing |
| `OperationalReportsController` | Logs **eligibility/load** failures only; does **not** re-log adapter fallbacks |
| Angular | No server diagnostic logs |

### RECOMMENDATION — Exception logging

For category C: either

- `LogError` with sanitized reason + exception **type/name** (not full dump of clinical data), **or**  
- `LogException` **once** in adapter with a **prefix message** that contains only safe fields  

Never both adapter `LogException` and controller `LogException` for the same presentation fault.

---

## 10. Observability Design

### RECOMMENDATION — Event vocabulary (string messages via `ILogger`)

| Event code | When | Level |
|------------|------|-------|
| `RTE_PRESENTATION_EXISTING` | Chose Angular without attempting render (A) | Info (optional) |
| `RTE_PRESENTATION_ATTEMPT` | Flag ON + custom declarative candidate | Info |
| `RTE_PRESENTATION_SUCCESS` | Integrity-passed declarative | Info |
| `RTE_PRESENTATION_FALLBACK` | Any fallback after attempt or integrity fail | Warning (B/D) or Error (C) |
| `RTE_PRESENTATION_SLOW` | Duration over baseline gate (E) | Warning |

### RECOMMENDATION — Safe fields in message

| Field | Allowed? |
|-------|----------|
| ReportType (Diagnostic/Radiology) | Yes |
| Rendering path (`Existing` / `Declarative`) | Yes |
| ResolutionSource / template category | Yes |
| TemplateId / VersionId / VersionNumber | Yes |
| FallbackReason code | Yes |
| Failure category A–E | Yes |
| ResolveMs / RenderMs / TotalPresentationMs | Yes |
| Flag state | Yes |
| Mode | Yes |
| Exception type name | Yes (C) |
| Correlation token | Yes if available **without PHI** |

### RECOMMENDATION — Correlation

**FACT:** Report print logs lack a first-class correlation ID in log4net pattern.  

**RECOMMENDATION:** Include a short opaque `PresentationTraceId` (e.g. `Guid` N-format) generated in the adapter and placed in the log message (and optionally echoed on the API presentation DTO for support). Do **not** require a new logging framework or DB table for this.

### Do NOT log (mandatory)

Patient name, MRN, phone, clinical results, findings, impression, comments, signatures/images, report HTML, report JSON, full definition JSON, raw exception messages if they might embed values (prefer type + stable reason code).

---

## 11. PHI / Security Considerations

### FACT

- Validator forbids PHI-like JSON keys and unsafe executable patterns in definitions.  
- Renderer binds from already-authorized assembled DTO; no extra clinical queries.  
- Print APIs are QAuthorize’d (`Reports` / `RadiologyReports`).

### RECOMMENDATION

| Risk | Mitigation |
|------|------------|
| Logging `ex.Message` from binding | Prefer reason codes; avoid logging raw messages that might include field values |
| Logging Html length only | OK; never Html body |
| Support copies logs | Treat logs as potentially sensitive still — minimize |
| FallbackReason to client | Codes only (`ValidationFailed`, `RenderException`, …) |

Operational diagnostics must not create a new report-content viewer.

---

## 12. Performance Measurement

### FACT

No Stopwatch around Diagnostic/Radiology print or declarative render today.  
`System.Diagnostics.Stopwatch` is already used elsewhere (`NotificationProcessor`) — reusable pattern, not a new dependency.

### RECOMMENDATION — Measure inside presentation adapter only

| Timer | Includes | Excludes |
|-------|----------|----------|
| **A. ResolveMs** | Mode/assignment/version reads for presentation | Clinical DTO assembly, enrichment |
| **B. RenderMs** | `Validate` + `DeclarativeReportRenderer.Render` + integrity checks | Angular client render, browser print, eligibility DB |
| **C. PresentationTotalMs** | A+B (+ decision overhead) | Manager Get*Report time |

Log A/B on Attempt/Success/Fallback when flag ON (or always sample in non-prod).

### Explicit non-confusion

Do not treat end-to-end HTTP duration as “renderer performance” without subtracting eligibility/DTO time.

---

## 13. Performance Baseline

### RECOMMENDATION — Future comparison matrix (measure later; no claimed numbers now)

| Scenario | Legacy (Angular path) | Declarative (when eligible) |
|----------|----------------------|-----------------------------|
| Diagnostic simple | E2E load + note PresentationTotal≈0 | Resolve+Render vs Angular-only |
| Diagnostic multi-test (Print All) | Same | Single Resolve (Step 3) + render |
| Diagnostic parameter-heavy | Same | Render CPU focus |
| Diagnostic Specific custom | N/A until custom | Specific resolve + render |
| Diagnostic Generic custom | N/A until custom | Generic resolve + render |
| Radiology | Baseline | Hold Mode SystemDefault initially |
| Radiology custom | N/A until enabled | After UAT |

### Classification

| Claim | Tag |
|-------|-----|
| Baseline design required before broad ON | **RECOMMENDATION** |
| Actual ms numbers | **UNKNOWN** (not measured in this step) |
| Hard SLA thresholds | **Do not invent** without evidence |

---

## 14. DB / Service Call Analysis

### FACT — Declarative render path

| Component | DB / service? |
|-----------|----------------|
| `DeclarativeReportRenderer` | **No** — DTO + definition in memory |
| `ReportTemplateBindingContext` | **No** — seeds from DTO |
| `ReportTemplateDefinitionValidator` | **No** |

### FACT — Resolve path (when invoked)

`ReportTemplateManager.Resolve` / `BuildResolveFromTemplate` reads Mode, assignments, templates, published versions via repos (**DB**).

### FACT — Preferred pipeline (Steps 2–4)

```
already assembled + enriched DTO
  + Resolve (template metadata)
  → Render
```

No clinical/business lookup inside the renderer (**FACT** + locked principle).

### RECOMMENDATION

- Call Resolve **at most once** per print GET (Step 3 Print All).  
- Do not re-Resolve inside Render.  
- Optional SaleInvoiceDetail context lookup for TestId (Step 3) is presentation-context only — keep minimal and outside renderer.  
- Do not optimize further in Step 6.

### Serialization

Attaching Html/Css to API response adds payload size when Declarative succeeds — **expected**; not a DB cost.

---

## 15. Timeout Analysis

### FACT

- `DeclarativeReportRenderer.Render` is **synchronous**; no `CancellationToken`, no timeout parameter.  
- Print controllers are synchronous Web API actions.  
- No reusable presentation-level timeout middleware found for this path.

### Classification

| Claim | Tag |
|-------|-----|
| Safe presentation timeout exists today | **UNKNOWN** / effectively **not supported** without new infra |
| Introducing async queues/workers for print | **Non-goal** (locked) |

### RECOMMENDATION — Fallback strategy without timeout infra

1. Rely on IIS/ASP.NET request limits already governing the API (ops-level — exact values **UNKNOWN** here).  
2. Prefer fail-closed Angular via exception catch if render faults.  
3. Use duration logging + ops disable flag if renders are slow (category E).  
4. Do **not** invent async architecture or worker queues for Phase 4.

---

## 16. Repeated Failure Handling

### FACT

No circuit-breaker library or report-template failure counter in source.

### RECOMMENDATION — Initial model (Step 5 aligned)

```
failure → fallback → observability → operator disables feature flag
         (and/or Mode → SystemDefault soft-stop)
```

Do **not** auto-disable the flag in code without an explicit later product decision.  
Repeated `RTE_PRESENTATION_FALLBACK` in logs is the operational signal.

---

## 17. Feature Flag Interaction

### RECOMMENDATION / confirm (Step 5)

```
Flag OFF → Existing renderer (no attempt) → optional quiet logging
Flag ON  → if custom declarative eligible → Attempt → Success or Fallback→Existing
Failure  → Existing renderer
```

**Do not change the flag in this step** (remains OFF).

Preview/Activate throw-when-true quirk remains an implementation prerequisite before ON (Step 5) — out of scope to fix here.

---

## 18. Eligibility Safety

### FACT — Boundary today

Eligibility exceptions are thrown in managers **before** enrichment completes; controller maps them to **400**. Successful path returns DTO.

### RECOMMENDATION — Preserve distinction

| Case | Rendering | Fallback that bypasses eligibility? |
|------|-----------|-------------------------------------|
| Eligibility failure | **No rendering** | **Forbidden** — no Angular print of ineligible data |
| Eligibility success + presentation failure | Existing Angular using **same** eligible enriched DTO | **Required** |

Presentation adapter runs **only** on eligibility success path.

### Explicit

A presentation failure must **never** be converted into `TestReportValidationException` or payment/approval messaging.

---

## 19. Production Observability Requirements

### RECOMMENDATION — Minimum evidence before/during rollout

| Signal | Required |
|--------|----------|
| Declarative attempt | Yes (when flag ON and candidate) |
| Declarative success | Yes |
| Declarative fallback | Yes |
| Fallback reason code | Yes |
| ResolveMs / RenderMs | Yes (for acceptance gate) |

### FACT

Current infrastructure **cannot** provide these for production print because the adapter is not wired and the renderer has no logger.

### RECOMMENDATION

Treat structured `ILogger` events above as a **later implementation requirement** of the presentation adapter (same Phase 4 implementation wave as wiring — not Step 6).  
Prefer **log files only** — **no DB audit table** for Step 6.

---

## 20. Performance Acceptance Gate

### RECOMMENDATION — Future gate (post-measurement)

1. Capture baselines for scenarios in §13 on a representative environment.  
2. Compare **PresentationTotalMs** (declarative) vs near-zero presentation overhead (Angular path) **plus** qualitative print UAT.  
3. Compare **full HTTP time** only as secondary (includes unchanged eligibility/DTO).  
4. Accept enablement only if:  
   - no correctness regressions (fallback works; eligibility unchanged), and  
   - no **unacceptable** degradation vs baseline **as judged by architect/ops after seeing real numbers**.  
5. **Do not** hard-code thresholds in design without measurements.

### Classification

Gate process = **RECOMMENDATION**. Numeric pass/fail cutoffs = **UNKNOWN** until measured.

---

## 21. Database Implications

### FACT / conclusion

Observability via existing log4net files does **not** require new tables.

**NO NEW DB CHANGE REQUIRED** for Step 6 failure-handling / observability / performance design.

If a future product wants persisted presentation audit trails, that would be a separate requirement (migration + deployment SQL in the implementing phase) — **not authorized now**.

---

## 22. RBAC Implications

### FACT

- Print: `Reports` / `RadiologyReports` CanView.  
- Designer: `ReportTemplateConfiguration`.  

### RECOMMENDATION

- No new authorization for fallback.  
- Log access remains server/ops filesystem privilege — not an end-user report API.  
- Do not expose report HTML through a new diagnostic endpoint.  
- Template configuration RBAC unchanged.

---

## 23. Risks / Unknowns

| Item | Tag | Mitigation |
|------|-----|------------|
| Soft-skip components with Success=true | **FACT** risk | Production integrity gate → Angular |
| No request correlation in log pattern | **FACT** | Adapter `PresentationTraceId` in message |
| Exact IIS timeout values | **UNKNOWN** | Ops documentation; duration warnings |
| When “slow” becomes unacceptable | **UNKNOWN** | Measure then set gate |
| Controller enrichment exceptions | **FACT** still 500 | Unrelated to presentation; leave unchanged |
| Duplicate logging if adapter rethrows | **RISK** | Never rethrow presentation faults |
| PHI via exception.ToString() | **RISK** | Sanitize; prefer reason codes |

---

## 24. Recommended Step 6 Architecture

```
[Eligible enriched DTO]
        │
        ▼
┌───────────────────────────────────────┐
│ Presentation adapter (sole boundary)  │
│  • flag / mode / resolve (time A)     │
│  • render (time B)                    │
│  • integrity gate                     │
│  • catch-all → Existing               │
│  • ONE ILogger decision event         │
└───────────────────────────────────────┘
        │
        ├─ Declarative success → attach Html/Css
        └─ Else → Existing (whole-report)
        │
        ▼
HTTP 200 Ok(report + presentation metadata)
        │
        ▼
Angular binds Declarative OR legacy template
        │
        ▼
Existing iframe / window.print
```

**Taxonomy:** A normal · B template · C renderer · D output · E performance — all fallback to Angular except pure success.

**Timeout/circuit breaker:** not introduced; ops use flag/Mode + logs.

---

## 25. Explicit Non-Goals

- Implementation of adapter, logging, or timers  
- Enabling the feature flag  
- New logging frameworks / APM products  
- DB audit tables / migrations / SQL  
- Async render queues / worker services  
- Automatic circuit breakers  
- Invented numeric SLA thresholds  
- Changing eligibility messages or manager throws  
- Mixing declarative + legacy sections  
- Server PDF  
- Approving Step 7 or starting implementation  

---

## Change Integrity (Step 6 deliverable)

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
`docs/Report-Template-Engine-Phase4-Step6-Design.md`

---

STEP 6 STATUS: PROPOSED — PENDING USER APPROVAL
