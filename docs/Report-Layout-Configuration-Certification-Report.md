# Report Layout Configuration — Certification Report

**Module:** Setup → Report Layout Configuration  
**Scope:** Physical print layout only (stationery clearance, margins, doctor signature placement/size) for Diagnostic and Radiology reports  
**Date:** 2026-09-11  
**Environment:** ZoryaLMS / AVILIS (`localhost:8081` API, `localhost:8080` Portal)

---

## 1. Executive Summary

Introduced an Administrator-only **Report Layout Configuration** framework that stores physical print-layout parameters in millimetres and applies them at print time via the existing Angular HTML/CSS + iframe print path.

- No new reporting engine, no server-side PDF, no template designer.
- Report content, business rules, approval gates, comments, and signature **sources** unchanged.
- Pre-printed stationery remains clearance-only (no logos/branding rendered).
- Technician signature fields are reserved but forced disabled — no technician signature source exists in product.
- Defaults reproduce prior production clearances (Diagnostic 50/50 mm; Radiology 40/50 mm).
- Configuration load failure does not block printing (client falls back to hardcoded defaults).

**Final Verdict: APPROVED FOR PROD**

---

## 2. Existing Report Architecture

| Aspect | Diagnostic | Radiology |
|--------|------------|-----------|
| Renderer | Angular template + injected print CSS | Same |
| Print | Browser iframe print | Same |
| Stationery | `@page` margins as clearance | Fixed header/footer bands + body padding |
| Prior header clearance | ~50 mm (5 cm) | ~40 mm (4 cm) |
| Prior footer clearance | ~50 mm | ~50 mm footer gap |
| Side margins | 12 mm | 10 mm |
| Doctor signature | In-flow, right, max 50×14 mm | Fixed footer, left, max 50×14 mm |

Prior analysis: `docs/Diagnostic-Radiology-Report-Format-Configuration-Analysis.md`.

---

## 3. Report Layout Configuration Architecture

```text
Admin UI (Setup → Report Layout Configuration)
        ↓
API ReportLayoutConfiguration (RBAC module)
        ↓
ReportLayoutConfiguration table (one active row per ReportType)
        ↓
OperationalReportsController enriches Layout on Diagnostic/Radiology DTOs
        ↓
Angular print components map Layout → CSS custom properties / @page / signature styles
        ↓
Existing iframe print (unchanged mechanism)
```

Failure path: enrich sets `Layout = null` → Angular `resolvePrintLayout()` uses production defaults → print continues.

---

## 4. Configuration Fields

| Field | Unit | Notes |
|-------|------|-------|
| ReportType | enum | Diagnostic \| Radiology |
| PageSize | text | Fixed A4 |
| Orientation | text | Fixed Portrait |
| HeaderHeightMm | mm | Pre-printed header clearance |
| FooterHeightMm | mm | Pre-printed footer clearance |
| LeftMarginMm / RightMarginMm | mm | Content side margins |
| DoctorSignatureEnabled | bool | Show/hide placement only |
| DoctorSignatureHorizontal | Left/Center/Right | |
| DoctorSignatureVertical | Bottom | Fixed for Phase 1 |
| DoctorSignatureWidthMm / HeightMm | mm | Image max box |
| TechnicianSignature* | reserved | Always disabled in Phase 1 |

Content Top/Bottom Offset fields were **not** added separately — they would duplicate clearance/margin systems already mapped from existing CSS.

---

## 5. Database Design

Additive table only: `dbo.ReportLayoutConfiguration`

- Unique index on `ReportType`
- Audit: `CreatedOn`, `CreatedBy`, `ModifiedOn`, `ModifiedBy`
- Seed script: `Scripts/add-report-layout-configuration.sql`
- Also seeds `UserModules` / `RoleModuleMappings` for Administrator

No clinical/report content tables modified.

---

## 6. API Design

| Method | Route | Access |
|--------|-------|--------|
| GET | `api/ReportLayoutConfiguration/{reportType}` | Module view |
| GET | `api/ReportLayoutConfiguration/defaults/{reportType}` | Module view |
| POST | `api/ReportLayoutConfiguration` | CanEdit |
| POST | `api/ReportLayoutConfiguration/reset/{reportType}` | CanEdit |

Print consumers do not call this API directly; layout is embedded on report DTOs from `OperationalReportsController`.

---

## 7. Administrator UI

