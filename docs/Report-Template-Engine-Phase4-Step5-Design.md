# Report Template Engine — Phase 4 Step 5 Design

**Project:** ZoryaLMS / AVILIS  
**Date:** 2026-09-12  
**Phase:** 4 — Design Step 5 (Feature Flag & Controlled Rollout)  
**Status:** PROPOSED — PENDING USER APPROVAL  
**Prerequisites:** Step 1–4 APPROVED  

**Scope:** Design / source inspection only. **No implementation. Flag remains OFF.**

---

## 1. Executive Summary

### FACT — Production enablement switch today

```csharp
public static class ReportTemplateEngineFeatureFlags
{
    public const bool UseDeclarativeRendererForProductionPrint = false;
}
```

| Attribute | Value |
|-----------|--------|
| Location | `LIS.DtoModel/Models/Reports/ReportTemplateEnginePhase2.cs` · `ReportTemplateEngineFeatureFlags` |
| Type | `const bool` (compile-time) |
| Default | `false` |
| Config source | **None** — not read from `Web.config` / `appSettings` / DB |
| Production print consumers | **None today** — Diagnostic/Radiology print paths never consult it |
| Current consumers | Admin **preview** and **Activate** paths that **throw if the flag is true** |

### FACT — Existing per-report-type controls (not the production flag)

| Control | Granularity | Effect on Resolve |
|---------|-------------|-------------------|
| `ReportTemplateModeSetting` | Per `ReportType` (Diagnostic / Radiology) | `SystemDefault` → factory only; `Custom` → Specific → Generic → System Default |
| Activate / Deactivate | Per custom template | Creates/clears active assignment used by `Resolve` |
| Save / Publish (via Activate) | Template version | Does not by itself change production print while flag is OFF |

### RECOMMENDATION — Step 5 controlled-rollout architecture

1. Keep **`UseDeclarativeRendererForProductionPrint = false`** until an explicit later enablement gate.  
2. Treat the flag as the **sole production presentation master switch** (application-wide binary).  
3. Do **not** invent a second feature flag for Diagnostic vs Radiology.  
4. Use **existing** `ReportTemplateModeSetting` + Activate to scope which report types / templates can resolve to Custom declarative when the flag is later ON.  
5. Even with flag ON: **System Default / builtin → existing Angular renderer** (Step 4 locked).  
6. Before any flag flip: **reconcile** the Phase 2/3 “throw if flag true” preview/Activate guards (they would otherwise break Admin when production is enabled).  
7. Flag OFF after any deployment ⇒ **exact existing production behavior**.

### Classification

| Claim | Tag |
|-------|-----|
| Flag is compile-time `const false` | **FACT** |
| Flag is not in Web.config today | **FACT** |
| Production print ignores the flag today | **FACT** |
| Diagnostic/Radiology can be scoped via Mode without a second flag | **RECOMMENDATION** (uses existing Mode) |
| Runtime/config-file flag without rebuild | **RECOMMENDATION** for a later implementation step if ops need non-redeploy rollback — **not required for Step 5 design correctness** |

---

## 2. Existing Feature Flag Implementation

### FACT — Definition

| Item | Traceability |
|------|--------------|
| Class | `ReportTemplateEngineFeatureFlags` |
| Member | `UseDeclarativeRendererForProductionPrint` |
| File | `LIS.DtoModel/Models/Reports/ReportTemplateEnginePhase2.cs` |
| Comment | “Phase 2 safety switch. Production print must never read this as true unless a later phase explicitly flips it.” |

### FACT — Read mechanism

Direct C# compile-time constant reference. No `ConfigurationManager`, no environment variable, no DB column, no Angular environment flag.

### FACT — Consumers (complete set found in source)

