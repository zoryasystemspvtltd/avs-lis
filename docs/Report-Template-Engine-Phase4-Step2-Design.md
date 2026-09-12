# Report Template Engine — Phase 4 Step 2 Design

**Project:** ZoryaLMS / AVILIS  
**Date:** 2026-09-12  
**Phase:** 4 — Design Step 2 (Production Enablement & Fallback Architecture)  
**Status:** PROPOSED — PENDING USER APPROVAL  
**Prerequisite:** Phase 3 ACCEPTED WITH MINOR FINDINGS  

---

## 1. Executive Summary

### FACT — Current production presentation

Production Diagnostic and Radiology reports today follow:

```
Eligibility + DTO assembly (server managers)
    → JSON DTO via OperationalReportsController
    → Angular hard-coded HTML templates
    → iframe / window.print()
```

There is **no** server PDF engine and **no** production use of `ReportTemplateManager.Resolve` or `DeclarativeReportRenderer`.

### RECOMMENDATION — Production presentation boundary

Add a **shared presentation adapter** invoked **after** eligibility, DTO assembly, and existing controller enrichment (signatures / branding / layout), and **before** the API returns the print payload to Angular.

```
Eligibility (unchanged)
    → DTO assembly (unchanged)
    → Controller enrichment (unchanged)
    → Production presentation decision (NEW adapter)
         ├─ declarative HTML+CSS (when enabled + resolvable custom declarative template + render success)
         └─ fallback indicator → existing Angular HTML renderer
    → Existing browser/client print
```

### Locked safety principles

| Principle | Disposition |
|-----------|-------------|
| Presentation only | Template engine never decides payment/approval/authorization |
| Existing renderer as fallback | Default and failure path |
| System Default / builtin | Treat as **existing Angular path** until a later approved design for declarative System Default production |
| Engine initially OFF | `UseDeclarativeRendererForProductionPrint = false` remains until a later enablement step |
| No new document engine | Reuse Angular print/iframe/`window.print()` |

---

## 2. Actual Current Diagnostic Production Print Path

### FACT — End-to-end flow

| Step | Location | Behavior |
|------|----------|----------|
| UI entry | `web/Lis.Web/.../test-report/test-report.component.ts` | Lab No selection; Print All vs Print Specific |
| Print options | `GET api/Reports/TestReportPrintOptions` | `TestReportManager.GetPrintableTestOptions` |
| Report load | `GET api/Reports/TestReport` | `OperationalReportsController.GetTestReport` |
| Eligibility + DTO | `TestReportManager.GetDiagnosticTestReport` | Payment, cancel, Print All/Specific, doctor approval, results |
| Enrichment | `OperationalReportsController` | Approver/technician signatures, branding, `ReportLayoutConfiguration` |
| Render | `test-report.component.html` | Hard-coded Angular HTML bound to DTO |
| Print | `print()` → iframe or `window.print()` | Client-side |

### FACT — Eligibility (upstream; must not move)

In `TestReportManager.GetDiagnosticTestReport`:

1. Lab No / Invoice required  
2. Invoice not cancelled  
3. **PaymentStatus == Paid** (blocks unpaid; no print)  
4. Print Specific: selected `testRequestDetailId` must belong to order; only that request validated  
5. Print All: all applicable requests validated  
6. `ValidateWorkflow`: each request must be `DoctorApproved`; results and result details must exist  

### FACT — Output mechanism

- No server PDF generation on this path.  
- Print clones `#diagnostic-test-report-print` into an iframe document and calls `contentWindow.print()`, with `window.print()` fallback.

### FACT — Auth

- `OperationalReportsController.GetTestReport`: `[QAuthorize(ModuleName = "Reports", ModulePermissionTypes = CanView)]`  
- Route guarded in Angular (`ROUTE_GUARDS`).

---

## 3. Actual Current Radiology Production Print Path

### FACT — End-to-end flow

