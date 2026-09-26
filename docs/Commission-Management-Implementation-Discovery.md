# Commission Management — Implementation Discovery (Stage 0)

**Date:** 2026-09-26  
**Result:** Discovery only for the inspection below. No code and no database changes were made during discovery.  
**Stop:** Stage 0 ended here. Stage 1 later added commission rates after the first-payout From date was fixed to the 1st of the implementation month.

This pass maps the approved Commission Management specification onto the current ZoryaLMS code. It replaces the earlier Stage 0 notes where those notes described fortnightly payouts or omitted fields the approved specification now requires.

---

## Discovered architecture

| Area | Existing implementation |
|------|-------------------------|
| Invoice | `SaleInvoice` / `SaleInvoiceDetail`. Save, update, and cancel are `SaleInvoiceManager`. The Angular workflow is `sale-invoice-form`. |
| Attribution | `SaleInvoice.ReferralDoctorId` (nullable, no FK). `SaleInvoice.CollectorId` (nullable FK to `SampleCollectorMaster`). Both may be set. No precedence check exists yet. |
| Referral Doctor | `ReferralDoctorMaster`, `ReferralDoctorManager`, `api/ReferralDoctor`, menu `MASTER_REFERRAL_DOCTOR`. |
| Sample Collector | `SampleCollectorMaster`, `SampleCollectorManager`, `api/SampleCollector`, menu `MASTER_SAMPLE_COLLECTOR`. Code, Name, Phone, IsActive, audit. |
| CollectedBy | `TestRequestDetail.CollectedBy`, written by sample collection. `ReportManager.GetCollectorWiseReport` groups that string. Menu `REPORT_COLLECTOR_WISE`. Not commission. |
| Money | `SaleInvoiceManager.Recalculate`. Header `NetAmount = Round(GrossAmount - DiscountAmount + TaxAmount, 2)`. Line tax is forced to 0. A header discount replaces line discounts and is not copied onto the lines. `Math.Round(value, 2)` is the money convention. |
| Payment | `PaymentStatusType`: Unpaid 0, Partial 1, Paid 2. Set from `PaidAmount` against `NetAmount` in `ApplyPaymentStatus`. |
| Invoice state | `InvoiceStatusType`: Draft 0, Confirmed 1, Paid 2, Cancelled 3. Confirm on the form sets status 1. Cancel sets status 3, payment Unpaid, and `IsActive` false. |
| Reports | `OperationalReportsController` (`api/Reports`), `ReportManager`, `ReportFilterOptions` (FromDate, ToDate, optional ReferralDoctorId). Angular `ReportPageBase` plus `report-excel-export.util.ts`. Excel is client-side from the fetched rows. |
| Print | Sale invoice print is a portal print view and `window.print()` (`sale-invoice-form`). Diagnostic/radiology print is a separate template pipeline and is not the payout memo path. Lab name comes from Web.config `Report:LabName` / `Report.LabName`, then `ClientApplication.Name`. |
| RBAC | `MenuCatalog` → route guard → `QAuthorize` → `UserModules`, `RoleModuleMappings`, `RoleMenuPermission`. New Administrator-only seeds follow `202609120131000_ReportLayoutConfigurationRbac`. |
| Transactions | `GenericUnitOfWork.BeginTransaction()` is `DbContext.Database.BeginTransaction()`. Invoice save already commits or rolls back this way. |
| Rates today | `TestRateMaster` has required `EffectiveStart` and `EffectiveEnd`. Overlap of active rows is rejected in `TestRateMasterManager`. It is test pricing, not commission. |
| Settings | Single system values live in Web.config appSettings (`Report:LabName`, feature flags). There is no commission implementation month. |
| Payout | No payout header, payout detail, commission rate, or commission amount storage. |

Step 2 is already in source and on `.\SQLEXPRESS` / `ZoryaLMS`. This stage did not reapply it.

---

## Exact classes and files

