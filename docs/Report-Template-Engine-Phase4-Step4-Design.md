# Report Template Engine — Phase 4 Step 4 Design

**Project:** ZoryaLMS / AVILIS  
**Date:** 2026-09-12  
**Phase:** 4 — Design Step 4 (Production Rendering & Output Behavior)  
**Status:** PROPOSED — PENDING USER APPROVAL  
**Prerequisites:** Step 1–3 APPROVED  

---

## 1. Executive Summary

### FACT — Two presentation engines exist today

| Engine | Role today |
|--------|------------|
| **Angular hard-coded HTML** (`test-report` / `radiology-report-print`) | **Production** print |
| **DeclarativeReportRenderer** | Admin **preview** only (Phase 2/3) |

Production path remains: eligible enriched DTO → Angular HTML → iframe / `window.print()`.  
There is **no** server PDF and **no** production wiring of Resolve/declarative render.

### RECOMMENDATION — Step 4 production presentation switch

After eligibility + DTO + enrichment (Steps 1–3):

```
If enablement OFF OR builtin/System Default OR resolve/render fails
    → Existing Angular renderer (mandatory fallback)
Else (Custom declarative published definition + successful whole-report render)
    → Server declarative HTML+CSS substituted into the SAME print root
    → Existing iframe / window.print() unchanged
```

### Critical non-equivalence

**System Default / Angular page-break fidelity is NOT proven.**  
Declarative CSS page-break rules are a **subset** of production Angular print CSS.  

**RECOMMENDATION:** Keep **existing Angular renderer** for System Default / builtin and for any case where page-break / parity risk is unacceptable, until a later controlled validation gate.

---

## 2. Current Declarative Renderer Architecture

### FACT — Entry / I/O

| Item | Source |
|------|--------|
| Interface | `IReportRenderer.Render` (`ReportTemplateEnginePhase2.cs`) |
| Implementation | `DeclarativeReportRenderer.Render` |
| Input | `ReportRenderRequestDto`: `ReportType`, `DefinitionJson`, `ReportData` (DTO), optional `LayoutOverride` |
| Output | `ReportRenderResultDto`: `Success`, `Html`, `Css`, layout mm fields, `UsesBuiltInRenderer`, `Warnings`, `Message` |
| Validation gate | `ReportTemplateDefinitionValidator.Validate` before render |
| Binding | `ReportTemplateBindingContext` (Diagnostic / Radiology DTO scopes) |
| Preview host | `ReportTemplateManager.PreviewSample` / `PreviewVersion` / `PreviewDefinition` |
| Admin UI | `report-template-viewer.component` ( DomSanitizer + HTML/CSS ) |

### FACT — Capabilities

- Component types: text/fields, SECTION + repeat, PARAMETER_TABLE, RPG, comments, SIGNATURE, IMAGE (safe), LINE, SPACER, radiology narratives  
- Conditions: Exists / NotEmpty / Equals / NotEquals  
- Styles: allow-listed + sanitized inline CSS; `visibility:hidden` skips render  
- Layout: reads `ReportLayoutConfigurationDto` from DTO or override; `BuildCss` emits header/footer band heights and L/R margins  
- HTML encoding of values; rejects unsafe images/URLs in image path  
- Builtin definition short-circuits to `UsesBuiltInRenderer=true` with **no** HTML  

### FACT — Error behavior

- Invalid definition → validator throws (caller must catch)  
- Missing report data → `ArgumentException`  
- Unknown component at render → warning + skip (not hard-fail for that node)  

### FACT — DB usage during render

Renderer operates on the **already-assembled DTO** (+ layout on DTO). It does **not** query clinical results.  
(Resolve/version load is separate and occurs before Render.)

---

## 3. Current Diagnostic Output Architecture

### FACT — Pipeline

```
TestReportManager.GetDiagnosticTestReport  (eligibility + DTO)
  → OperationalReportsController enrich (approver, technician, branding, layout)
  → JSON to Angular test-report.component
  → HTML #diagnostic-test-report-print
  → print(): clone innerHTML + getPrintStyles() into iframe → print()
       fallback: body class + window.print()
```

### FACT — Presentation details

- Blank `.report-print-header` / `.report-print-footer-zone` for **pre-printed stationery**  
- Patient panel table; DepartmentGroups primary; ProfileGroups/Sections legacy  
- Parameter table with abnormal styling; comments; doctor approval comments  
- Technician then Doctor signature zones from enriched header fields  
- `resolvePrintLayout('Diagnostic')` drives `@page` margins from `report.layout`  
- Rich **page-break** rules in component TS CSS string + `.css` (avoid breaks inside patient table, dept headers, param rows, signatures, etc.)

### RECOMMENDATION — Substitution point