| Step | Location | Behavior |
|------|----------|----------|
| UI entry | `radiology-report-print.component.ts` | Accession selection |
| Accessions | `GET api/Reports/RadiologyPrintAccessions` | Authorized/Released + Paid |
| Report load | `GET api/Reports/RadiologyReport` | `GetRadiologyReportForPrint` |
| Eligibility + DTO | `RadiologyReportManager.GetRadiologyReportForPrint` | Status, payment, findings/impression |
| Enrichment | Controller | Authorizer signature + layout |
| Render | `radiology-report-print.component.html` | Hard-coded Angular HTML |
| Print | iframe / `window.print()` | Client-side |

### FACT — Eligibility (upstream)

In `GetRadiologyReportForPrint`:

1. Request exists  
2. Status **Authorized** or **Released**  
3. Invoice found; not cancelled  
4. **Payment Paid**  
5. Findings and Impression required  

### FACT — Auth

- `[QAuthorize(ModuleName = "RadiologyReports", ModulePermissionTypes = CanView)]`

---

## 4. Phase 1/2/3 Integration Points

### FACT — What already exists

| Capability | Location | Production print wired? |
|------------|----------|-------------------------|
| Templates / versions / assignments | `ReportTemplate*` entities + `ReportTemplateManager` | **No** |
| Mode SystemDefault / Custom | `ReportTemplateModeSetting` + designer APIs | **No** |
| Resolve Specific → Generic → System Default | `ReportTemplateManager.Resolve` | **No** |
| Validator | `ReportTemplateDefinitionValidator` | Preview/designer only |
| Declarative renderer | `DeclarativeReportRenderer` | Preview only |
| Binding context | `ReportTemplateBindingContext` | Preview only |
| Feature flag | `ReportTemplateEngineFeatureFlags.UseDeclarativeRendererForProductionPrint = false` | Guards preview paths; **const false** |
| Designer / preview APIs | `ReportTemplateConfigurationController` | Admin `ReportTemplateConfiguration` RBAC |
| Compatibility / System Default twin | `ReportTemplateCompatibilityDefinitions` | Designer preview / create-from; **not** production Angular |

### FACT — Resolve result shape

`ReportTemplateResolveResultDto` includes:

- `UsesBuiltInRenderer` (true when `BuiltInRendererKey` set — factory System Default)  
- `DefinitionJson` (published version)  
- `ResolutionSource`, `TemplateId`, etc.

### FACT — Not connected

- `TestReportManager` / `RadiologyReportManager` do **not** reference template Resolve/renderer.  
- Angular production report components do **not** reference template APIs.  
- No production response field today for declarative HTML.

---

## 5. Proposed Production Presentation Boundary

### RECOMMENDATION — Safest integration point

**After** manager eligibility + DTO assembly **and after** existing controller enrichment; **before** `Ok(report)`.

**Why safest (FACT-based):**

1. Eligibility failures already throw `TestReportValidationException` → HTTP 400; never enter presentation.  
2. Enrichment supplies signature images, branding, and layout the declarative renderer expects (same as Admin preview needs).  
3. Managers stay free of presentation concerns (minimum invasive).  
4. One shared adapter can serve Diagnostic and Radiology without merging clinical pipelines.  
5. Angular remains the print host (existing iframe/`window.print()`).

### RECOMMENDATION — Shared adapter / separate pipelines

| Layer | Shared? |
|-------|---------|
| Eligibility / DTO build | **Separate** (Diagnostic vs Radiology managers) |
| Controller enrichment | **Separate** methods (already) |
| Production presentation adapter | **Shared** conceptual service |
| Declarative renderer / validator / Resolve | **Shared** (already) |
| Angular print shell | **Separate** components; same consumption pattern |

### Conceptual sequence

```
[Business] GetDiagnosticTestReport / GetRadiologyReportForPrint
[Business] Enrich signatures / branding / layout
[Presentation] If engine disabled → mark UseExistingRenderer
[Presentation] Else Resolve(reportType, optional TestId/ProfileId)
[Presentation] If UsesBuiltInRenderer OR no published declarative definition → UseExistingRenderer
[Presentation] Else Render(definition, enriched DTO)
[Presentation] On render failure → UseExistingRenderer (do not return partial HTML as success)
[Client] If declarative HTML present and success → show it; else existing Angular template
[Client] print() prints the visible document
```

### What remains unchanged

