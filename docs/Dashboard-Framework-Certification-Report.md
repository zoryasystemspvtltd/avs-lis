# Dashboard Framework — Final Certification Report
### ZoryaLMS (Zorya Laboratory Management System) — avs-lis

**Phase:** Design & Implementation (production build)
**Date:** 26 Jul 2026
**Predecessor:** `docs/Dashboard-Business-Analysis-Report.md`
**Status:** Implemented, built, deployed to production and smoke-tested. See §11.

---

## 1. Requirement Coverage

| # | Requirement | Status | Evidence |
|---|-------------|--------|----------|
| 1 | One Dashboard Framework, no per-role dashboards | Met | `dashboard.component.ts` + `widget-registry.ts` — single host, single registry |
| 2 | Widgets render dynamically from RBAC | Met | `resolveVisibleWidgets(user)` filters by module bitmask |
| 3 | No hardcoded role names | Met | Registry declares **modules**, never roles; `isAdministrator()` is the existing bypass |
| 4 | Administrator sees all widgets | Met | Existing `isAdministrator()` short-circuit in `hasAnyModuleAccess` |
| 5 | Future roles work without redesign | Met | Grant a module → its widgets appear; zero code change |
| 6 | Widget owns UI/API/loading/error/empty/permission/refresh | Met | Each widget component implements all six states |
| 7 | One widget failure cannot affect others | Met | Per-widget `subscribe(next, error)`; server returns empty set on manager exception |
| 8 | Widget Registry (name, component, order, permission, API, refresh, enabled) | Met | `DashboardWidgetDef` in `dashboard.model.ts` |
| 9 | No DB tables, no personalization UI | Met | Registry is a TypeScript constant |
| 10 | Layout: Welcome, KPI, Charts, Pending Queue, Recent Activity, Alerts | Met | All six present, ordered via `order` |
| 11 | Landing page only | Met | Only `home.component.*` changed in the UI |
| 12 | Reuse before build | Met | See §5 |
| 13 | Remove unnecessary anonymous access | Met | `DailyStatus` `[AllowAnonymous]` removed — verified 401 |
| 14 | Hidden widgets not reachable via UI manipulation | Met | Every endpoint carries `[QAuthorize]` matching its widget's module |
| 15 | Parallel, non-blocking widget loading | Met | Independent HTTP calls per widget |
| 16 | V1 excludes drill-down/personalization/designer/analytics | Met | None implemented |

---

## 2. Dashboard Architecture

```
Landing route  /  →  HomeComponent  (thin host)
                        │
                        ▼
                  DashboardComponent            ← framework host
                        │
                        ├─ resolveVisibleWidgets(user)
                        │        │
                        │        ├─ DASHBOARD_WIDGETS  (Widget Registry)
                        │        └─ hasAnyModuleAccess (existing RBAC util)
                        ▼
                  Visible widgets (ordered)
                        │
                        ▼   *ngFor + ngSwitch on widget.kind
   ┌──────────┬───────────┬────────────┬──────────┬───────────┐
   │   kpi    │  alerts   │   queue    │  chart   │  recent   │
   └────┬─────┴─────┬─────┴──────┬─────┴─────┬────┴─────┬─────┘
        ▼           ▼            ▼           ▼          ▼
  DashboardService (HTTP)              existing components
        │                              (raw/processed/doctor
        ▼                               chart, his-sample)
  API  [QAuthorize per endpoint]
        ▼
  DashboardManager / TestRequestDetailsManager
        ▼
  ModuleRepo<T> → EF (SQL COUNT / SUM)
```

**Key property:** the framework contains no business data and no role logic. Visibility is a pure function of `(registry, user permissions)`.

---

## 3. Widget Inventory

