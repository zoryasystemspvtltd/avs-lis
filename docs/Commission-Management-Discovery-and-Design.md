# Commission Management — Discovery and Design

**Date:** 2026-09-26  
**Status:** **STOPPED before implementation**  
**Reason:** Required business fields are missing. They were not invented.

No source, schema, migration, menu, or production data was changed.

---

## 1. Decision

Implementation did **not** start.

The approved rules need three facts on every eligible invoice test line:

1. Who the commission person is (Referral Doctor master, or a Sample Collector master).
2. Whether the sample is **Outside** or **Inside**.
3. Which **BusinessUnit** the rate belongs to.

Discovery found:

| Required fact | In the current application |
|---------------|----------------------------|
| Referral Doctor master and `SaleInvoice.ReferralDoctorId` | Present |
| Invoice transaction date | Present: `SaleInvoice.InvoiceDate` |
| Test id on the bill line | Present: `SaleInvoiceDetail.TestId` |
| Line amount before tax | Present: `SaleInvoiceDetail.NetAmount` = Rate × Quantity − line discount. Line tax is forced to 0 |
| BusinessUnit / Enterprise / Company | **Not present** anywhere in the solution |
| Outside / Inside sample | **Not present**. `SampleTypeMaster` is specimen (for example serum), not collection source |
| Sample Collector master | **Not present**. Collection stores `TestRequestDetail.CollectedBy` as a free text user name |

Section 3 of the implementation prompt says: if Outside/Inside semantics differ, stop and report the discrepancy before changing business logic. Section 50 says: if a required field is missing or ambiguous, stop and do not invent a replacement.

Creating an Outside/Inside flag, a BusinessUnit hierarchy, or a second collector identity would be inventing those replacements.

---

## 2. Referral Doctor (reused, not redesigned)

| Item | Actual |
|------|--------|
| Entity | `ReferralDoctorMaster` → table `ReferralDoctorMaster` |
| Identity | `Id` (int), `Code`, `Name`, `Phone`, `Email`, `Address`, `IsActive` |
| Audit | `CreatedBy`, `CreatedOn`, `ModifiedBy`, `ModifiedOn` |
| BusinessUnit | None |
| Commission % | None on the master (correct; rates must stay off this row) |
| Invoice link | `SaleInvoice.ReferralDoctorId` (`int?`) |
| Display fallback | `SaleInvoice.RefDoctorName` (string). Not an id. Not used as the commission person |
| API | Existing `ReferralDoctorController` : `MasterApiControllerBase<ReferralDoctorMaster>` |

Referral Doctor identity can be reused as-is. No doctor master change is required for identity.

---

## 3. Sample Collector (gap)

There is no `SampleCollector` entity, table, or master screen.

What exists instead:

| Item | Actual |
|------|--------|
| Field | `TestRequestDetail.CollectedBy` (`nvarchar(80)`) |
| Written by | `SampleCollectionManager.CollectSample` sets `request.CollectedBy = identity?.ActivityMember ?? "system"` |
| Meaning | The signed-in user who clicked Collect, not a selectable collector master |
| Also | `CollectorName` on report filters is a string search over that same text (`ReportManager` collector-wise report) |

`CollectedBy` cannot be the commission person key:

- It is not a stable id.
- It is not BusinessUnit-scoped.
- It is not chosen at booking; it is stamped at collection.
- Invoice lines can exist before collection, so many billable lines have no collector yet.
- One invoice can have several test requests with different `CollectedBy` values. The prompt asks for one recipient per invoice/test line, but it does not say how to pick among several collectors on one invoice.

A new Sample Collector Master, and a foreign key from the invoice or the test request onto that master, would be new business data. That was not added.

---

## 4. Outside / Inside sample (gap)

Searched entities, invoice, test request, patient visit, and the Angular sample/invoice UI.

No column, enum, or screen stores Outside versus Inside collection.

`SampleTypeMaster` (`Code`, `Name`) is the specimen lookup used with unit, method, and container. It is not a sample-source flag.

Rules that cannot be evaluated today:

| Rule | Blocker |
|------|---------|
| No doctor + collector + **Outside** → collector commission | No Outside flag |
| No doctor + collector + **Inside** → zero | No Inside flag |
| Doctor present → doctor wins regardless of source | Doctor id exists; source is still unknown and must not be guessed |

---

## 5. Invoice structure (verified)

### Header — `SaleInvoice`

| Field | Use for commission |
|-------|--------------------|
| `Id` | Invoice id |
| `InvoiceNo` | Report column |
| `InvoiceDate` | **Commission date** (facility wall-clock date already stored on the invoice; not `CreatedOn`) |
| `InvoiceStatus` | `InvoiceStatusType`: Draft = 0, Confirmed = 1, Paid = 2, Cancelled = 3 |
| `PaymentStatus` | Unpaid / Partial / Paid. Not a commission gate in the approved rules |
| `PatientId`, `PatientVisitId` | Patient link |
| `ReferralDoctorId` | Doctor recipient when not null |
| `CorporateId` | Pricing context only. Not a commission recipient |
| `GrossAmount`, `DiscountAmount`, `TaxAmount`, `NetAmount` | Header totals. Header `NetAmount` = gross − header discount **+ tax**. **Not** the commission base |
| `PaidAmount`, `DueAmount` | Payment. Out of scope |
| BusinessUnit | **Absent** |

`InvoiceDate` is already the date passed into `GetEffectiveRateForInvoice`. It is the deterministic bill date. No new date field is required **if** implementation is later approved.

### Line — `SaleInvoiceDetail`

| Field | Meaning in code |
|-------|-----------------|
| `Id` | Line id |
| `SaleInvoiceId` | Header |
| `TestId` | Test (required FK) |
| `TestProfileId` | Optional profile on the line |
| `Rate`, `Quantity` | Inputs |
| `Amount` | `Round(Rate × Quantity, 2)` |
| `DiscountAmount` | Line discount in rupees |
| `TaxAmount` | Forced to **0**. Comment in `SaleInvoiceManager`: tax is entered once on the header |
| `NetAmount` | `Round(Amount − DiscountAmount, 2)`. **Pre-tax line net** |
| `RequestDetailId` | Optional link to `TestRequestDetail` |
| `SampleNo` | Copied onto the line when present |

Commission base, if this design is approved later, is **`SaleInvoiceDetail.NetAmount`**. That figure excludes tax. It includes the line discount. It does not include header-level discount. Header discount is not allocated onto lines in `SaleInvoiceManager`.

That header-discount gap is recorded here. It was not “fixed” by spreading header discount across lines.

Cancelled exclusion can use `InvoiceStatus == Cancelled`. Draft invoices are a separate product choice (not specified). They were not treated as confirmed billable lines.

---

## 6. OperationalDateTime

`OperationalDateTime` (`LIS.Businesslogic/Helper/OperationalDateTime.cs`) converts facility “now” and inbound timestamps to India Standard Time wall-clock (`DateTimeKind.Unspecified`).

`SaleInvoice.InvoiceDate` is already that business date. Commission selection should compare **date** (not a new UTC conversion) against the rate’s effective range. No new clock logic is required.

---

## 7. BusinessUnit (gap)

The prompt says reuse Enterprise → Company → BusinessUnit and scope every rate to `BusinessUnitId`.

There is no `BusinessUnit`, `Company`, or `Enterprise` type in this solution. Sale invoices, referral doctors, and test rates are not business-unit scoped.

Test Rate overlap is scoped by test + rate type + corporate + referral doctor + profile + dates, not by business unit (`TestRateMasterManager`).

Inventing a BusinessUnit table, or silently using a global scope and calling it BusinessUnit, would contradict the stop rule.

---

## 8. Effective-date pattern that already exists

`TestRateMaster`:

- `EffectiveStart`, `EffectiveEnd` (end is **required**, not null)
- `IsActive`
- Overlap: active rows for the same test and context where ranges intersect (`start <= otherEnd && end >= otherStart`)

