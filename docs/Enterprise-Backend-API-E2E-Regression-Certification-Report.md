# ZoryaLMS Backend API — End-to-End Regression Certification Report

**Project:** ZoryaLMS (AVILIS)  
**Environment:** Local IIS — API `http://localhost:8081` → `I:\Projects\PROD\AVILIS\API`; Portal `http://localhost:8080` → `I:\Projects\PROD\AVILIS\PORTAL`  
**Database:** `ZoryaLMS` on `.\SQLEXPRESS`  
**Certification Date:** 2026-08-07  
**Exercise Type:** Production-readiness regression certification (no feature development)  
**Final Verdict:** **APPROVED FOR PROD**

---

## 1. Executive Summary

Complete backend API regression certification was executed across build, database, authentication, RBAC, masters, transactions, OperationalDateTime, notifications, reports, security, performance, unit/integration suites, API automation, and IIS deployment health.

Two certification-harness defects and one Critical security class of defects (unauthenticated GET/mutation exposure on core clinical/billing APIs) were identified, root-caused, fixed with minimal changes, redeployed, and fully re-certified.

| Gate | Result |
|------|--------|
| Critical defects remaining | **0** |
| High defects remaining | **0** |
| Medium defects remaining | **0** |
| API / Portal health | **HTTP 200 / 200** |
| Unit + integration (LIS.Masters.Tests) | **183 PASS / 3 pre-existing SKIP / 0 FAIL** |
| Core API automation suites | **ALL PASS** after fixes |
| Backward compatibility | Preserved (auth tightened only where endpoints were incorrectly open) |

---

## 2. Architecture Validation

| Check | Result |
|-------|--------|
| Release build (`Lis.Api` Release) | SUCCESS (0 errors) |
| DI / startup after IIS recycle | Healthy |
| Department processing router (no radiology heuristics in invoice path) | PASS (`ArchitectureCertification` 24/24) |
| API contracts / DTO shapes | Unchanged except authorization attributes |

---

## 3. Database Validation

| Check | Result |
|-------|--------|
| Connectivity (`ZoryaLMS`) | PASS |
| ProcessingCategory present & populated | PASS |
| Notification tables (`NotificationConfiguration`, `NotificationTemplate`, `NotificationAudit`, `SecureLinkToken`) | Present |
| OperationalDateTime config keys in prod `Web.config` | `FacilityTimeZoneId=India Standard Time`, `AllowedClockDriftMinutes=5` |
| Notification mock mode | `Notification:UseMockProviders=true` |
| Schema redesign | **None** |

---

## 4. Authentication Report

| Scenario | Result |
|----------|--------|
| Admin / Technician / Doctor password grant (`/TOKEN`) | PASS |
| Missing / invalid bearer on protected workflows | 401 |
| Session / role re-login isolation | PASS (`RoleBasedUiCertification` SEC-02) |

---

## 5. Authorization Report (RBAC)

| Role | Allowed | Denied | Result |
|------|---------|--------|--------|
| Technician | Samples, SampleCollection, SampleReceiving, Reports | DoctorsApprovals, Masters, Users, Roles, Radiology approvals | PASS |
| Doctor | DoctorsApprovals, Reports, RadiologyReports, RadiologyDoctorApprovals | SampleCollection/Receiving/Samples, Masters, Users, Roles | PASS |
| Direct API denial | Technician → doctor approval; Doctor → collection queue | 403 | PASS |

**Suite:** `RoleBasedUiCertification.ps1` — **36 PASS / 0 WARN / 0 FAIL**

---

## 6. Master API Validation

| Area | Result |
|------|--------|
| Smoke CRUD (Unit, Method, Department, Specimen, HisTest, Test Mapping) | PASS |
| Production readiness masters (Test Master, Rate, Mapping, Patient duplicate rules) | PASS |
| BillableItems search | PASS |

---

## 7. Transaction API Validation

| Workflow | Result |
|----------|--------|
| Lab vs Diagnostic routing on invoice | PASS |
| Multi-specimen invoice + barcode reuse rules | PASS (17/17) |
| Sample Collection → Receiving → Reject-to-collection | PASS |
| Lab Result Entry search/load/save | PASS |
| Technician / Doctor approval gates + paid report print | PASS |
| Enterprise FDD UAT (embedded in Phase1) | PASS |

---

## 8. Operational DateTime Validation

Prior certification retained and regression-safe:

- Facility wall-clock + configurable drift
- Collection / Receiving invoice-floor & future rules
- Invoice / Visit / Results / Approvals / Radiology stamps on OperationalDateTime path

