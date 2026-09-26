# Commission Management — Production Readiness Step 1

## RBAC and Deployment Verification

**Verification status:** PASS WITH LIMITATIONS  
**Production approval:** Not given. This is Step 1 only.

## 1. Executive Summary

Source and the shared `ZoryaLMS` database contain Commission Management. The IIS API and Portal that serve AVILIS do not.

Administrator is granted Doctor Commission, Collector Commission, and Commission Payout in source and in `RoleMenuPermission`. Receptionist and Technician have a Reports menu overlay and do not have those menu keys, so the portal hides the new report links and denies the routes. The Doctor role has the Reports module and no Reports menu rows. The existing report-menu fallback would show Doctor Commission and Collector Commission to that role once the new portal is deployed. None of the three non-administrator roles have the Commission Payout module.

`Commission:ImplementationMonth` exists in source `Web.config` and the value is empty. Empty fails closed in source. The deployed API `Web.config` does not contain the key, and the deployed API binary does not contain the commission code.

No production approval follows from this check.

## 2. Verification Date/Time

2026-09-27, about 01:00 IST (UTC+5:30).

## 3. Environment Verified

| Layer | What was inspected |
|-------|--------------------|
| Source | `I:\Projects\LIS\avs-lis` |
| Database | `.\SQLEXPRESS` / `ZoryaLMS`, the catalog in both source and deployed API connection strings. Read-only queries only. |
| IIS API | Site `AVILIS_API`, `http/*:8081`, physical path `I:\Projects\PROD\AVILIS\API`. Site state **Stopped**. App pool `AVILIS_API_POOL` state **Started**. |
| IIS Portal | Site `AVILIS_PORTAL`, `http/*:8080`, physical path `I:\Projects\PROD\AVILIS\PORTAL`. Site state **Started**. App pool `AVILIS_PORTAL_POOL` state **Started**. |
| Same API folder, second app | `AVILIS_PORTAL/lis` uses physical path `I:\Projects\PROD\AVILIS\API` and pool `AVILIS_API_POOL`. |

TriVita LIS (`I:\Projects\PROD\TriVita\services\LISService`) has no `Lis.Api.dll` and no commission setting. It is not the AVILIS deployment.

## 4. Administrator RBAC

Administrator access is present in source and in the database.

Portal `isAdministrator` is true when the user has at least 10 modules and every module has the full bitmask 63. The Administrator role has 21 module mappings, and all 21 are full (CanView, CanAdd, CanEdit, CanDelete, CanAuthorize, CanReject). The portal guard returns allowed before the route rule. API `QAuthorize` returns allowed when the user is in the role `Administrator`, before the module check.

The database rows are also present, so access does not depend only on the bypass.

| Item | Route | Menu key | Module | Database |
|------|-------|----------|--------|----------|
| Doctor Commission | `/reports/doctor-commission` | `REPORT_DOCTOR_COMMISSION` | `Reports` | Administrator, CanView true, CanAdd true, active, module `Reports` |
| Collector Commission | `/reports/collector-commission` | `REPORT_COLLECTOR_COMMISSION` | `Reports` | Administrator, CanView true, CanAdd true, active, module `Reports` |
| Commission Payout | `/commission-payouts` | `COMMISSION_PAYOUT` | `CommissionPayout` | Administrator, CanView true, CanAdd true, active, module `CommissionPayout` |

Menu visibility in source:

- Doctor and Collector Commission use `showReportMenu` with `reports.doctorCommission` and `reports.collectorCommission`.
- Commission Payout uses `hasMenu('CommissionPayout','commission.payout')`. The section is hidden unless that check passes.

Route authorization in source:

- `permission.guard.ts` allows Administrator immediately.
- The route rules are `Reports` + `REPORT_DOCTOR_COMMISSION`, `Reports` + `REPORT_COLLECTOR_COMMISSION`, and `CommissionPayout` + `COMMISSION_PAYOUT`.

API authorization in source. Permission bits are CanView = 32 and CanAdd = 1.

| API | Attribute |
|-----|-----------|
| `GET api/Reports/DoctorCommission` | `QAuthorize` module `Reports`, CanView |
| `GET api/Reports/CollectorCommission` | `QAuthorize` module `Reports`, CanView |
| `GET api/CommissionPayout/schedule` | module `CommissionPayout`, CanView |
| `POST api/CommissionPayout/preview` | module `CommissionPayout`, CanView |
| `GET api/CommissionPayout` (history) | module `CommissionPayout`, CanView |
| `GET api/CommissionPayout/{id}` (memo) | module `CommissionPayout`, CanView |
| `POST api/CommissionPayout/confirm` | module `CommissionPayout`, CanAdd |

Preview and history require CanView. Confirm requires CanAdd. Those attributes do not set `MenuKey`.

`UserModules` contains `CommissionPayout` at `/commission-payouts`, order 210. Administrator `RoleModuleMappings` for that module has every permission bit true.

## 5. Non-Administrator RBAC