| Consumer | Method | Behavior when flag == `true` |
|----------|--------|------------------------------|
| `ReportTemplateManager` | `PreviewSample` | Throws `InvalidOperationException` (“must remain false in Phase 2”) |
| `ReportTemplateManager` | `PreviewVersion` | Throws (same) |
| `ReportTemplateManager` | `PreviewDefinition` | Throws (same) |
| `ReportTemplateManager` (Designer partial) | `ActivateTemplate` | Throws (“must remain false in Phase 3”) |

**Not consumers (FACT):**

- `OperationalReportsController.GetTestReport` / `GetRadiologyReport`
- `TestReportManager` / `RadiologyReportManager`
- Angular `test-report` / `radiology-report-print`
- `DeclarativeReportRenderer.Render` (documents intent in comments only)
- `Resolve`

### FACT — Environment behavior

Same binary value for all environments that run that build. Changing the value requires a **code change + rebuild + redeploy** of assemblies that embed the constant (DtoModel and dependents).

### FACT — Diagnostic vs Radiology control via this flag

**Application-wide only.** One boolean. Cannot distinguish Diagnostic from Radiology.

### FACT — Tests asserting OFF

- `LIS.Masters.Tests/Reports/ReportTemplateEnginePhase2Tests.cs` · `FeatureFlag_ProductionDeclarativePrint_Is_Off`
- `LIS.Masters.Tests/Reports/ReportTemplateEnginePhase3Tests.cs` · `FeatureFlag_Still_Off`

### FACT — UI messaging (awareness, not control)

`report-template-configuration.component.ts` · `setMode`: when switching to Custom, message states activated templates may apply at resolution but **“production print flag remains OFF”**. No UI control flips the production flag.

---

## 3. Flag Default / Fail-Closed Behavior

### FACT — Current mechanism

| Condition | Behavior |
|-----------|----------|
| Flag = `false` | Current production: Angular always for print; preview/Activate allowed |
| Flag “missing” | **N/A** — compile-time const always present |
| Flag “malformed” | **N/A** — not string/config-parsed |
| Configuration unavailable | **N/A** — no configuration read |

### RECOMMENDATION — Fail-closed principle (locked)

> Any uncertainty → existing Angular renderer.

When production wiring is later implemented, the presentation adapter must treat:

| Situation | Presentation |
|-----------|--------------|
| Flag false / disabled | Existing Angular |
| Flag unreadable / parse failure (if moved to config) | Existing Angular |
| Config section missing | Existing Angular |
| Exception reading enablement | Existing Angular |

### FACT — Natural support today

The **const `false`** naturally fail-closes production (nothing consults it for print). It does **not** yet implement “ON → declarative” because production paths ignore it.

### RECOMMENDATION — If implementation later moves to `appSettings`

Prefer:

```text
absent / empty / non-true → false → Existing Angular
```

Only an explicit true-like value enables declarative production attempts.  
**Do not implement in Step 5.** Documented as implementation requirement only.

### CRITICAL FACT — Inverted safety on preview/Activate

Today, flag `true` **breaks Admin preview and Activate**. That is the opposite of a production master switch.

**RECOMMENDATION (implementation prerequisite, not Step 5 work):** Before enabling production use of the flag:

1. Remove or invert “throw if flag true” on `PreviewSample` / `PreviewVersion` / `PreviewDefinition` / `ActivateTemplate`.  
2. Production adapter: declarative only when flag true **and** Step 4 conditions.  
3. Preview remains Admin-only and usable regardless of production flag.

Until that reconciliation, **do not set the const to true** even in a lab build intended for designer work.

---

## 4. Production Activation Matrix

Assumptions for matrix rows (approved Steps 3–4):

- Eligibility already succeeded; enriched DTO available.  
- Whole-report switch only.  
- System Default / builtin → **Existing Angular** even if flag ON.  
- Resolve / Mode / Activate behave as Phase 3 source.

