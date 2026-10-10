# Commission Management — Comprehensive Pre-Production QA Report

**Date:** 2026-10-11  
**Mode:** Verification only  
**Status:** FAIL  
**Scope:** Invoice attribution, rates, calculation, reports, payout, implementation month, RBAC, and regression risk

No source, database, configuration, permission, or IIS change was made for this review. No payout was confirmed. No rate was edited. No migration was run.

---

## 1. Executive Summary

The deployed API and Portal match the current source build. The running API configuration has `Commission:ImplementationMonth` set to `2026-09`. Commission schema, unique payout indexes, and Administrator-only menu rows are present in `ZoryaLMS`.

Ordinary invoice generation does **not** match the approved rule in section 3.2 of this review. If a referral doctor is selected and no active doctor rate covers the invoice date, draft save and confirm are rejected. The same rejection applies to a collector when no doctor is selected. The deployed `LIS.BusinessLogic.dll` is byte-identical to source, and that source throws before the invoice is saved. No commission amount is invented; the invoice is blocked instead.

Calculation and reports do not store a zero or a guessed amount when a rate is missing. They reject the whole request. One eligible invoice without a single covering rate fails the report for every other invoice in that range. That behavior is visible in source that matches the deployed binary. It was not re-executed against live invoices in this pass.

Payout code matches the approved payout design: fixed From date, preview without insert, confirm in one transaction, carry-forward, rate split, and stored reprint. Those confirm and lock paths were **not** executed in this pass, because they would persist payout and rate changes. Two doctor payouts and eight locked doctor rates already exist in the database from earlier activity. This review did not add to them.

Authenticated report, payout, and role-editor scenarios are blocked. There is no isolated login for this pass, and the automated suite was not re-run because it inserts rates and confirms payouts in the same catalog the API uses.

---

## 2. Environment and Build Identity

| Item | Observed |
|---|---|
| Git branch | `sougata` |
| Commit | `310d23e195c01ed36c824f5ae02a849d4738249b` (2026-09-27 01:25:46 +0530) |
| Working tree | Clean against `origin/sougata` |
| Portal site | `AVILIS_PORTAL`, `http/*:8080`, Started. Physical path `I:\Projects\PROD\AVILIS\PORTAL` |
| API application | `AVILIS_PORTAL/lis`, pool `AVILIS_API_POOL`, physical path `I:\Projects\PROD\AVILIS\API` |
| Site `AVILIS_API` | `http/*:8081`, **Stopped**. Not started during this review |
| Database | `.\SQLEXPRESS` / `ZoryaLMS` in the running API `Web.config` |
| Portal bundle | `main-es2015.80521cd0c21a24afcc56.js` |
| Portal hash | SHA256 prefix `8609A4C89757E574`, 1,940,622 bytes, identical in source `dist/DxI800` and the deployed portal |
| `Lis.Api.dll` | SHA256 prefix `FA524EDFF84431AD`, 390,656 bytes, identical in source `web/Lis.Api/bin` and `I:\Projects\PROD\AVILIS\API\bin` |
| `LIS.BusinessLogic.dll` | SHA256 prefix `D088A7BF0586D218`, 538,112 bytes, identical |
| `LIS.DtoModel.dll` | SHA256 prefix `D1C18FCA6E9097FB`, 229,376 bytes, identical |
| Running `Commission:ImplementationMonth` | `2026-09` |
| Source `Web.config` month | `2026-09` |
| Running Gate 6 flag | `ReportTemplate:UseDeclarativeRendererForProductionPrint=true` |
| Source `Web.config` Gate 6 flag | `false` |

The running API is the `/lis` application on port 8080. `http://localhost:8080/` and `http://localhost:8080/reports/doctor-commission` returned HTTP 200. The report URL returns the portal shell; that does not prove the route guard allowed a signed-in user.

This pass did not call an authenticated report or payout API. Unauthenticated calls returned HTTP 401:

- `GET /lis/api/Reports/DoctorCommission`
- `GET /lis/api/Reports/CollectorCommission`
- `GET /lis/api/CommissionPayout/schedule?recipientType=1&recipientId=1`

---

## 3. Approved Business Rules

This review used the rules in the QA request as the baseline.

