# Commission Management — Final Acceptance Gap Closure Report

**Date:** 2026-10-11  
**Mode:** Verification and test-environment preparation. No deployment.  
**Baseline report:** `docs/Commission-Management-Post-P1-P2-Integrated-Acceptance-Report.md`

Application business rules were not changed. Two test methods were added so the missing collector-only confirm and the duplicate-rate payout calls could run through the existing managers.

## 1. Fresh database baseline and connection evidence

The established procedure is a `COPY_ONLY` backup of `ZoryaLMS` and a restore with `MOVE` onto new files. The backup already on disk was used. It was not overwritten, and `ZoryaLMS_P1QA` was not restored over.

| Item | Value |
|---|---|
| Backup | `I:\Projects\LIS\p1qa\ZoryaLMS_P1QA.bak` |
| Header | `DatabaseName=ZoryaLMS`, `BackupType=1`, `IsCopyOnly=True`, `BackupStartDate=2026-10-10 13:54:34` |
| New catalog | `ZoryaLMS_GapQA` |
| Restore | Started and finished `2026-10-10 16:10:52` to `16:10:53` (+05:30) |
| Data file | `I:\Projects\LIS\p1qa\ZoryaLMS_GapQA.mdf` |
| Log file | `I:\Projects\LIS\p1qa\ZoryaLMS_GapQA_log.ldf` |
| Schema check | `SaleInvoice`, `CommissionPayout`, `CommissionPayoutDetail`, `ReferralDoctorCommissionRate`, and `SampleCollectorCommissionRate` are present |

Counts immediately after restore, before tests:

| Catalog | Invoices | Payouts | Locked doctor rates |
|---|---|---|---|
| `ZoryaLMS` (IIS catalog, `C:\Program Files\Microsoft SQL Server\MSSQL15.SQLEXPRESS\MSSQL\DATA\ZoryaLMS.mdf`) | 2247 | 2 | 8 |
| `ZoryaLMS_P1QA` (left as the accumulated copy) | 2339 | 4 | 11 |
| `ZoryaLMS_GapQA` | 2247 | 2 | 8 |

The fresh catalog matched the live catalog’s counts at restore time. It is a separate database with its own files.

The test project build left `LIS.Masters.Tests\bin\Release\LIS.Masters.Tests.dll.config` on `Database=ZoryaLMS_P1QA`. That string was then changed explicitly to `Database=ZoryaLMS_GapQA`. The run started only after the file contained `Database=ZoryaLMS_GapQA` and did not contain `Database=ZoryaLMS` or `Database=ZoryaLMS_P1QA`. Entity Framework reads `name=DefaultConnection` from that test-host file. After the run the host file still named `ZoryaLMS_GapQA`.

`LIS.Masters.Tests\App.config`, source `web\Lis.Api\Web.config`, and `I:\Projects\PROD\AVILIS\API\Web.config` still name `Database=ZoryaLMS`.

Counts after the tests:

| Catalog | Invoices | Payouts | Locked doctor rates |
|---|---|---|---|
| `ZoryaLMS` | 2247 | 2 | 8 |
| `ZoryaLMS_P1QA` | 2339 | 4 | 11 |
| `ZoryaLMS_GapQA` | 2297 | 4 | 11 |

The live catalog and the accumulated copy did not change. The fresh catalog gained 50 invoices, 2 payouts, and 3 locked doctor rates from the isolated payout tests.

## 2. Additional scenario results

`dotnet vstest` on `ZoryaLMS_GapQA`, filter `InvoiceCommissionRateValidationTests|SampleCollectorMasterTests|CommissionCalculationTests|CommissionStages4To8Tests`:

**Passed 39, Failed 0, Skipped 0.** Duration 2 minutes 2 seconds. Exit code 0.