- Payment / approval / authorization / Print All / Print Specific rules  
- DTO construction logic  
- Signature storage  
- `ReportLayoutConfiguration` semantics  
- Existing Angular HTML as fallback renderer  
- Browser print mechanism  
- Admin designer RBAC  

---

## 6. Template Resolution Flow

### FACT — Approved Phase 3 model (reuse; do not invent)

**Mode = SystemDefault:** factory System Default only.  

**Mode = Custom:**

```
Test/Profile Specific (if context provided)
    → Active Custom Generic
    → System Default
```

### RECOMMENDATION — How Resolve is called from production

Use existing `ReportTemplateManager.Resolve(ReportTemplateResolveRequest)`:

| Field | Diagnostic | Radiology |
|-------|------------|-----------|
| `ReportType` | `"Diagnostic"` | `"Radiology"` |
| `TestId` | Optional; see gaps | Optional; see gaps |
| `ProfileId` | Optional; see gaps | Usually N/A |
| `DepartmentCode` | Phase 3 Custom Resolve **skips** department (FACT) | N/A |

### FACT / GAP — Resolver context not on print DTOs

| Need | Available today? | Notes |
|------|------------------|-------|
| ReportType | Yes | Implicit from endpoint |
| TestId (HisTestMaster.Id) | **Not on print DTO** | `TestRequestDetail` has `HISTestCode` only; manager already maps code→`HisTestMaster` internally in places |
| ProfileId | **Not on print DTO** | Exists on `SaleInvoiceDetail.TestProfileId` during build; not exposed on `DiagnosticTestReportDto` / profile group |
| Radiology TestId | **Not on Radiology DTO** | `RadiologyRequestDetail` has `HISTestCode` / name only |

### RECOMMENDATION — Resolve context strategy (no schema change required)

Derive resolve keys **at presentation time** from the same request identifiers already used for print (not by inventing DTO fields unless a later step chooses to):

1. **Diagnostic Print Specific** (`testRequestDetailId` present): look up request → `HISTestCode` → `HisTestMaster.Id` as `TestId`; optionally resolve `ProfileId` from invoice line membership.  
2. **Diagnostic Print All**: call Resolve **without** TestId/ProfileId → Active Generic or System Default (Specific templates apply to targeted print, not whole-order amalgamation).  
3. **Radiology**: look up accession request → `HISTestCode` → `HisTestMaster.Id` when Specific templates are used; otherwise Generic/System Default.

### RECOMMENDATION — System Default / builtin in production

Factory System Default sets `BuiltInRendererKey` and `UsesBuiltInRenderer = true`.

For Phase 4 Step 2 architecture:

> **Builtin / System Default ⇒ existing Angular renderer**  
> Declarative System Default twin remains designer/preview/create-from only until a later approved step explicitly authorizes declarative System Default in production.

This satisfies locked clarification that System Default/built-in path falls back to existing renderer unless later design says otherwise.

### RECOMMENDATION — When Custom declarative runs

Only when **all** hold:

1. Production enablement ON (later step; currently OFF)  
2. Mode = Custom (or equivalent enablement rule from later step)  
3. Resolve returns a **non-builtin** template with published declarative `DefinitionJson`  
4. Definition validates  
5. Renderer succeeds  

Otherwise → existing Angular path.

---

## 7. Fallback Architecture

### RECOMMENDATION — Fallback matrix

| Case | Presentation outcome |
|------|----------------------|
| A. Engine disabled | Existing Angular renderer |
| B. No resolvable custom declarative template | Existing Angular renderer |
| C. Resolve → builtin / System Default | Existing Angular renderer |
| D. Invalid / unpublished / validation failure | Existing Angular renderer |
| E. Renderer exception / empty unexpected failure | Existing Angular renderer |
| F. Success declarative render | Declarative HTML+CSS for client print |

### Mandatory rules

1. **Failed declarative output must never be printed.**  
   Do not send partial HTML as a successful declarative presentation.  
2. **Fallback uses the same already-assembled eligible DTO.**  
   No re-eligibility; no alternate clinical pipeline.  
3. **Fallback is silent to clinical workflow** (may log presentation reason; must not convert to business error).

---

## 8. Business Eligibility vs Presentation Failure Boundary