- `LIS.DtoModel/Models/DTO/SaleInvoice/SaleInvoice.cs`
- `LIS.DtoModel/Models/DTO/SaleInvoice/SaleInvoiceDetails.cs`
- `LIS.DtoModel/Models/DTOEnums.cs`
- `LIS.Businesslogic/SaleInvoiceManager.cs`
- `LIS.DtoModel/Models/DTO/Masters/ReferralDoctorMaster.cs`
- `LIS.DtoModel/Models/DTO/Masters/SampleCollectorMaster.cs`
- `LIS.Businesslogic/LookupMasterManagers.cs`
- `LIS.Businesslogic/TestRateMasterManager.cs`
- `LIS.Businesslogic/ReportManager.cs`
- `LIS.DtoModel/Models/ReportFilterOptions.cs`
- `LIS.DtoModel/Models/Reports/SaleInvoiceRegisterRow.cs`
- `web/Lis.Api/Controllers/Api/OperationalReportsController.cs`
- `web/Lis.Api/Providers/MenuCatalog.cs`
- `web/Lis.Api/Migrations/202609120131000_ReportLayoutConfigurationRbac.cs`
- `LIS.DataModel/Repo/GenericUnitOfWork.cs`
- `web/Lis.Web/src/app/masters/sale-invoice/sale-invoice-form.component.ts`
- `web/Lis.Web/src/app/masters/sale-invoice/sale-invoice-form.component.html`
- `web/Lis.Web/src/app/reports/report-page.base.ts`
- `web/Lis.Web/src/app/reports/report-excel-export.util.ts`

---

## Mapping that matches the approved design

**Commission base.** `SaleInvoice.NetAmount - SaleInvoice.TaxAmount`. Do not use `SaleInvoiceDetail.NetAmount`. Do not add a total column. Do not change `Recalculate`.

**Formula.** Per invoice, `Round((NetAmount - TaxAmount) * percent / 100, 2)`, then sum those rounded amounts. One `CommissionCalculationService` in `LIS.Businesslogic`. Reports and payout call it. Invoice save does not.

**Attribution.** No one, doctor only, collector only, or both with doctor precedence. Collector rate is not required when a doctor is selected. Rate date is `InvoiceDate.Date`, inclusive, same comparison style as test-rate lookup.

**Payment exclusion.** `PaymentStatus` already distinguishes Unpaid, Partial, and Paid. Partial and Unpaid are excluded. Cancelled (`InvoiceStatus = Cancelled`) is excluded. They are omitted, not shown as zero.

**Rates.** Two tables, not one generic recipient table, and not `TestRateMaster`.

`ReferralDoctorCommissionRate`: Id, ReferralDoctorId, CommissionPercent decimal(5,2), EffectiveFrom, EffectiveTo null, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn.

`SampleCollectorCommissionRate`: same shape with CollectorId.

Names follow those masters (`EffectiveFrom` / `EffectiveTo` match the approved words; test rates use Start/End and are not copied). Overlap is rejected in the manager, and a unique filtered approach is not available for open-ended ranges in a single index, so the transactional check is the business save inside `BeginTransaction`, matching test-rate overlap. Adjacent dates (end 30-Sep, next start 01-Oct) are valid. Null `EffectiveTo` is open-ended and overlaps any later start for that recipient.

**Rate lock.** On payout confirm, a rate whose period crosses `ToDate` is split inside the same transaction: the existing row’s `EffectiveTo` becomes the payout `ToDate` and that row is the locked slice; a second row keeps the same percent from the next day through the old end (or null). Later edits may change only the open row. A change that would alter a locked slice is rejected. Multiple payout boundaries repeat the same split. This is the approved rule, not a new one.

**Locked flag.** The approved examples mark a slice LOCKED. The minimum storage is the closed payout range itself: a rate slice is locked when some confirmed payout for that recipient covers any date in the slice. A separate lock column is not required for that test. Deactivating a locked slice (`IsActive = false`) would remove it from runtime report resolution, so a locked slice cannot be deactivated. Inactive rows follow test-rate overlap: they are ignored for overlap and for lookup. That is the existing active-row convention applied to the approved `IsActive` field.

**Reports.** Two menus under the existing Reports module, Summary or Detail, not both. From/To required. Optional doctor or collector, default all. Date filter is `InvoiceDate`. Excel uses `report-excel-export.util.ts` for the selected format. `FddReportComponent` is one grid and is not reused as-is. The diagnostic template engine is not used. The Angular report page caps a range at two months; the API does not. Commission reports do not inherit that cap, because the approved filters do not include it.

Detail columns stay Invoice No, Invoice Date, Recipient, Commission Base, Rate, and Amount. Patient name is already on the invoice register. It is not added here, because the approved detail list does not require it.