| Scenario | Result |
|---|---|
| Collector-only Confirm with no applicable rate | PASS. New test `Confirm_Succeeds_When_Collector_Has_No_Rate`. Status Confirmed, collector id stored, doctor id empty, gross 1000, tax 0, net 1000, paid 0, no commission property. |
| Unknown collector and newly selected inactive collector | PASS. Existing `SampleCollectorMasterTests`. |
| Cancelled invoice cannot be edited | PASS. `Cancelled_Invoice_Update_Is_Still_Rejected`. |
| Header totals and tax on the invoice-rate saves | PASS. Same assertions as the P1 tests, now on the fresh catalog. |
| Missing-rate report keeps rated invoices and lists the others | PASS. `Report_Excludes_Missing_Rates_And_Keeps_Rated_Invoices`. |
| Duplicate covering rates fail the report | PASS. `Report_Duplicate_Rate_And_Invalid_Range_Still_Fail_Closed`. |
| Summary and Detail totals reconcile on rated lines | PASS. The missing-rate test compares the two formats, and `Report_Summary_And_Detail_Are_Exclusive` still passes. |
| Inactive rate, 0% rate, and pre-implementation-month omission | PASS on this catalog. |
| Direct calculation of a locked rate | PASS. `Locked_Rate_Still_Calculates`. This locks a rate only for a disposable doctor on `ZoryaLMS_GapQA`. |

## 3. Excel and browser results

Excel workbook XML was produced by executing `exportRowsToExcel` from `web/Lis.Web/src/app/reports/report-excel-export.util.ts` (bundled with esbuild, browser download mocked). The columns were the ones the commission report component passes.

| Format | First sheet | First-sheet headers | Second sheet | Second-sheet headers |
|---|---|---|---|---|
| Summary | `Summary` | Recipient, Invoices, Amount | `Missing Rates` | Invoice No, Invoice Date, Recipient, Reason |
| Detail | `Detail` | Invoice No, Invoice Date, Recipient, Commission Base, Rate, Amount | `Missing Rates` | Invoice No, Invoice Date, Recipient, Reason |

The exclusion sheet included invoice `INV-X` and the missing-rate reason. It did not have Rate or Amount columns. This is execution of the export function, not a click in the running portal and not a file opened in Excel.

Browser scenarios are **BLOCKED**. The production bundle sets `ApplicationServer` to `/lis`, which is the IIS application whose API uses `ZoryaLMS`. Serving that bundle on another port does not provide an isolated API. Pointing the screen at the running IIS API would write invoices to the live catalog. No second site was created, and IIS was not changed. Draft Save, Confirm, collector price reload, doctor price reload, the report screen, Summary/Detail selection, and the Excel download click were not performed in a browser.

Source of the price behavior, not runtime proof: Referral Doctor `(change)` calls `onReferralDoctorChange()`, which calls `onRateContextChange()` and reloads line rates. Sample Collector has no change handler.

## 4. Payout regression results

All of these ran only against `ZoryaLMS_GapQA`.

| Scenario | Result |
|---|---|
| Duplicate covering rates on Preview and Confirm | PASS. New test `Payout_Duplicate_Rate_Preview_And_Confirm_Fail_Closed`. Both throw “More than one commission rate”. The payout count for that doctor stays unchanged. |
| Missing rate on Preview and Confirm | PASS. `Payout_Missing_Rate_Fails_Closed_And_Does_Not_Insert`. |
| First From date is the implementation-month start, and an empty confirm does not insert | PASS. `Payout_First_From_Is_Fixed_And_Preview_Does_Not_Insert`. |
| Carry-forward uses the rate for the original invoice date | PASS. `Payout_Confirm_Carry_Forward_Splits_Rate_And_Reprint_Stays_Stored`. The carried invoice stays at 10% / 100 after the open slice is changed to 12%. |
| Confirmation stores invoice-level details | PASS in that test. Memo details keep sale invoice id, percent, and amount. |
| Rate split and lock keep the historical slice | PASS in that test. A locked slice ends on the payout To date, and the next open slice starts the next day. |
| Stored reprint does not follow a later rate edit | PASS in that test. After the open slice is changed to 15%, `GetMemo` still returns 10% / 100 and 12% / 120. |

## 5. Blocked tests and reasons

