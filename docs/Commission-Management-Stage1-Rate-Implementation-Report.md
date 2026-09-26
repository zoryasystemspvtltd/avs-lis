# Commission Management — Stage 1 Rate Implementation Report

**Date:** 2026-09-26  
**Status:** PASS WITH LIMITATIONS  
**Scope:** Commission rate foundation only. Reports, invoice validation, calculation, and payout were not started.

## Decision recorded for later payout work

The first payout From date is not user-selectable. It is always the 1st day of the Commission Implementation Month. Later payouts stay flexible, and the next From date is the day after the previous To date. That rule is not implemented in this stage.

Draft invoices that are fully paid remain an open eligibility question for Stage 3. Stage 1 does not use it.

## What was implemented

Two rate tables, separate from test pricing:

- `ReferralDoctorCommissionRate` — `ReferralDoctorId`, `CommissionPercent` decimal(5,2), `EffectiveFrom`, nullable `EffectiveTo`, `IsActive`, `IsLocked`, audit
- `SampleCollectorCommissionRate` — same shape with `CollectorId`

`IsLocked` is not on the entry form. It is set only by `ApplyClosedBoundary`, which payout confirmation will call later. A closed boundary splits a crossing rate into a locked slice through the closed date and an open slice from the next day, keeping the same percent. A locked slice cannot be edited or deactivated. An open slice can.

Validation, in the business layer:

- Percent from 0 through 100, at most 2 decimal places
- `EffectiveFrom` required
- `EffectiveTo` null means open-ended
- `EffectiveFrom` after `EffectiveTo` is rejected
- Active periods for the same recipient cannot overlap, including an open-ended period
- The next day after an end date is allowed
- Another doctor or collector can use the same dates

APIs follow the existing master controller: `api/ReferralDoctorCommissionRate` and `api/SampleCollectorCommissionRate`, module `Masters`.

Screens: `/doctor-commission-rates` and `/collector-commission-rates`, using the existing master list and form. The doctor or collector list is loaded from the existing active master lookup. A locked period disables Save.

Menus `MASTER_DOCTOR_COMMISSION_RATE` and `MASTER_COLLECTOR_COMMISSION_RATE` copy role permissions from Referral Doctor and Sample Collector. The role editor list also includes Sample Collector, which was already in the API menu catalog.

## Database

Migration `202609262330000_CommissionRateFoundation` and `Scripts/add-commission-rates.sql` match. Both are idempotent. No invoice rows were changed. No commission amounts were backfilled.

The script was applied to `.\SQLEXPRESS` / `ZoryaLMS` so the tests could insert rates. Portal and API were not copied to IIS, and no app pool was recycled.

Open-ended overlap cannot be expressed as a single unique index. The save checks overlap inside the existing transaction. Two simultaneous saves could still both pass that check. A later payout confirm should keep using one transaction around the boundary split.

## Tests

`CommissionRateTests`: 6 passed, 0 failed.

- 0%, 100%, and 10.50% with an open end
- Negative, above 100, and 3 decimal places rejected
- End before start rejected
- Overlap rejected, including two open-ended rates
- 30-Sep then 01-Oct allowed
- A second doctor and a collector can share the same dates
- An unlocked rate can be edited
- Closing 30-Sep locks 01-Sep–30-Sep and leaves 01-Oct–31-Dec open
- The locked slice cannot be edited or deactivated
- The open slice can change to 12%
- A second close through 31-Oct locks October and leaves November onward open at 12%
- Menu permissions match the parent master

Invoice, payment, and sample-collection tests were not rerun. Those managers were not changed.

## Build

Release build of the test project succeeded, including the API. Existing warnings only.

Angular production build succeeded (`main-es2015.cc30b3613c061461f970.js`). Existing budget warnings only. The bundle was not deployed.

## Not changed

Invoice save, invoice totals, payment, sample collection, `CollectedBy`, Collector Wise Report, diagnostic printing, Gate 6, and production IIS.

## Stop

Stage 2 has not started.