| Failure type | Example | Behavior |
|--------------|---------|----------|
| **Business eligibility** | Unpaid invoice; not doctor-approved; radiology not authorized | Existing `TestReportValidationException` → **HTTP 400**; **no print**; **no fallback render** |
| **Presentation/template** | Flag off; invalid template; render throw | Return DTO for **existing Angular** path; optional non-PHI log that fallback was used |

### FACT — Current API already separates these

Eligibility exceptions are thrown from managers **before** any presentation layer would run. The adapter must be invoked only on the success path after enrichment.

---

## 9. Diagnostic vs Radiology Architecture

| Area | Diagnostic | Radiology |
|------|------------|-----------|
| Eligibility | `TestReportManager` payment + DoctorApproved + results; Print All/Specific | `RadiologyReportManager` Authorized/Released + Paid + findings/impression |
| DTO | `DiagnosticTestReportDto` | `DiagnosticRadiologyReportDto` |
| API | `GET .../TestReport` (+ print options) | `GET .../RadiologyReport` (+ accessions) |
| Auth module | `Reports` | `RadiologyReports` |
| Existing renderer | `test-report.component.*` | `radiology-report-print.component.*` |
| Browser output | iframe / `window.print()` | iframe / `window.print()` |
| Resolver context | TestId/ProfileId via request/code lookup (gap documented) | TestId via HISTestCode lookup (gap documented) |
| Declarative capability | RPG / parameter table / signatures | Narratives / accession / authorizer |
| Shared presentation adapter | Yes (recommended) | Yes (recommended) |
| Fallback | Existing Angular | Existing Angular |

**Do not merge** Diagnostic and Radiology business pipelines.

---

## 10. Existing Output Path Integration

### FACT

Production output is **client HTML + browser print**, not server PDF.

### RECOMMENDATION

1. API optionally attaches presentation payload, e.g. conceptual fields:  
   - `PresentationMode`: `Existing` | `Declarative`  
   - `Html` / `Css` (only when Declarative success)  
   - `FallbackReason` (optional, non-PHI, for diagnostics)  
2. Angular:  
   - If `PresentationMode == Declarative` and Html present → bind via trusted HTML viewer pattern (similar to Admin `report-template-viewer`) inside the existing print root element.  
   - Else → existing component templates.  
3. Existing `print()` continues to print the visible root (`#diagnostic-test-report-print` / `#diagnostic-radiology-report-print`).

### Explicit non-introduction

No server PDF library, no new print microservice, no report snapshot storage (unless a future product decision separately requires it).

---

## 11. Feature Enablement Boundary

### FACT

`UseDeclarativeRendererForProductionPrint` is a **compile-time const `false`**.

### RECOMMENDATION — Architecture must support later (not finalize now)

| State | Meaning |
|-------|---------|
| Disabled | Always existing renderer (current production) |
| Controlled testing | Enable only in non-prod / flagged environments (later design) |
| Per report type | Diagnostic and/or Radiology independently (later design) |
| Broader production | Only after evidence + approval |

Step 2 does **not** choose config store (const vs appSetting vs DB). It only requires the presentation adapter to consult an enablement gate that can start as the existing flag and evolve without changing eligibility.

**Do not enable anything in this step.**

---

## 12. Failure Handling Concept

### RECOMMENDATION — Conceptual result (exact types later)

A presentation decision result should distinguish:

| Outcome | Printable via declarative? | Client action |
|---------|----------------------------|---------------|
| SuccessDeclarative | Yes | Show declarative HTML |
| FallbackToExisting | No | Show Angular template |
| (Eligibility failure) | N/A | Never reaches presentation |

Requirements:

- No “success” declarative print with empty/broken HTML.  
- Exceptions inside adapter caught → FallbackToExisting (unless later policy chooses otherwise for hard-fail modes in test environments).  
- Business exceptions propagate unchanged.

Exact class names (`PresentationResult`, etc.) are deferred to a later design/implementation step.

---

## 13. Logging / Observability Requirements

### RECOMMENDATION — Capture eventually (no DB tables in Step 2)

Non-PHI events:

