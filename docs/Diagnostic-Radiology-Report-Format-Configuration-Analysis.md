# Diagnostic & Radiology Report Format Configuration — Architectural Analysis

**Type:** Read-only discovery  
**Date:** 2026-09-11  
**Solution:** ZoryaLMS / avs-lis  
**Database (runtime):** `ZoryaLMS` on `.\SQLEXPRESS` (from deployed API `Web.config`)  
**Scope:** How Diagnostic and Radiology report **formats** are configured, selected, generated, and rendered  

---

## 1. Executive Summary

ZoryaLMS does **not** implement a Report Format Configuration Framework.

| Concern | Diagnostic (Lab Test Report) | Radiology Report |
|---------|------------------------------|------------------|
| Layout / template | **Hardcoded** Angular HTML + CSS | **Hardcoded** Angular HTML + CSS |
| Format selection (Test / Dept / Centre / type) | **None** — single fixed layout | **None** — single fixed layout |
| Format master / template table | **Not found** | **Not found** |
| Admin UI to configure formats | **Not found** | **Not found** |
| Renderer | Browser HTML print (iframe) | Browser HTML print (iframe) |
| Server PDF engine | **Not found** | **Not found** |
| Branding strings (`Report:*`) | Loaded into DTO; mostly unused on print surface | **Not applied** |
| Letterhead | Blank band for **pre-printed stationery** (~5 cm) | Blank band for **pre-printed stationery** (~4 cm header / 5 cm footer gap) |
| Content | Parameter/result grid + ranges + comments | Narrative Clinical History / Findings / Impression / Recommendation |

**Bottom line:** Report **structure and presentation are code-owned**. Administrators cannot change Diagnostic or Radiology print layouts without a software change/deployment. Content (results, ranges, radiology narrative, doctor signature) is data-driven; layout is not.

---

## 2. Diagnostic Report Architecture

### End-to-end flow

```text
UI /reports/test-report (TestReportComponent)
        ↓
ReportService.getTestReportLabNumbers / getTestReport
        ↓
GET api/Reports/TestReportLabNumbers
GET api/Reports/TestReport?labNo=
        ↓
OperationalReportsController
        ↓
TestReportManager.GetDiagnosticTestReport / GetPrintableLabNumbers
        ↓
DiagnosticTestReportDto
        ↓
Controller EnrichLabApprover + EnrichReportBranding
        ↓
Angular HTML template (#diagnostic-test-report-print)
        ↓
print(): hidden iframe + getDiagnosticPrintStyles() → browser print
```

### Angular

| Item | Location |
|------|----------|
| Component | `web/Lis.Web/src/app/reports/test-report/test-report.component.ts` |
| Template | `.../test-report.component.html` |
| Styles | `.../test-report.component.css`, `reports/reports.shared.css` |
| Route | `/reports/test-report` (`app.routing.ts`) |
| Menu / RBAC | `REPORT_DIAGNOSTIC` → module `Reports` |
| Service | `web/Lis.Web/src/app/_services/report.service.ts` |

### Print mechanism

1. Clone `#diagnostic-test-report-print` into a hidden iframe.  
2. Inject hardcoded CSS from `getDiagnosticPrintStyles()`.  
3. `iframe.contentWindow.print()`.  
4. Fallback: `printViaWindow()` + body class `test-report-print-mode`.

**No server-side PDF generation** was found for this path.

### Secure download (related)

- `GET api/report/download/{token}` → `ReportDownloadController` returns **JSON** `DiagnosticTestReportDto` (anonymous token path).  
- Does **not** call `EnrichLabApprover` / `EnrichReportBranding` (portal print path does).

---

## 3. Radiology Report Architecture

### End-to-end flow (print)