| Widget | Kind | Modules (any, CanView=32) | API | Refresh |
|--------|------|---------------------------|-----|---------|
| Operational Alerts | alerts | Samples, Reports | `GET api/Dashboard/Alerts` | 2 min |
| Patient Registration | kpi | PatientDetails, Masters | `GET api/Dashboard/Registration` | 2 min |
| Billing | kpi | SaleInvoices | `GET api/Dashboard/Billing` | 2 min |
| Sample Collection | kpi | SampleCollection, Samples | `GET api/Dashboard/Collection` | 2 min |
| Sample Receiving | kpi | SampleReceiving, Samples | `GET api/Dashboard/Receiving` | 2 min |
| Radiology | kpi | RadiologyReports, RadiologyDoctorApprovals | `GET api/Dashboard/Radiology` | 2 min |
| Laboratory Workload | kpi | Reports, Samples | `GET api/Dashboard/Laboratory` | 2 min |
| Doctor Approval | kpi | DoctorsApprovals | `GET api/Dashboard/DoctorApproval` | 2 min |
| Pending Sample Collection | queue | SampleCollection, Samples, Reports | `GET api/Dashboard/PendingCollectionQueue` | 3 min |
| Pending Radiology | queue | RadiologyReports, RadiologyDoctorApprovals | `GET api/Dashboard/PendingRadiologyQueue` | 3 min |
| Daily sample status | chart | Samples, Reports | `GET api/DailyStatus/0` | on load |
| Technician daily status | chart | Reports, Samples | `GET api/DailyStatus/1` | on load |
| Doctor daily approval status | chart | DoctorsApprovals | `GET api/DailyStatus/2` | on load |
| Recent Samples | recent | Samples | `GET api/Patients` | on load |

### KPI definitions

| Metric | Rule |
|--------|------|
| New Patients | `PatientDetails.CreatedOn` = today, `IsActive` |
| Visits Today | `PatientVisit.VisitDateTime` = today, `IsActive` |
| Invoices / Net / Collected / Outstanding | `SaleInvoice.InvoiceDate` = today, `IsActive`, status ≠ Cancelled |
| Pending Collection | `ReportStatus = New` AND `CollectedBy` empty |
| Collected Today | `CollectedBy` set AND `SampleCollectionDate` = today |
| Pending Receiving | `CollectedBy` set, `ReceivedBy` empty, not FinallyRejected |
| Received Today | `ReceivedBy` set AND `SampleReceivedDate` = today |
| Pending Result Entry | `ReceivedBy` set AND `ReportStatus = New` |
| On Analyzer | `ReportStatus = SentToEquipment` |
| Pending Tech Approval | `ReportStatus = ReportGenerated` |
| Pending Doctor Approval | `ReportStatus = TechnicianApproved` |
| Approved / Rejected Today | distinct `TestResult.TestRequestId` where `AuthorizationDate` = today and request is DoctorApproved / DoctorRejected |
| Pending Radiology | `RadiologyRequestDetail.ReportStatus` ∈ {Pending, Draft, UnderReview} |
| Authorized Today | status ∈ {Authorized, Released} AND `ModifiedOn` = today |

### Alert rules
- **Critical** — uncollected orders older than **4 h**
- **Warning** — awaiting doctor approval longer than **24 h**
- **Info** — samples currently in any rejected state

---

## 4. RBAC Validation

- Visibility uses the **existing** `hasAnyModuleAccess` / `isAdministrator` helpers. No new permission model, no schema change, no role change.
- Required bit lowered from the old landing page's **63 (full)** to **32 (CanView)** — read-only widgets now correctly appear for read-only users. This fixes the Critical finding in the BA report where Doctor/Billing/Reception landed on an empty page.
- **Defence in depth:** every widget's API carries `[QAuthorize]` with the same module(s) as the registry entry, so hiding a widget cannot be bypassed by calling the endpoint directly.

### Role-by-role outcome

| Role (or grant set) | Widgets rendered |
|---|---|
| **Administrator** | All 14 (bypass) |
| **Technician** (Samples, SampleCollection, SampleReceiving, Reports) | Alerts, Collection, Receiving, Laboratory Workload, Pending Collection queue, sample + technician charts, Recent Samples |
| **Doctor** (DoctorsApprovals, Reports, RadiologyDoctorApprovals, RadiologyReports) | Alerts, Doctor Approval, Radiology, Laboratory Workload, Pending Radiology queue, technician + doctor charts |
| **Reception** (PatientDetails, SaleInvoices) | Patient Registration, Billing |
| **Billing** (SaleInvoices) | Billing |
| **Radiology** (RadiologyReports) | Radiology, Pending Radiology queue |
| **No relevant grants** | Welcome banner + explicit "no widgets enabled" notice |

---

## 5. API Reuse Summary

**Reused unchanged (no modification):**
- `GET /api/Patients` — Recent Samples
- `GET /api/DailyStatus/{0,1,2}` — all three charts (security attribute only)
- `/reports/pending-collection`, `/reports/radiology/pending` — "View all" destinations
- `HisSampleComponent`, `RawSampleChartComponent`, `ProcessedSampleChartComponent`, `DoctorSampleChartComponent`, `StatusChartComponent` — reused as-is
- `hasAnyModuleAccess`, `isAdministrator`, `isEmailConfirmed`, `QAuthorize`, `ModuleRepo`, `GenericUnitOfWork`