- Engine disabled  
- Resolve source (SystemDefault / CustomGeneric / Specific / None)  
- Builtin path chosen  
- Validation failure (message category, not definition PHI)  
- Renderer exception type/message (sanitized)  
- Fallback used  
- ReportType  
- TemplateId / VersionId when available  
- Correlation: LabNo/InvoiceNo/Accession **hashes or ids only if already used in ops logs** — prefer template ids + report type over patient identifiers  

**Avoid logging:** patient names, MRN, result values, narrative clinical text, raw definition with any embedded PHI (definitions should not contain PHI by Phase 1–3 rules).

No new audit tables required by Step 2 architecture.

---

## 14. Security Boundary

### FACT

- Production report GETs already QAuthorize’d.  
- Designer/preview APIs use `ReportTemplateConfiguration` module permissions.  
- Validator enforces component/binding/style/condition allow-lists and rejects unsafe content.  

### RECOMMENDATION

1. **Do not** add a public/anonymous “render template” endpoint for production print.  
2. Production declarative render must run only inside the already-authorized report GET success path (or an equally authorized internal call).  
3. Client must treat declarative HTML as server-trusted only after server validation/render (same DomSanitizer bypass pattern as Admin viewer — server remains the trust boundary).  
4. Preserve Admin-only designer; production users only consume presentation output.

---

## 15. Performance Considerations

### Likely costs when enabled

- Template Resolve DB lookups (mode, assignments, versions)  
- Definition JSON parse + validate  
- Declarative HTML generation  

### RECOMMENDATION

- Do not introduce caching in Step 2.  
- Later step may consider short-lived cache of published definition by `VersionId` if profiling shows need.  
- Avoid reconstructing clinical DTOs for fallback (reuse enriched instance).  
- Print All should not N-resolve per section unless a future design explicitly requires multi-template composition (not recommended now).

---

## 16. Backward Compatibility

### Requirement (locked)

Existing customer + no custom activation + engine OFF → **identical** presentation path to today.

### How architecture satisfies it

1. Enablement OFF → adapter always returns FallbackToExisting.  
2. Mode SystemDefault → Resolve builtin → existing Angular.  
3. Custom templates saved but not activated / mode not Custom → existing Angular.  
4. No change to eligibility or DTO shape required for fallback clients (optional presentation fields ignored by old clients if additive).

**No big-bang replacement.**

---

## 17. RBAC Implications

### FACT

- Designer: `ReportTemplateConfiguration` (+ menu catalog).  
- Diagnostic print: `Reports` CanView.  
- Radiology print: `RadiologyReports` CanView.  

### RECOMMENDATION

- Production presentation uses **existing report view permissions** only.  
- Do not require `ReportTemplateConfiguration` to print.  
- Do not create parallel authorization.  
- Designer remains Admin-configured via existing RBAC.

---

## 18. Database / Migration Implications

### Step 2 architecture conclusion

**No new DB change required by Step 2 architecture.**

Existing Phase 1–3 tables already support Resolve + published definitions + mode.

### Later Phase 4 implementation note (locked project rule)

If a later enablement design introduces configuration storage beyond the compile-time flag, Phase 4 **implementation** must ship migration + deployment SQL for the final model. That is **not** authorized by Step 2 and is not required for the boundary described here.

---

## 19. Files / Classes That Would Eventually Need Modification

*(Inventory only — do not modify now.)*

| Area | Candidates |
|------|------------|
| New shared adapter | e.g. production presentation service beside `ReportTemplateManager` / `DeclarativeReportRenderer` |
| API | `OperationalReportsController.GetTestReport`, `GetRadiologyReport` |
| Response contract | Additive DTO/wrapper fields for presentation mode/html/css |
| Enablement | `ReportTemplateEngineFeatureFlags` and/or later config |
| Angular Diagnostic | `test-report.component.ts/html` |
| Angular Radiology | `radiology-report-print.component.ts/html` |
| Possibly reuse | `report-template-viewer` patterns for trusted HTML |
| Tests | New presentation/fallback tests; existing print/eligibility regressions must stay green |
| DI | `SimpleInjectorConfig` registration for adapter |

**Should not need changes for Step 2 architecture:** payment managers, approval workflows, Sample Collection, signature storage entities, `ReportLayoutConfiguration` core logic (consume as today).

