# Commission Management Step 2 — Sample Collector Master Implementation Report

**Date:** 2026-09-26  
**Status:** PASS WITH LIMITATIONS  
**Scope:** Sample Collector Master and optional invoice attribution only. Stopped after Step 2.

---

## 1. Scope implemented

- Independent `SampleCollectorMaster` using the Referral Doctor master pattern.
- Optional `SaleInvoice.CollectorId` persisted on create, update, reload, and clear.
- API, Angular master screens, invoice selector, RBAC menu, EF migration, and deployment SQL.
- No commission rate, calculation, report, ledger, payout, BusinessUnit, or Inside/Outside field.

## 2. Discovery findings

Documented in `docs/Commission-Management-Step2-Collector-Master-Discovery.md`.

The safe path is the existing Referral Doctor master (`MasterCrudManager`, `MasterApiControllerBase`, generic Angular master, `Masters` module). Sample Collector is not an `AspNetUsers` row and is not `TestRequestDetail.CollectedBy`.

## 3. SampleCollectorMaster final structure

Table `dbo.SampleCollectorMaster`.

| Column | Type | Notes |
|--------|------|--------|
| Id | int identity PK | Stable key |
| Code | nvarchar(20) not null | Required. No unique index. Duplicate codes are allowed, same as Referral Doctor |
| Name | nvarchar(100) not null | Required |
| Phone | nvarchar(15) null | |
| IsActive | bit not null | Delete sets this false. `GetAll` returns active rows only |
| CreatedBy | nvarchar(max) null | |
| CreatedOn | datetime not null | |
| ModifiedBy | nvarchar(max) null | |
| ModifiedOn | datetime not null | |

No commission percent, effective dates, BusinessUnit, email, or address.

## 4. SaleInvoice.CollectorId final structure

- `SaleInvoice.CollectorId` nullable int.
- Index `IX_SaleInvoice_CollectorId`.
- `FK_SaleInvoice_SampleCollectorMaster` to `SampleCollectorMaster.Id`.
- `ON DELETE NO ACTION`, `ON UPDATE NO ACTION`.
- Existing rows stay `NULL`. No backfill. Verified `COUNT(CollectorId IS NOT NULL) = 0` immediately after the script, before tests created new invoices.
- `ReferralDoctorId` and `CorporateId` are unchanged and still have no database foreign key.
- `0` or negative is stored as NULL.
- Unknown id is rejected: "Selected sample collector was not found."
- A newly chosen inactive collector is rejected: "Selected sample collector is inactive." An invoice that already stores that same id can still be saved.
- Doctor and collector may both be set. No mutual-exclusion check.
- Not added to `SaleInvoiceDetail` or `TestRequestDetail`.

## 5. API changes

- `api/SampleCollector` matches `api/ReferralDoctor`: Get, GetAll, Post, Put, Delete. Module `Masters`.
- Registered in SimpleInjector next to Referral Doctor.
- `SaleInvoice` DTO and `SaleInvoiceManager` persist `CollectorId` on create and update and return it from `GetById` / list mapping.
- Create and update remain compatible when `CollectorId` is omitted or null.
- Invoice totals, payment status, and sample collection are not recalculated from the collector.

## 6. UI changes

- Setup list and form at `/sample-collectors`, fields code, name, phone, active.
- Invoice form adds an optional Sample Collector select on the same row as Corporate and Referral Doctor.
- Payment Type stays the same control and was moved to the following row so the three selectors fit one row.
- The selector does not reload rates. Corporate and Referral Doctor still call `onRateContextChange`.
- Empty selection saves NULL. Edit reloads `collectorId`.
- Print meta shows the selected collector name.
- The dropdown is filled from active collectors only, same as Referral Doctor.

## 7. RBAC changes

- Menu key `MASTER_SAMPLE_COLLECTOR`, label Sample Collector, route `/sample-collectors`, order 105, module `Masters`.
- Left nav, route guard, and role catalog use that key.
- Deployment SQL copies `RoleMenuPermission` from `MASTER_REFERRAL_DOCTOR`. Roles without Referral Doctor do not receive the new menu.
- Invoice selection uses the existing Sale Invoice permission. No new invoice permission.

## 8. Migration details

`LIS.DataModel/Migrations/202609261530000_SampleCollectorMaster.cs`

Idempotent SQL: create table if missing, add nullable column, index, foreign key, and permission copy. `Down()` is empty, consistent with other additive scripts in this solution. EF initializer remains null, so the script is the apply path.

## 9. Deployment SQL details

`Scripts/add-sample-collector-master.sql` is the same schema and permission SQL, split with `GO`.

Applied to `.\SQLEXPRESS` / `ZoryaLMS` so the focused tests could persist the column. Confirmed table, nullable `CollectorId`, foreign key `NO ACTION`, and matching menu rows. No historical `CollectorId` backfill. `CollectedBy` was not in the script.

This was not an IIS deployment. Portal and API under `I:\Projects\PROD\AVILIS` were not updated, and no app pool was recycled. The running site is still the previous binaries. The shared database now has the new table and column, which existing code ignores because the column is nullable.

## 10. Tests executed and results

