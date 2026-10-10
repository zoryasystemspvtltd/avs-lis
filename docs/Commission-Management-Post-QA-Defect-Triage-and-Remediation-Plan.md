# Commission Management — Post-QA Defect Triage and Remediation Plan

**Date:** 2026-10-11  
**Mode:** Read-only investigation and planning. No implementation.  
**Baseline:** `docs/Commission-Management-Comprehensive-PreProduction-QA-Report.md`  
**Status of that QA run:** FAIL (10 PASS, 8 FAIL, 23 BLOCKED, 79 NOT RUN, 120 scenarios)

No source, test, SQL, migration, database, configuration, permission, or deployment file was changed while producing this plan.

---

## 1. Executive summary

All eight QA failures are the same invoice-save gate, reached with different inputs. They are not eight independent defects, and they are not the missing-rate report behavior. `SaleInvoiceManager.RequireCommissionRate` runs on insert and on update. The sale-invoice form calls the same check before both Draft Save and Confirm. If the selected doctor, or a collector with no doctor, has no active rate covering `InvoiceDate`, save throws and the screen blocks. Inactive rates and future rates fail that same predicate because they are not applicable on the invoice date.

The approved rule is now the opposite: ordinary invoice generation must succeed, and no commission amount may be invented or stored. The smallest correction is to remove that gate on the server and on submit, and to retarget the tests that currently require the throw. Invoice totals, tax, payment, pricing, sample collection, calculation, reports, payout, and RBAC stay untouched.

The report HTTP 400 is a separate, undecided rule. One missing or duplicate rate throws inside `CommissionCalculationService.Calculate`, which reports and payout both call. Payout must keep failing closed. A report-only change needs an explicit Product Owner choice. This plan recommends showing the valid invoices and listing the unrated invoices separately. It does not treat a missing rate as zero percent, and it does not implement that choice.

None of the 23 blocked scenarios are additional failures. They were blocked because they would persist a payout or a rate lock, or because no authenticated session was available. RBAC had no failed scenario. The Doctor-menu fallback and the report APIs without `MenuKey` match the existing authorization model.

---

## 2. QA report baseline and environment limitations

| Item | Evidence |
|---|---|
| QA status | FAIL. Eight failures are A02, A03, A04, A06, A07, A08, A09, and A12 |
| Build | Source and the running API/Portal hashes matched at QA time. Commit `310d23e` on `sougata` |
| Running month | `Commission:ImplementationMonth` = `2026-09` on the running API config |
| Catalog | `ZoryaLMS` on `.\SQLEXPRESS`, shared by IIS and the automated tests |
| What this triage did not do | No save, no report call with a token, no payout, no rate edit, no IIS recycle, no migration |

The QA run did not execute the eight failures against a live invoice. The result is still confirmed: the deployed `LIS.BusinessLogic.dll` hash matched source, and source throws before insert and before update. The portal blocks in `onSubmit` before `saveInvoice`.

Automated tests were not re-run. `InvoiceCommissionRateValidationTests` still asserts the old Stage 2 rule (save must throw). Those assertions are now the opposite of the approved rule. They are a test-design conflict, not a second product defect.

---

## 3. Detailed analysis of all eight failures

Each failure is listed on its own. The confirmed cause is the same method. The input that makes the predicate fail is different. No failure in this set is a configuration error, a bad test row, or the report HTTP 400.

Shared facts, confirmed in source:

- Insert calls `RequireCommissionRate` only when `header.Id <= 0`, after `Recalculate` and invoice-date normalization, and before `invoiceRepo.Add`.
- Update calls it after the cancelled-invoice throw and before `ApplyInvoiceHeaderUpdate`.
- `SaleInvoiceController` save goes through `ISaleInvoiceManager.Save`. No second save path calls this check.
- The form uses one method, `onSubmit(confirm = false)`. Confirm passes `confirm = true` and sets invoice status to Confirmed. Both hit `commissionRateError()` and return before the HTTP call when rate lists are loaded.
- The method does not write a commission amount and does not call `CommissionCalculationService`.
- Doctor precedence is the early return after a covering doctor rate. A missing collector rate does not block when a doctor rate covers the date. That case was A05 and was not a failure.

### A02 — Doctor selected, no doctor rate

