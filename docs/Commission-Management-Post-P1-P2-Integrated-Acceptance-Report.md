# Commission Management — Post-P1/P2 Integrated Acceptance Report

**Date:** 2026-10-11  
**Mode:** Verification only. No source, configuration, schema, permission, or deployment edit was made.  
**Reviewed:** P1 invoice-save report, P2 missing-rate report, and the post-QA triage plan.

## 1. Executive summary

P1 and P2 are present together in the uncommitted working tree. On this run, draft and confirm saves succeed without an applicable rate, commission reports keep rated invoices and list missing-rate invoices separately, and payout preview and confirm still throw when an eligible invoice has no rate and leave no payout row.

That evidence comes from 35 automated tests on the accumulated disposable catalog `ZoryaLMS_P1QA`. It is not a clean baseline. A fresh copy was not created, because pointing the test host at a new database would require editing configuration.

Payout confirmation that inserts a memo, splits a rate, or locks a rate was not run. Excel output and the sale-invoice and report screens were not exercised in a browser. The pre-production IIS binaries and portal bundle do not contain P1 or P2.

**Acceptance result: not ready for a pre-production deployment decision.**

## 2. Environment and build identity

| Item | Value |
|---|---|
| Git HEAD | `310d23e195c01ed36c824f5ae02a849d4738249b` (2026-09-27, “Update commission implementation month in configuration to a specific date”) |
| P1/P2 code | Uncommitted modifications on top of that commit. HEAD itself does not contain the invoice-save or missing-rate report changes. |
| Source `Commission:ImplementationMonth` | `2026-09` in `web/Lis.Api/Web.config` |
| Running API `Commission:ImplementationMonth` | `2026-09` in `I:\Projects\PROD\AVILIS\API\Web.config` |
| Source Gate 6 (`ReportTemplate:UseDeclarativeRendererForProductionPrint`) | `false` |
| Running API Gate 6 | `true` |
| Live catalog | `Server=.\SQLEXPRESS;Database=ZoryaLMS` |
| Test host catalog | `Database=ZoryaLMS_P1QA` in `LIS.Masters.Tests\bin\Release\LIS.Masters.Tests.dll.config` |
| Source `App.config` | Still `Database=ZoryaLMS`. The test process does not use that file while the output config names the copy. |
| Local API build | Release, exit code 0. Outputs were already current, so hashes match the assembly the tests loaded. |
| Local `LIS.BusinessLogic.dll` | `C81EAF5734A91863A69E05FBAD33224EAB180FE032601782E0235BB2CAC496EC` (2026-10-10 14:57:36). Contains the missing-rate message. `RequireCommissionRate` is absent. |
| Local `LIS.DtoModel.dll` | `7828BA4CDFF62FC4DD45B978A9250EB8657FACCAA2BDDA4294E5664A672D8378`. Contains `ReportMissingRatesAsExclusions`. |
| Local `Lis.Api.dll` | `4B62CE69192A478E801F59F8AA2997D2C5459FBE5B03A380172ACA152A770EC2` |
| Pre-production `I:\Projects\PROD\AVILIS\API\LIS.BusinessLogic.dll` | `A1A170C08F166AD63CA0C71D9FA0EFB85FA11ED124FB3CAC99DFD9FE67F17E30` (2026-08-05 14:58:44) |
| Pre-production `LIS.DtoModel.dll` | `87715BA954FDC08E7F705E963D1594549DED9F7EFFBDCD5591346212E38803B3`. Does not contain `ReportMissingRatesAsExclusions`. |
| Pre-production `Lis.Api.dll` | `B33029562BBD328E44FFAA2E215038601DA0A4B2F92BD837812FB8CB76DAE541` |
| Local portal bundle | `main-es2015.cee60f291f48943f3c3f.js`, build hash `ef481fc7e3df90659494` |
| Pre-production portal | `I:\Projects\PROD\AVILIS\PORTAL\index.html` loads `main-es2015.80521cd0c21a24afcc56.js` |

No migration file is part of the P1/P2 working tree. P1 and P2 do not require a schema change. `SaleInvoice` still has no commission amount property.

## 3. Test database safety

