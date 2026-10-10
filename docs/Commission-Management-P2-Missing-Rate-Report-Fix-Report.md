# Commission Management — P2 Missing-Rate Report Fix Report

**Date:** 2026-10-11  
**Scope:** Report-only handling of an eligible invoice with no single applicable active commission rate.  
**Deployment:** not performed. No further phase was started.

## 1. Report-only design

`CommissionCalculationService.Calculate` remains the shared calculator for reports and payout. The request now carries `ReportMissingRatesAsExclusions`, which defaults to false.

- Commission reports set the flag to true. When an eligible invoice has zero covering active rates, the calculator records a `CommissionRateExclusion` and continues with the other invoices. The exclusion has invoice number, invoice date, recipient, and a reason. It has no commission percent and no amount.
- Payout Preview and Confirm leave the flag false. `CommissionPayoutManager.EligibleLines` still calls `Calculate` for the current period and for carry-forward without the flag. A missing rate still throws `InvalidOperationException`, and Confirm still rolls back.
- More than one covering rate always throws `InvalidOperationException` ("More than one commission rate covers invoice …"), with or without the report flag. That case is not written as an exclusion.
- Only `matchCount == 0` under the report flag becomes an exclusion. Database failures, invalid ranges, and other calculation exceptions are not caught and continued.
- Invoices before the implementation month never enter the eligible set, so they are neither rated lines nor missing-rate exclusions.
- An inactive rate is not loaded. On a report it is a missing-rate exclusion. On direct calculation and payout it still throws.
- A stored zero-percent rate is one match. It becomes a normal line with percent 0 and amount 0, and it is not an exclusion.
- Totals are the sum of rated line amounts only. Nothing is written back to `SaleInvoice`.

## 2. Files changed

| File | Change |
|---|---|
| `LIS.DtoModel/Models/CommissionCalculation.cs` | Added `ReportMissingRatesAsExclusions`, `CommissionCalculationResult.Exclusions`, and `CommissionRateExclusion`. |
| `LIS.DtoModel/Models/CommissionReport.cs` | `CommissionReportPage.ExcludedInvoices` is returned for both Summary and Detail. |
| `LIS.Businesslogic/CommissionCalculationService.cs` | Missing rate excludes only when the report flag is set. Duplicate coverage still throws. |
| `LIS.Businesslogic/CommissionReportService.cs` | Sets the report flag. Summary groups rated lines only. Exclusions are returned in full and are not paged into the rated grid. |
| `web/Lis.Web/src/app/_services/report.service.ts` | Maps `excludedInvoices` separately from rated Summary or Detail rows. |
| `web/Lis.Web/src/app/reports/report-excel-export.util.ts` | Optional second worksheet. Callers that omit it are unchanged. |
| `web/Lis.Web/src/app/reports/commission-report/commission-report.component.ts` | Stores exclusions, shows them on export even when rated rows are empty, and adds a Missing Rates sheet. |
| `web/Lis.Web/src/app/reports/commission-report/commission-report.component.html` | Separate Missing Rate / Excluded Invoices section. |
| `LIS.Masters.Tests/Masters/CommissionStages4To8Tests.cs` | Report exclusion cases and payout fail-closed case. |

`OperationalReportsController` was not changed. It already returns `CommissionReportPage` and maps `InvalidOperationException` and `ArgumentException` to HTTP 400, so a duplicate rate still fails the request.

Not changed: `SaleInvoiceManager`, `CommissionPayoutManager` (the flag is simply left unset), rate managers, payout scheduling, carry-forward, locking, confirmation, stored reprint, RBAC, menus, `Web.config`, migrations, and schema.

## 3. API, UI, and Excel

`GET api/Reports/DoctorCommission` and `CollectorCommission` return the existing page plus `excludedInvoices`.

- Summary sets `detail` to null. Detail sets `summary` to null. `totalRecord` and `totalAmount` count rated invoices only.
- Each exclusion includes invoice number, invoice date, recipient name, and reason text: "No commission rate is defined for the selected referral doctor|sample collector on this invoice date."
- The report page shows those rows under **Missing Rate / Excluded Invoices**, after the rated Summary or Detail table. The section states that the invoices are eligible, are omitted from the totals, and are not given a zero rate.
- Excel keeps the selected format on the first sheet (Summary: Recipient, Invoices, Amount; Detail: Invoice No, Invoice Date, Recipient, Commission Base, Rate, Amount). Exclusions go on a second sheet named **Missing Rates** with Invoice No, Invoice Date, Recipient, and Reason. That sheet has no rate and no amount.

## 4. Tests run and results