| State | Resolution | Rendering path | Fallback | Customer-visible effect |
|-------|------------|----------------|----------|-------------------------|
| **A. Flag OFF** | Resolve may still run for Admin tools; **production print must not use declarative** | Existing Angular | N/A (primary path) | **No change** vs pre–Phase 4 |
| **B. Flag ON + System Default** (Mode SystemDefault **or** Resolve → System Default / builtin) | System Default / `UsesBuiltInRenderer` | **Existing Angular** | N/A | **No declarative change**; matches locked Step 4 |
| **C. Flag ON + no active custom template** (Mode Custom, no Specific/Generic assignment) | Falls through to System Default | **Existing Angular** | N/A | Same as B |
| **D. Flag ON + valid Custom Generic** (Mode Custom, active Generic, published declarative definition, Print All or no Specific match) | `CustomGeneric` | Declarative whole-report if Validate+Render succeed | On fail → Existing Angular | Custom Generic layout for that ReportType |
| **E. Flag ON + valid Custom Specific** (Mode Custom, deterministic Test/Profile context, active Specific) | Test or Profile Specific | Declarative whole-report if success | On fail → Existing Angular | Specific custom layout |
| **F. Flag ON + unresolved Specific context** | **Do not guess** (Step 3): Resolve **without** TestId/ProfileId → Generic if active, else System Default | D or B accordingly | — | Never invent Specific |
| **G. Flag ON + invalid template** | Activate blocked when invalid (**FACT** Phase 3); if invalid definition still reached at print | Validation fails | **Existing Angular** | No blank/partial declarative |
| **H. Flag ON + render failure** | Template resolved | Exception / `Success=false` / empty Html | **Existing Angular** | Same DTO, Angular presentation |
| **I. Flag ON + unsupported component/binding** | At Activate: ValidateDesign blocks (**FACT**); at render: unknown component may warn+skip (**FACT** renderer) | Prefer treat hard validation failure as fallback; soft skip is existing renderer behavior | **RECOMMENDATION:** if post-render HTML empty/malformed → Angular | Avoid silent corrupt output |

### FACT supporting rows

| Fact | Source |
|------|--------|
| Mode SystemDefault short-circuits customs | `ReportTemplateManager.Resolve` |
| Specific → Generic → System Default in Custom mode | `ReportTemplateManager.Resolve` |
| Activate validates before assignment | `ActivateTemplate` + `ValidateDesign` |
| Builtin / System Default sets `UsesBuiltInRenderer` | `BuildResolveFromTemplate` |
| Renderer validates definition before render | `DeclarativeReportRenderer` / `ReportTemplateDefinitionValidator` |

### UNKNOWN

Exact client handling of “soft skip unknown component” producing acceptable print quality — treat as risk; prefer Angular fallback if output fails integrity checks (empty root, missing required structure). Integrity checklist is an implementation detail for a later step.

---

## 5. Diagnostic Rollout

### FACT

Single production flag cannot enable Diagnostic alone.

### FACT — Existing Diagnostic-scoped controls

- `ReportTemplateModeSetting` row for `ReportType = Diagnostic`  
- Diagnostic Custom Generic / Specific Activate  
- Print Specific vs Print All resolution rules (Step 3)

### RECOMMENDATION — Safest rollout without a second flag

```
1. Deploy with flag OFF  → verify zero print change
2. Prepare Diagnostic Custom templates; Activate; Mode=Custom (Admin)
3. Keep Radiology Mode=SystemDefault
4. Only after prerequisites: enable production flag (later gate)
5. Effect: Diagnostic Custom paths may use declarative;
   Radiology Resolve stays System Default → Angular (Step 4)
```

Customer-visible Diagnostic change occurs only when **all** of: flag ON + Mode Custom + resolvable active custom + successful render.

### RECOMMENDATION

Do **not** create `UseDeclarativeRendererForDiagnostic` unless a future product requirement proves Mode insufficient. Current architecture already supports Diagnostic-first via Mode.

---

## 6. Radiology Rollout

### FACT

Same application-wide flag as Diagnostic.