| Item | Status | Reason |
|---|---|---|
| Browser Draft Save and Confirm | BLOCKED | The local production bundle calls `/lis` on IIS, which uses `ZoryaLMS`. |
| Collector change does not reload test prices | BLOCKED | Same browser limit. The collector select has no change handler in source. |
| Doctor change reloads test prices | BLOCKED | Same browser limit. The doctor select calls `onRateContextChange()`. |
| Report screen, Summary/Detail choice, and Excel download click | BLOCKED | Same browser limit. The export function itself was executed outside the browser. |
| Authenticated HTTP calls as Doctor, Receptionist, or Technician | BLOCKED | No isolated user session. Menu-seed SQL for Administrator still passed on `ZoryaLMS_GapQA`. |

## 6. Source and build identity

| Item | Value |
|---|---|
| Branch | `sougata` |
| HEAD | `310d23e195c01ed36c824f5ae02a849d4738249b` (2026-09-27, implementation month). P1/P2 are uncommitted. |
| `LIS.BusinessLogic.dll` | `C81EAF5734A91863A69E05FBAD33224EAB180FE032601782E0235BB2CAC496EC` |
| `LIS.DtoModel.dll` | `7828BA4CDFF62FC4DD45B978A9250EB8657FACCAA2BDDA4294E5664A672D8378` |
| `Lis.Api.dll` | `4B62CE69192A478E801F59F8AA2997D2C5459FBE5B03A380172ACA152A770EC2` |
| Local portal bundle | `main-es2015.cee60f291f48943f3c3f.js` |
| IIS portal | `main-es2015.80521cd0c21a24afcc56.js` |
| IIS API DLLs | Still the 2026-08-05 binaries from the prior acceptance report. They were not replaced. |

Files that belong in a later P1/P2 release:

- `LIS.Businesslogic/SaleInvoiceManager.cs`
- `LIS.Businesslogic/CommissionCalculationService.cs`
- `LIS.Businesslogic/CommissionReportService.cs`
- `LIS.DtoModel/Models/CommissionCalculation.cs`
- `LIS.DtoModel/Models/CommissionReport.cs`
- `web/Lis.Web/src/app/masters/sale-invoice/sale-invoice-form.component.ts`
- `web/Lis.Web/src/app/masters/sale-invoice/sale-invoice-form.component.html`
- `web/Lis.Web/src/app/_services/report.service.ts`
- `web/Lis.Web/src/app/reports/commission-report/commission-report.component.ts`
- `web/Lis.Web/src/app/reports/commission-report/commission-report.component.html`
- `web/Lis.Web/src/app/reports/report-excel-export.util.ts`
- Test updates in `InvoiceCommissionRateValidationTests.cs` and `CommissionStages4To8Tests.cs` (not deployed)

No migration file is in the working tree. `SaleInvoice` has no commission amount column. A deployment does not need a database migration.

## 7. Release preparation findings

`Commission:ImplementationMonth` is `2026-09` in both source `web/Lis.Api/Web.config` and the running API config. This work did not change either value.

Gate 6, `ReportTemplate:UseDeclarativeRendererForProductionPrint`, is `false` in source `Web.config` and `true` in `I:\Projects\PROD\AVILIS\API\Web.config`. Copying the source `Web.config` onto the running API would turn Gate 6 off. A later package must deploy the compiled API binaries and the portal bundle without replacing that running key.

Proposed checklist for a separately authorized deployment:

1. Commit the P1/P2 files listed above and record that commit.
2. Build Release API and the Angular production bundle. Record the DLL hashes and `main-es2015` name.
3. Deploy `Lis.Api.dll`, `LIS.BusinessLogic.dll`, `LIS.DtoModel.dll`, and their dependent assemblies into the API bin. Do not copy source `Web.config` over the running file.
4. Confirm the running keys stay `Commission:ImplementationMonth=2026-09` and `ReportTemplate:UseDeclarativeRendererForProductionPrint=true`.
5. Deploy the portal bundle beside the current IIS site only under that separate authorization. Do not recycle IIS as part of this verification.
6. Do not run `Update-Database` against `ZoryaLMS`.
7. After deployment, repeat one collector-only confirm and one missing-rate report on disposable data, not on operational doctors.

This checklist was not executed.

## 8. Updated acceptance matrix