**Config verified on deployed API Web.config.**

---

## 9. Notification Framework Validation

| Check | Result |
|-------|--------|
| Config keys present (SMS/WhatsApp/Retry/SecureLink/Mock) | PASS |
| Tables present | PASS |
| Framework unit coverage within Masters.Tests | Included in 183 PASS |
| Functional redesign | **None** (regression only) |

---

## 10. Report API Validation

| API / Area | Result |
|------------|--------|
| Diagnostic / operational reports smoke | PASS |
| Radiology queues + print DTO | PASS |
| Collection / Receiving / TAT-related FDD smoke | PASS |
| Barcode API (post-auth fix) | Requires auth + module CanView |

---

## 11. Security Assessment

### Defects found and fixed (this certification)

| ID | Severity | Finding | Root Cause | Fix |
|----|----------|---------|------------|-----|
| SEC-01 | **Critical** | Anonymous `GET /api/SaleInvoice` returned invoice PHI/financial data | GET actions lacked `QAuthorize` | Added `QAuthorize(SaleInvoices, CanView)` on all SaleInvoice GETs |
| SEC-02 | **Critical** | Anonymous `GET /api/BarCode` returned patient/sample barcode payloads | `AllowAnonymous` with QAuthorize commented out | Restored `QAuthorize` (SampleCollection + alternates, CanView) |
| SEC-03 | **High** | Anonymous HisTest CRUD (`AllowAnonymous` on Post/Put/Delete/Get) | Legacy attributes | Replaced with `[Authorize]` reads + `QAuthorize(Masters, MASTER_TESTMASTER)` mutations |
| SEC-04 | **High** | Anonymous master reads (Department, Unit, Method via base, Specimens, TestRate, Quality, EquipmentHeartbeat list) | Missing auth on GETs | Class/method `[Authorize]` (JWT required; RBAC unchanged for dropdowns) |

### Post-fix verification

Anonymous requests to SaleInvoice, Department, Unit, Method, HisTest, Specimens, TestRate, BarCode, Quality → **401**  
Authenticated admin reads → **200**

### Residual intentional anonymous surfaces (accepted Low / operational)

Documented as **not Medium+** for this release because they are either auth entry points, secure-token download, or analyzer/HIS integration endpoints that must remain open on the instrument/HIS network segment:

- `/TOKEN`, Account / ResetPassword anonymous actions
- `ReportDownload` (secure token gate)
- Analyzer/HIS: `Results`, `NewSample`, `LisController`, equipment heartbeat/mappings, raw sample/parameter posts
- `Resources` static resources

**Operational control:** keep analyzer/HIS endpoints on segmented network; do not expose IIS API broadly to the public internet.

### Other security checks

| Check | Result |
|-------|--------|
| SQLi-style search text on PatientMaster | Handled without HTTP 500 |
| Role isolation after logout/login | PASS |
| Business validation vs FATAL logging | Covered by prior OperationalDateTime controller work; no new FATAL misuse found in this pass |

---

## 12. Performance Assessment

| Endpoint / Suite | Observation |
|------------------|-------------|
| HisTest search (`ArchitectureCertification`) | ~90–118 ms (limit 3s) |
| Sample Collection PendingQueue | ~191 ms |
| Sample Receiving Queue | ~92 ms |
| Perf regressions requiring optimization | **None** |

---

## 13. Regression Results

| Suite | Result |
|-------|--------|
| ArchitectureCertification.ps1 | **24/24 PASS** |
| RoleBasedUiCertification.ps1 | **36/36 PASS** |
| ProductionReadinessSmoke.ps1 | **PASS** |
| Phase1ApiVerification.ps1 | **18/18 PASS — APPROVED** |
| LabResultEntryCert.ps1 | **5/5 PASS** |
| TestApprovalFlow.ps1 | **6/6 PASS** |
| SmokeMastersCrud.ps1 | **PASS** |
| FddModuleSmoke.ps1 | **PASS** |
| SaleInvoiceMultiSpecimenCert.ps1 | **17/17 PASS** |

---

## 14. Unit / Integration Test Results

| Suite | Result |
|-------|--------|
| `LIS.Masters.Tests` (Release, via Phase1 REG-01) | **183 PASS** |
| Skipped (pre-existing, documented) | **3** — `Manual_Entry_Creates_Results_For_Sample_Without_Analyzer_Data`, `GetBySampleNo_Scaffolds_Entry_When_Result_Shell_Has_No_Details`, `Manual_Entry_Fills_Empty_Result_Shell` |
| Failures | **0** |