| Item | Detail |
|---|---|
| Expected | Draft and confirm succeed. No commission amount is stored. |
| Actual | `ArgumentException`: “No commission rate is defined for the selected referral doctor on this invoice date…” |
| Evidence | `RequireCommissionRate` doctor branch, `!covered`. UI `commissionRateError` returns the same text. QA binary hash matched this source. |
| Root cause | Confirmed. Stage 2 required a covering doctor rate before save. The current rule does not. |
| Classification | Code defect against the current approved rule. The tests that expect the throw are a test-design conflict. |
| Impact | High. A doctor with no rate for that date cannot be placed on a new or edited invoice. |
| Smallest correction | Stop calling the gate. Do not add a zero commission. |
| Regression | Save with a valid doctor rate, with no doctor, and with both doctor and collector must still store the same totals. |
| Tests | Replace `Draft_And_Confirmed_Require_Rate_When_Doctor_Or_Collector_Is_Selected` doctor cases so save succeeds and no commission field is written. |

### A03 — Collector selected, no doctor, no collector rate

| Item | Detail |
|---|---|
| Expected | Draft and confirm succeed. |
| Actual | `ArgumentException`: “…selected sample collector…” |
| Evidence | The method reaches the collector branch only when no doctor id is set. `IsActive` and date coverage use `SampleCollectorCommissionRate`. |
| Root cause | Confirmed. Same gate, collector predicate. Not the doctor branch. |
| Classification | Code defect against the current rule. |
| Impact | High. Collector-only invoices are blocked when that collector has no covering rate. |
| Smallest correction | Same removal. Do not require a collector rate at save. |
| Regression | Unknown collector and newly selected inactive collector must still be rejected by `NormalizeAndValidateCollector`. That check is separate and must stay. |
| Tests | Collector half of `Draft_And_Confirmed_Require_Rate…` must expect success. `Invoice_Rejects_Unknown_CollectorId` and `Invoice_Rejects_Newly_Selected_Inactive_Collector` must still fail. |

### A04 — Both selected, no rates

| Item | Detail |
|---|---|
| Expected | Invoice generation succeeds. Doctor remains the commission recipient later. No amount is stored. |
| Actual | Doctor message. The collector branch is not evaluated. |
| Evidence | After a failed doctor lookup the method throws and returns. Collector absence is irrelevant. |
| Root cause | Confirmed. Doctor predicate fails first. |
| Classification | Code defect. Same gate as A02, different selection. |
| Impact | High. Selecting both people without a doctor rate blocks save even though the collector is not the commission recipient. |
| Smallest correction | Remove the gate. Do not start rejecting on the collector when both are selected. |
| Regression | Later calculation must still ignore the collector when a doctor id is present. |
| Tests | A save with both ids and no rates succeeds, and both ids are stored. Calculation is not invoked by save. |

### A06 — Both selected, collector rate exists, doctor rate does not

| Item | Detail |
|---|---|
| Expected | Save succeeds. Doctor precedence does not mean “a doctor rate is required at save.” |
| Actual | Doctor message, even though a collector rate exists. |
| Evidence | `Invoice_Rate_Rules_Follow_Doctor_Precedence` expects this throw today. The collector rate is added first, then save throws. |
| Root cause | Confirmed. Doctor branch does not look at collector rates. |
| Classification | Code defect against the current save rule. Doctor precedence for calculation remains correct and is a different method. |
| Impact | High. A valid collector rate cannot unblock a doctor who has no rate. |
| Smallest correction | Remove the save gate only. Leave calculation precedence in `CommissionCalculationService` as it is. |
| Regression | A05 must stay possible: both selected, doctor rate present, collector rate absent, save succeeds. That path already returns after the doctor rate matches. |
| Tests | Rewrite the first assertion of `Invoice_Rate_Rules_Follow_Doctor_Precedence` from throw to successful save. Keep the later assertions that both ids persist and net amount stays 1000. |

### A07 — Doctor selected, only an inactive rate