### RECOMMENDATION — Mirror Diagnostic strategy

Keep Radiology on `SystemDefault` Mode until Radiology Custom templates are UAT-approved. Then Mode=`Custom` + Activate Specific/Generic as needed.

Independent “Radiology flag” is **not** required by source evidence.

### RISK (FACT adjacent)

Radiology Angular chrome (title/legal) vs declarative twin parity remains **UNKNOWN** (Step 4). Prefer longer hold on Radiology Mode=SystemDefault after flag ON.

---

## 7. System Default Handling

### Locked decision (preserve)

```
System Default → Existing Angular Renderer
```

for current Phase 4 rollout.

### FACT

- Factory System Default uses `BuiltInRendererKey` / builtin pointer.  
- Conceptual twin (`ReportTemplateCompatibilityDefinitions`) is for Admin preview/create-from.  
- Equivalence to production Angular page-breaks/CSS is **not proven** (Step 4).

### RECOMMENDATION

| Flag | Mode | Path |
|------|------|------|
| OFF | any | Existing Angular |
| ON | SystemDefault | Existing Angular |
| ON | Custom, fallthrough to System Default | Existing Angular |
| ON | Custom + active custom declarative success | Declarative |

**Do not** enable declarative System Default merely because the flag is ON.  
A future change requires an **explicit validation gate** proving output equivalence (separate decision).

---

## 8. Custom Template Activation Interaction

### FACT — Lifecycle verbs (Phase 3)

| Action | Effect on production print (flag OFF) | Effect when flag ON (designed) |
|--------|----------------------------------------|--------------------------------|
| Create | Saves inactive custom; no print change | Same unless later activated + Mode Custom |
| Save / SaveDesign | Draft/working definition only | Still no production use until Activated published path |
| Preview | Sample/Admin HTML only | Must remain Admin-only; must work after flag semantics fix |
| Publish | Hidden behind Activate’s `EnsurePublishedForActivation` | Published definition is what Resolve returns |
| Activate | Creates active assignment; validates | Eligible for Resolve **only if** Mode=`Custom` |
| Deactivate | Clears assignment | No longer selected by Resolve |
| SetMode SystemDefault | Customs preserved; Resolve ignores customs | Production stays Angular for that ReportType |
| SetMode Custom | Customs may resolve | Production may use declarative **only if flag ON** |

### FACT

`Save_Does_Not_Activate` test proves Save ≠ Activate.  
UI messages: create says “not activated”; Custom mode warns production flag remains OFF.

### RECOMMENDATION — Production change condition (all required)

```
Flag ON
  AND Mode == Custom for that ReportType
  AND active assignment matches Resolve context
  AND published declarative definition
  AND NOT System Default / builtin
  AND Validate + Render success
  AND whole-report integrity OK
→ Declarative
ELSE → Existing Angular
```

Creating, saving, previewing, or publishing alone must **not** change customer print output.

---

## 9. Existing-Customer Safety

### RECOMMENDATION / requirement

Immediately after Phase 4 deployment with flag OFF:

```
Flag OFF
  → Existing Angular renderer
  → No customer-visible report change
```

### FACT supporting safety

- Const defaults false; tests assert false.  
- Production print never calls Resolve/declarative today.  
- Mode defaults to SystemDefault (`EnsureModeRow`).  
- Switching Mode preserves customs (`SetMode` comment + tests).  
- No automatic Activate of customer templates in EnsureSystemDefaults path for customs.

### RECOMMENDATION

- No automatic conversion of Angular layouts to declarative.  
- No automatic migration of customer report formats.  
- Existing template rows remain intact when Mode toggles.  
- Rollback of declarative presentation does not require deleting templates.

---

## 10. Rollback Strategy

### Preferred rollback (RECOMMENDATION)

```
Disable declarative production flag
        ↓
Subsequent reports use existing Angular renderer
```

### FACT / implication by flag storage