- Attribution: no recipient, doctor only, collector only, or both. Doctor precedence. `CollectedBy` is not invoice collector attribution.
- Invoice generation must remain possible when no commission rate exists. No invented commission amount. Totals, tax, payment, test pricing, sample collection, and `CollectedBy` stay unchanged.
- Rates: 0–100, at most two decimals, required From, optional To, no same-recipient overlap, invoice date selects the rate, inactive rates do not apply, locked slices keep their percent.
- Eligible invoices: payment Paid, invoice status Confirmed or Paid, on or after the implementation month, with one applicable active rate. Draft (including Draft + Paid), Partial, Unpaid, Cancelled, pre-month, and collector-only attribution when a doctor is selected are excluded.
- Amount: `Math.Round((NetAmount - TaxAmount) * percent / 100, 2)` per invoice, then sum.
- Reports: Doctor and Collector, Summary or Detail, required dates, optional recipient, no two-month cap, Excel for the requested format.
- Payout design is unchanged: preview does not save, empty confirm is rejected, From date is fixed, carry-forward uses the original invoice date, confirm locks a crossing rate, reprint uses stored rows.
- Implementation month is `yyyy-MM` on the running API.
- RBAC stays on the existing Reports and CommissionPayout modules.

---

## 4. Test Data and Safety Strategy

The API and the automated tests share `ZoryaLMS`. The test suite creates doctors, collectors, rates, invoices, and confirmed payouts in that catalog. Re-running it would persist payout and rate changes, so it was not executed.

Read-only SQL was used for schema, counts, role grants, and eligibility counts. No invoice numbers, patient names, or recipient names are included here.

Observed counts, not created by this review:

| Object | Count |
|---|---|
| Doctor commission rates / active / locked | 55 / 55 / 8 |
| Collector commission rates / active / locked | 11 / 9 / 0 |
| Sample collectors | 29 |
| Payout headers / detail rows | 2 / 3 |
| Payouts | Recipient type 1 only. From as early as 2026-09-01. To as late as 2026-09-30. Combined stored total 320.00 |
| Invoices since 2026-09-01 with payment Paid, status Confirmed or Paid, and a doctor | 28 |
| Same window, collector only | 3 |
| Draft invoices in that window with a recipient | 2 |
| Cancelled invoices in that window | 183 |

Migrations recorded for the domain context include `202609261530000_SampleCollectorMaster`, `202609262330000_CommissionRateFoundation`, `202609270100000_CommissionPayout`, and `202609270115000_CommissionSchemaModelSync`.

Historical automated tests exist and were not treated as results of this pass:

- `InvoiceCommissionRateValidationTests` asserts that draft and confirm **reject** a missing rate. That assertion conflicts with section 3.2 of this review.
- `CommissionCalculationTests`, `CommissionRateTests`, and `CommissionStages4To8Tests` cover eligibility, rounding, report format, payout preview, carry-forward, and Administrator seeding. They were last run in an earlier implementation session, not on 2026-10-11.

---

## 5. Overall Test Statistics

| Status | Count |
|---|---|
| PASS | 10 |
| FAIL | 8 |
| BLOCKED | 23 |
| NOT RUN | 79 |
| **Total** | **120** |

PASS items are read-only configuration, schema, role-seed, portal shell, and unauthenticated API checks. FAIL items are the invoice-save rejections that contradict section 3.2. BLOCKED items need a persisted payout, a rate edit, or an authenticated session. NOT RUN items match the deployed source on inspection, but this pass did not execute them.

---

## 6. Invoice Attribution and Save Behavior

`SaleInvoiceManager.RequireCommissionRate` runs on insert and on update, for draft and confirm. It does not calculate or store a commission amount.

| Selection | Current behavior |
|---|---|
| No doctor and no collector | No rate check |
| Doctor selected | Active doctor rate must cover `InvoiceDate`. Collector rate is not required. Method returns after the doctor check |
| Collector selected, no doctor | Active collector rate must cover `InvoiceDate` |
| Missing, inactive, or not-yet-effective rate | `ArgumentException` with the “No commission rate is defined…” message |

The portal `onSubmit` blocks both draft and confirm with the same messages before the API call, once rate lists have loaded. If the lists have not loaded, the client does not block; the API still does. Changing the collector calls `warnCommissionRate` only. Changing the doctor also calls `onRateContextChange`, which reloads test prices. `RequireCommissionRate` does not read or write `TestRequestDetail.CollectedBy`.