| Item | Detail |
|---|---|
| Expected | An inactive rate is not applicable, so save still succeeds. |
| Actual | The doctor lookup requires `r.IsActive`, so the inactive row does not count and save throws the doctor message. |
| Evidence | Predicate `r.IsActive && ReferralDoctorId && EffectiveFrom <= day && (EffectiveTo == null \|\| EffectiveTo >= day)`. UI `rateCovers` also rejects `isActive === false`. |
| Root cause | Confirmed. “No applicable rate” includes inactive. It is not a separate inactive-rate bug. |
| Classification | Code defect against the current save rule. Inactive rates must still be ignored by calculation. |
| Impact | High. Deactivating a rate blocks new invoices for that doctor on dates the inactive row used to cover. |
| Smallest correction | Remove the save gate. Do not treat an inactive rate as applicable in `Calculate`. |
| Regression | `Inactive_Rate_And_Duplicate_Coverage_Fail_Closed` must still fail calculation, not invoice save. |
| Tests | Save with only an inactive doctor rate succeeds. A later calculation test still excludes that rate. |

### A08 — Collector selected, only an inactive rate

| Item | Detail |
|---|---|
| Expected | Save succeeds. |
| Actual | Collector lookup requires `IsActive`, then throws the collector message. |
| Evidence | `Inactive_Rate_Does_Not_Satisfy_Invoice_Date` deactivates a collector rate and expects `ArgumentException` on draft save. |
| Root cause | Confirmed. Same gate as A03, with an inactive row instead of no row. |
| Classification | Code defect against the current save rule. The test encodes the old rule. |
| Impact | High. Same billing block as A03 for collectors whose only rate was deactivated. |
| Smallest correction | Remove the save gate. Leave rate deactivation and calculation exclusion in place. |
| Regression | `NormalizeAndValidateCollector` must still reject a newly selected inactive collector master. That is the person, not the rate. |
| Tests | Retarget `Inactive_Rate_Does_Not_Satisfy_Invoice_Date` so the invoice saves. Add or keep a calculation assertion that the inactive rate is not used. |

### A09 — Doctor rate starts after InvoiceDate

| Item | Detail |
|---|---|
| Expected | A rate that does not cover `InvoiceDate` is not applicable, so save succeeds. |
| Actual | `EffectiveFrom <= day` fails, so save throws the doctor message. The day before From and the day after To fail the same way. |
| Evidence | `Draft_And_Confirmed_Require_Rate…` expects a throw on 2026-10-01 and on 2026-08-31 around a rate that covers only September 2026. |
| Root cause | Confirmed. Date coverage is part of “applicable,” not a different validator. |
| Classification | Code defect against the current save rule. Date coverage must remain in calculation and payout. |
| Impact | High. A future or expired rate blocks the invoice even though commission will not use it. |
| Smallest correction | Remove the save gate. Keep `EffectiveFrom` / `EffectiveTo` in `Calculate` and in rate maintenance. |
| Regression | Rate overlap, adjacent dates, and locked-rate edits must not change. |
| Tests | The October and 31 August saves in that test must succeed. Boundary tests for calculation stay as they are. |

### A12 — Draft Save and Confirm when the required rate is missing

| Item | Detail |
|---|---|
| Expected | Both workflows succeed. |
| Actual | Both are blocked. |
| Evidence | `onSubmit(confirm)` sets status to Confirmed only when `confirm` is true, and both values pass through `commissionRateError()` first. Server insert and update both call `RequireCommissionRate` with no status check. `Draft_And_Confirmed_Require_Rate…` expects both throws. |
| Root cause | Confirmed. This is the workflow pair of A02–A09, not a third validator. Draft and confirm share one UI method and one server method. |
| Classification | Code defect. Two call sites (UI submit and server save), one rule. |
| Impact | High. Users cannot draft the invoice and confirm it later while a rate is still missing. |
| Smallest correction | Remove the submit `return` and both server calls. If the warning toast remains, its text must not say to remove the doctor or collector, because save is allowed. The smallest consistent change is to remove the blocking toast as well. |
| Regression | Confirm must still set invoice status to Confirmed. Cancelled invoices must still throw “Cancelled invoice cannot be edited” before any attribution update. |
| Tests | One draft save and one confirm save with a doctor and no rate, and the same pair for a collector. Assert status, ids, net amount, and that no commission amount exists on the invoice. |

---

## 4. Analysis of the blocked scenarios

None of these were marked FAIL. They are verification gaps.