`ZoryaLMS_P1QA` already existed from the earlier P1 copy and the later P2 writes. It was not rebuilt. Before this run it held 2295 invoices, 4 payouts, and 11 locked doctor rates. That is not the clean copy baseline (2247 invoices, 2 payouts, 8 locked doctor rates).

The connection string was read from `LIS.Masters.Tests.dll.config` immediately before `dotnet vstest`. It named `ZoryaLMS_P1QA` and did not name `Database=ZoryaLMS`. Entity Framework uses `name=DefaultConnection` from that test-host config.

| Catalog | When | Invoices | Payouts | Locked doctor rates |
|---|---|---|---|---|
| `ZoryaLMS` | Before and after | 2247 | 2 | 8 |
| `ZoryaLMS_P1QA` | Before | 2295 | 4 | 11 |
| `ZoryaLMS_P1QA` | After | 2339 | 4 | 11 |

The live catalog did not change. The copy gained 44 invoices from the disposable test data. Payout count and locked doctor-rate count on the copy stayed the same. After the API build, the test-host config still named `ZoryaLMS_P1QA`.

These tests were not executed, because they confirm a payout or lock a rate:

- `Payout_Confirm_Carry_Forward_Splits_Rate_And_Reprint_Stays_Stored`
- `Locked_Rate_Still_Calculates`

The P2 report records an earlier pass of the confirm test on this same accumulated copy. That earlier pass is not treated as a clean-baseline acceptance result.

## 4. P1 results

Runtime on `ZoryaLMS_P1QA`, from `InvoiceCommissionRateValidationTests` and `SampleCollectorMasterTests` (all passed in the 35-test run):

| Check | Result |
|---|---|
| Draft and Confirm when the doctor has no applicable rate | PASS. `Draft_And_Confirm_Succeed_When_Doctor_Has_No_Rate` |
| Draft when the collector has no applicable rate and no doctor | PASS. `Draft_Succeeds_When_Collector_Has_No_Rate` |
| Confirm when only the collector is selected and has no rate | NOT RUN. No test confirms that collector-only case. Confirm with both recipients and no rates did pass. |
| Both recipients stored when neither has a rate | PASS. `Confirm_Succeeds_When_Both_Have_No_Rate` |
| Inactive rate or a rate that starts after the invoice date | PASS. Those saves succeed. |
| Update can add a doctor or collector without a rate | PASS |
| Unknown collector and a newly selected inactive collector | PASS. Still rejected. |
| Cancelled invoice update | PASS. Still rejected. |
| Totals on the tested invoices | PASS. Assertions keep gross 1000, discount 0, tax 0, net 1000, paid 0. |
| No commission amount stored | PASS. The invoice type has no public property whose name contains `Commission`. |
| Collector change does not reload test prices, and doctor change still does | NOT RUN in a browser. |
| Broader sale-invoice, tax, payment, and pricing suites | NOT RUN |

`RequireCommissionRate` is absent from the local `LIS.BusinessLogic.dll` that the tests loaded. That binary fact matches the passing save tests. It is not a statement about the IIS binary.

## 5. P2 results

Runtime on the same copy, from the report tests inside `CommissionStages4To8Tests` (passed; the confirm/lock test was excluded):

| Check | Result |
|---|---|
| Rated invoices are returned when other eligible invoices have no rate | PASS. `Report_Excludes_Missing_Rates_And_Keeps_Rated_Invoices` (two rated, two excluded) |
| Exclusion lists invoice number, date, recipient, and reason | PASS in that test |
| Exclusions are outside Summary, Detail, and totals | PASS. Summary total matches the rated detail total. |
| A stored 0% rate stays a rated line | PASS. `Report_Zero_Percent_Rate_Is_Not_A_Missing_Rate` |
| An inactive rate is not applied | PASS. The report excludes it. Direct `Calculate` without the report flag still throws. |
| Duplicate covering rates fail the report | PASS. `Report_Duplicate_Rate_And_Invalid_Range_Still_Fail_Closed` |
| An invalid From/To range still throws | PASS in that same test |
| Summary and Detail stay mutually exclusive | PASS. `Report_Summary_And_Detail_Are_Exclusive` |
| A range longer than two months is allowed | PASS |
| An invoice before the implementation month is omitted and is not a missing-rate row | PASS. `Report_Invoice_Before_Implementation_Month_Is_Not_A_Missing_Rate_Row` |
| Excel first sheet keeps Summary or Detail, and exclusions go to a Missing Rates sheet with no rate or amount | NOT RUN. No SpreadsheetML file was generated or opened. |
| Report screen in the browser | NOT RUN |
| Report HTTP call against the running IIS API | NOT RUN. That API’s `LIS.DtoModel.dll` does not contain the report flag. |

