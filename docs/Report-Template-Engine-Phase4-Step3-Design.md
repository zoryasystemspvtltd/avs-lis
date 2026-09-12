# Report Template Engine — Phase 4 Step 3 Design

**Project:** ZoryaLMS / AVILIS  
**Date:** 2026-09-12  
**Phase:** 4 — Design Step 3 (Production Template Resolution Context)  
**Status:** PROPOSED — PENDING USER APPROVAL  
**Prerequisites:** Step 1 APPROVED · Step 2 APPROVED  

---

## 1. Executive Summary

### Core question

> Can the existing production workflow provide deterministic Test/Profile identity to the Phase 3 resolver without introducing new clinical/business logic?

### Answer (source-based)

| Scenario | Deterministic Specific context? | Conclusion |
|----------|----------------------------------|------------|
| **Diagnostic Print Specific** | **Yes**, when `SaleInvoiceDetail.RequestDetailId` links the selected `TestRequestDetailId` | Pass `TestId` (and optional `TestProfileId`) from that invoice line into Resolve |
| **Diagnostic Print All** | **Not safely for multi-Specific composition** | Resolve **without** Specific context → Generic / System Default / existing renderer |
| **Radiology** | **Conditionally yes** via invoice line `TestId` matched to the accession’s test | Prefer invoice-line `TestId`; do **not** rely solely on `HISTestCode → HisTestMaster.Id` |

### Critical mapping verdict

| Mapping | Verdict |
|---------|---------|
| `TestRequestDetailId` → `SaleInvoiceDetail` → `TestId` / `TestProfileId` | **SAFE** when the link exists (FK identity already used by booking/print ordering) |
| `HISTestCode` → `HisTestMaster.Id` alone | **UNSAFE as sole Specific selector** (app uniqueness on write; no proven DB unique index; code uses `FirstOrDefault` / `GroupBy.First`) |
| `ProfileCode` → `TestProfileMaster.Id` alone | **UNSAFE as sole Specific selector** (same pattern; prefer `SaleInvoiceDetail.TestProfileId`) |

### Principle

The presentation adapter should **receive** identity already owned by the print request / invoice linkage.  
If Specific identity cannot be established deterministically: **do not guess** → resolve without Specific (Generic/System Default) or fall back to existing renderer.

---

## 2. Actual Diagnostic Print Specific Flow

### FACT — User → API → Manager

| Step | Source | Identifier |
|------|--------|------------|
| UI selection | `test-report.component.ts` | `selectedTestRequestDetailId` |
| HTTP | `report.service.ts` `getTestReport(labNo, testRequestDetailId)` | query `testRequestDetailId` |
| Controller | `OperationalReportsController.GetTestReport` | `long? testRequestDetailId` |
| Manager | `TestReportManager.GetDiagnosticTestReport(..., testRequestDetailId)` | filters `TestRequestDetail` to that Id |

### FACT — What Print Specific proves in business layer

- Selected Id must belong to the invoice’s requests.  
- Only that request is workflow-validated (siblings may remain pending).  
- Payment already validated for the invoice.  
- Assembled DTO sections carry `TestRequestDetailId`, `TestCode`, `TestName`, `Department` — **not** `TestId` / `ProfileId`.

### FACT — Where master TestId / ProfileId already exist

`SaleInvoiceDetail` (booking line):

- `TestId` → FK `HisTestMaster` (**same integer space** as Phase 3 `ReportTemplateAssignment.TestId` / `TargetTestId`)  
- `TestProfileId` → optional profile package id (**same space** as Phase 3 `ProfileId` / `TargetProfileId`)  
- `RequestDetailId` → optional FK to `TestRequestDetail.Id` (set during invoice/request linking in `SaleInvoiceManager`)

`TestReportManager.BuildInvoiceTestOrderLookup` already joins active invoice lines by `RequestDetailId` for print ordering — **FACT** that this linkage is part of the current print workflow.

### RECOMMENDATION — Context without changing business rules

For Print Specific, after eligibility succeeds, the presentation adapter (or a thin context helper used only by presentation) may:

1. Take `testRequestDetailId` already on the API call.  
2. Load the invoice’s `SaleInvoiceDetail` where `RequestDetailId == testRequestDetailId` and `IsActive`.  
3. If **exactly one** matching line (or a deterministic single chosen line):  
   - `Resolve.TestId = line.TestId`  
   - `Resolve.ProfileId = line.TestProfileId` (if present)  