| IDs | Why blocked | Can existing tests or read-only inspection close it? | Isolated data required? | Persists payout or rate lock? |
|---|---|---|---|---|
| B15 Concurrent overlapping rate creates | No safe concurrent harness. Overlap is enforced in the rate transaction, not by a unique index | Read-only: indexes `IX_ReferralDoctorCommissionRate_Doctor_From` and `IX_SampleCollectorCommissionRate_Collector_From` are non-unique. Code rejects overlap. A concurrent test was not run | Yes, disposable rates | Rate rows only. Not required to fix A02–A12 |
| F01–F16 Payout schedule, preview, empty confirm, next From, overlap, gap, carry-forward, separation, duplicate confirm, history | Authenticated call or a confirm would persist operational payout state. Two doctor payouts already exist; this triage did not open them | `CommissionStages4To8Tests` already encodes first From, preview-does-not-insert, empty confirm, carry-forward, and stored reprint. Those tests were not run in QA because they write to `ZoryaLMS` | Yes. Run them on a copy, not on the IIS catalog | Confirm, carry-forward, and duplicate-confirm tests persist payouts and can lock rates |
| F17 Crossing rate splits and locks | Same as confirm | `Payout_Confirm_Carry_Forward_Splits_Rate_And_Reprint_Stays_Stored` and `DoctorRate_Edit_Before_Lock_And_Lock_Splits_History` | Copy of the catalog | Yes. Locks and inserts an open slice |
| F20 Stored amount after a later open-slice edit | Needs a confirmed payout and a later rate edit | The same payout test changes the open slice and reloads the memo | Copy | Yes |
| F21 Reprint | Memo was not opened | `GetMemo` does not call `Calculate`. The payout test asserts stored amounts | Copy for a live memo; source inspection already shows no recalculation | Reading a memo does not. Creating one does |
| G08 Report API with Reports CanView and no menu key | No Receptionist, Technician, or Doctor token | Confirmed in source: `GetDoctorCommission` and `GetCollectorCommission` set module `Reports` and CanView, and do not set `MenuKey`. SQL showed those three roles have Reports CanView and do not have the commission menu keys | A disposable user or a token for an existing role. A GET does not persist payout data | No |
| G09 Payout CanView versus CanAdd | No token | Confirmed in source: schedule, preview, list, and memo are CanView; confirm is CanAdd | Token. Preview should not insert. Confirm would | Confirm would persist |
| G10 Confirm without CanAdd | No token | Attribute on `confirm` is CanAdd | Token for a role that has CanView and not CanAdd. Do not call confirm on the IIS catalog | The call is rejected before save if authorization works. Still do not use a real recipient |

Payout blocked cases do not justify a payout redesign. The code already implements the approved sequence, fixed From date, preview without insert, empty-confirm rejection, carry-forward from `InvoiceDate`, `ApplyClosedBoundary`, and stored reprint.

---

## 5. Critical untested scenarios requiring follow-up

Not marked passed. These are the NOT RUN cases that acceptance still needs after a fix. The rest of the 79 are the same rules exercised through code inspection (rate percent bounds, rounding formula, Excel column switch) and can follow on the disposable catalog.

| Scenario | Why it was not run | Follow-up |
|---|---|---|
| A01, A05, A10, A11 Save with no recipient, with a covering rate, and with doctor rate but no collector rate | QA did not save invoices | Required on a disposable catalog after the gate is removed. A05 is the precedence case that must keep working |
| A13, A14 Update attribution, and a failed save must not leave a partial doctor id | Not executed. A14’s historical test expected a throw and then a reload | Required. After the fix, adding a doctor with no rate must persist. Cancelled-invoice edit must still throw before update |
| A15 Collector change does not reload test prices | UI not clicked. `onCollectorChange` does not call `onRateContextChange`. Doctor change still does | Required UI check on the disposable app. Do not change doctor price reload |
| A16 and H04–H05 `CollectedBy` and sample collection | Not exercised. Commission save does not reference `CollectedBy` | One collector-attributed invoice on disposable data, then confirm `CollectedBy` is unchanged |
| C01–C10 and D01–D11 Eligibility and rounding | Report API was not called with a token | Re-run `CommissionCalculationTests` on a copy. Do not point it at the IIS database |
| D10 Missing rate on a report range | Not called. Source throws from `SinglePercent` | Do not retest as a pass until the Product Owner chooses the report behavior |
| E01–E10 Summary, Detail, dates, two-month range, Excel | Not called | Re-run `Report_Summary_And_Detail_Are_Exclusive` and `Report_Allows_A_Range_Longer_Than_Two_Months` on a copy. Download Excel only on that copy |
| G06 Doctor fallback screen | Not opened. Doctor has Reports fully granted and no `REPORT_*` menu rows, so `showReportMenu` uses `hasReportAccess()` | UI check only if the Product Owner wants to confirm the fallback. Not a code change by default |
| G07 Role editor grant and revoke | Editor not used | Optional. Catalog already lists the three keys. Do not change grants on the shared catalog |
| H01–H03, H06–H08 Totals, payment, print, unrelated reports | Not exercised | Run the existing sale-invoice regression tests on a copy. Do not toggle Gate 6. Running config is `true`; source `Web.config` is `false` |
| M02–M05 Empty month, invalid month, pre-month invoices | Changing the setting was forbidden | `Missing_And_Invalid_ImplementationMonth_Fail_Closed` and `Before_Implementation_Month_And_Outside_Period_Are_Omitted` on a copy. Do not edit the running `Web.config` |

