# Commission Management — Stage 2 Invoice Rate Validation Report

**Date:** 2026-09-26  
**Status:** PASS WITH LIMITATIONS  
**Scope:** Selecting a referral doctor or sample collector requires an active commission rate that covers `SaleInvoice.InvoiceDate`. Draft and confirmed saves are both blocked when that rate is missing. Commission is not calculated.

## Rules enforced

- No doctor and no collector: no rate check. The invoice saves.
- Doctor only: an active doctor rate must cover the invoice date, or the save is rejected.
- Collector only: an active collector rate must cover the invoice date, or the save is rejected.
- Both selected: doctor precedence. Only the doctor rate is required. A missing collector rate does not block the save.
- The date used is `InvoiceDate` after the existing facility wall-clock normalization. Payment date, collection date, report date, payout date, and today are not used.
- An inactive rate does not count. A locked rate counts when it is still active and covers the date.
- The API is authoritative. The invoice form shows the same message when the rate lists have loaded, and Save Draft and Confirm both stop before the request.

Messages:

- `No commission rate is defined for the selected referral doctor on this invoice date. Define the rate or remove the referral doctor.`
- `No commission rate is defined for the selected sample collector on this invoice date. Define the rate or remove the sample collector.`

A rejected update does not keep the new doctor or collector. Removing both still saves. A cancelled invoice still fails with the existing cancelled-invoice message before this check.

## What changed

`SaleInvoiceManager.RequireCommissionRate` runs on insert and on update, after the invoice date is normalized and before the header is written. It does not change totals, paid amount, or payment status.

The sale invoice form loads `ReferralDoctorCommissionRate` and `SampleCollectorCommissionRate`. Changing the doctor still reloads test prices. Changing the collector does not. Either change, and an invoice-date change, refreshes the inline warning. If those lists have not loaded yet, the form does not block; the API still rejects the save.

Existing collector invoice tests that save with a doctor or collector now insert a covering rate first, because those saves are no longer valid without one.

No new table, column, or migration. Stage 1 rate tables are unchanged.

## Tests

Release build of the test project succeeded. Existing warnings only.

`InvoiceCommissionRateValidationTests` and `SampleCollectorMasterTests`: 12 passed, 0 failed.

- Draft and confirmed doctor saves without a rate are rejected
- Draft collector save without a rate is rejected
- 30-Sep is inside 01-Sep–30-Sep and saves; 01-Oct and 31-Aug are rejected
- Doctor plus collector without a doctor rate is rejected even when a collector rate exists
- Doctor plus collector with a doctor rate saves both ids, `NetAmount` 1000, `PaidAmount` 0
- Collector only and neither-selected drafts save
- A rejected update does not persist the doctor; a later save with a rate does; clearing both saves
- An inactive rate does not satisfy the invoice date
- Collector master create, edit, deactivate, unknown id, inactive newly selected collector, and menu permission still pass

`SaleInvoiceManagerTests`, `SaleInvoiceRegressionTests`, and `SaleInvoiceEditWorkflowTests`: 30 passed, 0 failed.

## Build

Angular production build succeeded (`main-es2015.9bb89eb763a2fdbdb231.js`). Existing budget warnings only. The bundle was not deployed.

## Not changed

Commission amounts, invoice totals, payment, sample collection, `TestRequestDetail.CollectedBy`, Collector Wise Report, diagnostic printing, Gate 6, payout, reports, and production IIS.

Draft invoices that are fully paid remain an open eligibility question for Stage 3. This stage does not use payment status. The first-payout From date remains the 1st of the implementation month and is still not implemented.

## Stop

Stage 3 has not started.