Roles in `AspNetRoles`: Administrator, Doctor, Receptionist, Technician. There is no role named Reception. No other operational role is present.

Commission menu keys exist only on Administrator. Doctor, Receptionist, and Technician have no `REPORT_DOCTOR_COMMISSION`, `REPORT_COLLECTOR_COMMISSION`, or `COMMISSION_PAYOUT` rows. They have no `CommissionPayout` module mapping.

| Role | Reports module | Reports menu overlay | Doctor / Collector Commission menu and route | Payout menu, route, and API |
|------|----------------|----------------------|-----------------------------------------------|-----------------------------|
| Receptionist | Yes, all bits true | Yes, 10 active rows. Invoice register, test booking, diagnostic, and the collection reports. Not the commission keys. | Hidden and route denied, because an overlay is present and the keys are absent. | No. No payout module. |
| Technician | Yes, all bits true | Yes, 2 active rows: `WORKING_BOARD_LAB_RESULT_EDIT` and `WORKING_BOARD_TECHNICIAN_APPROVAL`. | Hidden and route denied, same overlay rule. | No. No payout module. |
| Doctor | Yes, all bits true | No Reports menu rows. | Shown and route allowed by the existing fallback, once the new portal code is what users load. | No. No payout module. |

The fallback is in source. `showReportMenu` uses `hasReportAccess()` when the role has no menu rows for that module. `hasReportAccess()` is true when Reports, SaleInvoices, Samples, or RadiologyReports has bitmask 63. The Doctor role’s Reports mapping is full, so both commission report links pass. The route rule then allows the page because `hasMenuAccess` allows the module when that module has no menu overlay.

Receptionist and Technician are not affected by that fallback. Their Reports overlay is active, and the new keys were not copied onto it.

API, if the new binary were deployed: report endpoints check the Reports module CanView and do not set `MenuKey`. Receptionist, Technician, and Doctor all have Reports CanView, so a direct call to `api/Reports/DoctorCommission` or `CollectorCommission` would be allowed even when the portal route is denied. Payout endpoints require the CommissionPayout module, which those three roles do not have, so preview, history, and confirm stay denied.

This check did not sign in as any user and did not change permissions.

## 6. Existing Authorization Architecture

Commission Management uses the existing chain.

Menu catalog entry, then portal route rule and `PermissionGuard`, then `QAuthorize` on the API, then `UserModules` / `RoleModuleMappings` / `RoleMenuPermission`.

There is no second authorization framework for commission. Payout is a new module name inside that same model. The report screens use the existing Reports module.

`QAuthorize` already treats `MenuKey` as optional. Commission endpoints leave it unset, so the API enforces the module bit only. That is the existing attribute behavior, applied here the same way as the other report actions that also omit `MenuKey`.

## 7. Commission:ImplementationMonth

| Location | Key | Value |
|----------|-----|-------|
| Source `web\Lis.Api\Web.config` | Present | **EMPTY** (`value=""`) |
| Deployed `I:\Projects\PROD\AVILIS\API\Web.config` | Absent | Not set |
| Portal `Web.config` | Not an API config | Key absent |

No other source `Web.*.config` defines the key.

In source, `CommissionCalculationService.ReadImplementationMonthStart` throws if the value is missing or whitespace, and if it is not `yyyy-MM`. Reports and payout call that method. An empty value fails closed. That empty state is intentional. This check did not fill it in.

The deployed API does not contain `CommissionCalculationService`, so the running files do not apply that fail-closed path. The feature is not in that binary.

## 8. Database Verification

Catalog: `.\SQLEXPRESS` / `ZoryaLMS`. Queries were `SELECT` only.

| Table | Present |
|-------|---------|
| `CommissionPayout` | Yes |
| `CommissionPayoutDetail` | Yes |
| `ReferralDoctorCommissionRate` | Yes |
| `SampleCollectorCommissionRate` | Yes |
| `SampleCollectorMaster` | Yes |

`CommissionPayout` columns match source: Id bigint, PayoutNo nvarchar(30), RecipientType int, RecipientId int, FromDate datetime, ToDate datetime, TotalAmount decimal(18,2), CreatedBy nvarchar(max) null, CreatedOn datetime.

`CommissionPayoutDetail` columns match source: Id bigint, CommissionPayoutId bigint, RecipientType int, RecipientId int, SaleInvoiceId bigint, InvoiceNo nvarchar(50) null, InvoiceDate datetime, CommissionBase decimal(18,2), CommissionPercent decimal(5,2), Amount decimal(18,2).

Indexes:

- `PK_CommissionPayout` on Id
- `UX_CommissionPayout_Recipient_From` unique on RecipientType, RecipientId, FromDate
- `PK_CommissionPayoutDetail` on Id
- `UX_CommissionPayoutDetail_Recipient_Invoice` unique on RecipientType, RecipientId, SaleInvoiceId
- `FK_CommissionPayoutDetail_CommissionPayout`
- Rate indexes `IX_ReferralDoctorCommissionRate_Doctor_From` (ReferralDoctorId, EffectiveFrom) and `IX_SampleCollectorCommissionRate_Collector_From` (CollectorId, EffectiveFrom), not unique