```text
Invoice (Diagnostic processing dept) → RadiologyRequestDetail
        ↓
Radiology Entry / Doctor Approval (workflow APIs)
        ↓
UI /reports/radiology-report (RadiologyReportPrintComponent)
        ↓
GET api/Reports/RadiologyPrintAccessions
GET api/Reports/RadiologyReport?radiologyRequestId=
        ↓
OperationalReportsController
        ↓
RadiologyReportManager.GetRadiologyReportForPrint / GetPrintableAccessions
        ↓
DiagnosticRadiologyReportDto
        ↓
Controller EnrichRadiologyApprover (branding NOT enriched)
        ↓
Angular HTML template (#diagnostic-radiology-report-print)
        ↓
print(): hidden iframe + getPrintStyles() → browser print
```

### Angular

| Item | Location |
|------|----------|
| Print component | `web/Lis.Web/src/app/reports/radiology-report-print/` |
| Entry | `web/Lis.Web/src/app/LIS/radiology/radiology-report-entry/` |
| Doctor approval | `.../radiology-doctor-approval/` |
| Approved list | `.../radiology-approved-reports/` (navigates to print) |
| Print route | `/reports/radiology-report` |
| Menu / RBAC | `REPORT_RADIOLOGY` → module `RadiologyReports` |
| Content editor | Plain `<textarea>` (not TinyMCE on radiology path) |

### Workflow APIs (not format config)

`RadiologyReportController` (`api/RadiologyReport`): PendingQueue, Save, Authorize, GetReport, etc.

### Print gates (manager)

Authorized/Released + invoice exists + not cancelled + **Paid** + Findings/Impression present.

---

## 4. Configuration Sources

### A. Web.config `appSettings` (Diagnostic branding only)

Source file (repo): `web/Lis.Api/Web.config`  
Consumed by: `OperationalReportsController.EnrichReportBranding` **only for Diagnostic** `GetTestReport`.

| Key | Purpose | Current repo value | Used by |
|-----|---------|--------------------|---------|
| `Report:LabName` | Lab display name | `Zorya Laboratory` | Diagnostic DTO header (print template largely does not bind lab name) |
| `Report:Tagline` | Tagline | `Accurate Diagnostics. Trusted Care.` | Diagnostic DTO |
| `Report:CentreName` | Centre name | `Zorya Diagnostic Centre` | Diagnostic patient panel (`centreName`) |
| `Report:LogoUrl` | Logo path | `assets/images/logo.png` | Diagnostic DTO (not bound in current print HTML) |
| `Report:LicenseName` | License/lab legal name | `Zorya Laboratory` | Diagnostic DTO |
| `Report:Address` | Address | empty | Diagnostic DTO |
| `Report:Email` | Email | empty | Diagnostic DTO |
| `Report:ContactNumbers` | Contacts | empty | Diagnostic DTO |
| `Report:PharmacyContact` | Pharmacy contact | empty | Diagnostic DTO |
| `Report:AppointmentContact` | Appointment contact | empty | Diagnostic DTO |

Aliases `Report.LabName` etc. are also accepted by `ReadSetting`.

**Fallback:** If LabName/CentreName empty → first `ClientApplications` row (`Name` / `Description`) via `IdentityDbContext`.

**Connection strings / secrets:** Configuration exists — values redacted from this report.

### B. Database

- **No** `ReportTemplate` / `ReportFormat` / `ReportLayout` / `PrintTemplate` tables found.  
- Operational + master data feed **content**, not layout selection.

### C. Angular / code templates

- Diagnostic layout: `test-report.component.html` + print CSS string/CSS files.  
- Radiology layout: `radiology-report-print.component.html` + `getPrintStyles()`.

### D. Physical stationery

Both reports reserve blank header/footer space for **pre-printed letterhead**; the application does not draw facility letterhead graphics in the current templates.

---

## 5. Database Objects

**Database name:** `ZoryaLMS` (deployed API connection).

### Diagnostic content (not format)