| ID | Scenario | Status | Evidence |
|---|---|---|---|
| A01 | No doctor, no collector | NOT RUN | Code skips the rate check. Save was not executed |
| A02 | Doctor only, no doctor rate | FAIL | Source throw, deployed DLL hash match. Contradicts 3.2 |
| A03 | Collector only, no collector rate | FAIL | Same |
| A04 | Both selected, no rates | FAIL | Doctor check fails first |
| A05 | Both selected, doctor rate exists, collector rate does not | NOT RUN | Code allows this. Save was not executed |
| A06 | Both selected, collector rate exists, doctor rate does not | FAIL | Doctor rate is still required |
| A07 | Doctor with only an inactive rate | FAIL | Inactive rows are excluded |
| A08 | Collector with only an inactive rate | FAIL | Same |
| A09 | Doctor rate that starts after InvoiceDate | FAIL | Date coverage is required |
| A10 | Doctor rate covering InvoiceDate | NOT RUN | Code allows. Save was not executed |
| A11 | Collector rate covering InvoiceDate | NOT RUN | Code allows. Save was not executed |
| A12 | Draft save and confirm when the required rate is missing | FAIL | Both paths call `RequireCommissionRate`; the form blocks both |
| A13 | Update that adds, removes, or changes doctor or collector | NOT RUN | Update calls the same check. Not executed |
| A14 | Rejected save must not persist attribution or change totals | NOT RUN | Not executed. Historical test `Invoice_Update_Can_Add_Rate_Or_Clear_Attribution` is not a result of this pass |
| A15 | Collector change must not reload test prices | NOT RUN | `onCollectorChange` does not call `onRateContextChange`. UI was not exercised |
| A16 | `CollectedBy` unchanged | NOT RUN | No write found in the commission save check. Sample collection was not exercised |

**Finding D-01, High.** Selecting a doctor or collector without a covering rate prevents ordinary invoice generation. Section 3.2 requires generation to remain possible and requires that no commission amount be invented. The implementation does the second and not the first.

---

## 7. Rate Maintenance and Effective-Date Boundaries

`CommissionRateRules` rejects a percent outside 0–100, a percent with more than two decimal places, a missing Effective From, and Effective To before Effective From. Blank Effective To is stored as null (open-ended). Overlap is inclusive on both ends. Adjacent dates do not overlap. The check is per recipient. A different recipient can use the same dates. The rate lookup for an invoice uses `InvoiceDate.Date`, `IsActive`, and inclusive From/To.

Locked rows cannot be updated or deactivated (`This commission rate period is locked and cannot be changed.`). Overlap is enforced inside the rate transaction. The unique indexes on the rate tables are non-unique `(recipient, EffectiveFrom)` indexes, so they do not stop overlapping open-ended ranges.

| ID | Scenario | Status | Evidence |
|---|---|---|---|
| B01 | Valid rate create | NOT RUN | Code path present. Not executed |
| B02 | 0%, 100%, two decimals | NOT RUN | `ValidatePercent` allows these. Not executed |
| B03 | More than two decimals | NOT RUN | Rejected in code. Not executed |
| B04 | Below 0 or above 100 | NOT RUN | Rejected in code. Not executed |
| B05 | Missing Effective From | NOT RUN | Rejected in code. Not executed |
| B06 | Blank Effective To | NOT RUN | Normalized to null. Not executed |
| B07 | Effective To before From | NOT RUN | Rejected in code. Not executed |
| B08 | Same recipient overlap | NOT RUN | `Overlaps` is inclusive. Not executed |
| B09 | Adjacent ranges | NOT RUN | Adjacent dates return false from `Overlaps`. Not executed |
| B10 | Different recipients, same dates | NOT RUN | Overlap query is per recipient. Not executed |
| B11 | Inactive rate | NOT RUN | Lookup requires `IsActive`. Not executed |
| B12 | Locked rate edit | NOT RUN | Update throws. Not executed. Eight locked doctor rates exist |
| B13 | Open rate followed by a later period | NOT RUN | Not executed |
| B14 | Edit after an invoice exists | NOT RUN | Not executed. Invoice save does not snapshot a percent |
| B15 | Concurrent overlapping creates | BLOCKED | Would write rates. No safe concurrent harness was run |