---

## 20. Risks / Unknowns

| Item | Type | Notes |
|------|------|-------|
| TestId/ProfileId not on print DTOs | **GAP / RECOMMENDATION** | Resolve via HISTestCode / invoice line at presentation time; confirm mapping rules in a later step |
| Print All + multiple Specific templates | **RECOMMENDATION** | Use Generic/SystemDefault for Print All; Specific for Print Specific only |
| Declarative vs Angular page-break parity | **UNKNOWN** until controlled UAT | Fallback preserves known-good Angular pagination |
| Trusted HTML in production Angular | **RISK** | Mitigate by server-only validated render; no client-supplied HTML |
| Enrichment ordering | **FACT** | Adapter must run **after** signature/branding/layout enrich |
| Radiology Specific targeting | **UNKNOWN** until HISTestCode→TestId mapping proven in data | Document for Step 3+ |
| Pixel parity of custom templates | **OUT OF SCOPE** for Step 2 | Presentation correctness + fallback safety first |

---

## 21. Explicit Non-Goals (Step 2)

- Implementation of wiring or flag flip  
- Rollout mechanism finalization  
- Declarative System Default as production default  
- Server PDF / new document engine  
- Merging Diagnostic and Radiology clinical pipelines  
- Moving eligibility into template engine  
- Schema changes  
- Caching framework  
- Full audit table design  
- Phase 4 Step 3+  

---

## 22. Step 2 Recommendation

### Proposed decision for user approval

1. Adopt the **post-enrichment, pre-response shared presentation adapter** as the production integration boundary.  
2. Keep **existing Angular HTML + browser print** as the mandatory fallback and as the System Default/builtin production path for now.  
3. Use existing Phase 3 **Resolve** hierarchy without inventing a new one.  
4. Treat enablement as a **later controlled step**; architecture assumes OFF by default.  
5. Resolve TestId/ProfileId from print request context via existing masters/invoice data (**no DB change required** for the boundary).  
6. Never print failed declarative output; never use presentation fallback to bypass business eligibility failures.

---

## Traceability legend

| Tag | Meaning |
|-----|---------|
| **FACT** | Verified in current source |
| **RECOMMENDATION** | Proposed architecture for approval |
| **UNKNOWN** / **GAP** | Cannot fully close from DTO surface alone; needs later confirmation |

### Key source citations (FACT)

- `LIS.Businesslogic/TestReportManager.cs` — `GetDiagnosticTestReport`, `ValidateWorkflow`, `GetPrintableTestOptions`  
- `LIS.Businesslogic/RadiologyReportManager.cs` — `GetRadiologyReportForPrint`, `GetPrintableAccessions`  
- `web/Lis.Api/Controllers/Api/OperationalReportsController.cs` — `GetTestReport`, `GetRadiologyReport`, enrich methods  
- `web/Lis.Web/src/app/reports/test-report/test-report.component.ts` — load/print  
- `web/Lis.Web/src/app/reports/radiology-report-print/radiology-report-print.component.ts` — load/print  
- `LIS.Businesslogic/ReportTemplateManager.cs` — `Resolve`, builtin System Default  
- `LIS.Businesslogic/Reports/DeclarativeReportRenderer.cs` — declarative HTML  
- `LIS.DtoModel/Models/Reports/ReportTemplateEnginePhase2.cs` — `UseDeclarativeRendererForProductionPrint = false`  
- `LIS.DtoModel/Models/Reports/ReportTemplateDtos.cs` — `ReportTemplateResolveRequest`  

---

## Final design decision

**STEP 2 STATUS: PROPOSED — PENDING USER APPROVAL**

Do not begin Step 3 until this document is approved.

---

## Change integrity (this Step 2 delivery)

| Item | Count |
|------|-------|
| Files modified other than this Step 2 design document | **0** |
| Source code changes | **0** |
| Angular changes | **0** |
| Database changes | **0** |
| SQL/migration changes | **0** |
| Configuration changes | **0** |
| Tests modified | **0** |
| Deployment changes | **0** |

*Only documentation added: `docs/Report-Template-Engine-Phase4-Step2-Design.md`.*