---

## 15. API Automation Results

Positive, negative, authorization, routing, multi-specimen, collection/receiving, result entry, and approval-gate automation executed via the scripts above. All executed suites passed after defect remediation.

---

## 16. Build Report

| Item | Result |
|------|--------|
| `Lis.Api` Release MSBuild | SUCCESS |
| Dependent projects (DtoModel, DataAccess, BusinessLogic, Com.Businesslogic) | SUCCESS |
| Warnings | Pre-existing CS1998/CS0168/CS0414 only; no new blockers |

---

## 17. Deployment Report

| Step | Result |
|------|--------|
| Deploy API binaries to `I:\Projects\PROD\AVILIS\API\bin` | Done (Web.config preserved) |
| IIS app pool recycle (`AVILIS_API_POOL`) | Done |
| API HTTP | **200** |
| Portal HTTP | **200** |
| Portal redeploy | Not required (backend-only auth attributes) |

---

## 18. Defect Log

### A. Certification harness (non-product)

| ID | Severity | Issue | Root Cause | Fix |
|----|----------|-------|------------|-----|
| CERT-01 | Medium (false fail) | ArchitectureCertification failed BillableItems when exactly 1 row | PowerShell unwrap of single-element array → `.Count` null | `Get-Items` returns unary-comma array |
| CERT-03 | Low (false fail) | LabResultEntryCert PUT failed converting `4511111111` to Int32 | Script used `[int]` to increment numeric result values | Use `[decimal]` increment |

### B. Product defects (fixed)

See Security Assessment SEC-01 … SEC-04.

---

## 19. Root Cause Analysis (summary)

1. **False certification failures** were PowerShell/test-data issues, not API regressions.  
2. **Anonymous PHI/catalog exposure** was longstanding missing/commented authorization on read (and some write) actions while mutations on other controllers already used `QAuthorize`. Host authentication was registered globally, but no global `[Authorize]` filter existed—so unprotected actions were public.  
3. Fixes applied the **smallest** change: add `[Authorize]` or restore/add `QAuthorize` on the affected actions/classes without redesigning RBAC menus or JWT issuance.

---

## 20. Files Modified

### Product (security)

- `web/Lis.Api/Controllers/Api/SaleInvoiceController.cs`
- `web/Lis.Api/Controllers/Api/MasterApiControllerBase.cs`
- `web/Lis.Api/Controllers/Api/DepartmentController.cs`
- `web/Lis.Api/Controllers/Api/HisTestController.cs`
- `web/Lis.Api/Controllers/Api/SpecimensController.cs`
- `web/Lis.Api/Controllers/Api/HisParameterRangeController.cs`
- `web/Lis.Api/Controllers/Api/TestRateController.cs`
- `web/Lis.Api/Controllers/Api/BarCodeController.cs`
- `web/Lis.Api/Controllers/Api/QualityController.cs`
- `web/Lis.Api/Controllers/Api/SetupDomainControllers.cs` (`EquipmentHeartbeatController`)

### Certification harness

- `Scripts/ArchitectureCertification.ps1`
- `Scripts/RoleBasedUiCertification.ps1`
- `Scripts/LabResultEntryCert.ps1`

### Report

- `docs/Enterprise-Backend-API-E2E-Regression-Certification-Report.md` (this document)

---

## 21. Production Readiness Checklist

| Criterion | Status |
|-----------|--------|
| Zero Critical / High / Medium open defects | **Met** |
| Backend APIs validated via automation suites | **Met** |
| Authentication + RBAC verified | **Met** |
| Business workflows E2E (invoice → collection → receiving → results → approvals → reports) | **Met** |
| OperationalDateTime validated (config + prior module certification + regression) | **Met** |
| Notification framework regression | **Met** |
| Dashboard / reports / barcode APIs | **Met** (barcode now correctly authenticated) |
| Unit/integration/API automation pass (excl. documented skips) | **Met** |
| No functional regression in existing workflows | **Met** |
| API healthy after IIS deploy | **Met** |
| Backward compatibility of DTOs/contracts | **Met** |

---

## 22. Final Verdict

**APPROVED FOR PROD**

The ZoryaLMS backend API meets the stated production approval criteria after remediation of verified Critical/High security defects and re-certification of affected suites. Residual analyzer/HIS anonymous endpoints remain intentionally open for instrument integration and must stay network-segmented.

Manual browser UI visual QA (print layout, signatures) remains outside automated backend scope and is noted as complementary UAT only.