`SampleCollectorMasterTests` — 8 passed, 0 failed:

1. Create, edit, deactivate, and exclusion from `GetAllActive`.
2. Duplicate code allowed, matching Referral Doctor.
3. Invoice without collector saves and reloads NULL.
4. Invoice with collector saves, reloads, and clears.
5. Referral doctor, collector, and corporate together save.
6. Unknown collector id rejected.
7. Newly selected inactive collector rejected.
8. `MASTER_SAMPLE_COLLECTOR` permissions match `MASTER_REFERRAL_DOCTOR`.

Invoice tests also assert line net 1000 with paid 0 on the collector save, and `TestRequestDetail.CollectedBy` remains empty after invoice save. `CollectSample` was not called.

## 11. Regression tests executed and results

50 passed, 0 failed, 0 skipped:

- `SaleInvoiceManagerTests`
- `SaleInvoiceRegressionTests`
- `SaleInvoiceEditWorkflowTests`
- `SaleInvoiceMultiDepartmentPaidTests`
- `LookupMasterCrudTests`
- `ActiveLookupFilterTests`
- `SampleCollectionInvoiceStatusTests`
- `PatientInvoiceSampleWorkflowTests`
- `RecentSampleListingTests`

The full `LIS.Masters.Tests` suite was not run.

## 12. Build results

- Release build of `LIS.Masters.Tests` succeeded, which builds DTO, data access, business logic, and `Lis.Api`. Existing warnings only. No new errors.
- Angular production build succeeded (`main-es2015.94b55a2d5b185081586a.js`). Existing budget warnings only. The bundle was not copied to IIS.

## 13. Files changed

- `LIS.DtoModel/Models/DTO/Masters/SampleCollectorMaster.cs`
- `LIS.DtoModel/Models/DTO/SaleInvoice/SaleInvoice.cs`
- `LIS.DtoModel/LIS.DtoModel.csproj`
- `LIS.DataModel/DAL/ApplicationDBContext.cs`
- `LIS.DataModel/Migrations/202609261530000_SampleCollectorMaster.cs`
- `LIS.DataModel/LIS.DataAccess.csproj`
- `Scripts/add-sample-collector-master.sql`
- `LIS.Businesslogic/LookupMasterManagers.cs`
- `LIS.Businesslogic/SaleInvoiceManager.cs`
- `web/Lis.Api/Controllers/Api/LookupMasterControllers.cs`
- `web/Lis.Api/App_Start/SimpleInjectorConfig.cs`
- `web/Lis.Api/Providers/MenuCatalog.cs`
- `web/Lis.Web/src/app/masters/master-schemas.ts`
- `web/Lis.Web/src/app/app.routing.ts`
- `web/Lis.Web/src/app/_components/left-nav-menu/left-nav-menu.component.html`
- `web/Lis.Web/src/app/_components/list-module/list-module.component.ts`
- `web/Lis.Web/src/app/_guards/permission.util.ts`
- `web/Lis.Web/src/app/administration/roles/role-permission-catalog.ts`
- `web/Lis.Web/src/app/masters/sale-invoice/sale-invoice-form.component.ts`
- `web/Lis.Web/src/app/masters/sale-invoice/sale-invoice-form.component.html`
- `LIS.Masters.Tests/Infrastructure/MasterTestDataBuilder.cs`
- `LIS.Masters.Tests/Infrastructure/TestServiceFactory.cs`
- `LIS.Masters.Tests/LIS.Masters.Tests.csproj`
- `LIS.Masters.Tests/Masters/SampleCollectorMasterTests.cs`
- `docs/Commission-Management-Step2-Collector-Master-Discovery.md`
- `docs/Commission-Management-Step2-Collector-Master-Implementation-Report.md`

## 14. Files and modules not changed

- `TestRequestDetail` and `CollectedBy`
- `SampleCollectionManager`
- Invoice amount calculation and payment logic
- Referral Doctor and Corporate persistence rules
- Report template engine, Gate 6, and production report printing
- Commission rate, calculation, report, ledger, and payout
- `I:\Projects\PROD\AVILIS` portal and API

## 15. CollectedBy was not modified

No source change to `TestRequestDetail.CollectedBy` or `SampleCollectionManager`. Invoice save does not copy `CollectorId` into `CollectedBy`. Focused tests confirm `CollectedBy` stays empty after save.

## 16. No commission calculation was implemented

`CollectorId` is stored and returned only. Precedence (doctor over collector) is not applied during save. No rate table, percent, or commission amount is calculated.

## 17. No production IIS deployment was performed

Portal and API were built locally and were not published to `I:\Projects\PROD\AVILIS`. No app pool recycle.

## 18. Limitations

- The additive schema is now on the shared `ZoryaLMS` database. The running IIS site still has the previous portal and API until a later, separate deploy.
- An invoice that already has an inactive collector reloads the id, but the selector lists active collectors only, so that name is not shown until a new active collector is chosen. Same active-only list as Referral Doctor.
- Duplicate collector codes are allowed because Referral Doctor has no duplicate-code rule.
- Full Masters regression was not executed. The invoice, referral doctor, corporate, sample collection, and payment classes listed above passed.

Commission rate setup was not started.