Boundary behavior (From day included, day before excluded, To day included, day after excluded) is what `EffectiveFrom <= day` and `EffectiveTo >= day` implement. It was not executed against live dates in this pass.

---

## 8. Commission Eligibility

`LoadEligible` keeps invoices where payment status is Paid and invoice status is Confirmed or Paid, the invoice date is inside the requested range clipped to the implementation-month start, and a doctor or collector id is present. Cancelled and Draft are other status values, so they are excluded, including Draft with payment Paid. A collector-filtered request also requires no doctor id. If the requested To date is before the implementation month, the result is empty.

`IsActive` is not part of this filter.

| ID | Scenario | Status | Evidence |
|---|---|---|---|
| C01 | Confirmed + Paid | NOT RUN | Included by the filter. Not executed |
| C02 | Invoice status Paid + payment Paid | NOT RUN | Included. Not executed |
| C03 | Draft + payment Paid | NOT RUN | Excluded by invoice status. Not executed |
| C04 | Partial | NOT RUN | Excluded. Not executed |
| C05 | Unpaid | NOT RUN | Excluded. Not executed |
| C06 | Cancelled | NOT RUN | Excluded by status. Not executed |
| C07 | Before 2026-09-01 | NOT RUN | Clipped by month start. Not executed |
| C08 | Doctor and collector both set | NOT RUN | Doctor branch is used. Collector report skips it. Not executed |
| C09 | Neither recipient | NOT RUN | Excluded from the unfiltered query. Not executed |
| C10 | Inactive, missing, or two covering rates | NOT RUN | See section 9. Not executed |

Read-only counts show 28 doctor-attributed and 3 collector-only invoices in the eligible status combination on or after 2026-09-01. Those rows were not opened and were not sent to the report API.

---

## 9. Calculation and Rounding