---

## 6. Confirmed defects versus unresolved decisions

| Item | Class | Decision state |
|---|---|---|
| A02, A03, A04, A06, A07, A08, A09, A12 | One confirmed defect: save is blocked when no applicable rate exists | Approved by section 2.A of this triage. Correction is specified, not implemented |
| D-02 Report HTTP 400 when one invoice has no single covering rate | Unresolved behavior. Current code is intentional fail-closed, not an accidental empty list | Product Owner must choose. Not a silent zero |
| D-03 Doctor fallback and report APIs without `MenuKey` | Matches the existing RBAC model. Not one of the eight failures | No code change unless the Product Owner wants a tighter rule |
| Payout F01–F21 | Not defects. Verification blocked | Design stays. Verify on a database copy |
| Stage 2 tests that expect the throw | Test-design conflict with the new save rule | Update the assertions in the same change as the gate removal |

---

## 7. Root-cause evidence and relevant source references

**Save gate**

- `LIS.Businesslogic/SaleInvoiceManager.cs` — `RequireCommissionRate`, call at insert (`header.Id <= 0`) and call at update (after the cancelled check).
- `web/Lis.Api/Controllers/Api/SaleInvoiceController.cs` — save delegates to `manager.Save`.
- `web/Lis.Web/src/app/masters/sale-invoice/sale-invoice-form.component.ts` — `onSubmit`, `commissionRateError`, `rateCovers`. `onCollectorChange` only warns. `onReferralDoctorChange` also reloads test prices.
- `LIS.Masters.Tests/Masters/InvoiceCommissionRateValidationTests.cs` — four tests assert the old throw.

**Report and payout share one calculator**

- `LIS.Businesslogic/CommissionCalculationService.cs` — `SinglePercent` throws when `matchCount` is 0 or greater than 1. `Calculate` is used for every eligible invoice in the range.
- `LIS.Businesslogic/CommissionReportService.cs` — calls `Calculate` once per format.
- `web/Lis.Api/Controllers/Api/OperationalReportsController.cs` — `RunCommission` maps `InvalidOperationException` to HTTP 400. It does not return an empty list.
- `LIS.Businesslogic/CommissionPayoutManager.cs` — `Preview` and `Confirm` call `EligibleLines`, which calls `Calculate` for the current period and for carry-forward. A throw aborts preview and rolls back confirm.

**RBAC, unchanged**

- Report actions: module `Reports`, CanView, no `MenuKey`.
- Payout actions: module `CommissionPayout`, CanView except confirm, which is CanAdd.
- SQL at QA time: only Administrator has `REPORT_DOCTOR_COMMISSION`, `REPORT_COLLECTOR_COMMISSION`, and `COMMISSION_PAYOUT`.

---

## 8. Proposed minimal corrections, ordered by priority

### P1 — Remove the invoice rate gate

Approved requirement. Do this first. No schema change.

1. Stop calling `RequireCommissionRate` on insert and on update. Delete the method once nothing calls it.
2. Remove the `onSubmit` block that returns on `commissionRateError`.
3. Remove the error toast that tells the user to define a rate or remove the doctor or collector. Leaving that text would describe a rule that no longer exists.
4. Do not add a commission column, a zero amount, or a call to `Calculate` from save.
5. Keep `NormalizeAndValidateCollector`, `Recalculate`, cancelled-invoice rejection, and doctor `onRateContextChange`.