| Flag form | Rollback action | Template data |
|-----------|-----------------|---------------|
| Current `const` | Rebuild/redeploy with `false` | Intact — no DB rollback |
| Future `appSettings` (if adopted) | Set key false / remove key; recycle IIS app pool | Intact |
| Mode SystemDefault | Immediate Resolve fallthrough to System Default → Angular even if flag still ON | Customs preserved |

### RECOMMENDATION — Dual control for ops

1. **Hard stop:** production flag OFF.  
2. **Soft stop per type:** Mode → SystemDefault (no redeploy if flag is already runtime; with const, Mode alone still forces Angular for that type even if flag ON).

No destructive DB operation required to disable rendering.

### Explicit non-requirement

Database rollback is **not** necessary merely to disable declarative production rendering.

---

## 11. Operational Ownership

### FACT — Who can change designer controls today

| Control | Authorization |
|---------|---------------|
| Mode / Activate / Save / Preview | `ReportTemplateConfigurationController` + `[QAuthorize(ModuleName = "ReportTemplateConfiguration")]` (+ CanAdd/CanEdit as applicable) |
| Production report print | `Reports` / `RadiologyReports` modules — consumers only |

### FACT — Production flag ownership today

**Developers / release engineers** via source const + deploy. Not exposed to ordinary report users. Not exposed as a designer toggle.

### RECOMMENDATION

| Concern | Owner |
|---------|--------|
| Designer templates / Mode / Activate | Report Template Configuration RBAC (existing) |
| Production flag enable/disable | Operational/release ownership (config or release), **not** report users |
| Designer must not flip production master switch | Keep flag out of Angular designer UI |

Do **not** create a parallel RBAC system for the flag.

### UNKNOWN

Whether a specific named ops role already owns Web.config changes in each customer deployment — organizational, not encoded in this repo.

---

## 12. Rollout Granularity

### FACT — What exists today

| Granularity | Supported? | Mechanism |
|-------------|------------|-----------|
| Application-wide production ON/OFF | **Yes (const)** | `UseDeclarativeRendererForProductionPrint` |
| Environment-level (dev/qa/prod different) | **Only via different builds/deploys** of the const | No environment-specific config key found for this flag |
| Report-type (Diagnostic vs Radiology) | **Partial — Mode, not the flag** | `ReportTemplateModeSetting` per ReportType |
| Facility / customer multi-tenant flag | **No** evidence on Report Template entities |
| Per-user rollout | **No** |
| Per-template production override of the master flag | **No** — Activate selects template; flag gates engine |

### RECOMMENDATION — Safest strategy supported by current architecture

1. **Master switch:** production flag (keep OFF until gate).  
2. **Type scope:** Mode SystemDefault vs Custom.  
3. **Template scope:** Activate Specific/Generic.  
4. **Context scope:** Step 3 Resolve rules (Print Specific / Print All / Radiology).  
5. **Always-on safety net:** Existing Angular fallback (Step 4).

Do not invent facility/user flags for Phase 4 unless product later requires them (would imply DB/config design beyond Step 5).

### RECOMMENDATION — Optional later evolution (not Step 5 implementation)

Move master switch to `Web.config` `appSettings` for environment-level ON/OFF without rebuild, still fail-closed. Still **no second Diagnostic/Radiology flag** if Mode remains sufficient.

---

## 13. Activation Prerequisites

### RECOMMENDATION — Gate before flag enablement

All must be satisfied **before** any authorized person enables production declarative print.  
**This Step 5 design does not claim these tests have been run for a production enablement decision.**