Host config checked immediately before execution: `LIS.Masters.Tests/bin/Release/LIS.Masters.Tests.dll.config` contained `Database=ZoryaLMS_P1QA` and did not contain `Database=ZoryaLMS`.

`dotnet vstest` filter `FullyQualifiedName~CommissionCalculationTests|FullyQualifiedName~CommissionStages4To8Tests`:

**Passed 20, Failed 0, Skipped 0.** Duration 1 minute 29 seconds. Exit code 0.

New report and payout cases:

- `Report_Excludes_Missing_Rates_And_Keeps_Rated_Invoices` — two rated invoices stay in Summary and Detail; two unrated eligible invoices are exclusions only; totals match the rated invoices; the exclusion type has no amount or percent property.
- `Report_Zero_Percent_Rate_Is_Not_A_Missing_Rate`
- `Report_Inactive_Rate_Is_Excluded_And_Direct_Calculation_Still_Fails`
- `Report_Invoice_Before_Implementation_Month_Is_Not_A_Missing_Rate_Row`
- `Report_Duplicate_Rate_And_Invalid_Range_Still_Fail_Closed`
- `Payout_Missing_Rate_Fails_Closed_And_Does_Not_Insert` — Preview and Confirm throw, and the payout list count for that doctor is unchanged.

Existing calculation and payout cases that still passed:

- `Missing_And_Invalid_ImplementationMonth_Fail_Closed`
- `Eligible_Confirmed_And_InvoiceStatus_Paid_Are_Calculated`
- `Draft_Partial_Unpaid_And_Cancelled_Are_Omitted`
- `Doctor_Precedence_Skips_Collector_When_Both_Are_Selected`
- `Base_Is_Net_Minus_Tax_After_Header_Discount`
- `Rounding_Is_Per_Invoice_Then_Summed`
- `Before_Implementation_Month_And_Outside_Period_Are_Omitted`
- `Inactive_Rate_And_Duplicate_Coverage_Fail_Closed`
- `Locked_Rate_Still_Calculates`
- `Report_Summary_And_Detail_Are_Exclusive`
- `Report_Allows_A_Range_Longer_Than_Two_Months`
- `Payout_First_From_Is_Fixed_And_Preview_Does_Not_Insert`
- `Payout_Confirm_Carry_Forward_Splits_Rate_And_Reprint_Stays_Stored`
- `Authorization_Seeds_Administrator_Only`

## 5. Build results

| Build | Result |
|---|---|
| Backend | The Release `LIS.Masters.Tests` assembly that executed the suite above compiled with the P2 changes and passed. |
| Frontend (`ng build --prod --configuration=production`) | Succeeded in 81 seconds. Hash `ef481fc7e3df90659494`. Bundle `main-es2015.cee60f291f48943f3c3f.js`. Existing budget and CommonJS warnings only. Not deployed. |

## 6. Database evidence

`ZoryaLMS_P1QA` on `.\SQLEXPRESS` was already present from the P1 disposable copy and was reused. No migration was run.

| Catalog | When | Invoices | Payouts | Locked doctor rates |
|---|---|---|---|---|
| `ZoryaLMS` | Before tests | 2247 | 2 | 8 |
| `ZoryaLMS` | After tests | 2247 | 2 | 8 |
| `ZoryaLMS_P1QA` | Before tests | 2261 | 2 | (not recounted before this run; P1 left it at 8) |
| `ZoryaLMS_P1QA` | After tests | 2295 | 4 | 11 |

The live catalog counts did not change. The copy gained 34 invoices and 2 payouts from the disposable doctors used by the calculation and payout tests, including the retained carry-forward confirm. Locked doctor rates on the copy ended at 11.

Source `LIS.Masters.Tests/App.config` and both source and running API `Web.config` files still name `Database=ZoryaLMS`. Only the test output config used for this run names `ZoryaLMS_P1QA`. A later rebuild that copies `App.config` over that output file would point the suite at the live catalog. Do not run these tests until that output file is checked again.

## 7. Tests not run

- No automated assertion opened the generated SpreadsheetML file. The second sheet is implemented in the export utility and covered by the production frontend build.
- The report screen was not clicked in a browser, and the new bundle was not deployed to IIS.
- No synthetic database-exception test was added. The calculator does not catch those exceptions; only zero matches under the report flag are exclusions.

## 8. Untouched behavior

Invoice save from P1 is unchanged. Payout period rules, carry-forward, locking, confirmation, and stored reprint were not modified; their existing tests passed on the disposable database. RBAC, menu permissions, rate maintenance, configuration, schema, and deployment were not changed.