4. If **no** matching line: **do not** invent TestId via code alone → Resolve **without** TestId/ProfileId (Generic/System Default path) or existing renderer.

This does **not** alter payment, approval, or DTO assembly. It reuses booking identity already stored for the selected request.

---

## 3. Actual Diagnostic Print All Flow

### FACT — Flow

| Step | Behavior |
|------|----------|
| UI | `printMode === 'all'`; `testRequestDetailId` omitted |
| API | `GetTestReport(labNo)` without specific Id |
| Manager | Validates **all** applicable requests; builds full DTO |
| Groups | `DepartmentGroups` (primary Angular path); also `ProfileGroups` + flat `Sections` |

### FACT — Identity on assembled DTO

| On DTO | Present? |
|--------|----------|
| Per-section `TestRequestDetailId` | Yes |
| Per-section `TestCode` / `TestName` / `Department` | Yes |
| Per-section `TestId` (HisTestMaster.Id) | **No** |
| `ProfileGroups[].ProfileName` / `ProfileCode` | Yes |
| `ProfileGroups[].ProfileId` | **No** |

### FACT — Phase 3 Resolve returns **one** template for the whole report

`ReportTemplateManager.Resolve` selects a single assignment/template.  
`DeclarativeReportRenderer` applies **one** definition to the entire DTO.

### RECOMMENDATION — Print All resolution

**Do not** attempt “Specific template per test/profile” inside a single Print All document without a new multi-template composition design (explicit non-goal).

Safe Print All behavior:

```
Resolve(ReportType=Diagnostic)  // no TestId, no ProfileId
  → Active Custom Generic (if Custom mode)
  → else System Default / builtin → existing Angular renderer
```

Individual Specific templates remain meaningful for **Print Specific** only.

### Outcome classification

| Option | Verdict |
|--------|---------|
| A. Each test/profile resolves independently in one print | **Not supported** by current Resolve/renderer (would require new design) |
| B. Only Generic / System Default | **RECOMMENDED** |
| C. Shared report-level template | Same as B (Generic is the report-level custom template) |
| D. Structure prevents safe Specific for Print All | **FACT** relative to single-template Resolve |

---

## 4. Actual Radiology Flow

### FACT — Flow

| Step | Identifier |
|------|------------|
| UI | Selected accession / `radiologyRequestId` |
| API | `GetRadiologyReport(long radiologyRequestId)` |
| Manager | `GetRadiologyReportForPrint` |
| Request entity | `RadiologyRequestDetail`: `Id`, `AccessionNo`, `HISTestCode`, `HISTestName`, `Department`, `Modality`, `HISRequestNo` |
| Print DTO | Header fields above; **no** `TestId` / `ProfileId` |

### FACT — How radiology requests relate to tests

`SaleInvoiceManager` creates/links radiology requests from invoice lines that have `TestId` (HisTestMaster). Radiology stores `HISTestCode` / name on the request, not the numeric `TestId`.

### FACT — Phase 3 Radiology Specific targets

Designer Specific targets use `HisTestMaster.Id` / `TestProfileMaster.Id` (`ReportTemplateManager.Designer` `ListTestProfileTargets`).  
There is **no** separate “Accession-specific” or “Modality-specific” template target in Phase 3.

### RECOMMENDATION — Radiology Specific target

Legitimate Specific target = **HisTestMaster.Id** (same as Diagnostic Test Specific), obtained by:

1. Load `RadiologyRequestDetail` by `radiologyRequestId` (already used for print).  
2. Resolve invoice via `HISRequestNo`.  
3. Find active `SaleInvoiceDetail` for that invoice whose `TestId` maps to the same test as the request (see §6).  
4. Pass that `TestId` into Resolve.  
5. If undeterministic / missing → Resolve without TestId (Generic/System Default) or existing renderer.

Do **not** invent AccessionNo-based or Modality-based Specific targets.

---

## 5. Available Resolution Identifiers