| Object | Role |
|--------|------|
| `SaleInvoice` / `SaleInvoiceDetail` | Lab no, invoice, payment gate, bookings |
| `PatientDetails` | Patient demographics |
| `TestRequestDetails` | Requests; doctor-approved gate |
| `TestResults` / `TestResultDetails` / `TestValues` | Approved results |
| `HISTestMaster` | Test name/code |
| `Departments` | Department grouping on print |
| `HISParameterMaster` | Parameter name/code/unit/comments |
| `HISParameterRangMaster` | Reference ranges |
| `TestParameterMappingMaster` | Test↔parameter |
| `TestMappingMaster` | Optional parameter section (`GroupName`) |
| `AspNetUsers` (+ designation/signature path) | Approver enrichment |
| `ClientApplications` | Branding fallback |

### Radiology content (not format)

| Object | Role |
|--------|------|
| `RadiologyRequestDetail` | Accession/request, modality, status |
| `RadiologyResultDetail` | ClinicalHistory, Findings, Impression, Recommendation, DigitalSignature |
| Sale invoice / patient / department / AspNetUsers | Same gates / demographics / signature |

### Schema scripts (tables only)

- `Scripts/fdd-sample-radiology-schema.sql` — creates radiology request/result tables; **not** format templates.  
- Demo seeds (e.g. `SeedDiagnosticReportDemo.sql`) — sample clinical data for UAT print, **not** layout config.

**Stored procedures / views dedicated to report format:** **Not found.**

---

## 6. API Inventory

### Diagnostic

| Endpoint | Controller | Permission |
|----------|------------|------------|
| `GET api/Reports/TestReportLabNumbers` | `OperationalReportsController` | `Reports` CanView |
| `GET api/Reports/TestReport` | `OperationalReportsController` | `Reports` CanView |
| `GET api/report/download/{token}` | `ReportDownloadController` | AllowAnonymous (token) — JSON DTO |

### Radiology print

| Endpoint | Controller | Permission |
|----------|------------|------------|
| `GET api/Reports/RadiologyPrintAccessions` | `OperationalReportsController` | `RadiologyReports` CanView |
| `GET api/Reports/RadiologyReport` | `OperationalReportsController` | `RadiologyReports` CanView |

### Radiology workflow (content, not format)

| Prefix | Controller |
|--------|------------|
| `api/RadiologyReport/*` | `RadiologyReportController` |

### Operational radiology lists (grids, not print format)

`PendingRadiology`, `AuthorizedRadiology`, `ModalityStatistics`, `RadiologistProductivity` via `IReportManager`.

---

## 7. Business Layer Inventory

| Report | Interface | Implementation | Key methods |
|--------|-----------|----------------|-------------|
| Diagnostic | `ITestReportManager` | `TestReportManager` | `GetDiagnosticTestReport`, `GetPrintableLabNumbers` |
| Radiology | `IRadiologyReportManager` | `RadiologyReportManager` | `GetRadiologyReportForPrint`, `GetPrintableAccessions`, Save/Authorize/queues |
| Range evaluation | — | `TestResultRangeEvaluator` | Used by Diagnostic parameter mapping |
| Signature files | — | `DoctorSignatureStorage` | Data-URI from `uploads/doctors` |

DI registration: `web/Lis.Api/App_Start/SimpleInjectorConfig.cs`.

---

## 8. DTO / ViewModel Flow

### Diagnostic — `DiagnosticTestReportDto`

- **Header:** patient, invoice/lab, dates, status, referral, centre, approver fields, branding fields.  
- **DepartmentGroups:** primary print grouping (invoice booking order).  
- **ProfileGroups / Sections:** legacy / flat compatibility.  
- **Section:** test name/code, specimen, parameters, Comment/Note, DoctorApprovalComment.  
- **Parameter:** name, result, unit, reference range, flags/abnormal.

### Radiology — `DiagnosticRadiologyReportDto`

- **Header:** accession, invoice, patient, modality, department, status, dates, authorizer + signature fields.  
- **Body:** ClinicalHistory, Findings, Impression, Recommendation (strings).

---

## 9. Template Inventory

| Report | Physical templates | Type | Load method |
|--------|-------------------|------|-------------|
| Diagnostic | `test-report.component.html` + CSS + inline `getDiagnosticPrintStyles()` | Angular HTML/CSS | Compiled SPA; styles injected into print iframe |
| Radiology | `radiology-report-print.component.html` + CSS + `getPrintStyles()` | Angular HTML/CSS | Same pattern |