Replace **only** the inner content of `#diagnostic-test-report-print` (or equivalent print root) when declarative presentation succeeds — **after** DTO is loaded. Do not touch filter panel, eligibility UI, or print() transport.

---

## 4. Current Radiology Output Architecture

### FACT — Pipeline

```
RadiologyReportManager.GetRadiologyReportForPrint
  → EnrichRadiologyApprover + EnrichRadiologyLayout
  → radiology-report-print.component
  → #diagnostic-radiology-report-print
  → same iframe / window.print pattern
```

### FACT — Presentation details

- Pre-printed header band + title bar + meta tables + narrative blocks  
- Authorizer signature / digital signature text fallback  
- Layout mm from `report.layout`  
- Print CSS in `getPrintStyles()` + component CSS  

### RECOMMENDATION — Equivalent boundary

Same as Diagnostic: whole-report HTML swap inside print root; keep Angular print() path.

---

## 5. Proposed Production Presentation Boundary

### RECOMMENDATION (aligns Steps 1–3)

```
Eligibility (unchanged)
  → DTO assembly (unchanged)
  → Controller enrichment (unchanged)  ← signatures, branding, Layout
  → Production presentation adapter
        ├─ flag OFF / builtin / no custom declarative / validate|render fail
        │     → PresentationMode = Existing
        └─ success
              → PresentationMode = Declarative + Html + Css
  → Angular:
        Existing → current templates
        Declarative → inject Html(+Css) into print root
  → Existing iframe / window.print()
```

### FACT — Shared vs separate

| Layer | Shared? |
|-------|---------|
| Clinical pipelines | Separate |
| Declarative renderer / validator / Resolve | Shared |
| Angular print shell | Separate components; same pattern |
| Presentation adapter | Shared concept |

### Whole-report only

**RECOMMENDATION:** One report = one engine. No section-level mixing of declarative + legacy HTML.

---

## 6. Diagnostic Rendering Design

### RECOMMENDATION

When Custom mode + activated declarative Generic/Specific resolves successfully:

1. Call `DeclarativeReportRenderer.Render` with enriched `DiagnosticTestReportDto` and published definition.  
2. Require non-empty `Html` and `Success`.  
3. Angular shows declarative document in print root.  

Otherwise: existing `test-report` template.

### FACT — Field coverage (Phase 3)

Catalog/bindings cover patient/order/invoice/visit/test/sample/department/profile collections/parameters/comments/signatures/branding. RPG supports DepartmentGroups / ProfileGroups.

### UNKNOWN / RISK

Pixel and CSS parity with production Angular (esp. patient table density, abnormal colors, exact signature geometry) is **not proven**. Acceptable for Custom activated templates under Admin design; **not** a reason to force System Default onto declarative without UAT.

---

## 7. Radiology Rendering Design

### RECOMMENDATION

Same switch pattern with enriched `DiagnosticRadiologyReportDto`.

### FACT

Compatibility definition / components cover accession, patient, narratives, recommendation (conditional), authorizer signature / digital signature binding.

### UNKNOWN / RISK

Radiology title bar / footer legal/disclaimer chrome in Angular may not exist 1:1 in System Default twin — another reason System Default stays on Angular until validated.

---

## 8. Report Layout Configuration Handling

### FACT — Production

`OperationalReportsController.EnrichReportLayout` / `EnrichRadiologyLayout` sets `report.Layout` from `IReportLayoutConfigurationManager.GetByReportType`.  
Angular `resolvePrintLayout` applies header/footer/left/right mm to `@page` / bands.

### FACT — Declarative

`DeclarativeReportRenderer` uses DTO `Layout` (or override) in `BuildCss` for `.rte-header-band` / `.rte-footer-band` heights and body L/R margins; signature sizes/align from layout flags.

### RECOMMENDATION

- Keep **ReportLayoutConfiguration** as sole physical layout authority.  
- Pass enriched DTO (with Layout) into Render — no second layout store.  
- Do not redesign A4/margin model.  
- When using declarative HTML inside Angular print root, **either**:  
  - rely on declarative `Css` bands, **or**  
  - strip duplicate outer bands carefully so Angular header/footer placeholders and declarative bands are not double-applied.  

**UNKNOWN until implementation detailing:** exact DOM nesting to avoid double clearance. Must be resolved in implementation design without inventing new layout config — prefer single band owner (declarative Css when declarative mode).

---

## 9. Preprinted Stationery Handling

### FACT

Production uses **empty** header/footer zones; letterhead is physical stationery. Angular comments state screen hints only; print uses margin clearance.

### FACT — Declarative

Emits empty `rte-header-band` / `rte-footer-band` (height from layout) — same intent, no letterhead graphics.

### RECOMMENDATION