| Identifier | Where available | Use for Phase 3 Resolve? |
|------------|-----------------|---------------------------|
| `ReportType` | Endpoint / known | **Yes** (required) |
| `TestRequestDetailId` | Print Specific query; DTO sections | Context to find invoice line; **not** a Resolve field |
| `SaleInvoiceDetail.TestId` | Invoice line | **Yes** → `Resolve.TestId` |
| `SaleInvoiceDetail.TestProfileId` | Invoice line (profile packages) | **Yes** → `Resolve.ProfileId` when present |
| `SaleInvoiceDetail.RequestDetailId` | Invoice line | Link key to Print Specific |
| `HISTestCode` / section `TestCode` | Request / DTO | Secondary only; see §6 |
| `ProfileCode` / `ProfileName` | ProfileGroups on DTO | Secondary only; see §7 |
| `Department` / `DepartmentCode` | Request / test master | Phase 3 Custom Resolve **skips** department (**FACT**) |
| `AccessionNo` / `RadiologyRequestDetail.Id` | Radiology print | Print key only; not Phase 3 Specific target |
| `Modality` | Radiology header | Not a Phase 3 Specific target |

---

## 6. TestId Mapping Analysis

| Target | Existing identifier | Deterministic? | Source | Safe for Specific? |
|--------|---------------------|----------------|--------|--------------------|
| Phase 3 `TestId` | `SaleInvoiceDetail.TestId` | **Yes** (FK) | Booking line | **Yes** when line linked |
| Phase 3 `TestId` | `HISTestCode` → `HisTestMaster.Id` | **Not proven** | Master lookup `FirstOrDefault` | **UNSAFE alone** |
| Print key | `TestRequestDetailId` | Yes | API / DTO | Context only |

### HISTestCode → HisTestMaster.Id (deep dive)

| Check | Finding | Tag |
|-------|---------|-----|
| Relationship | Requests/lines store code; master has `Id` + `HISTestCode` | FACT |
| App uniqueness | `HISTestMasterManager.ValidateUniqueTest` rejects duplicate codes on add/update | FACT |
| DB unique index | Not found in inspected migrations/index scripts for `HISTestMaster.HISTestCode` | UNKNOWN / not proven |
| Existing usage | Widespread `FirstOrDefault` by code (`TestReportManager`, `SaleInvoiceManager`, etc.) | FACT |
| Duplicate handling elsewhere | `ReportManager` uses `GroupBy(HISTestCode).First()` | FACT — defensive against duplicates |
| New clinical behavior? | Using code-only pick for Specific could select wrong template if duplicates exist | RISK |
| Extra lookup | Yes, if used | FACT |

### Verdict

**Do not approve `HISTestCode → Id` as the primary Specific resolution mechanism.**

Prefer **invoice-line `TestId`**. Use code-only mapping only if a later step proves uniqueness in data + DB constraint; until then treat as **UNSAFE / UNKNOWN** for Specific selection.

---

## 7. ProfileId Mapping Analysis

| Target | Existing identifier | Deterministic? | Source | Safe? |
|--------|---------------------|----------------|--------|-------|
| Phase 3 `ProfileId` | `SaleInvoiceDetail.TestProfileId` | **Yes** when set | Booking | **Yes** |
| Phase 3 `ProfileId` | `ProfileCode` → `TestProfileMaster.Id` | App uniqueness on write; `First`-style risk if legacy dupes | Master | **UNSAFE alone** |
| DTO | `ProfileGroups.ProfileCode` / `Name` | Present | Print DTO | Insufficient alone |

### FACT — Profile code uniqueness (app)

`TestProfileMasterManager.ValidateProfile` rejects duplicate `Code` / `Name` on save.

### FACT — Print DTO

`DiagnosticTestReportProfileGroup` exposes `ProfileName` / `ProfileCode` only — **not** `Id`.

### Consequence if ProfileId unavailable

- Profile Specific templates cannot be selected safely from DTO fields alone.  
- Print Specific can still supply `ProfileId` from the invoice line when `TestProfileId` is set.  
- Print All should not depend on Profile Specific.  
- If line has no `TestProfileId`, Resolve simply omits `ProfileId` (Test Specific and/or Generic still apply).

---

## 8. Proposed Resolution Context

### RECOMMENDATION — Minimum context for existing Resolve

Only fields already on `ReportTemplateResolveRequest`:

| Field | Required? | Content |
|-------|-----------|---------|
| `ReportType` | Yes | `Diagnostic` / `Radiology` |
| `TestId` | Optional | HisTestMaster.Id when deterministically known |
| `ProfileId` | Optional | TestProfileMaster.Id when deterministically known |
| `DepartmentCode` | Optional / unused in Custom Resolve | Omit for Phase 4 Custom path |