Open-ended `EffectiveTo = NULL` is **not** how Test Rate works. A later commission setup should either require an end date (match Test Rate) or explicitly approve null end dates. That choice was not implemented.

---

## 9. What was not designed into the database

No tables were added for:

- `ReferralDoctorCommissionSetup`
- `SampleCollectorMaster`
- `SampleCollectorCommissionSetup`
- A commission ledger

A ledger is still unnecessary for this phase. The blocker is missing inputs, not storage of calculated rows.

---

## 10. Proposed flow (not implemented)

When the missing facts exist, the service would be:

1. Take non-cancelled invoice lines.
2. Commission date = `SaleInvoice.InvoiceDate` (date).
3. If `ReferralDoctorId` has a value → recipient is that doctor, even when the doctor rate is missing or 0%. Collector amount is 0.
4. Else if a collector id exists **and** the line is Outside → recipient is that collector.
5. Else → no commission.
6. Resolve one active rate for that person + test + business unit whose range contains the invoice date.
7. Amount = `SaleInvoiceDetail.NetAmount × percent / 100`.
8. Reports call that service. Summary totals equal detail totals.

This flow is **not** in the application.

---

## 11. Conflicts with the current system

| Approved rule | Current system |
|---------------|----------------|
| One BusinessUnit-scoped rate per person and test | No BusinessUnit |
| Collector master selected for attribution | Only `CollectedBy` user text at collect time |
| Outside vs Inside | No field |
| Doctor suppresses collector | Doctor id exists; collector and sample source do not, so the rule cannot be executed |
| Do not change sample collection except to read an existing attribution field | The existing field is not a collector master id |

---

## 12. Uncertainties that block coding

1. Where Outside/Inside is recorded today, if it lives outside this database.
2. Whether the collector should be a new master, or `CollectedBy` should be replaced by a user id (User Management must not be replaced; the prompt forbids that).
3. Whether rates are global until a BusinessUnit model exists, or BusinessUnit must be introduced first.
4. Whether header discount reduces the commission base. Line `NetAmount` does not include it.
5. Whether Draft invoices are in scope. Only Cancelled was specified for exclusion.
6. Whether `EffectiveTo` may be null. Test Rate requires an end date.

---

## 13. Fields reused vs fields that would have to be created

**Reuse without change (once unblocked):**

- `ReferralDoctorMaster`
- `SaleInvoice.ReferralDoctorId`
- `SaleInvoice.InvoiceDate`
- `SaleInvoice.InvoiceStatus` (Cancelled = 3)
- `SaleInvoiceDetail.TestId`, `Id`, `NetAmount`, `Quantity`, `Rate`, `DiscountAmount`
- `OperationalDateTime` only as the existing standard for dates already stored
- Test Rate overlap **algorithm** (not its table)
- Master CRUD, menu, and `QAuthorize` patterns

**Must not be invented in this stop:**

- Outside/Inside
- Sample Collector master and the link from invoice or request to that master
- BusinessUnit on rates and on invoices
- Commission setup tables (they depend on the three items above)

---

## 14. Production

No production database, Gate 6, report template, invoice, payment, or sample-collection change was made.

---

## 15. What is needed before implementation

Answer these, then implementation can start without guessing:

1. **Sample source.** Add an authoritative Outside/Inside value on the invoice or the test request, or identify an existing field that was not in this codebase.
2. **Collector identity.** Add a Sample Collector master and store its id on the invoice line or test request, or define another existing id that is not `CollectedBy` text.
3. **Business unit.** Introduce the hierarchy and put `BusinessUnitId` on commission setup and on the invoice, or explicitly approve a single global scope and drop BusinessUnit from this phase.
4. **Header discount.** Confirm commission base stays line `NetAmount` (tax excluded, header discount not allocated).
5. **Open end date.** Confirm null `EffectiveTo` or require an end date like Test Rate.

---

COMMISSION MANAGEMENT:
STOPPED — REQUIRED BUSINESS FIELDS ARE MISSING.
NO SOURCE CHANGE WAS MADE.
NO SCHEMA WAS ADDED.