| Gate | Purpose |
|------|---------|
| Backend automated suite (template engine Phase 1–3) | Resolver, Activate, validation, Mode |
| Declarative renderer tests | Validate/render/bindings/security allow-lists |
| Feature-flag-OFF regression | Print paths unchanged |
| Presentation adapter tests (when implemented) | Flag OFF → Angular; ON + System Default → Angular; ON + Custom success → declarative; failures → Angular |
| Diagnostic Print Specific / Print All scenarios | Step 3 context + whole-report |
| Radiology print scenarios | Mode hold + Specific context rules |
| Existing renderer fallback tests | Every failure class in matrix G–I |
| Browser print validation (manual/UAT) | iframe / `window.print` unchanged |
| Page-break validation (UAT) | Especially Custom templates on preprinted A4 |
| Signature validation (UAT) | Placement only; auth unchanged |
| ReportLayoutConfiguration validation | No duplicate bands; clearance preserved |
| Payment / eligibility regression | HTTP 400 behavior unchanged |
| Security / RBAC regression | Designer vs Reports permissions |
| Preview/Activate usable with intended flag semantics | **Must fix throw-when-true before ON** |
| Performance sanity | Resolve+render cost acceptable on sample prints |
| Monitoring minimum (see §14) | Enough signal to detect fallback storms |

### FACT

Phase 2/3 tests assert flag is currently false. They do **not** prove production declarative path because that path is not wired.

---

## 14. Monitoring Requirements

### FACT — Current observability gap

- `DeclarativeReportRenderer` does not inject/`use` `ILogger`.  
- Production print does not log Resolve/presentation decisions (path not wired).  
- `ReportTemplateManager` has `ILogger` but preview/Activate do not emit structured presentation decision events for production.

### RECOMMENDATION — Required evidence (implement in a later Phase 4 step, not Step 5)

Non-PHI events when production adapter exists:

| Event | Meaning |
|-------|---------|
| `DeclarativePrintDisabled` | Flag OFF — Angular |
| `DeclarativePrintAttempt` | Flag ON, Resolve selected custom declarative |
| `DeclarativePrintSuccess` | Html/Css produced; PresentationMode Declarative |
| `DeclarativePrintFallback` | Reason code (SystemDefault, NoTemplate, ValidationFailed, RenderException, EmptyHtml, UnresolvedSpecificSafePath, etc.) |
| Identifiers | ReportType, TemplateId, VersionId, ResolutionSource — avoid patient PHI / result values |

### Classification

| Item | Tag |
|------|-----|
| Logging insufficient for production enablement today | **FACT** |
| Structured presentation logging required before broad ON | **RECOMMENDATION** |
| Exact log sink / correlation format | **UNKNOWN** / later design |

Do not implement observability in Step 5.

---

## 15. Security

### FACT

| Surface | Flag exposed? |
|---------|----------------|
| Public/anonymous API | **No** |
| Ordinary report-user UI | **No** |
| Template designer UI | **No** toggle (message only that flag is OFF) |
| `ReportTemplateConfiguration` APIs | Mode/Activate/Preview only — not production flag |

### RECOMMENDATION

- Keep master switch out of designer and report UIs.  
- Production declarative render only inside already-authorized report GET success path (Step 2/4).  
- Flag enablement must not bypass eligibility or Report Template Configuration RBAC.  
- No parallel authorization mechanism.

### RISK

If flag later moves to `Web.config`, protect server config ACLs (ops concern). Still not end-user controllable.

---

## 16. Performance

### FACT / implications when flag OFF

No Resolve/render on production print path today ⇒ **zero added cost** from the engine for production print.

### RECOMMENDATION / expected cost when flag ON (design only)

| Work | When |
|------|------|
| Read Mode + Resolve assignments + published version | Each print presentation decision |
| Validate + Render CPU | Only when attempting declarative custom |
| Extra clinical DB for renderer | **None** (DTO already assembled) — **FACT** for renderer |
| Optional context lookup (SaleInvoiceDetail for TestId) | Step 3 presentation helper — small, not clinical re-assembly |

### Do not optimize in Step 5

Document only. Print All remains single Resolve (Step 3) — avoids N× Specific resolves.

---

## 17. Database Implications

### FACT

Production flag is **not** stored in the database.

### FACT