Observed row counts, not modified by this check: `CommissionPayout` 2, `CommissionPayoutDetail` 3.

## 9. API IIS Deployment Verification

Deployed API is **not** the Commission Management build.

| Fact | Deployed `I:\Projects\PROD\AVILIS\API` | Current source |
|------|----------------------------------------|----------------|
| `Lis.Api.dll` | 2026-09-12 23:52:05, 384,000 bytes | 2026-09-27 00:21:08, 390,656 bytes |
| `CommissionPayoutController` | Not in the DLL | In source |
| `DoctorCommission` | Not in the DLL | In source |
| `CommissionCalculationService` | Not in the DLL | In source |
| `Commission:ImplementationMonth` | Not in `Web.config` | Present, empty |
| Database | `.\SQLEXPRESS` / `ZoryaLMS` | Same catalog |

Site `AVILIS_API` on port 8081 is Stopped. The application `AVILIS_PORTAL/lis` points at the same API folder and its pool is Started. Either way, the files in that folder do not contain the commission implementation. This check did not call the API and did not recycle anything.

## 10. Portal IIS Deployment Verification

Deployed portal is **not** the Commission Management build.

| Fact | Deployed `I:\Projects\PROD\AVILIS\PORTAL` | Current source build |
|------|-------------------------------------------|----------------------|
| Bundle named by `index.html` | `main-es2015.3fbf6ca83eafc813d396.js` | `main-es2015.80521cd0c21a24afcc56.js` |
| Bundle file time | 2026-09-18 23:36:25 | 2026-09-27 00:24:48 |
| `doctor-commission`, `commission-payouts`, `REPORT_DOCTOR_COMMISSION` | Not found in the deployed bundle | Present in the source bundle |

Site `AVILIS_PORTAL` on port 8080 is Started. This check did not copy files and did not recycle the pool.

## 11. Source vs Database vs IIS Consistency

| Item | Source | Database `ZoryaLMS` | IIS |
|------|--------|---------------------|-----|
| Commission report and payout code | Present | Not applicable | Absent from API DLL and portal bundle |
| Payout and rate tables | Migration and `Scripts/add-commission-payout.sql` | Present, indexes match | IIS still runs an older API against this same database |
| Administrator commission permissions | Catalog and route rules | Rows exist for Administrator only | Deployed portal cannot show the new menus |
| `Commission:ImplementationMonth` | Key present, value empty | Not stored in the database | Key absent on the deployed API |

The mismatch is: source and database are ahead of IIS. IIS is an older API (12 Sep 2026) and an older portal (18 Sep 2026). The shared database already has the payout tables.

## 12. Findings

1. Commission Management is not deployed to AVILIS API or Portal.
2. The deployed API and the source API both use `ZoryaLMS`, and that database already has the payout schema.
3. Administrator has the three commission permissions in the database, with CanView and CanAdd. Confirm is the CanAdd operation. Preview and history are CanView.
4. Receptionist and Technician do not receive the three menus. Their Reports overlay blocks the fallback.
5. Doctor has Reports and no Reports menu rows. The existing fallback would show and allow Doctor Commission and Collector Commission for that role after the new portal is deployed. Doctor would not receive Commission Payout.
6. Report APIs do not set `MenuKey`. After deployment, any role with Reports CanView could call the commission report APIs directly, including Receptionist and Technician, whose portal routes would still deny the pages.
7. Implementation month is empty in source and fails closed there. It is not configured on the deployed API, and that API does not contain the reader.

No finding was corrected.

## 13. PASS / FAIL / BLOCKED Matrix

| Check | Result |
|-------|--------|
| Administrator menu, route, and API mapping in source | PASS |
| Administrator rows in `ZoryaLMS` | PASS |
| Payout CanView for preview/history and CanAdd for confirm | PASS |
| Receptionist and Technician do not hold the new menu keys | PASS |
| Doctor role vs report fallback | PASS WITH LIMITATIONS |
| Report API is module-only because `MenuKey` is unset | PASS WITH LIMITATIONS |
| Authorization uses the existing Menu → Route → QAuthorize model | PASS |
| `Commission:ImplementationMonth` empty and fail-closed in source | PASS |
| Deployed API has the same setting | FAIL — key absent; feature binary absent |
| Payout and rate tables, columns, and indexes | PASS |
| API IIS contains this implementation | FAIL |
| Portal IIS contains this implementation | FAIL |
| Source, database, and IIS are the same build | FAIL |

## 14. Changes Made

NO CODE CHANGES  
NO DATABASE CHANGES  
NO CONFIGURATION CHANGES  
NO PERMISSION CHANGES  
NO DEPLOYMENT  
NO IIS RECYCLE

## Stop

Production deployment and commission activation were not started.