| ID | Criterion | Status |
|---|---|---|
| A1 | Draft and Confirm succeed when the doctor has no applicable rate | PASS |
| A2 | Draft succeeds when the collector has no applicable rate | PASS |
| A3 | Confirm succeeds when only the collector has no applicable rate | PASS |
| A4 | Both recipients are stored when neither has a rate | PASS |
| A5 | Unknown collector, newly selected inactive collector, and cancelled-invoice edit stay rejected | PASS |
| A6 | Tested invoice gross, tax, net, and paid amounts stay on the existing invoice math | PASS |
| A7 | Doctor and collector test-price reload in the browser | BLOCKED |
| A8 | No commission amount is stored on the invoice | PASS |
| B1 | Rated invoices are processed when another eligible invoice has no rate | PASS |
| B2 | Exclusions show invoice number, date, recipient, and reason | PASS |
| B3 | Exclusions are absent from rated lines and totals | PASS |
| B4 | A valid 0% rate remains a rated line | PASS |
| B5 | An inactive rate is not applied | PASS |
| B6 | Duplicate covering rates fail the report | PASS |
| B7 | Summary and Detail stay mutually exclusive and reconcile on rated lines | PASS |
| B8 | Date range and implementation-month eligibility stay in force | PASS |
| B9 | Excel export function keeps Summary or Detail and adds Missing Rates without a rate or amount | PASS |
| B10 | Excel download click in the browser | BLOCKED |
| C1 | Payout Preview fails closed on a missing rate and inserts nothing | PASS |
| C2 | Payout Confirm fails closed on a missing rate and leaves no payout row | PASS |
| C3 | Duplicate rates fail payout Preview and Confirm | PASS |
| C4 | First From date and preview-does-not-insert | PASS |
| C5 | Carry-forward uses the original invoice-date rate | PASS |
| C6 | Confirmation stores stable invoice-level details | PASS |
| C7 | Rate split and lock preserve the historical slice | PASS |
| C8 | Stored reprint stays on persisted details after a later rate edit | PASS |
| D1 | Fresh isolated catalog, not the accumulated copy and not the IIS catalog | PASS |
| D2 | Browser session against an isolated API | BLOCKED |
| D3 | Local build identity matches pre-production IIS | FAIL |
| D4 | `Commission:ImplementationMonth` remains `2026-09` | PASS |
| D5 | No migration required | PASS |
| D6 | Running Gate 6 would survive a config-file copy of source `Web.config` | FAIL until the package excludes that file |

## 9. Remaining risks

- Pre-production still serves the older API binaries and `main-es2015.80521cd0c21a24afcc56.js`.
- P1/P2 are uncommitted on `sougata`.
- The invoice and report screens were not clicked. Collector and doctor price reload are confirmed in markup only.
- The Excel result is the workbook XML from the export function, not an Excel application open from the portal.
- Source Gate 6 is `false` and the running API Gate 6 is `true`.
- `App.config` still names `ZoryaLMS`. A rebuild that replaces the test-host config would point the suite at the live catalog. The host file currently names `ZoryaLMS_GapQA` and must be checked again before any later run.
- `ZoryaLMS_GapQA` now contains the disposable payouts and locks from this run. It is no longer an untouched restore.

## 10. Recommendation

Server-side acceptance for P1, P2, and the payout fail-closed, carry-forward, lock, and stored-reprint rules passed on a fresh catalog. That is enough to prepare a deployment package. It is not enough to approve pre-production deployment.

Keep the deployment decision separate until the package is built from a recorded commit, the running `Web.config` is left in place, and the blocked browser checks run against an API that is not the live `ZoryaLMS` catalog.

## 11. Confirmation

This work did not change `SaleInvoiceManager`, `CommissionCalculationService`, `CommissionReportService`, `CommissionPayoutManager`, RBAC, source `Web.config`, the running API `Web.config`, or IIS files. It did not deploy, restart IIS, or run a migration.

The live catalog stayed at 2247 invoices, 2 payouts, and 8 locked doctor rates. `ZoryaLMS_P1QA` stayed at 2339 invoices, 4 payouts, and 11 locked doctor rates. Writes from this run landed on `ZoryaLMS_GapQA` only.

The test-host output config was changed explicitly from `ZoryaLMS_P1QA` to `ZoryaLMS_GapQA` after the build and was verified before `dotnet vstest`.