Flow after the change:

`Sale invoice form submit` → `SaleInvoiceController.Save` → `SaleInvoiceManager.Save` → existing invoice tables.  
No write to `CommissionPayout` or to rate tables. Reports and payout read attribution later and still require a real rate at calculation time.

### P2 — Report missing-rate behavior (do not implement until chosen)

`Calculate` is shared. Changing `SinglePercent` to skip, or to return zero, would also change payout preview and confirm. That would alter the approved payout design.

The smallest change that does not weaken payout is a report-only policy:

- Add a calculation mode used only by `CommissionReportService`.
- Payout keeps the current throw.
- Do not store a percent of 0 for a missing rate.

Options and consequences:

| Option | What the user sees | Valid invoices in the same range | Payout if this is applied inside `Calculate` | Risk |
|---|---|---|---|---|
| A. Keep HTTP 400 (current) | The report fails. The message names the invoice and date | Not returned | Preview and confirm fail closed. No partial payout | One bad invoice hides a good range. No wrong amount |
| B. Skip silently | Report succeeds | Returned. The unrated invoice disappears | If shared, payout would omit the invoice and could close the period without it | Silent under-payment. Rejected as a recommendation |
| C. Skip and list the unrated invoices (recommended if a change is approved) | Summary and Detail show rated invoices. A separate list names invoice number, date, and recipient, with no amount and no zero percent | Returned | Must not be the payout mode. Payout stays on option A | Extra response fields. No schema change. Users can see who was left out |
| D. Treat missing as 0% | A zero line | Returned, with a zero that looks like a configured rate | Would store zero on a payout | Rejected. A missing rate is not a zero-percent rate |

Recommendation: leave payout on option A. For reports, prefer option C over A only after the Product Owner accepts it. Do not implement B or D.

### P3 — No RBAC code change

The blocked authorization checks are not failures. Initial access is Administrator-only in the seed, and the role editor already lists the three keys. Tightening report APIs with `MenuKey`, or hiding the Doctor fallback, would be a new authorization rule. Do not add it in the invoice-gate change.

### P4 — No payout code change

Do not alter sequential periods, the implementation-month From date, preview, confirm, carry-forward, original invoice-date rates, splitting, locking, or reprint to accommodate the save rule or the report rule.

---

## 9. Required regression tests for each correction

### After P1

Run on a disposable copy of the catalog, not on the database named in the running API config.

- New or rewritten invoice tests: doctor with no rate, collector with no rate, both with no rates, both with only a collector rate, inactive doctor rate, inactive collector rate, rate that starts after the invoice date, draft and confirm. Each save returns an id. Net and tax match the pre-commission calculation. No commission amount is persisted.
- Keep: neither recipient; both recipients when the doctor rate exists and the collector rate does not; unknown collector; newly selected inactive collector; cancelled invoice cannot be edited; update that clears attribution.
- Re-run `CommissionCalculationTests` and `CommissionRateTests` unchanged. Calculation must still reject a missing, inactive, or duplicate rate.
- Re-run the existing sale-invoice regression set on the copy (`SaleInvoiceManagerTests`, `SaleInvoiceRegressionTests`, `SaleInvoiceEditWorkflowTests` if those assemblies are present).
- UI: collector change does not reload line prices; doctor change still does; Draft and Confirm both submit when no rate exists.

### After P2, only if option C is approved

- A range with one rated invoice and one unrated invoice returns the rated line and names the unrated invoice, with no amount of 0.
- Summary total equals the sum of rated detail lines only.
- Payout preview for that recipient still throws and inserts nothing.
- Duplicate covering rates still fail the report and the payout.
- Excel contains only the requested Summary or Detail rows, not a fabricated zero.

### Not part of P1

Do not require a payout confirm on the IIS catalog. `CommissionStages4To8Tests` remains the payout proof, on the disposable copy.

---

## 10. Risks and dependencies