**Not found:** Razor report views, RDLC/SSRS, Crystal, embedded report library templates, DB-stored HTML templates.

---

## 10. Renderer / PDF Mechanism

| Mechanism | Diagnostic | Radiology |
|-----------|------------|-----------|
| Browser print via iframe | **Yes** | **Yes** |
| `window.print` fallback | **Yes** | **Yes** |
| Server PDF (iText, PdfSharp, wkhtml, etc.) | **Not found** | **Not found** |
| Download as PDF API | **Not found** (secure download returns JSON) | **Not found** |

---

## 11. Format Selection Logic

**Actual implementation:** there is **no multi-format selection hierarchy**.

```text
Always use the single hardcoded Angular print template
        for Diagnostic   → TestReportComponent
        for Radiology    → RadiologyReportPrintComponent
```

Selection is only:

1. **Which report type page** the user opens (menu/route).  
2. **Which accession/lab no** to load (data identity).  
3. **Eligibility gates** (paid, approved/authorized, required content).

**Not implemented (searched, not found):**

- Per-Test format  
- Per-Department format  
- Per-Centre format  
- Per-Report-Type template master  
- Per-Modality print template  
- Per-Doctor template  

Do **not** invent a Test → Department → Global cascade — **code does not implement one**.

---

## 12. Administrator Configuration UI

**No report-format configuration UI was found.**

What exists instead:

| UI | Purpose |
|----|---------|
| `/reports/test-report` | Print Diagnostic report (data + fixed layout) |
| `/reports/radiology-report` | Print Radiology report (data + fixed layout) |
| Parameter / Range / Test masters | Feed Diagnostic **content** (not layout) |
| Users (doctor designation / signature upload) | Signature content |
| Web.config `Report:*` | Branding strings (Diagnostic DTO; limited print binding) |
| Role/module permissions | Who may open Reports / RadiologyReports |

---

## 13. Header / Footer Mechanism

| Element | Diagnostic | Radiology |
|---------|------------|-----------|
| Facility letterhead drawing | Blank reserved band (pre-printed paper) | Blank letterhead zone (pre-printed paper) |
| Header clearance | ~**5 cm** (`@page` / CSS vars in print styles) | ~**4 cm** header total / letterhead height vars |
| Footer clearance | ~**5 cm** | Footer gap ~**5 cm** (CSS vars) |
| Patient/exam header block | Hardcoded HTML table | Hardcoded HTML table + fixed title “Radiology Diagnostic Report” |
| Lab branding footer contacts | In DTO from config; **not rendered** on current Diagnostic print HTML | Branding enrich **not called** |
| Configurable per template | No alternate templates | No alternate templates |

---

## 14. Doctor Signature Mechanism

| Concern | Diagnostic | Radiology |
|---------|------------|-----------|
| Source user | `Header.ApprovedBy` (approval actor) | Authorize user / `AuthorizedBy` |
| Enrichment | `EnrichLabApprover` | `EnrichRadiologyApprover` |
| Name | AspNet display name | AspNet display name |
| Qualification | `user.Qualification` | **Not conclusively filled the same way on radiology header** (designation + image are; qualification field not present on radiology DTO) |
| Designation | `user.DoctorDesignation` | `AuthorizedByDesignation` |
| Image | `DoctorSignatureStorage.GetSignatureDataUri(DoctorSignaturePath)` under `uploads/doctors` | Same storage helper |
| Typed signature | N/A on Diagnostic print (image/name/designation) | `DigitalSignature` required at authorize; stored on `RadiologyResultDetail`; used if no image |
| Position configurable? | **No** — hardcoded CSS (right-aligned zone) | **No** — hardcoded footer signature block |
| Shared mechanism? | **Same user signature file storage**; **separate enrich methods/DTOs/print zones** | |

---

## 15. Parameter / Reference Range Relationship (Diagnostic)