- Route: `/report-layout-configuration`
- Menu: Setup → Report Layout Configuration (`SETUP_REPORT_LAYOUT_CONFIGURATION`)
- Report type selector, stationery clearance, margins, doctor signature controls
- Technician section present but disabled with explanatory note
- Simple zone preview (layout zones only — not a second report renderer)
- Save / Reset to Default

---

## 8. Diagnostic Integration

`test-report.component.ts` → `getDiagnosticPrintStyles()`:

- Reads `report.layout` (or defaults)
- Applies `@page` margin-top/bottom/left/right from clearance/margins
- Applies doctor signature display, horizontal alignment, and max width/height

Content/approval/comment logic untouched.

---

## 9. Radiology Integration

`radiology-report-print.component.ts` → `getPrintStyles()`:

- Maps header clearance → `--rad-header-total`
- Maps footer clearance → `--rad-footer-gap`
- Maps side margins and signature align/size
- Preserves fixed-band print model (`@page { margin: 0 }`)

Narrative DTO fields unchanged.

---

## 10. Default Layout Mapping

| Setting | Diagnostic | Radiology |
|---------|------------|-----------|
| Header clearance | 50 mm | 40 mm |
| Footer clearance | 50 mm | 50 mm |
| Left/Right margin | 12 / 12 mm | 10 / 10 mm |
| Doctor signature | Right, 50×14 mm | Left, 50×14 mm |
| Technician signature | Disabled | Disabled |

Post-deploy DB confirmation: both rows match the table above.

---

## 11. Signature Configuration

- **Doctor:** position (horizontal) and size configurable; identity/qualification/image path/approval logic unchanged (`DoctorSignaturePath` / report enrichers unchanged).
- **Technician:** investigated — no technician signature image/path/profile mechanism exists. Fields stored disabled; UI disabled; manager forces `TechnicianSignatureEnabled = false`. No technician approval workflow changes.

---

## 12. Header/Footer Clearance

Configuration controls reserved vertical space for pre-printed stationery only. No logos, addresses, or footer graphics were added.

---

## 13. Preview Implementation

Admin screen includes a lightweight zone sketch (header / content / footer / signature markers). Not a duplicate report renderer. Full live print preview of clinical content was not required and was not built.

---

## 14. Security / RBAC

- Module: `ReportLayoutConfiguration`
- Administrator role mapping via SQL seed + MenuCatalog / role-permission-catalog
- Write endpoints require `CanEdit`
- Existing report print permissions unchanged
- No new auth mechanism

---

## 15. Validation

Server-side:

- Clearance 0–120 mm
- Margins 0–50 mm
- Signature size 5–80 mm when doctor signature enabled
- Header + Footer must leave ≥ 80 mm content height on A4
- Left + Right must leave ≥ 60 mm content width
- PageSize/Orientation forced to A4/Portrait
- Technician always disabled

Client-side form validators align with these ranges.

---

## 16. Unit Test Results

`LIS.Masters.Tests.Reports.ReportLayoutConfigurationManagerTests` — **11/11 passed**

- Defaults Diagnostic / Radiology
- GetByReportType
- Save/Update/Reset Diagnostic and Radiology
- Reject clearance above max
- Reject insufficient content height/width
- Reject negative margin
- Reject invalid signature size
- Reject unknown report type
- Technician forced off on save

---

## 17. Integration Test Results

- SQL migration applied successfully on `ZoryaLMS`
- Module `ReportLayoutConfiguration` present
- Administrator role mapping present
- Layout enrichment path compiled into `OperationalReportsController` with try/catch fail-open

Manual browser print of clinical samples was not exhaustively automated; defaults match prior CSS constants and DB seed.

---

## 18. Print Test Results

| Check | Result |
|-------|--------|
| Defaults match prior CSS constants | Pass (code + DB) |
| Diagnostic CSS uses runtime layout / fallback | Pass |
| Radiology CSS uses runtime layout / fallback | Pass |
| Config null → client defaults | Pass (code path) |
| No second PDF/report engine | Pass |

Physical multi-page printer regression on letterhead should be spot-checked by lab staff after first stationery change.

---

## 19. Regression Results

Unchanged by design:

- Patient/invoice/visit data
- Parameters, results, units, ranges, flags
- Comments/Notes retrieval and display logic
- Doctor/Radiology approval gates and eligibility
- Signature image source
- Reference-range evaluation
- Notification, dashboard, barcode, masters unrelated to layout