Rollout scoping for report type already uses existing `ReportTemplateModeSetting` and assignment tables (Phase 3).

### Conclusion

**NO NEW DB CHANGE REQUIRED** for the Step 5 feature-flag and controlled-rollout design.

### If a gap appears later

| Need | Preference |
|------|------------|
| Runtime ON/OFF without rebuild | Prefer `appSettings` (no DB) |
| Facility-level flag | Would be a **new** product feature → document then; not authorized now |

Remember locked Phase 4 rule: if a future implementation **does** require DB changes, that implementation must include/update migration and deployment SQL. Step 5 does not authorize that.

---

## 18. Backward Compatibility

### Mandatory guarantee

```
Flag OFF = existing production behavior
```

### FACT today

Flag is OFF and unused by print ⇒ compatibility holds for current deployed intent.

### RECOMMENDATION — After production wiring exists

| Check | Expected |
|-------|----------|
| Flag OFF | Zero declarative substitution; Angular HTML only |
| Mode SystemDefault | Angular even if flag ON |
| No active customs | Angular |
| Eligibility failures | Unchanged HTTP errors — not “fallback prints” |

Do not weaken this rule for convenience enablement.

---

## 19. Risks / Unknowns

| Item | Tag | Mitigation |
|------|-----|------------|
| Preview/Activate throw when flag true | **FACT** / HIGH for enablement | Reconcile before any ON |
| Const requires redeploy to rollback | **FACT** | Optional later appSettings; Mode soft-stop |
| No production presentation logs | **FACT** | §14 before broad rollout |
| Page-break / System Default parity | **UNKNOWN** | Keep System Default on Angular |
| Soft-skip unsupported components at render | **FACT** behavior / quality **UNKNOWN** | Integrity check → Angular |
| Org ownership of config changes | **UNKNOWN** | Document ops procedure at enablement |
| Radiology chrome parity | **UNKNOWN** | Hold Radiology on SystemDefault Mode longer |

---

## 20. Recommended Step 5 Architecture

```
                    ┌─────────────────────────────────────┐
                    │ UseDeclarativeRendererForProduction │
                    │ Print (master switch; default OFF)  │
                    └─────────────────┬───────────────────┘
                                      │
                         OFF ─────────┴───────── ON
                          │                       │
                          ▼                       ▼
                   Existing Angular      Mode per ReportType
                                          │
                          SystemDefault ──┤── Custom
                                │         │
                                ▼         ▼
                         Existing Angular   Resolve (Step 3)
                                            │
                         SystemDefault/─────┼── Custom Generic/Specific
                         builtin            │
                                │           ▼
                                │    Validate + Render (Step 4)
                                │           │
                                │    success ┴ failure/empty
                                │           │
                                ▼           ▼
                         Existing Angular   Declarative HTML
                                            in same print root
                                            → iframe/window.print
```

### Principles

1. **Fail closed** to Existing Angular.  
2. **Whole-report** only.  
3. **System Default never forced to declarative** by flag alone.  
4. **No second feature flag** — use Mode + Activate for Diagnostic/Radiology sequencing.  
5. **Admin create/save/preview ≠ production change.**  
6. **Rollback = disable flag** (+ optional Mode soft-stop); templates preserved.  
7. **Do not enable the flag in this step.**

---

## 21. Explicit Non-Goals

- Enabling `UseDeclarativeRendererForProductionPrint`  
- Implementing production adapter / Angular substitution  
- Creating additional feature flags  
- DB migrations / SQL for flags  
- Implementing monitoring/logging  
- Proving System Default ≡ Angular  
- Facility/user targeting infrastructure  
- Changing eligibility, signatures, layout authority, or print transport  
- Approving Step 6 or beginning implementation  

---

## Change Integrity (Step 5 deliverable)

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
`docs/Report-Template-Engine-Phase4-Step5-Design.md`

---

STEP 5 STATUS: PROPOSED — PENDING USER APPROVAL
