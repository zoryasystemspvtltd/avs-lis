# Commission Management — Final Report

**Date:** 2026-09-26  
**Status:** PASS WITH LIMITATIONS  
**Scope:** Stages 4 through 8, on top of the rate, invoice-validation, and calculation work already in place. Production IIS was not updated.

## Eligibility and money

An invoice is eligible only when payment status is Paid and invoice status is Confirmed or Paid. Draft, including a draft whose paid amount equals the net amount, Partial, Unpaid, and Cancelled are omitted. They are not shown as zero.

The base is `SaleInvoice.NetAmount - SaleInvoice.TaxAmount`. Each invoice amount is `Math.Round(base * percent / 100, 2)`. Totals sum those rounded amounts.

A selected doctor uses the doctor rate. A collector is used only when no doctor is selected. The rate date is the invoice date. An active locked rate still counts. A missing, inactive, or duplicate covering rate fails the calculation.

`Commission:ImplementationMonth` in Web.config is `yyyy-MM`. It is empty, so calculation, reports, and payout fail closed until an operator sets it. September 2026 is not hardcoded. Invoices before that month stay ineligible.

Invoice save does not calculate commission and does not store a commission amount. It still requires a covering rate before a draft or confirmed invoice with a doctor or collector can be saved.

## Stage 4 — Reports

Two report menus, both under Reports:

- Doctor Commission, `/reports/doctor-commission`
- Collector Commission, `/reports/collector-commission`

Each page asks for From and To, an optional recipient (default all of that type), and one format: Summary or Detail. The API rejects a request that does not name exactly one format, and it returns only that format.

Summary columns are Recipient, invoice count, and amount. Detail columns are Invoice No, Invoice Date, Recipient, Commission Base, Rate, and Amount. Patient name is not included.

Excel uses the existing SpreadsheetML export for the format that was requested. The two-month cap on other report pages is not applied here. The API does not cap the range either.

Direct calls use `QAuthorize` on module `Reports`, CanView. A missing rate returns an error rather than an empty total.

## Stages 5 and 6 — Payout

`CommissionPayout` and `CommissionPayoutDetail` store a confirmed payout. Preview does not insert. Confirm recalculates, then writes the header, the detail rows, and the rate split in one transaction. A failure rolls that transaction back.

The From date is not chosen by the user.

- The first payout for a recipient starts on the first day of the implementation month.
- The next From date is the day after that recipient’s previous To date.
- To date is chosen by the user and must be on or after From.

A payout with no eligible invoices can be previewed as an empty list. Confirm is rejected. An invoice is stored once per recipient. Unique indexes are `(RecipientType, RecipientId, FromDate)` on the header and `(RecipientType, RecipientId, SaleInvoiceId)` on the detail.

Carry forward includes an invoice whose date is before the current From date, on or after the implementation month, now fully paid, and not already in a payout for that recipient. The rate is the rate for the original invoice date.

Confirm calls `ApplyClosedBoundary` for that recipient through the payout To date. A crossing rate becomes a locked slice through that date and an open slice from the next day, at the same percent. Reprint reads the stored detail rows. It does not recalculate, so a later edit of the open slice does not change a printed memo.

The memo is a print view (`window.print()`), with the lab name from `Report:LabName`, the stored lines, the stored total, and a blank acknowledgement line. It is not Excel and it is not the diagnostic renderer.

Payout number shape is `CP-{yyyyMMdd}-{sequence}`.

History lists confirmed payouts and opens the stored memo.

## Stages 7 and 8 — Authorization and certification

Report menu keys `REPORT_DOCTOR_COMMISSION` and `REPORT_COLLECTOR_COMMISSION` are seeded for Administrator only, on the existing Reports module. They are not copied onto every role that can open Invoice Reports.

Payout is a separate module, `CommissionPayout`, menu `COMMISSION_PAYOUT`, route `/commission-payouts`. Administrator receives that module. Invoice users do not. Preview and history require CanView. Confirm requires CanAdd. The same module is on the API.

The role editor lists the two report menus and the payout menu. The left nav shows the report links with the other Reports entries, and payout in its own Commission section.

## Tests

Release build succeeded. Existing warnings only.

`CommissionCalculationTests` and `CommissionStages4To8Tests`: 14 passed, 0 failed.

- Missing and invalid implementation month fail closed
- Confirmed and invoice-status Paid calculate; Draft-with-Paid, Partial, Unpaid, and Cancelled do not
- Doctor precedence, tax excluded from the base, per-invoice rounding then sum
- Summary and Detail are not returned together
- A collector report does not pay an invoice that also has a doctor
- A range longer than two months is accepted
- The first From date must be the implementation-month start; preview does not insert; confirm with no eligible invoices is rejected
- Carry forward pays the earlier invoice at the original rate; the later invoice uses the open-slice rate; an invoice already paid out is not repeated
- Confirm splits and locks the closed rate
- After the open rate is changed, the stored memo amounts stay as confirmed
- Administrator has the two report menus and the payout module; other roles were not given them by this seed

## Build and database

Angular production build succeeded (`main-es2015.80521cd0c21a24afcc56.js`). Existing budget warnings only. The bundle was not deployed.

`Scripts/add-commission-payout.sql` matches migration `202609270100000_CommissionPayout`. Both are idempotent. The script was applied to `.\SQLEXPRESS` / `ZoryaLMS` so the tests could confirm payouts. Portal and API files were not copied to IIS, and no app pool was recycled.

## Limitations

- `Commission:ImplementationMonth` is empty. Reports and payout fail closed until it is set to `yyyy-MM`.
- The schema change is on the shared ZoryaLMS database used by tests. Production IIS is unchanged.
- Roles that have the Reports module and no menu overlay still see every report link, including the new ones, because that is how the existing report menu fallback works. Roles that have a menu overlay do not receive the new keys unless an administrator grants them.
- An unfiltered commission report fails if any eligible invoice in the range has no single covering rate.
- Open-ended rate overlap is still enforced inside the rate transaction, not by a unique index.
- Two confirms for the same recipient and From date are rejected by the unique index if they pass the in-memory check together.

## Not changed

Invoice totals, payment posting, sample collection, `TestRequestDetail.CollectedBy`, Collector Wise Report, diagnostic printing, and Gate 6.

## Stop

Commission stages 1 through 8 are implemented in source. Production deployment was not part of this work.