Report format is **not** selected by Parameter/Range masters. Those masters supply **cell content**:

```text
HISParameterMaster / mappings
        ↓
Approved TestValues
        ↓
TestReportManager.MapParameter
        ↓
TestResultRangeEvaluator.Apply  → reference interval + H/L
        ↓
HISParameterMaster.Comments     → section Comment / Note
        ↓
Angular parameter table columns (fixed):
  Parameter | Result | Unit | Bio. Ref. Interval
```

Radiology does **not** use Parameter Range Master for print body (narrative text only).

---

## 16. Hardcoded vs Configurable Elements

| Element | Configurable? | Source |
|---------|---------------|--------|
| Patient header panel | Structure hardcoded; values from DB | Angular + `BuildHeader` |
| Department heading | Structure hardcoded; name from Department | Angular + department groups |
| Test name | Structure hardcoded; value from test master/results | DTO section |
| Parameter table | **Hardcoded columns/layout** | Angular HTML/CSS |
| Reference range values | Data from Range Master + evaluator | DB + business logic |
| Radiology narrative sections | Structure hardcoded; text from DB | Angular + `RadiologyResultDetail` |
| Doctor signature block | Position/style hardcoded; image/name from Users | CSS + AspNetUsers + file store |
| Header / footer letterhead graphics | Assumed pre-printed paper; blank CSS bands | Hardcoded margins |
| Fonts / A4 / spacing / page breaks | Hardcoded print CSS | Angular |
| Lab name / logo / contacts | Config keys exist | Web.config → DTO; **mostly unused on Diagnostic print surface**; unused on Radiology |
| Multi-format templates | **No** | — |

---

## 17. Diagnostic vs Radiology Comparison

| Dimension | Same? | Notes |
|-----------|-------|-------|
| Overall shell (Angular HTML → iframe print) | **Yes** | Shared architectural pattern |
| Managers / DTOs / entities | **No** | Fully separate |
| Content model | **No** | Grid+ranges vs narrative |
| Branding enrich (`Report:*`) | **Diagnostic only** | Radiology print skips branding enrich |
| Letterhead assumption | **Yes** | Blank band for stationery |
| Format config UI / tables | **Neither has them** | |
| PDF engine | **Neither** | |
| Payment gate for print | **Yes** (Paid invoice) | |
| Doctor signature image storage | **Shared helper** | Separate enrich/print binding |

---

## 18. Architecture Diagram

```text
                    ┌─────────────────────────┐
                    │   Web.config Report:*   │
                    │  (branding strings)     │
                    └───────────┬─────────────┘
                                │ Diagnostic only
                                ▼
┌──────────────┐    ┌───────────────────────┐    ┌────────────────────┐
│ AspNetUsers  │───▶│ OperationalReports    │◀───│ ClientApplications │
│ + signature  │    │ Controller enrichers  │    │ (fallback branding)│
└──────────────┘    └───────────┬───────────┘    └────────────────────┘
                                │
            ┌───────────────────┴───────────────────┐
            ▼                                       ▼
 ┌─────────────────────┐                 ┌──────────────────────┐
 │ TestReportManager   │                 │ RadiologyReportManager│
 │ DiagnosticTestReport│                 │ DiagnosticRadiology   │
 │ Dto                 │                 │ ReportDto             │
 └──────────┬──────────┘                 └──────────┬───────────┘
            │                                       │
            ▼                                       ▼
 ┌─────────────────────┐                 ┌──────────────────────┐
 │ TestReportComponent │                 │ RadiologyReportPrint │
 │ HARDCODED HTML/CSS  │                 │ HARDCODED HTML/CSS   │
 └──────────┬──────────┘                 └──────────┬───────────┘
            │                                       │
            └───────────────────┬───────────────────┘
                                ▼
                     Browser print (iframe)
                     Pre-printed letterhead paper
```

**There is no central Report Format service or template selector.**

---

## 19. Current Limitations