**Payout.** Header and detail tables. Flexible From/To, not 1–15 / 16–month-end. One recipient per payout. Next `FromDate` must be the day after that recipient’s previous `ToDate`. Preview does not insert. Confirm re-reads eligibility, duplicates, sequence, and rates, then inserts header and details and applies the rate split in one transaction. Rollback on failure. Zero eligible invoices: preview shows none; confirm is rejected. An invoice is stored once per recipient. Unique indexes: `(RecipientType, RecipientId, FromDate)` and `(RecipientType, RecipientId, SaleInvoiceId)`.

Payout number follows `GenerateInvoiceNo`: a date prefix plus a sequence, for example `CP-{yyyyMMdd}-{seq}`.

Reprint reads stored detail rows. It does not call the calculation service.

Memo print follows the sale-invoice print view (`window.print()`), with lab identity from the existing `Report:LabName` settings. It is not Excel and not the diagnostic renderer. A signature line is a blank acknowledgement area on that print view.

**Implementation month.** Not in the database. Minimum place: one Web.config appSetting, `Commission:ImplementationMonth`, value `yyyy-MM`, read only by the calculation service. Same pattern as `Report:LabName`. The September 2026 text in the specification is an example and must not be hardcoded. If the setting is missing, calculation fails closed. No backfill of earlier months. No `CollectorId` backfill.

**Carry forward.** Invoice date before the current From date, on or after the implementation month, not cancelled, now fully paid, not already in a payout for that recipient, rate taken from the original `InvoiceDate`. Current means `InvoiceDate` inside the payout From/To.

**RBAC.** Rate screens use the Masters module and the same Administrator-oriented seed style as report-layout configuration, plus menu, route, and `QAuthorize`. Report menus use module `Reports`. Payout uses its own module and menu so invoice users do not receive it. Direct API calls use the same `QAuthorize` module.

**Migrations.** Idempotent EF `Sql()` plus `Scripts/`, as in `202609261530000_SampleCollectorMaster`. Do not recreate the collector table or column. Do not run scripts against production in this implementation.

---

## Conflicts

### 1. Draft invoice with payment status Paid

Payment status does distinguish Unpaid, Partial, and Paid. That stop condition is not met.

Invoice status is a second field. The form can save a Draft (status 0) with `PaidAmount` equal to `NetAmount`. `ApplyPaymentStatus` then stores Payment Status Paid while Invoice Status stays Draft. Confirm is a separate action that sets Invoice Status Confirmed.

The approved rule says eligible invoices are fully Paid/Confirmed, and names Partial, Unpaid, and Cancelled as excluded. It does not name Draft.

**Resolved for Stage 3.** Eligible only when `PaymentStatus = Paid` and `InvoiceStatus` is Confirmed or Paid. Draft, Partial, Unpaid, and Cancelled are excluded, including a Draft whose payment status is Paid.

### 2. First payout From date

Resolved for later payout work, not built in Stage 1.

The first payout From date is not chosen by the user. It is always the first day of the Commission Implementation Month. A later payout still uses a flexible To date, and the next From date is the day after the previous To date. Invoices before the implementation month stay ineligible. This removes the gap in which a fully paid invoice could fall before a mid-month first period and never enter a payout.

No other approved rule conflicts with the code that was inspected. `NetAmount` and `TaxAmount` match the approved base. `CollectorId` matches Step 2. RBAC can take new menus without a new permission framework. Excel export can serve Summary or Detail. EF idempotent migrations can add the new tables without rewriting existing invoices.

---

## Stage plan after approval

| Stage | Work | Blocked by |
|-------|------|------------|
| 1 | Rate tables, validation, overlap, lock protection, API, UI, RBAC, migration, script | Nothing found. Lock is enforced when a payout exists; with no payouts, every slice is editable. |
| 2 | Invoice save rate check only | Nothing found. |
| 3 | Calculation service | Conflict 1 is resolved. Eligible when payment is Paid and invoice status is Confirmed or Paid. |
| 4 | Doctor and collector reports, Summary or Detail, Excel, RBAC | Implemented. See the final report. |
| 5–6 | Payout, preview, confirm, carry forward, rate split, history, memo | Implemented. First-payout From date is the 1st of the implementation month. |
| 7–8 | UI/API authorization, regression, certification | Implemented. Production was not deployed. |

Production deployment is not part of this stage.

---

## Stage 0 checklist

- Code was not modified.
- The database was not modified.
- Production was not deployed.
- Stage 1 has not started.