- Do **not** add stationery artwork, backgrounds, or replacement letterheads.  
- Branding fields remain optional bound text/logo per Phase 2/3 rules (logo only if safe relative/data URI).  
- Preserve clearance-only bands.

---

## 10. Signature Handling

### FACT — Authority

Signatures come from enriched DTO (`EnrichLabApprover` / `EnrichLabTechnician` / `EnrichRadiologyApprover`) using existing user signature storage — not from templates.

### FACT — Declarative presentation

`RenderSignature` reads name/image/meta bindings; respects layout enable flags and mm sizes; alignment classes.

### RECOMMENDATION

Renderer may control **placement/visibility/alignment/size presentation** only.  
Must not authorize, approve, or fetch alternate identity stores.

---

## 11. Page-Break Handling

### FACT — Production Angular (authoritative)

Diagnostic print CSS includes extensive `page-break-inside/after` / `break-inside` rules for patient tables, department headers, test headers, parameter rows, comments, signatures.

### FACT — Declarative today

```
@media print {
  @page { size: ... }
  .rte-report { page-break-inside:auto; }
  .rte-parameter-table tr { page-break-inside:avoid; }
}
```

Much thinner than production Angular.

### RECOMMENDATION

1. Do **not** invent a competing pagination engine.  
2. For **System Default / builtin** → **existing Angular** (preserves approved page-break behavior).  
3. For **Custom declarative** → accept declarative CSS as presentation defined by Admin template; optionally later (future enhancement) port selected Angular break rules into declarative `BuildCss` **without** changing Angular’s own path.  
4. If custom declarative print UAT shows clipping/overlap → fallback to existing renderer for that site until fixed (ops policy), not a silent partial print.

### UNKNOWN

Whether declarative output + Angular iframe styles interact adversely if both CSS sets apply — implementation must scope CSS to declarative root only.

---

## 12. Browser Print Integration

### FACT

Both reports:

1. Prefer hidden iframe with `source.innerHTML` + print stylesheet  
2. Fallback `window.print()` with body print-mode class  

No server PDF.

### RECOMMENDATION

Preserve this exactly. Declarative mode only changes **what HTML** sits in the print root before `print()` runs.  
Reuse Admin viewer trust pattern: server-produced HTML only; `DomSanitizer.bypassSecurityTrustHtml` for that payload (same as `report-template-viewer`).

---

## 13. Whole-Report Rendering Strategy

### RECOMMENDATION

| Mode | Engine |
|------|--------|
| Declarative success | Entire print root = declarative HTML |
| Else | Entire print root = existing Angular templates |

No hybrid sections. Matches single Resolve result + single definition (Step 3 Print All = Generic-level).

---

## 14. Fallback Strategy

| Trigger | Presentation |
|---------|--------------|
| Feature flag disabled | Existing Angular |
| Mode SystemDefault / Resolve builtin | Existing Angular |
| No published declarative definition | Existing Angular |
| Validation failure | Existing Angular |
| Renderer exception / empty Html | Existing Angular |
| Unsupported critical failure | Existing Angular |
| Eligibility failure | **No print** (HTTP 400) — not a presentation fallback |

### Mandatory

- No blank/partial declarative print as success.  
- Fallback uses same enriched eligible DTO.  
- Fallback does not re-run clinical assembly or bypass payment/approval.

---

## 15. System Default Strategy

### FACT

Factory System Default has `BuiltInRendererKey` and Resolve `UsesBuiltInRenderer=true`.  
Phase 3 “conceptual twin” (`ReportTemplateCompatibilityDefinitions`) is for designer preview/create-from — **not** proven equivalent to production Angular HTML/CSS/page-breaks.

### Classification

| Claim | Tag |
|-------|-----|
| Builtin key exists and points away from declarative HTML | FACT |
| Twin equals production Angular output | **UNKNOWN** (not demonstrated) |
| Use existing Angular for System Default in production | **RECOMMENDATION** |

Retain existing renderer for System Default until a later validation/rollout gate explicitly approves declarative System Default.

---

## 16. Feature Flag Boundary

### FACT

```csharp
ReportTemplateEngineFeatureFlags.UseDeclarativeRendererForProductionPrint = false
```

in `ReportTemplateEnginePhase2.cs` (compile-time const).

### FACT — Current usage quirk

`ReportTemplateManager.PreviewSample` / `PreviewVersion` / `PreviewDefinition` **throw** if the flag is `true` (Phase 2 safety against accidental production semantics in preview). Production print paths **ignore** the flag today (never consulted).

### RECOMMENDATION

- Conceptual production guard: adapter uses declarative **only when flag is true** (and other Step 3 conditions).  
- When flag is false: always Existing (today’s behavior).  
- **Do not enable** in this step.  
- Later implementation must reconcile preview vs production flag semantics carefully (preview must remain usable while production stays off).