**`DoctorSampleChartComponent` was dead code before this change** (declared but never rendered). It is now live, resolving a BA finding with zero new chart code.

**New endpoints and why they were necessary:**
- KPI endpoints — the existing report APIs return full row sets; deriving counts from them would transfer large payloads on every dashboard load.
- Queue endpoints — `ReportManager.GetPendingCollectionReport` / `GetPendingRadiologyReport` execute `patientRepo.Get().ToDictionary(...)`, loading the **entire patient table into memory per call**. Acceptable for an on-demand report, not for a landing page opened by every user on a timer. The new queue endpoints fetch only the top N rows and resolve just those patients, and cap page size at 20.

---

## 6. New vs. Reused Components

**New (9 files, all additive):**

| File | Purpose |
|------|---------|
| `LIS.DtoModel/Models/DashboardModels.cs` | DTOs |
| `LIS.DtoModel/Interfaces/IDashboardManager.cs` | Contract |
| `LIS.Businesslogic/DashboardManager.cs` | Read-only aggregation |
| `web/Lis.Api/Controllers/Api/DashboardController.cs` | 10 authorized read endpoints |
| `web/Lis.Web/src/app/dashboard/dashboard.model.ts` | Framework contracts |
| `.../dashboard/widget-registry.ts` | Widget Registry |
| `.../dashboard/dashboard.service.ts` | HTTP access |
| `.../dashboard/dashboard.component.*` | Framework host |
| `.../dashboard/widgets/*` | 3 widget components + shared CSS |

**Modified (8 files, minimal):**

| File | Change |
|------|--------|
| `home.component.html` | Now renders `<app-dashboard>` |
| `home.component.ts` | Reduced to a host (logic moved into the framework) |
| `app.module.ts` | +1 import, +4 declarations |
| `SimpleInjectorConfig.cs` | +1 DI registration |
| `DailyStatusController.cs` | `[AllowAnonymous]` → `[QAuthorize]` (+1 using) |
| 3 × `.csproj` | File registrations (old-style projects) |

Net source diff on existing files: **+20 / −77 lines.**

---

## 7. QA Certification

| Test | Result | Evidence |
|------|--------|----------|
| .NET solution build | **PASS** | Exit 0; no new warnings |
| Angular AOT production build | **PASS** | Exit 0; no new warnings |
| TypeScript/lint | **PASS** | No linter errors |
| API startup / DI verification | **PASS** | API booted under IIS Express — `container.Verify()` succeeded with `IDashboardManager` |
| Anonymous access to `DailyStatus` | **CLOSED** | `GET /api/DailyStatus/0` → **401** (was 200 anonymous) |
| Anonymous access to all 10 dashboard endpoints | **BLOCKED** | All → **401** |
| Widget isolation | **PASS (design)** | Independent subscriptions; server returns empty set on error |
| Queue page-size abuse | **BLOCKED** | `take` clamped to 20 |

**Not completed:** an authenticated data-path test against live data. A token was issued successfully, but all `QAuthorize`-protected endpoints returned 401 in the ad-hoc IIS Express run. The pre-existing, untouched `api/Reports/PendingCollection` returned **the same 401 under identical conditions**, establishing that this is a characteristic of the ad-hoc local host configuration and not a regression introduced here. **KPI values must be confirmed once on the deployed environment.**

**Not produced:** UI screenshots for the four role views. No browser automation is available in this environment; §4 documents the expected per-role widget set for manual confirmation.

---

## 8. Regression Certification

No file belonging to the following areas was opened for modification:

Patient Registration · Sale Invoice · Test Booking · Sample Collection · Sample Receiving · Analyzer Integration · Result Entry · Technician Approval · Doctor Approval · Radiology · Diagnostic Reports · Working Board · Reports · Masters · Settings · User Management · Role Management · Menu Management · RBAC · Authentication · Authorization · existing routes · existing navigation · existing validations

**Verified by change footprint** (`git status`): the only modified files are the 8 listed in §6 — none of which belong to the areas above, with two deliberate, in-scope exceptions:

1. `DailyStatusController.cs` — the explicitly requested removal of anonymous access. Its only consumer is the dashboard chart component.
2. `SimpleInjectorConfig.cs` / `.csproj` — additive registration lines required for any new class to exist.