| Risk | Why it matters | Mitigation |
|---|---|---|
| Tests and IIS share `ZoryaLMS` | The current test base writes doctors, rates, invoices, and payouts into the catalog the API uses | Point the test run at a restored copy. Do not execute `Update-Database` against the running catalog |
| Removing the warning but leaving the server throw | The UI would submit and the API would still reject | Remove both gates in one change |
| Leaving the old toast text | Users would still be told to remove the doctor | Remove that message with the gate |
| Changing `SinglePercent` for reports | Payout preview and confirm use `Calculate` | Report-only mode, or no report change until the Product Owner chooses |
| Skipping an unrated invoice inside payout | The period could close without that invoice | Keep payout on the current throw |
| Doctor price reload | `onReferralDoctorChange` calls `onRateContextChange` for test prices, not for commission | Do not remove that call |
| Source `Web.config` Gate 6 is `false`; the running file is `true` | A later copy of source config over the API would change print behavior | Do not deploy `Web.config` as part of the invoice fix |
| Historical Stage 2 report | It documents the old “block save” rule | Do not edit that report in the fix. The new tests are the current rule |

No schema change is required. The invoice tables already store doctor and collector ids without a commission amount.

---

## 11. Product Owner decisions required

1. Report missing-rate behavior: keep HTTP 400 for the whole range (option A), or return rated invoices plus an explicit unrated list (option C). Option B (silent skip) and option D (zero percent) are not recommended.
2. Whether the Doctor role should continue to see commission report links through the existing report fallback, and whether report APIs should stay module-only. This is not required to fix the eight failures. The current behavior matches the existing RBAC model.
3. Approval to run payout and calculation tests on a database copy. They must not run against the live IIS catalog.

The invoice-save rule is not an open decision. Section 2.A already requires save to succeed when no applicable rate exists.

---

## 12. Recommended implementation sequence

1. Product Owner confirms this plan and chooses the report option. P1 does not depend on that choice and can proceed first.
2. Apply P1 only: server gate, submit gate, blocking message, and the four invoice-rate tests.
3. Run the P1 regression set on a catalog copy.
4. Stop. Do not change payout or RBAC.
5. If option C is chosen, implement the report-only mode in a second change and run the P2 tests on the same kind of copy.
6. Deploy only after that verification, in a separate authorization. Do not deploy as part of planning.

---

## 13. Files and components expected to change, if implementation is approved

### P1

| File | Change |
|---|---|
| `LIS.Businesslogic/SaleInvoiceManager.cs` | Remove `RequireCommissionRate` and both calls |
| `web/Lis.Web/src/app/masters/sale-invoice/sale-invoice-form.component.ts` | Remove the submit block and the blocking rate error. Leave collector and doctor price behavior |
| `web/Lis.Web/src/app/masters/sale-invoice/sale-invoice-form.component.html` | Remove the inline rate error if it only repeats the blocking message |
| `LIS.Masters.Tests/Masters/InvoiceCommissionRateValidationTests.cs` | Expect successful saves for the cases that currently expect a throw |

No migration, no `Web.config` edit, no menu seed, no payout class.

### P2, only after a report decision

| File | Change |
|---|---|
| `LIS.Businesslogic/CommissionCalculationService.cs` | Report-only handling for zero matches. Payout call keeps the throw. Do not map a missing rate to 0 |
| `LIS.Businesslogic/CommissionReportService.cs` | Return rated rows and a separate unrated list if option C is chosen |
| `LIS.DtoModel/Models/CommissionReport.cs` | Fields for that list, only if option C is chosen |
| `web/Lis.Api/Controllers/Api/OperationalReportsController.cs` | Keep HTTP 400 for real argument errors. Option C would not use 400 for a single missing rate |
| `web/Lis.Web/src/app/reports/commission-report/commission-report.component.ts` and `.html` | Show the unrated list. Excel stays on Summary or Detail rated rows |
| `LIS.Masters.Tests/Masters/CommissionCalculationTests.cs` and `CommissionStages4To8Tests.cs` | Report expectation only. Payout tests stay fail-closed |

`CommissionPayoutManager.cs`, rate managers, and authorization files are not in the expected change set.

---

## 14. Explicit confirmation that no changes were made

This document was added. Application source, tests, SQL scripts, migrations, database records, configuration, permissions, IIS, and deployment artifacts were not modified. No payout was created or confirmed. No commission rate was changed. No fix was implemented.