---

## 17. Security

### FACT

Validator: unsafe HTML/JS/SQL patterns, binding roots, component whitelist, style allow-list, condition ops.  
Renderer: HtmlEncode values; safe image rules.  
Production report APIs: QAuthorize `Reports` / `RadiologyReports`.  
Designer: `ReportTemplateConfiguration`.

### RECOMMENDATION

- No new public render endpoint.  
- Declarative HTML only from server adapter on authorized report GET success path.  
- No extra PHI retrieval in renderer.  
- Client must not accept client-authored HTML for production print.

---

## 18. Performance

### FACT / RECOMMENDATION

| Work | When |
|------|------|
| DTO already built | Always (unchanged) |
| Resolve + version read | Only if flag on |
| Validate + Render CPU | Only if declarative path attempted |
| Extra clinical DB | **None** in renderer |

No caching in Step 4 design. Print All remains single Resolve (Step 3).

---

## 19. Backward Compatibility

### RECOMMENDATION / requirement

With flag **OFF** (current): zero presentation change — Angular path only.  
Custom templates saved but inactive / Mode SystemDefault: existing Angular.  
Only later enablement + Custom + successful declarative render may change presentation.

---

## 20. Database Implications

**NO NEW DB CHANGE REQUIRED** for Step 4 rendering/output architecture.

Uses existing enriched DTO, layout config, and Phase 1–3 template tables.

---

## 21. RBAC Implications

No parallel auth. Production rendering rides existing report CanView. Designer remains ReportTemplateConfiguration. No bypass via template engine.

---

## 22. Risks / Unknowns

| Item | Tag | Mitigation |
|------|-----|------------|
| Page-break parity Angular vs declarative | UNKNOWN / HIGH risk for System Default | Keep System Default on Angular |
| Double header/footer bands if both CSS owners apply | UNKNOWN | Single band owner when declarative |
| Trusted HTML XSS if non-server HTML introduced | RISK | Server-only HTML |
| Preview flag throw-when-true vs production true-means-on | FACT quirk | Reconcile in later implementation design |
| Custom template poor pagination | RISK | Fallback / UAT before broad enablement |
| Radiology chrome (title/legal) missing in twin | UNKNOWN | System Default → Angular |

---

## 23. Recommended Step 4 Architecture

1. **Whole-report** switch after enrichment; shared adapter concept.  
2. **Existing Angular + iframe/window.print** remains transport and fallback.  
3. Declarative path only for **successful Custom declarative** renders when enablement allows.  
4. **System Default / builtin → existing Angular** until proven.  
5. **ReportLayoutConfiguration** remains authoritative; avoid duplicate layout systems.  
6. **Preprinted clearance only** — no new stationery art.  
7. Signatures from enriched DTO only.  
8. **Do not enable** `UseDeclarativeRendererForProductionPrint` in this step.  
9. **No PDF / no new document engine / no DB changes.**

---

## 24. Explicit Non-Goals

- Implementation / flag flip / deployment  
- Pixel-perfect System Default parity project  
- New pagination engine  
- Partial section mixing  
- Server PDF  
- Stationery redesign  
- Step 5+ (rollout plan)  

---

## Traceability (material)

| Topic | Location |
|-------|----------|
| Declarative Render | `DeclarativeReportRenderer.Render`, `BuildCss`, `RenderSignature`, RPG/table/conditions |
| Validator | `ReportTemplateDefinitionValidator` |
| Feature flag | `ReportTemplateEngineFeatureFlags` |
| Preview / flag throw | `ReportTemplateManager.PreviewSample` (+ Version/Definition) |
| Admin viewer | `report-template-viewer.component.ts` |
| Diagnostic print | `test-report.component.ts/html/css` — `print`, `getPrintStyles`, `resolvePrintLayout` |
| Radiology print | `radiology-report-print.component.ts/html/css` |
| Layout enrich | `OperationalReportsController.EnrichReportLayout` / `EnrichRadiologyLayout` |
| Compatibility twin | `ReportTemplateCompatibilityDefinitions` |
| Builtin resolve | `ReportTemplateManager.BuildResolveFromTemplate` / `EnsureBuiltin` |

---

## Change integrity

| Item | Count |
|------|-------|
| Source changes | **0** |
| Angular changes | **0** |
| DTO changes | **0** |
| DB changes | **0** |
| SQL/migration changes | **0** |
| Configuration changes | **0** |
| Feature flag changes | **0** |
| Test changes | **0** |
| Deployment changes | **0** |

*Only documentation added: `docs/Report-Template-Engine-Phase4-Step4-Design.md`.*

---

## Final status

**STEP 4 STATUS: PROPOSED — PENDING USER APPROVAL**
