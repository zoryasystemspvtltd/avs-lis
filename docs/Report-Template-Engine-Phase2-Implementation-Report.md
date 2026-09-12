# Report Template Engine — Phase 2 Implementation Report

**Project:** ZoryaLMS / AVILIS  
**Date:** 2026-09-12  
**Phase:** 2 — Component model + Declarative renderer + Admin preview viewer  
**Baseline:** `docs/Report-Designer-Current-State-Discovery.md`, `docs/Report-Template-Engine-Phase1-Implementation-Report.md`

---

## 1. Architecture

```
Existing Report DTO (DiagnosticTestReportDto / DiagnosticRadiologyReportDto)
        +
Published/Draft Template Definition JSON (validated)
        ↓
DeclarativeReportRenderer (IReportRenderer)
        ↓
Safe HTML + CSS
        ↓
Admin Report Template Viewer (preview only)

Production print path (UNCHANGED):
DTO → Angular hard-coded HTML → iframe/window.print()
```

**Safety switch:** `ReportTemplateEngineFeatureFlags.UseDeclarativeRendererForProductionPrint = false`  
Production print is **not** wired to the declarative renderer.

---

## 2. Component model

Canonical types (`ReportTemplateComponentTypes`):

| Type | Diagnostic | Radiology |
|------|------------|-----------|
| TEXT | ✓ | ✓ |
| PATIENT_FIELD | ✓ | ✓ |
| ORDER_FIELD | ✓ | |
| INVOICE_FIELD | ✓ | ✓ |
| VISIT_FIELD | ✓ | ✓ |
| TEST_FIELD | ✓ | ✓ |
| SAMPLE_FIELD | ✓ | |
| PARAMETER_TABLE | ✓ | |
| DOCTOR_FIELD | ✓ | ✓ |
| TECHNICIAN_FIELD | ✓ | |
| SIGNATURE | ✓ | ✓ |
| IMAGE | ✓ | ✓ |
| LINE / SECTION / SPACER | ✓ | ✓ |
| ACCESSION_FIELD | | ✓ |
| RADIOLOGY_FIELD | | ✓ |

Registry: `ReportComponentRegistry` (palette contract for Phase 3 designer).

---

## 3. JSON schema

- `schemaVersion`: **1** (extended in-place; no DB migration)
- `reportType`: Diagnostic | Radiology
- `renderer`: `builtin` | `declarative`
- `layoutSource`: `ReportLayoutConfiguration` (global stationery clearance remains authoritative)
- `page`: size / orientation / `useGlobalLayoutClearance`
- `body` (preferred) or `components[]`: tree of typed components
- Component props: `type`, `id`, `label`, `binding`, `style`, `visibleWhen`, `repeat`, `children`, table `columns`
- **No arbitrary HTML blobs**, no PHI field payloads, no SQL/C#/JS

---

## 4. Binding model

Whitelisted roots (Phase 1 + repeat aliases):

`Patient`, `Order`, `Invoice`, `Visit`, `Test`, `Parameter`, `Doctor`, `Technician`, `Accession`, `Radiology`, `Section`, `Header`, `Layout`, `Branding`, plus aliases `dept` / `section` / `group` / `item` / `row`.

Resolution is against the **already assembled DTO** via `ReportTemplateBindingContext` (dictionary + whitelisted property reads). No SQL, no expression evaluation, no HTTP per field.

---

## 5. Diagnostic schema

Compatibility definition (`ReportTemplateCompatibilityDefinitions.BuildDiagnosticConceptual`):

- Patient / order / invoice / visit panel
- Repeat `DepartmentGroups` → sections
- `PARAMETER_TABLE` with columns Parameter / Result(+flag) / Unit / ReferenceRange
- Conditional Comment + DoctorApprovalComment
- Technician + Doctor signatures (images from DTO; geometry from layout config)

---

## 6. Radiology schema

Compatibility definition (`BuildRadiologyConceptual`):

- Patient / accession / invoice / visit / test / modality / department
- Clinical History, Findings, Impression
- Conditional Recommendation (`NotEmpty`)
- Doctor/authorizer signature

**No radiology image series** — current DTO has no image collection.

---

## 7. Renderer architecture

Class: `DeclarativeReportRenderer` : `IReportRenderer`

**Positioning decision (documented):** **Section/block document flow** (hybrid with CSS alignment for signatures).  

Why not absolute canvas coordinates as primary model:
- Healthcare A4 print needs predictable page breaks and table flow
- Absolute designer coords often break under multi-page parameter tables
- Signature left/right/center maps cleanly to existing `ReportLayoutConfiguration` horizontal/vertical hints

Flow:
1. Validate definition  
2. If `renderer=builtin` → return without HTML (points at Angular)  
3. Bind DTO context  
4. Render components / repeats / conditions  
5. Emit HTML-encoded values + controlled CSS from layout clearance  

---

## 8. Viewer architecture

- Angular: `ReportTemplateViewerComponent` (`app-report-template-viewer`)
- Hosted on Admin Setup → Report Template Configuration
- Zoom, A4 page frame, shows renderer CSS + HTML
- **Not** duplicated into production print iframe path

---

## 9. Security

- Admin module `ReportTemplateConfiguration` + `QAuthorize` on all preview APIs
- Preview uses **controlled sample data only** (no real PHI endpoints)
- Validator rejects: script/iframe/object/embed, `javascript:`, event handlers, SQL-ish tokens, unknown bindings/components, unsafe styles/`url()`, arbitrary HTML, excessive nesting
- Signature images: `data:image/...;base64` or relative `uploads/` only (no remote http)

---

## 10. Validation