---

## 20. Build Results

| Build | Result |
|-------|--------|
| API Release (`Lis.Api.csproj`) | Pass |
| Angular production (`ng build --configuration=production`) | Pass (existing budget warnings only) |
| Masters tests project | Pass |

---

## 21. Deployment Results

| Step | Result |
|------|--------|
| SQL `Scripts/add-report-layout-configuration.sql` | Applied |
| API deploy → `I:\Projects\PROD\AVILIS\API` | Success; site Started |
| Portal deploy → `I:\Projects\PROD\AVILIS\PORTAL` | Success |
| `http://localhost:8081/` | HTTP 200 |
| `http://localhost:8080/` | HTTP 200 |
| DB defaults after tests | Diagnostic 50/50; Radiology 40/50 |

---

## 22. Files Modified / Added

**Backend**

- `LIS.DtoModel/Models/Reports/ReportLayoutConfiguration.cs`
- `LIS.DtoModel/Models/Reports/ReportLayoutConfigurationDto.cs`
- `LIS.DtoModel/Interfaces/IReportLayoutConfigurationManager.cs`
- `LIS.DtoModel/Models/Reports/DiagnosticTestReportDto.cs` (additive `Layout`)
- `LIS.DtoModel/Models/Reports/DiagnosticRadiologyReportDto.cs` (additive `Layout`)
- `LIS.Businesslogic/ReportLayoutConfigurationManager.cs`
- `LIS.DataModel/DAL/ApplicationDBContext.cs`
- `web/Lis.Api/Controllers/Api/ReportLayoutConfigurationController.cs`
- `web/Lis.Api/Controllers/Api/OperationalReportsController.cs`
- `web/Lis.Api/App_Start/SimpleInjectorConfig.cs`
- `web/Lis.Api/Providers/MenuCatalog.cs`
- Project file includes (DtoModel, BusinessLogic, Lis.Api, Masters.Tests)

**Frontend**

- `web/Lis.Web/src/app/setup/report-layout-configuration/*`
- `web/Lis.Web/src/app/_services/report-layout-configuration.service.ts`
- `web/Lis.Web/src/app/reports/test-report/test-report.component.ts`
- `web/Lis.Web/src/app/reports/radiology-report-print/radiology-report-print.component.ts`
- Routing, module, left-nav, permission catalog/util

**Scripts / Tests / Docs**

- `Scripts/add-report-layout-configuration.sql`
- `LIS.Masters.Tests/Reports/ReportLayoutConfigurationManagerTests.cs`
- `docs/Report-Layout-Configuration-Certification-Report.md` (this file)

---

## 23. Database Changes

- **Added:** `dbo.ReportLayoutConfiguration` (+ unique index)
- **Seeded:** Diagnostic + Radiology default rows
- **RBAC:** `UserModules` / `RoleModuleMappings` for Administrator
- **Not modified:** clinical, result, parameter, test, invoice, radiology narrative tables

---

## 24. Known Limitations

1. Phase 1: one active layout per report type only (no per-centre/test/department).
2. Technician signature UI reserved but non-functional until a signature source exists.
3. No advanced X/Y millimetre coordinate editor (horizontal Left/Center/Right + size sufficient for current renderers).
4. Layout preview is schematic, not a full clinical print preview.
5. Exact pixel-perfect printer proof on physical letterhead remains an operational check.

---

## 25. Production Readiness Checklist

- [x] Administrator-only Report Layout Configuration
- [x] Diagnostic configuration + print wiring
- [x] Radiology configuration + print wiring
- [x] Default layout preserves prior production constants
- [x] Header/footer clearance configurable (no new stationery content)
- [x] Doctor signature position/size configurable
- [x] Technician signature not invented; fields disabled
- [x] Page A4 Portrait only
- [x] Report content/business logic unchanged
- [x] Comment/Note logic unchanged
- [x] Config failure fails open for print
- [x] RBAC via existing module permissions
- [x] Automated manager tests pass (11/11)
- [x] API Release build pass
- [x] Angular production build pass
- [x] IIS deploy + HTTP 200 API/Portal

---

## 26. Final Verdict

**APPROVED FOR PROD**

Report Layout Configuration is deployed as a layout-only control plane over the existing Angular + iframe print architecture, with production-equivalent defaults and safe print fallback.