### Explicitly exclude

- Patient/MRN/names  
- Result values / narratives  
- Payment or approval flags  
- PHI of any kind  

### Conceptual carrier (not implementing)

Presentation receives something equivalent to:

```
ReportType
+ optional TestId
+ optional ProfileId
+ already-enriched eligible DTO
+ enablement flag (later step)
```

Owner of TestId/ProfileId: **existing booking/print identifiers** (`testRequestDetailId` / `radiologyRequestId` + invoice lines), not the template engine inventing clinical identity.

---

## 9. Print Specific Resolution

### RECOMMENDATION

```
Eligibility already passed
    ↓
Build context from SaleInvoiceDetail via RequestDetailId
    ↓
If TestId known:
      Resolve(Diagnostic, TestId, ProfileId?)
        → Specific Test
        → else Specific Profile (if ProfileId)
        → else Active Generic
        → else System Default / builtin → existing Angular
    ↓
If TestId unknown:
      Resolve(Diagnostic) without Specific
        → Generic / System Default / existing Angular
    ↓
Never guess a TestId
```

### Phase 3 precedence (FACT)

Resolve checks **Test** assignment before **Profile** assignment. Passing both is valid; Test Specific wins when present.

---

## 10. Print All Resolution

### RECOMMENDATION

```
Resolve(Diagnostic)  // no TestId, no ProfileId
  → Active Custom Generic
  → else System Default / builtin → existing Angular
```

Do not select a Specific template based on “first test,” “majority,” or any heuristic.

---

## 11. Radiology Resolution

### RECOMMENDATION

```
Eligibility already passed (Authorized/Released + Paid + content)
    ↓
Derive TestId from invoice line matching this accession’s test (deterministic)
    ↓
If TestId known:
      Resolve(Radiology, TestId)
        → Specific Test
        → else Active Generic
        → else System Default / builtin → existing Angular
    ↓
If TestId unknown:
      Resolve(Radiology) without TestId
        → Generic / System Default / existing Angular
```

Profile Specific for Radiology is unlikely in current booking (radiology lines typically non-profile in `LinkRadiologyRequestsToLines`); if `TestProfileId` ever present on a matching line, same optional ProfileId rules apply — **do not invent** radiology-only profile semantics.

---

## 12. Fallback Behavior

### Locked

| Condition | Action |
|-----------|--------|
| Specific context not deterministic | Do **not** guess; omit Specific or use existing renderer |
| Engine disabled / builtin System Default | Existing Angular renderer (Step 2) |
| Invalid template / render failure | Existing Angular renderer |
| Unpaid / ineligible | **No print**; eligibility failure already blocked upstream |

Fallback never converts a business failure into a successful print.

---

## 13. Business / Presentation Boundary

| Concern | Owner |
|---------|-------|
| Payment, approval, radiology auth, Print All/Specific eligibility, DTO assembly | Existing managers (**unchanged**) |
| Choosing presentation template | Presentation adapter + Phase 3 Resolve |
| Looking up `SaleInvoiceDetail` by `RequestDetailId` for Resolve keys | Presentation-context helper only (identity already stored by booking) |

### Boundary risk to document (not fix)

If a future implementation put eligibility checks inside the template adapter, that would violate Step 1. Step 3 design forbids it.

Using invoice-line FKs for Resolve keys does **not** redefine clinical identity; it reuses booking FKs that already drove request creation.

---

## 14. Performance Implications

| Path | Likely cost |
|------|-------------|
| Print Specific context | One invoice-detail query by `RequestDetailId` (or in-memory if adapter receives preloaded lines) |
| Print All | No per-test Resolve loops recommended |
| Radiology | Invoice + matching line lookup |
| Resolve itself | Mode + assignment + version reads (existing Phase 3) |

No caching proposed in Step 3. Avoid N Resolves per section for Print All.

---

## 15. Security Implications

- No new anonymous endpoints.  
- Context derived only inside already-authorized report GET success path.  
- Context contains ids only (ReportType, TestId, ProfileId) — no PHI.  
- Designer RBAC unchanged (`ReportTemplateConfiguration`).  
- Production print RBAC unchanged (`Reports` / `RadiologyReports`).