**Database:** no schema change, no migration, no seed change, no write path. Every new query is `SELECT COUNT/SUM/TOP`.

**Routing:** `app.routing.ts` untouched; `/` still resolves to `HomeComponent`.

---

## 9. Performance Assessment

| Aspect | Outcome |
|--------|---------|
| Requests on load | 1 per visible widget, issued in parallel; a Technician loads ~8, an Administrator ~14 |
| Duplicate calls | None — each endpoint is owned by exactly one widget |
| Query cost | KPIs are SQL `COUNT`/`SUM`; queues are `TOP N` + targeted patient lookup |
| Full-table loads | **Avoided** — this was the reason the report APIs were not reused for queues |
| Blocking | None; every widget renders independently |
| Payload | KPI responses are a handful of scalars |

**Known residual:** an Administrator issues ~14 parallel requests, which browsers serialise at ~6 per host. Acceptable for V1 given each is a lightweight count. If it becomes an issue, the natural next step is a single batched endpoint — deliberately deferred to keep this change minimal.

---

## 10. Production Readiness

**Certified ready for deployment**, subject to the two open items below.

| Gate | Status |
|------|--------|
| Builds clean (API + UI) | Yes |
| Starts successfully (DI verified) | Yes |
| Security hole closed and verified | Yes |
| Server-side authorization on every widget endpoint | Yes |
| Zero schema/migration/write impact | Yes |
| Change footprint confined to landing page + additive files | Yes |
| No TODOs, dead code, commented-out code, or temporary code | Yes |
| Authenticated KPI values confirmed against live data | **Open — confirm by signing in** |
| Per-role UI screenshots | **Open — manual capture** |

---

## 11. Deployment Verification

**Deployed:** 26 Jul 2026
**Topology:** portal on `http://localhost:8080`, API mounted as the `/lis` virtual directory of the same site (matches `environment.prod.ts` → `ApplicationServer: '/lis'`).

| Target | Path | Deployed |
|--------|------|----------|
| API | `I:\Projects\PROD\AVILIS\API\bin` | `Lis.Api.dll`, `LIS.BusinessLogic.dll`, `LIS.DtoModel.dll` (+ pdb), Release build |
| Portal | `I:\Projects\PROD\AVILIS\PORTAL` | `dist/DxI800` AOT production build |

**Rollback point:** `I:\Projects\PROD\AVILIS\_backup\20260726-224056` (90 API assemblies + portal `index.html` and `web.config`).

### Smoke test results

| Check | Expected | Actual |
|-------|----------|--------|
| Portal root | 200 | **200** |
| Portal main bundle | 200 | **200** |
| 10 × `/lis/api/Dashboard/*` | 401 (live + secured) | **401** — routed, DI resolved, `QAuthorize` executed |
| `/lis/api/DailyStatus/{0,1,2}` | 401 (was anonymous 200) | **401** |
| `/lis/api/Reports/PendingCollection` | 401 (unchanged) | **401** |
| `/lis/api/Patients/`, `/lis/api/Roles` | 401 (unchanged) | **401** |
| `/lis/api/Equipments/`, `/lis/api/Department/` | 200 (pre-existing anonymous) | **200** |

**Startup health:** every new endpoint returned 401 rather than 404 or 500, and pre-existing endpoints still serve normally. This confirms the API restarted cleanly on the new assemblies and SimpleInjector's `container.Verify()` accepted the `IDashboardManager` registration in production.

**Bundle content check:** the deployed `main-es2015.*.js` contains `api/Dashboard/Registration`, `api/Dashboard/PendingCollectionQueue`, `Operational Alerts`, `Laboratory Workload`, `Pending Sample Collection`, `zl-widget` and the empty-state message — confirming the new dashboard UI is live.

**Remaining manual step:** sign in and confirm the KPI figures and per-role widget sets described in §4.

### Rollback procedure
1. Copy `_backup\20260726-224056\API-bin\*` back to `I:\Projects\PROD\AVILIS\API\bin`.
2. Copy `_backup\20260726-224056\PORTAL\index.html` back to `I:\Projects\PROD\AVILIS\PORTAL`.

Older portal bundles were overlaid, not deleted, so restoring `index.html` alone reverts the UI.

---

*Analysis, implementation, verification and deployment completed without modifying any existing business functionality, workflow, API contract, database schema, or RBAC configuration.*