Extended `ReportTemplateDefinitionValidator`:

- Component type + report-type support  
- Repeat sources whitelist  
- Condition ops: `Exists`, `NotEmpty`, `Equals`, `NotEquals`  
- Dimensions / nesting depth / component count caps  
- Style key allow-list  

---

## 11. Assignment integration

Phase 1 resolver unchanged:

Test → Department → System → IsSystemDefault  

Resolve returns **Published** definition only. Draft is Admin-previewable, not production-resolved.

---

## 12. ReportLayoutConfiguration integration

- Remains **global per ReportType**
- Renderer reads clearance / signature geometry from DTO.Layout (or sample defaults)
- Template `page.useGlobalLayoutClearance=true`; does **not** duplicate HeaderHeightMm etc. into new tables

---

## 13. Signature integration

- Template specifies show/position/size intent via SIGNATURE component + layout config
- Images come from existing DTO fields (`ApprovedBySignatureImage`, `ReviewedBySignatureImage`, `AuthorizedBySignatureImage`)
- **No** new signature storage

---

## 14. Sample data

`ReportTemplateSampleDataFactory`:

- Synthetic Diagnostic + Radiology DTOs  
- Tiny transparent PNG data-URI for signatures  
- Clearly labeled SAMPLE-* identifiers  

---

## 15. Tests

`ReportTemplateEnginePhase2Tests` — validation, bindings/encoding, diagnostic/radiology sample render, repeats, conditions, registry, draft preview, archive blocked, feature flag off.

Regression executed with Phase 2 build:

| Suite | Result |
|-------|--------|
| ReportTemplateEnginePhase2Tests | PASS |
| ReportTemplateEnginePhase1Tests | PASS |
| ReportTemplateConfigurationRbacTests | PASS |
| DiagnosticReportPrintSelectionTests | PASS (prior run) |
| ReportLayoutConfigurationManagerTests | PASS (prior run) |

---

## 16. Compatibility results

| Item | Status |
|------|--------|
| Conceptual Diagnostic structure via new renderer | Yes (sample) |
| Conceptual Radiology structure via new renderer | Yes (sample) |
| Pixel parity with Angular HTML | **Not claimed** |
| Production print output | **Unchanged** |
| Differences | New HTML uses `rte-*` classes; patient panel is stacked labeled fields (not identical 2-column table CSS); pagination is CSS `page-break` heuristics only |

---

## 17. Performance observations

- Single DTO in memory; no per-component DB/HTTP  
- Binding context cloned per repeat item (shallow scope dictionary)  
- Suitable for Admin preview; production path not activated  

---

## 18. Limitations (Phase 2)

1. No drag-and-drop designer / canvas / palette UI  
2. No production print switch-over  
3. Not pixel-identical to current Angular reports  
4. Pagination is CSS-only (not a full page-break engine)  
5. No server PDF / snapshots  
6. No template import/export  
7. No radiology image layout  
8. Preview = sample data only (by design)  
9. `IReportViewer` implemented for Admin preview; not a clinical workstation viewer  

---

## 19. Phase 3+ (deferred)

| Phase | Scope |
|-------|--------|
| **3** | Visual designer: palette, canvas, property inspector, snap/align, undo/redo, assignment UI |
| **4** | Controlled production rollout behind feature flag + fallback to built-in |
| **5** | Server PDF / secure download (if approved) |
| **6** | Historical snapshot / reproducibility (if approved) |
| **7** | Import/export / template packs (if approved) |

---

## 20. API additions (Admin only)

| Method | Route |
|--------|-------|
| GET | `/api/ReportTemplateConfiguration/components` |
| POST | `/api/ReportTemplateConfiguration/preview/sample` |
| POST | `/api/ReportTemplateConfiguration/preview/version/{versionId}` |
| POST | `/api/ReportTemplateConfiguration/preview/definition` |

All require `ReportTemplateConfiguration` permission. No PHI payload accepted.

---

## 21. Database

**No new tables/columns.** DefinitionJson carries the Phase 2 component tree.

---

## 22. Build / deployment

- Backend `LIS.Masters.Tests` Release build: **SUCCESS**
- Phase 1+2 template tests: **PASS** (35/35 for Phase1+Phase2+RBAC catalog)
- Angular production build: **SUCCESS** (with `NODE_OPTIONS=--openssl-legacy-provider` on Node 24; Angular 10 / webpack OpenSSL constraint)
- **Not deployed** to IIS/production (explicit Phase 2 rule)

---

## 23. Production safety verification

| Gate | Status |
|------|--------|
| Existing report Angular components untouched | Yes |
| Payment gate untouched | Yes |
| Approval gate untouched | Yes |
| Print All / Individual Print untouched | Yes |
| Signature storage untouched | Yes |
| ReportLayoutConfiguration functional | Yes (regression PASS) |
| Feature flag off | Yes |
| Built-in fallback remains production path | Yes |

---

## Files added / touched

**Added:** `ReportTemplateEnginePhase2.cs`, `ReportComponentRegistry.cs`, `ReportTemplateSampleDataFactory.cs`, `ReportTemplateCompatibilityDefinitions.cs`, `ReportTemplateBindingContext.cs`, `DeclarativeReportRenderer.cs`, Phase2 tests, Angular viewer, this report.

**Extended:** validator, `ReportTemplateManager` (preview + registry), API controller, Angular setup page/service, DI registrations, csproj Compile lists.

**Not touched:** `test-report.component.*`, `radiology-report-print.component.*`, payment/approval/result workflows, signature storage, notification download.

---

*Phase 2 complete. STOP — do not implement the visual designer or production print switch without a separate Phase 3 / Phase 4 prompt.*