---

## 16. Database Implications

**No new DB change required for Step 3 architecture.**

Existing `SaleInvoiceDetail.TestId`, `TestProfileId`, `RequestDetailId` and Phase 3 assignment tables are sufficient when linkage is present.

### Gap (data quality, not schema)

If some environments have print-eligible requests **without** `SaleInvoiceDetail.RequestDetailId`, Specific resolution is unavailable for those prints until data/link quality is addressed — presentation must fall back safely (no schema invention in Step 3).

---

## 17. RBAC Implications

No RBAC changes. Production resolution must not bypass report view authorization or designer Admin gates.

---

## 18. Risks / Unknowns

| Item | Tag | Notes |
|------|-----|-------|
| Legacy lines missing `RequestDetailId` | UNKNOWN prevalence | Specific unavailable → Generic/fallback |
| Historical duplicate `HISTestCode` | UNKNOWN | Why code-only mapping is unsafe |
| Radiology line matching ambiguity (same code, multiple lines) | RISK | Require deterministic single match or omit Specific |
| Multi-template Print All | OUT OF SCOPE | Would need new composition design |
| DB unique constraint on HISTestCode | UNKNOWN | App enforces; DB not proven |

---

## 19. Recommended Step 3 Architecture

1. **Print Specific (Diagnostic):** Derive `TestId` (+ optional `ProfileId`) from `SaleInvoiceDetail` where `RequestDetailId = testRequestDetailId`. Pass into existing Resolve.  
2. **Print All (Diagnostic):** Resolve with ReportType only (Generic / System Default / existing renderer).  
3. **Radiology:** Derive `TestId` from invoice line linked to the accession’s test; else omit Specific.  
4. **Never** use `HISTestCode`/`ProfileCode` alone to pick Specific templates.  
5. **Never** guess; incorrect Specific is worse than Generic/existing renderer.  
6. **No DTO redesign** required to force Specific onto Print All.  
7. **No DB changes** for this step.

---

## 20. Explicit Non-Goals

- Implementation of context helper or adapter wiring  
- DTO schema changes to embed TestId/ProfileId  
- Multi-template composition for Print All  
- Accession/Modality Specific targets  
- DB unique-index projects (unless a later approved step)  
- Enablement / feature-flag flip  
- Step 4+ design  

---

## Traceability (material claims)

| Claim | Location |
|-------|----------|
| Print Specific API id | `report.service.ts` `getTestReport`; `OperationalReportsController.GetTestReport`; `TestReportManager.GetDiagnosticTestReport` |
| Print options expose `TestRequestDetailId` + `TestCode` | `TestReportManager.GetPrintableTestOptions` |
| Invoice line FKs | `SaleInvoiceDetail.TestId`, `TestProfileId`, `RequestDetailId` |
| RequestDetailId set on booking | `SaleInvoiceManager` (request link / line update) |
| Print order uses RequestDetailId | `TestReportManager.BuildInvoiceTestOrderLookup` |
| Profile groups lack ProfileId | `DiagnosticTestReportProfileGroup` |
| Resolve fields / Test-then-Profile | `ReportTemplateResolveRequest`; `ReportTemplateManager.Resolve` |
| HISTestCode uniqueness on write | `HISTestMasterManager.ValidateUniqueTest` |
| Duplicate-code defensive grouping | `ReportManager` `GroupBy(HISTestCode).First()` |
| Radiology request identifiers | `RadiologyRequestDetail`; `GetRadiologyReportForPrint` |
| Radiology created from TestId lines | `SaleInvoiceManager.LinkRadiologyRequestsToLines` / related |
| Phase 3 Specific targets | `ReportTemplateManager.Designer` `ListTestProfileTargets` / activate assignments |

---

## Final status

**STEP 3 STATUS: PROPOSED — PENDING USER APPROVAL**

Do not begin Step 4 or implementation until approved.

---

## Change integrity

| Item | Count |
|------|-------|
| Source code changes | **0** |
| Angular changes | **0** |
| DTO changes | **0** |
| Database changes | **0** |
| SQL/migration changes | **0** |
| Configuration changes | **0** |
| Test changes | **0** |
| Deployment changes | **0** |

*Only documentation added: `docs/Report-Template-Engine-Phase4-Step3-Design.md`.*
