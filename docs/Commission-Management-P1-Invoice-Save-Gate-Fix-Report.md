# Commission Management — P1 Invoice Save Gate Fix Report

**Date:** 2026-10-11  
**Scope:** P1 only. Invoice generation succeeds when no applicable commission rate exists.  
**P2, payout, RBAC, schema, configuration, and deployment:** not changed.

## 1. Summary

Draft Save and Confirm no longer require a covering commission rate. The server method `RequireCommissionRate` was removed from insert and update. The sale-invoice form no longer blocks submit, and it no longer tells the user to define a rate or remove the doctor or collector.

Save still validates the collector master, recalculates totals, normalizes the invoice date, and rejects edits to a cancelled invoice. It does not calculate commission and does not store a commission amount. `SaleInvoice` has no commission property.

A missing rate still fails closed inside commission calculation and reports. That path was not changed.

## 2. Files changed

| File | Change |
|---|---|
| `LIS.Businesslogic/SaleInvoiceManager.cs` | Removed `RequireCommissionRate` and both calls. Removed the unused doctor and collector rate repositories from this manager. |
| `web/Lis.Web/src/app/masters/sale-invoice/sale-invoice-form.component.ts` | Removed the submit gate, the rate-error helpers, and the rate-list load used only for that gate. Doctor change still calls `onRateContextChange`. |
| `web/Lis.Web/src/app/masters/sale-invoice/sale-invoice-form.component.html` | Removed the inline rate error. Collector change no longer calls a commission warning. |
| `LIS.Masters.Tests/Masters/InvoiceCommissionRateValidationTests.cs` | Save-without-a-rate cases now expect a persisted invoice. Cancelled-invoice edit still expects a rejection. |

## 3. Before and after

| Case | Before | After |
|---|---|---|
| Doctor selected, no rate | Draft and confirm throw | Both save. Doctor id is stored. |
| Collector selected, no doctor, no rate | Save throws | Save succeeds. Collector id is stored. |
| Both selected, no rates | Save throws on the doctor check | Save succeeds. Both ids are stored. |
| Both selected, collector rate only | Save throws because the doctor rate is missing | Save succeeds. |
| Only an inactive rate | Save throws | Save succeeds. The inactive rate is not turned into a commission amount. |
| Rate starts after the invoice date | Save throws | Save succeeds. |
| Update that adds a doctor or collector with no rate | Update throws and the attribution is not stored | Update stores the attribution. |
| Unknown collector, inactive collector master, cancelled invoice | Rejected | Still rejected. |

Doctor change still reloads test prices. Collector change does not.

## 4. Tests executed

Connection used by the test host: `Server=.\SQLEXPRESS;Database=ZoryaLMS_P1QA`.

`dotnet vstest` filter `InvoiceCommissionRateValidationTests|SampleCollectorMasterTests`:

**Passed 17, Failed 0, Skipped 0.** Duration 39 seconds.

Invoice tests:

- `Draft_And_Confirm_Succeed_When_Doctor_Has_No_Rate`
- `Draft_Succeeds_When_Collector_Has_No_Rate`
- `Confirm_Succeeds_When_Both_Have_No_Rate`
- `Confirm_Succeeds_When_Only_Collector_Rate_Exists`
- `Draft_Succeeds_When_Only_Inactive_Doctor_Rate_Exists`
- `Draft_Succeeds_When_Only_Inactive_Collector_Rate_Exists`
- `Draft_And_Confirm_Succeed_When_Rate_Starts_After_Invoice_Date`
- `Update_Can_Add_Doctor_Or_Collector_Without_A_Rate`
- `Cancelled_Invoice_Update_Is_Still_Rejected`

Collector tests kept their existing rejections for an unknown collector id and a newly selected inactive collector master, and they passed with the invoice saves.

Each successful invoice assertion checks doctor id, collector id, invoice status, gross 1000, discount 0, tax 0, net 1000, paid 0, and that `SaleInvoice` has no public property whose name contains `Commission`.

## 5. Build results

| Build | Result |
|---|---|
| Backend (`Lis.Api` and `LIS.BusinessLogic`, Release) | Succeeded. Existing warnings only. |
| Frontend (`ng build --prod --configuration=production`) | Succeeded. Existing budget warnings only. Bundle `main-es2015.25c8f04fa558637ed6c1.js`. Not deployed. |

## 6. Database and test-environment safety

| Catalog | Role | Invoices | Payouts | Locked doctor rates |
|---|---|---|---|---|
| `ZoryaLMS` before and after | Catalog in the running API `Web.config` | 2247 | 2 | 8 |
| `ZoryaLMS_P1QA` before | Copy-only backup of `ZoryaLMS` | 2247 | 2 | 8 |
| `ZoryaLMS_P1QA` after | Database the tests wrote to | 2261 | 2 | 8 |

The live catalog did not gain invoices, payouts, or locked rates. The copy gained 14 invoices from the disposable test doctors, collectors, and patients. Payout count and locked-rate count on the copy stayed the same.

`LIS.Masters.Tests/App.config` and both source and running `Web.config` files still name `ZoryaLMS`. Only `LIS.Masters.Tests/bin/Release/LIS.Masters.Tests.dll.config` was pointed at `ZoryaLMS_P1QA` for this run. A rebuild from `App.config` would put `ZoryaLMS` back into that output file. Do not run the write-capable suite against that restored output while it names the IIS catalog.

The copy database `ZoryaLMS_P1QA` was left in place on `.\SQLEXPRESS`. It was not attached to IIS.

## 7. Regression checks

- Cancelled invoice update still throws `Cancelled invoice cannot be edited`.
- Unknown collector id and a newly selected inactive collector master still fail in `SampleCollectorMasterTests`.
- Tested invoices stay at gross 1000, tax 0, net 1000, paid 0.
- No commission amount is stored. The invoice type has no commission property, and save does not call `CommissionCalculationService`.
- `CommissionCalculationService.cs`, `CommissionReportService.cs`, and `CommissionPayoutManager.cs` were not edited.

## 8. Limitations and tests not run

- Commission calculation, report, and payout tests were not run. They still expect a missing rate to fail the calculation, and the payout tests confirm payouts. Those writes were kept off both catalogs for this change.
- The broader sale-invoice regression suite was not run.
- The Angular bundle was built and not deployed. IIS still serves the previous portal bundle.
- The form was not clicked in a browser. Submit blocking was removed in source and covered by the server tests.
- A report that includes an eligible invoice with no covering rate still returns HTTP 400. That is P2 and was left as-is.

## 9. Confirmation of untouched areas

P2 report missing-rate behavior, payout preview and confirm, carry-forward, rate locking, reprint, RBAC, menu permissions, `Web.config`, `Commission:ImplementationMonth`, migrations, deployment SQL, and IIS were not changed. No payout was confirmed. The running API catalog was not written by this test run.