## 6. Payout fail-closed verification

| Check | Result |
|---|---|
| Preview throws when an eligible invoice has no rate, and no payout row is inserted | PASS. `Payout_Missing_Rate_Fails_Closed_And_Does_Not_Insert` |
| Confirm throws on that condition and leaves the payout count unchanged | PASS in that same test. `Confirm` opens the transaction before `EligibleLines`, and `EligibleLines` calls `Calculate` with the report flag left false. The test observed no committed payout. |
| Duplicate covering rates fail `Calculate` and the report | PASS |
| Duplicate covering rates invoked through payout Preview or Confirm | NOT RUN |
| First payout From date is the implementation-month start, and Preview does not insert | PASS. `Payout_First_From_Is_Fixed_And_Preview_Does_Not_Insert` |
| Carry-forward, rate split, locking, and stored reprint | NOT RUN in this verification |
| Payout tests against `ZoryaLMS` | Not run |

`CommissionPayoutManager` does not set `ReportMissingRatesAsExclusions`. `GetMemo` still reads stored details and does not call `Calculate`. Those are source facts. They are not a substitute for the unrun confirm test.

## 7. Regression results

`dotnet vstest` filter:

`InvoiceCommissionRateValidationTests`  
`SampleCollectorMasterTests`  
`CommissionCalculationTests` except `Locked_Rate_Still_Calculates`  
`CommissionStages4To8Tests` except `Payout_Confirm_Carry_Forward_Splits_Rate_And_Reprint_Stays_Stored`

**Passed 35, Failed 0, Skipped 0.** Duration 2 minutes 14 seconds. Exit code 0.

Included calculation regressions that passed: missing or invalid implementation month, paid confirmed eligibility, draft/partial/unpaid/cancelled omission, doctor precedence, net-minus-tax base, per-invoice rounding, invoices outside the period, inactive and duplicate coverage on direct `Calculate`, and administrator-only commission menu rows on the copy (read-only SQL).

Not run: `Locked_Rate_Still_Calculates`, the carry-forward confirm test, the wider sale-invoice regression classes, authenticated report or payout HTTP calls, and any browser session.

## 8. Build results

| Build | Result |
|---|---|
| `Lis.Api` Release via MSBuild | Exit code 0. Projects reported built. The three commission-related DLL hashes and timestamps stayed at the 2026-10-10 14:57 binaries the tests had just executed, so this was an up-to-date build of that same output. |
| `ng build --prod --configuration=production` | Exit code 0. About 45 seconds. Hash `ef481fc7e3df90659494`. Bundle `main-es2015.cee60f291f48943f3c3f.js`. Existing budget and CommonJS warnings only. |

Neither output was copied to IIS. IIS was not restarted.

A successful build is not runtime proof of the screens. The runtime proof is the 35 tests above, limited to what those tests assert.

## 9. Blocked and unverified scenarios

| Item | Status | Why |
|---|---|---|
| Fresh disposable database | BLOCKED | Creating one and retargeting the test host would change configuration. `ZoryaLMS_P1QA` remains an accumulated copy. |
| Collector-only Confirm with no rate | NOT RUN | Draft for that shape passed. Confirm passed when both recipients had no rate. |
| Doctor and collector test-price reload in the UI | NOT RUN | No browser session. |
| Excel Missing Rates sheet | NOT RUN | No generated workbook was opened. |
| Report and invoice screens on the new portal bundle | NOT RUN | Bundle was built and not deployed. |
| Payout carry-forward, split, lock, and stored reprint | NOT RUN | Those tests persist a payout and lock rates. No isolated confirm strategy was approved for this run. |
| Duplicate rate through payout Preview or Confirm | NOT RUN | Covered on `Calculate` and on the report service only. |
| Locked rate still calculates | NOT RUN | The test calls `ApplyClosedBoundary`. |
| Behavior of the binaries currently in IIS | NOT RUN as P1/P2 acceptance | Those DLLs and the portal bundle are different from this working tree. |
| RBAC tokens for Doctor, Receptionist, and Technician | NOT RUN | The copy’s administrator seed rows were read. No user session was opened. |