The base is `SaleInvoice.NetAmount - SaleInvoice.TaxAmount`. Each line uses `Math.Round(base * percent / 100m, 2)` (banker's rounding, the framework default for that overload). `TotalAmount` is the sum of those rounded line amounts. Summary totals sum the same rounded line amounts. Invoice save does not call `CommissionCalculationService`.

`SinglePercent` throws `InvalidOperationException` when the match count is 0 or greater than 1. The report controller maps that exception to HTTP 400. It does not return an empty list. Other operational reports swallow unexpected exceptions into an empty list; commission reports do not.

| ID | Scenario | Status | Evidence |
|---|---|---|---|
| D01 | Tax excluded | NOT RUN | Formula uses net minus tax. Not executed |
| D02 | Header discount in the net amount | NOT RUN | Base uses header net. Not executed |
| D03 | 0% | NOT RUN | Percent is passed through. Not executed |
| D04 | 100% | NOT RUN | Same |
| D05 | Two-decimal rate | NOT RUN | Same |
| D06 | Per-invoice rounding then sum | NOT RUN | `TotalAmount` sums line amounts. Not executed |
| D07 | Aggregate rounding would differ | NOT RUN | Not executed |
| D08 | Doctor precedence | NOT RUN | Doctor branch returns before collector. Not executed |
| D09 | Collector only | NOT RUN | Used when no doctor id. Not executed |
| D10 | Missing rate | NOT RUN | Throws and fails the whole calculation. Live report not called. See D-02 |
| D11 | Inactive or duplicate covering rates | NOT RUN | Inactive omitted from the loaded set. Two matches throw. Not executed |

**Finding D-02, Product decision.** An otherwise eligible invoice with no single active covering rate does not become a zero line and is not skipped. The calculation throws, and the report returns HTTP 400. Every other invoice in that request is withheld with it. The message names the invoice and date. This does not silently invent an amount. Whether the report should skip that invoice or fail the range is not decided by the payout design and needs a product decision.

---

## 10. Doctor Commission Report

Route `/reports/doctor-commission`. API `GET api/Reports/DoctorCommission`. Module `Reports`, CanView. `MenuKey` is not set.

The page requires From and To and rejects From after To on the client. The API requires both dates and exactly one of Summary or Detail. Summary returns recipient, invoice count, and amount. Detail returns invoice number, invoice date, recipient, base, rate, and amount. The other list is null. Excel columns follow the selected format and request `recordPerPage` 0, which the service treats as “all rows”. The component does not use the two-month cap used by other report pages.

| ID | Scenario | Status | Evidence |
|---|---|---|---|
| E01 | Summary only | NOT RUN | Service returns Summary and null Detail. Authenticated call not made |
| E02 | Detail only | NOT RUN | Same for Detail |
| E03 | Dates required | NOT RUN | Client and API throw if dates are missing. Not executed with a token |
| E04 | From after To | NOT RUN | Client message and API `From date cannot be after To date.` Not executed with a token |
| E05 | Date boundaries | NOT RUN | Filter is `InvoiceDate >= start` and `< To + 1 day`, then clipped to month start |
| E06 | One recipient and all recipients | NOT RUN | Optional doctor id. Not executed |
| E07 | Range longer than two months | NOT RUN | No two-month check in this component or service. Not executed |
| E08 | Same invoice on both reports | NOT RUN | Doctor report keeps the doctor. Not executed |
| E09 | Missing-rate invoice mixed with valid invoices | NOT RUN | Whole request becomes HTTP 400. Not executed. See D-02 |
| E10 | Excel matches the selected format | NOT RUN | Column builder switches on `format`. File not downloaded |
| E11 | Portal shell for the report route | PASS | `http://localhost:8080/reports/doctor-commission` returned 200. Guard not proven |
| E12 | Unauthenticated doctor report API | PASS | HTTP 401 |

---

## 11. Collector Commission Report

Route `/reports/collector-commission`. API `GET api/Reports/CollectorCommission`. Same format, date, Excel, and error rules as the doctor report. The calculation skips invoices that have a doctor.

No separate authenticated collector-report call was made. Collector scenarios C08, D08, and E08 cover that rule and are NOT RUN. Unauthenticated `CollectorCommission` returned HTTP 401 (counted as E12 for the pair of report APIs; the collector call was the same 401 result).

---

## 12. Payout and Carry-Forward

`Preview` resolves the period and returns lines. It does not insert. `Confirm` opens a transaction, rejects zero lines with `No eligible invoices for this payout.`, rejects an invoice already stored for that recipient, inserts the header and details, calls `ApplyClosedBoundary`, and commits. `DbUpdateException` rolls back and becomes `This payout period was already confirmed.`

The first From date is the implementation-month start (`2026-09-01` at the current setting). A later From date is the previous To date plus one day for that recipient. A client From date that differs is rejected. To must be on or after From. The From input on the payout page is disabled. Confirm on the page is disabled when the preview has no lines.

Carry-forward asks the calculation service from the month start through the day before From, then drops invoice ids already stored for that recipient. The rate is resolved from the original invoice date inside `Calculate`. Doctor and collector payouts use recipient type and id, so a doctor-attributed invoice is not a collector line.

Payout number format in code is `CP-` plus the facility date `yyyyMMdd` plus a four-digit sequence.

Unique indexes present:

- `UX_CommissionPayout_Recipient_From` on `(RecipientType, RecipientId, FromDate)`
- `UX_CommissionPayoutDetail_Recipient_Invoice` on `(RecipientType, RecipientId, SaleInvoiceId)`

| ID | Scenario | Status | Evidence |
|---|---|---|---|
| F01 | First From date is 2026-09-01 | BLOCKED | `RequiredFrom` returns month start. Schedule API was not called with a token |
| F02 | Preview with no eligible invoices | BLOCKED | Code returns an empty preview. Not called |
| F03 | Preview does not insert | BLOCKED | `Preview` has no insert. Not observed against the database |
| F04 | Confirm with eligible invoices | BLOCKED | Would persist a payout. Not run |
| F05 | Confirm rollback | BLOCKED | Would require a forced failure inside a transaction |
| F06 | Empty confirm rejected | BLOCKED | Code throws. UI disables the button. Not called |
| F07 | Next From is previous To + 1 day | BLOCKED | Code. Not called. Existing payouts were not used to open the next period |
| F08 | Overlap rejected | BLOCKED | From-date mismatch and unique index. Not called |
| F09 | Gap rejected | BLOCKED | User cannot choose a later From. Not called |
| F10 | Late-paid invoice carried forward | BLOCKED | Would confirm a payout |
| F11 | Carry-forward uses original invoice date | BLOCKED | `Calculate` uses `InvoiceDate`. Not executed |
| F12 | Already paid invoice not repeated | BLOCKED | `PaidInvoiceIds` filter. Not executed |
| F13 | Doctor and collector payouts stay separate | BLOCKED | Recipient type is part of the key. Not executed |
| F14 | Doctor invoice is not paid as collector commission | BLOCKED | Collector calculation skips doctor invoices. Not executed |
| F15 | Duplicate confirm | BLOCKED | Unique index and catch of `DbUpdateException`. Not executed |
| F16 | History filter | BLOCKED | `List` filters by recipient. Not called |

The two existing payout headers show that confirm has succeeded at least twice for recipient type 1, with a From date of 2026-09-01. This review did not reprint them and did not open their invoice detail, so they are not treated as a passed carry-forward or reprint test.

---

## 13. Rate Locking and Stored Reprint

`ApplyClosedBoundary` skips a rate that starts after the closed To date. A locked rate that still extends past that date throws. A rate that already ends on or before that date is marked locked. A rate that crosses the date, or is open-ended, is closed on that date, locked, and a new unlocked row is inserted from the next day through the old end at the same percent.

`GetMemo` reads the stored header and details and the lab name. It does not call `Calculate`.

| ID | Scenario | Status | Evidence |
|---|---|---|---|
| F17 | Crossing rate splits and locks | BLOCKED | Counted with the payout block. Eight doctor rates are already locked. This pass did not lock another |
| F18 | Locked slice unchanged | NOT RUN | Update of a locked row throws. Not executed |
| F19 | Open slice can be edited | NOT RUN | Unlocked update path exists. Not executed |
| F20 | Stored amount unchanged after a later rate edit | BLOCKED | Would require a confirmed payout and a later edit |
| F21 | Reprint uses stored rows | BLOCKED | `GetMemo` does not recalculate. Memo was not opened |

F17, F20, and F21 are blocked with the other payout persist cases. F18 and F19 were not executed. Those five IDs are included in the section 5 totals.

---

## 14. Implementation-Month Configuration

The running file `I:\Projects\PROD\AVILIS\API\Web.config` contains:

`Commission:ImplementationMonth` = `2026-09`

That value matches `yyyy-MM`. `ReadImplementationMonthStart` returns 2026-09-01. Empty or whitespace throws. Any other format throws. Invoices before that date are outside `rangeStart`. A rate effective date does not move that boundary.

The source `Web.config` has the same month value. This pass did not change either file. The empty-and-invalid fail-closed branches were not executed against the running API, because that would require changing the setting.

| ID | Scenario | Status | Evidence |
|---|---|---|---|
| M01 | Running value is present and `yyyy-MM` | PASS | Running Web.config value `2026-09` |
| M02 | Empty value fails closed | NOT RUN | Source throw. Setting was not cleared |
| M03 | Invalid format fails closed | NOT RUN | Source throw. Setting was not altered |
| M04 | Pre-month invoices excluded | NOT RUN | `rangeStart` logic. Report not called |
| M05 | Rate effective date does not replace the month boundary | NOT RUN | Both checks exist. Not executed |

M01 is one of the ten PASS rows. M02–M05 are four of the NOT RUN rows.

---

## 15. RBAC and API Authorization

Database grants, read-only:

| Role | Commission menu keys | Reports module | CommissionPayout module |
|---|---|---|---|
| Administrator | `REPORT_DOCTOR_COMMISSION`, `REPORT_COLLECTOR_COMMISSION`, `COMMISSION_PAYOUT`. CanView and CanAdd true | Yes, CanView and CanAdd | Yes, CanView and CanAdd |
| Receptionist | None of the three keys. Has other report menu rows | Yes, CanView | No |
| Technician | None of the three keys. No `REPORT_*` menu rows | Yes, CanView | No |
| Doctor | None of the three keys. No `REPORT_*` menu rows. Reports bits are all true, including CanView | Yes | No |

Payout API: schedule, preview, list, and memo require `CommissionPayout` CanView. Confirm requires CanAdd.

Report API: `Reports` CanView. `MenuKey` is not set, so the menu overlay is not applied on the API. A role with Reports CanView can call the commission report APIs even when the portal route requires the commission menu key.

Portal routes for the two reports and payout are registered before the generic `/reports` route and name the menu keys. `showReportMenu` uses the menu overlay when the role has any menu rows for that module. Receptionist has a Reports overlay, so the commission links stay hidden unless granted. Doctor and Technician have no `REPORT_*` rows. For Doctor, `hasReportAccess()` is true because Reports is fully granted, so the existing fallback shows both commission report links after this portal build. Doctor does not have the payout module, so the payout section stays off that fallback.

| ID | Scenario | Status | Evidence |
|---|---|---|---|
| G01 | Administrator report menu rows exist | PASS | SQL |
| G02 | Administrator payout module exists | PASS | SQL |
| G03 | Receptionist has no commission menu keys | PASS | SQL. Screen not opened |
| G04 | Technician has no commission menu keys | PASS | SQL |
| G05 | Doctor has no commission menu keys | PASS | SQL |
| G06 | Doctor fallback shows the report links | NOT RUN | Code plus full Reports bits. Screen not opened |
| G07 | Role editor grant and revoke | NOT RUN | Catalog contains the three keys. Editor was not used |
| G08 | Report API with Reports CanView and no menu key | BLOCKED | No token for Receptionist, Technician, or Doctor |
| G09 | Payout CanView versus CanAdd | BLOCKED | Attributes inspected. No token |
| G10 | Confirm without CanAdd | BLOCKED | No token |
| G11 | Unauthenticated payout schedule | PASS | HTTP 401 |
| G12 | Report payload has no patient name | PASS | `CommissionCalculationLine` and the Excel columns have no patient field. Deployed DTO matches source. Live payload not fetched |

**Finding D-03, Product decision.** Doctor is not seeded the commission menus, and the payout module is absent, but the existing report fallback can show Doctor Commission and Collector Commission. Receptionist, Technician, and Doctor all have Reports CanView, and the report APIs do not check the menu key. Hiding the portal link does not by itself deny the API.

---

## 16. Regression Results

| ID | Area | Status | Evidence |
|---|---|---|---|
| H01 | Invoice totals and tax | NOT RUN | Commission save does not call the calculation service. A save was not performed |
| H02 | Payment posting | NOT RUN | Not exercised |
| H03 | Draft save and confirm besides the rate gate | NOT RUN | Not exercised. The rate gate itself is D-01 |
| H04 | Sample collection | NOT RUN | Not exercised |
| H05 | `TestRequestDetail.CollectedBy` | NOT RUN | No write found in the commission check |
| H06 | Test price reload | NOT RUN | Doctor change still calls `onRateContextChange`. Collector change does not. UI not exercised |
| H07 | Diagnostic print and Gate 6 | NOT RUN | Running flag remains `true`. Print was not run. Source `Web.config` still says `false`; the running file was not replaced |
| H08 | Unrelated reports and menus | NOT RUN | Not exercised |

No regression failure was observed, because those flows were not run. The Gate 6 value on the running API was left as `true`.

---

## 17. Defects and Business Rule Discrepancies

| ID | Severity | Requirement | Actual behavior | Reproduction |
|---|---|---|---|---|
| D-01 | High | 3.2 Invoice generation must remain possible when no applicable rate exists | Draft and confirm throw, and the form blocks, when the selected doctor has no covering active rate, or when only a collector is selected and that collector has no covering active rate | Select a doctor with no rate for the invoice date. Save draft or confirm. Expect the message `No commission rate is defined for the selected referral doctor on this invoice date...` |
| D-02 | Product decision | 3.6 and 3.7 Missing rate must not invent an amount. Effect on the rest of the range was not specified | The calculation throws. The report returns HTTP 400 for the whole request. Valid invoices in the same range are not returned | Include one eligible paid invoice whose recipient has no single covering rate, plus a valid invoice. Call Doctor or Collector Commission |
| D-03 | Product decision | 3.10 Other roles follow existing overlay and fallback rules | That is what the code does. Doctor can see the new report links through the fallback. Any role with Reports CanView can call the report APIs because `MenuKey` is unset | Sign in as Doctor and open Reports. Separately call `api/Reports/DoctorCommission` as Receptionist |

D-01 is a failed requirement. D-02 and D-03 match the code that was deployed; they need a product decision before they are treated as defects to fix.

---

## 18. Blocked and Not-Run Scenarios

Blocked because they would persist a payout or a rate change, or because no authenticated session was available:

- B15, F01–F17, F20, F21, G08, G09, G10

Not run because this pass did not execute the screen or the API, while inspection of the deployed-matching source shows an implementation:

- A01, A05, A10, A11, A13–A16
- B01–B14, F18, F19
- C01–C10, D01–D11
- E01–E10
- G06, G07
- H01–H08
- M02–M05

Historical automated tests were not re-run and are not counted as PASS.

---

## 19. Risk Assessment

| Risk | Level | Why |
|---|---|---|
| Invoices with a doctor or collector cannot be saved until a rate exists | High | Contradicts the approved generation rule. Affects draft and confirm |
| One missing rate fails an entire commission report | Medium | Valid invoices in the same range are hidden behind HTTP 400 |
| Doctor role sees commission reports through the existing fallback | Medium | The role was not given those menu keys, and the report API will also allow the call |
| Payout confirm, carry-forward, lock, and reprint were not executed on this build in this pass | Medium | Code matches the approved design. Live confirm remains unverified for this QA |
| Source Gate 6 value differs from the running API | Low for commission | Copying source `Web.config` over the running file would turn declarative print off. This review did not copy it |
| Shared catalog already contains payouts and locked rates | Low | This review did not add or change them. A later test run can still write more |

---

## 20. Recommendations for Product Owner Decisions

1. Confirm whether invoice save must stay blocked until a covering rate exists, or whether section 3.2 (save allowed, no invented amount) replaces the Stage 2 rule. The current build implements the Stage 2 rule.
2. Confirm whether a missing or duplicate rate should fail the whole report or skip that invoice. The current build fails the whole report and does not invent an amount.
3. Confirm whether the Doctor role should see Doctor Commission and Collector Commission through the report fallback, and whether Receptionist and Technician may call those report APIs while the menu hides the pages.
4. Do not treat payout as re-certified by this pass. The design in code matches the approved payout rules. Confirm, carry-forward, lock, and reprint were not run here.

No fix was started.

---

## 21. Final Acceptance Matrix

| Requirement | Result | Basis |
|---|---|---|
| Attribution precedence in calculation | NOT RUN | Implemented in the deployed-matching calculation service. Not executed |
| Invoice generation without a rate | FAIL | D-01. Draft and confirm are rejected |
| No invented commission on the invoice | PASS | Save does not call the calculation service and does not store an amount. Combined with D-01, generation itself still fails |
| Rate percent, dates, overlap, lock | NOT RUN | Rules are in source. Not executed this pass |
| Effective-date boundaries | NOT RUN | Inclusive From/To is in source. Not executed |
| Eligibility statuses, including Draft + Paid | NOT RUN | Filter matches the rule. Not executed |
| Implementation month 2026-09 on the running API | PASS | Running Web.config |
| Empty or invalid month fails closed | NOT RUN | Source throw. Setting not changed |
| Formula and per-invoice rounding | NOT RUN | Formula matches. Not executed |
| Missing rate does not invent an amount | NOT RUN | Throw is in source. Live report not called. Product decision D-02 |
| Summary and Detail are exclusive and reconcile | NOT RUN | Service returns one list and sums the same rounded lines. Not executed |
| No two-month cap | NOT RUN | Cap is absent in this report. Not executed |
| Excel follows the selected format | NOT RUN | Column code. File not downloaded |
| Payout design: fixed From, preview, empty confirm, carry-forward, lock, reprint | BLOCKED | Code matches the approved design. Persist paths not run |
| Doctor and collector payout separation | BLOCKED | Code. Not run |
| Administrator grants | PASS | SQL |
| Other roles follow overlay and fallback | NOT RUN | Data and code agree with the existing fallback. Screens not opened. Product decision D-03 |
| Unauthenticated API denied | PASS | HTTP 401 |
| Report API menu key | BLOCKED | `MenuKey` unset. No role token |
| Regression of totals, payment, collection, print | NOT RUN | Not exercised |
| Running build is the current source build | PASS | DLL and portal bundle hashes match |

**Final status: FAIL**

Invoice generation without a commission rate fails the approved rule. Payout confirmation and most calculation and report cases were not executed in this pass, so this is not a full acceptance even apart from that failure.

---

## 22. Changes Made

None to application source, tests, database, configuration, permissions, or IIS.

This report file is the only file added.
