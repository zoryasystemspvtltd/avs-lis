# Commission Management — Stage 3 Calculation Service Report

**Date:** 2026-09-26  
**Status:** PASS WITH LIMITATIONS  
**Scope:** Commission calculation only. Reports, payout, and invoice save do not call it.

## Eligibility

An invoice is included only when `PaymentStatus` is Paid and `InvoiceStatus` is Confirmed or Paid.

Draft, Partial, Unpaid, and Cancelled are omitted, including a Draft whose paid amount equals the net amount. Omitted invoices are not returned as zero.

## Calculation

`CommissionCalculationService.Calculate` is read-only.

- Base is `SaleInvoice.NetAmount - SaleInvoice.TaxAmount`. Line net amounts are not used. Header discount is already inside that base. Tax is not commissioned.
- Amount is `Math.Round(base * percent / 100, 2)` per invoice, the same two-decimal round used for invoice money. The total is the sum of those rounded amounts.
- A selected doctor uses the doctor rate. A collector is used only when no doctor is selected. Both selected means the doctor line only.
- The rate date is `InvoiceDate`. An active locked rate still counts. An inactive rate does not. No covering rate, or more than one covering rate, fails the calculation.
- Invoices before `Commission:ImplementationMonth` are omitted. The setting is `yyyy-MM` in Web.config. It is empty, so calculation fails closed until an operator sets it. September 2026 is not hardcoded.

Invoice save does not call this service and does not store a commission amount.

## Tests

Release build succeeded. Existing warnings only.

`CommissionCalculationTests`: 9 passed, 0 failed.

- Missing and invalid implementation month fail closed
- Confirmed and invoice-status Paid, both with payment Paid, calculate 10% of 1000 as 100
- Draft with payment Paid, Partial, Unpaid, and Cancelled are omitted
- Doctor plus collector pays the doctor only; collector-only pays the collector
- Tax is excluded from the base; a header discount reduces the base
- 12.5% of 1 rounds to 0.12 per invoice; two invoices total 0.24
- The day before the implementation month is omitted; the first day is included; a date outside From/To is omitted
- An inactive rate and two covering rates fail closed
- A locked rate still calculates
- Invoice net and paid amounts are unchanged after calculation

## Not changed

Invoice save rules, invoice totals, payment, sample collection, `CollectedBy`, reports, payout, diagnostic printing, Gate 6, and production IIS. No new table.

The Web.config key was added empty. Portal and API were not deployed.

## Stop

Stage 4 has not started.