## 10. Final acceptance matrix

| ID | Criterion | Status |
|---|---|---|
| A1 | Draft and Confirm succeed when the doctor has no applicable rate | PASS |
| A2 | Draft succeeds when the collector has no applicable rate | PASS |
| A3 | Confirm succeeds when only the collector has no applicable rate | NOT RUN |
| A4 | Both recipients are stored when neither has a rate | PASS |
| A5 | Unknown collector, newly selected inactive collector, and cancelled-invoice edit stay rejected | PASS |
| A6 | Tested invoice gross, tax, net, and paid amounts stay on the existing invoice math | PASS |
| A7 | Broader tax, payment, and test-pricing behavior | NOT RUN |
| A8 | No commission amount is stored on the invoice | PASS |
| B1 | Rated invoices are processed when another eligible invoice has no rate | PASS |
| B2 | Exclusions show invoice number, date, recipient, and reason | PASS |
| B3 | Exclusions are absent from rated lines and totals | PASS |
| B4 | A valid 0% rate remains a rated line | PASS |
| B5 | An inactive rate is not applied | PASS |
| B6 | Duplicate covering rates fail the report | PASS |
| B7 | Summary and Detail stay mutually exclusive and reconcile on rated lines | PASS |
| B8 | Date range and implementation-month eligibility stay in force | PASS |
| B9 | Excel keeps the selected format and adds a Missing Rates sheet without a fabricated amount | NOT RUN |
| C1 | Payout Preview fails closed on a missing rate and inserts nothing | PASS |
| C2 | Payout Confirm fails closed on a missing rate and leaves no payout row | PASS |
| C3 | Duplicate rates fail payout Preview or Confirm | NOT RUN |
| C4 | First From date and preview-does-not-insert | PASS |
| C5 | Carry-forward, rate split, locking, and stored reprint | NOT RUN |
| D1 | Safe regression set on the disposable catalog | PASS (35, on an accumulated copy) |
| D2 | Backend and Angular production builds | PASS |
| D3 | Local build identity matches pre-production IIS | FAIL |
| D4 | `Commission:ImplementationMonth` remains `2026-09` on source and the running API | PASS |
| D5 | P1/P2 need no migration | PASS |
| D6 | Clean-baseline database | BLOCKED |

## 11. Deployment recommendation

Do not treat this working tree as cleared for pre-production deployment.

The safe automated checks for the approved P1 save rule and the approved P2 report rule passed, and payout still fails closed on a missing rate without inserting a row. The release is still short of a deployment decision because:

- Pre-production is serving different API binaries and portal bundle `main-es2015.80521cd0c21a24afcc56.js`.
- P1 and P2 are uncommitted changes on top of `310d23e`.
- Acceptance ran on an accumulated copy, not a fresh catalog.
- Carry-forward, locking, and stored reprint were not re-run.
- Excel and the browser flows were not opened.
- Source Gate 6 is `false` and the running API Gate 6 is `true`. A later copy of source `Web.config` over the running file would change production print behavior. This verification did not change either file.

A later deployment decision needs its own authorization, a package that does not overwrite the running Gate 6 value, and the gaps in section 9 closed on an isolated database.

## 12. Confirmation

This verification did not modify source, tests, `Web.config`, `App.config`, schema, permissions, or IIS files. It did not deploy, restart IIS, run a migration, or run write-capable tests against `ZoryaLMS`.

The authorized tests did insert invoices into `ZoryaLMS_P1QA`. They did not add a payout or a locked doctor rate on that copy during this run.