1. Cannot change print layout without code change + portal rebuild/deploy.  
2. Cannot assign different formats per Test, Department, Centre, or Modality.  
3. No admin “Report Template” master.  
4. Branding config exists but Diagnostic print largely ignores logo/lab/address fields; Radiology ignores branding enrich entirely.  
5. Diagnostic vs Radiology letterhead clearances differ (5 cm vs ~4 cm header) — not unified via config.  
6. No server PDF; print quality depends on browser print settings (e.g. Headers and footers).  
7. Secure Diagnostic download returns JSON without branding/approver enrich — alternate clients must handle presentation themselves.

---

## 20. Potential Future Extension Points

*(Analysis only — not implemented.)*

### Backend

- New Report Format / Template master + repository  
- Format selector service (Centre → Department → Test → Global)  
- Controllers for admin CRUD; print APIs accept/resolve `formatId`  
- Optional server HTML→PDF pipeline  

### Frontend

- Admin configuration screens + RBAC module  
- Template-driven print components or shared renderer  

### Reporting

- Externalize HTML/CSS templates (DB or files)  
- Unify Diagnostic/Radiology shell (margins, letterhead, branding binding)  

### Security

- New module permissions for format admin vs print  

### Deployment

- Template files or DB seeds; avoid baking layout only into SPA bundles if runtime config is required  

**Safest near-term extension point if formats become required:** keep current DTOs and print gates; introduce a **read-only template key** resolved once in the print controller/manager, then swap Angular template sections or CSS theme by key — without changing result evaluation or radiology narrative storage.

---

## 21. Files Reviewed (primary)

| Area | Paths |
|------|-------|
| Diagnostic UI | `web/Lis.Web/src/app/reports/test-report/*` |
| Radiology print UI | `web/Lis.Web/src/app/reports/radiology-report-print/*` |
| Report service | `web/Lis.Web/src/app/_services/report.service.ts` |
| Routes / menus | `app.routing.ts`, `MenuCatalog.cs`, `role-permission-catalog.ts` |
| API | `OperationalReportsController.cs`, `RadiologyReportController.cs`, `ReportDownloadController.cs` |
| Business | `TestReportManager.cs`, `RadiologyReportManager.cs`, `TestResultRangeEvaluator.cs` |
| DTOs | `DiagnosticTestReportDto.cs`, `DiagnosticRadiologyReportDto.cs` |
| Signature | `DoctorSignatureStorage.cs` |
| Config | `web/Lis.Api/Web.config` (`Report:*`) |
| Schema scripts | `Scripts/fdd-sample-radiology-schema.sql`, diagnostic seed scripts |
| Prior docs | `docs/Diagnostic-Report-Print-Correction-Certification-Report.md` |

---

## 22. Confirmation: No Code / DB / Configuration Changes Made

| Change type | Count |
|-------------|-------|
| Code changes for this investigation | **0** |
| Database changes | **0** |
| Configuration / Web.config changes | **0** |
| Deployment | **0** |

**Deliverable of this task:** this analysis document only.

> Note: The working tree may contain **unrelated** prior Parameter Range Master edits from earlier tasks. Those were **not** introduced by this read-only investigation.

---

## Architectural Assessment Answers

1. **Is Diagnostic format configuration centralized?** No — layout is hardcoded in the SPA; branding keys are a small config island.  
2. **Is Radiology configuration centralized?** No — hardcoded SPA layout.  
3. **Are they independent?** Yes for managers/DTOs/content; similar print *pattern*, separate templates.  
4. **Are templates reusable?** Not as configurable assets; code templates only.  
5. **Are layouts hardcoded?** Yes.  
6. **Config in DB or code?** Layout in **code**; branding strings in **Web.config** (+ ClientApplications fallback); clinical content in **DB**.  
7. **Can Admin change format without deployment?** **No** (layout). Branding strings require Web.config / app settings change (and still may not appear on print).  
8. **Different department formats?** **Not supported.**  
9. **Different test formats?** **Not supported.**  
10. **Safest future extension point?** Template-key resolution at print API boundary + pluggable Angular/CSS themes or stored HTML templates, leaving evaluation/workflow untouched.
