# Sample Collection DateTime — Regression Review

**Date:** 2026-09-26  
**Scope:** Forensic review of the previously approved Sample Collection DateTime wall-clock fix  
**Classification:** **A. NO REGRESSION FOUND**

---

## 1. Executive Summary

The earlier Sample Collection DateTime fix is still present and still behaves as approved.

Angular still sends a timezone-less `yyyy-MM-ddTHH:mm:ss` value. The API binds that string as `DateTimeKind.Unspecified`. `SampleCollectionManager.CollectSample` still normalizes with `OperationalDateTime.ToFacilityWallClock` and validates with `SampleCollectionDateRules` against invoice time and facility-now plus configured drift. Facility timezone remains `India Standard Time`. Drift remains **5** minutes.

No source, configuration, database, or production deployment change was made.

---

## 2. Previous approved behavior baseline

| Rule | Baseline |
|------|----------|
| Facility time | Configured timezone; this deployment uses **India Standard Time** |
| Wire format | Timezone-less `yyyy-MM-ddTHH:mm:ss` (no `Z`, no offset) |
| Unspecified Kind | Treated as facility wall-clock; not converted again |
| Utc / Local Kind | Converted once into the facility timezone |
| Floor | Collection DateTime **>=** Invoice DateTime |
| Ceiling | Collection DateTime **<=** FacilityNow + configured drift |
| Authority | Server validation (`SampleCollectionDateRules`) |
| Excluded | CreatedOn, tokens, logging, and other technical timestamps |

---

## 3. Current implementation trace

Example entered by the user (this machine, IST): **2026-09-26 13:08**.

| Step | Actual behavior |
|------|-----------------|
| UI default | `prepareCollectModal` shifts by `getTimezoneOffset()` then `toISOString().slice(0, 16)` so the `datetime-local` value is the **browser wall-clock**, not UTC |
| Angular sends | `toFacilityWallClockPayload` → `2026-09-26T13:08:00` (no `Z`) |
| API receives | Newtonsoft default (`DateTimeZoneHandling.RoundtripKind`, not overridden in `WebApiConfig`) binds `"2026-09-26T13:08:00"` as **Unspecified** `2026-09-26 13:08:00` |
| Business layer | `OperationalDateTime.ToFacilityWallClock` keeps Unspecified digits; `SampleCollectionDateRules.Validate` compares to invoice wall-clock and `GetFacilityNow()` + drift |
| Database | `TestRequestDetail.SampleCollectionDate` is SQL `datetime` (no offset). Assigned the normalized wall-clock value |
| API returns | `MapRow` copies `request.SampleCollectionDate` with no Kind rewrite |
| UI display | Receiving queue formats `sampleCollectionDate` with the Angular date pipe. A timezone-less ISO string is shown as those clock digits |

No unexpected UTC shift, extra offset, dropped seconds, or added milliseconds was found on the timezone-less path.

A `Z` value is bound as **Utc** and converted **once** to facility wall-clock (`07:38Z` → `13:08` IST). That is the existing approved helper, covered by `Utc_Wire_Value_Is_Converted_To_Facility_Wall_Clock`. It is not a second conversion of the timezone-less payload.

---

## 4. UI → API → Business → DB → Read-back

**UI** — `web/Lis.Web/src/app/LIS/sample-workflow/sample-collection/sample-collection.component.ts`

- Default uses the timezone-offset compensation before `toISOString`.
- Submit uses `toFacilityWallClockPayload` and posts `{ collectionDateTime }` through `SampleWorkflowService.collectSample`.

**API** — `SampleCollectionController.Collect` → `ISampleCollectionManager.CollectSample`. No custom DateTime converter.

**Business** — `SampleCollectionManager.CollectSample`:

```csharp
var collectionWallClock = OperationalDateTime.ToFacilityWallClock(action.CollectionDateTime);
SampleCollectionDateRules.Validate(
    collectionWallClock,
    invoice?.InvoiceDate,
    OperationalDateTime.GetFacilityNow(),
    OperationalDateTime.GetAllowedClockDriftMinutes());
request.SampleCollectionDate = collectionWallClock;
```

**DB** — column type `datetime` (migration `202605170711055_fresh`). No timezone stored.

**Read-back** — same property copied onto `SampleWorkflowQueueRow.SampleCollectionDate`.

---

## 5. Facility timezone analysis

| Source | Value |
|--------|--------|
| `web/Lis.Api/Web.config` | `OperationalDateTime:FacilityTimeZoneId` = `India Standard Time` |
| PROD `I:\Projects\PROD\AVILIS\API\Web.config` | Same key = `India Standard Time` (read only; not modified) |
| `GetFacilityNow()` | `ConvertTimeFromUtc(DateTime.UtcNow, facilityTz)` then `DateTimeKind.Unspecified` |
| Missing/invalid zone | Falls back to `TimeZoneInfo.Local` (existing behavior; config is valid IST, so this fallback is not active) |

Validation uses facility now, not `DateTime.Now` on the IIS host. The prior false-future case (IST morning vs UTC host clock) remains covered by `False_Future_Against_Utc_Server_Clock_Is_Avoided_When_Using_Facility_Now`.

---

## 6. Drift validation

Configured drift: **`OperationalDateTime:AllowedClockDriftMinutes` = 5** (source Web.config and PROD Web.config).

`SampleCollectionDateRules` rejects `collection > facilityNow.AddMinutes(drift)` with `Collection time cannot be in the future.`  
It rejects `collection < invoice` with `Collection date/time cannot be earlier than Invoice date/time.`

---

## 7. Test matrix

| ID | Case | Expected | How verified |
|----|------|----------|----------------|
| 1 | Collection after invoice, within now | PASS | `Collection_Three_Minutes_After_Invoice_Passes`, `Past_Timestamp_After_Invoice_Passes` |
| 2 | Collection equals the reference “now” used as both collection and facility now, after invoice | PASS | `Current_Timestamp_Within_Drift_Passes` |
| 3 | Collection before invoice | REJECT | `Collection_Before_Invoice_Fails` |
| 4 | Collection after facility now + drift | REJECT | `Collection_Beyond_Configured_Clock_Drift_Fails`, `Genuine_Future_Beyond_Drift_Fails` |
| 5 | Collection inside configured drift (+4 min, drift 5) | PASS | `Collection_Within_Configured_Clock_Drift_Passes` |
| 6 | Browser timezone | Digits are not UTC-shifted | Node replay of the UI default under `Asia/Kolkata`, `UTC`, and `America/New_York`: sent value matched that timezone’s wall clock (`match: true`). Raw `toISOString` without the offset compensation would have sent UTC (`13:08` IST → `07:38Z`) |
| 7 | Timezone-less payload | Unspecified wall-clock kept | Newtonsoft deserialize of `"2026-09-26T13:08:00"` → Kind **Unspecified**, value **13:08:00**. Unit test `Unspecified_Local_Serialization_Remains_Wall_Clock` |
| 8 | Read-back | Same business clock | Source: SQL `datetime` stores clock components; `MapRow` returns them unchanged. No live row was written |
| 9 | Workflow path | Same rules inside collection | Source: `CollectSample` is the workflow entry and calls the same helper/rules. Integration tests that call `CollectSample` were **not** re-run because they write `ZoryaLMS` |

---

## 8. Test results

Executed:

```
vstest LIS.Masters.Tests.dll
  /TestCaseFilter: FullyQualifiedName~SampleCollectionDateTimeRulesTests|FullyQualifiedName~OperationalDateTimeStandardizationTests
```

| Result | Count |
|--------|-------|
| Passed | **21** |
| Failed | **0** |
| Exit | **0** |

Newtonsoft binding check (read-only, no app change):

| Payload | Kind | Value |
|---------|------|--------|
| `2026-09-26T13:08:00` | Unspecified | 2026-09-26 13:08:00 |
| `2026-09-26T07:38:00Z` | Utc | 2026-09-26 07:38:00 |
| `2026-09-26T13:08:00+05:30` | Local | 2026-09-26 13:08:00 |

Browser-clock replay (no source change): IST `2026-09-26T13:08:00` sent as `2026-09-26T13:08:00`, not `2026-09-26T07:38:00`.

---

## 9. Regression classification

**A. NO REGRESSION FOUND**

The approved helper, rules, Angular payload, facility timezone, and drift setting are still in the collection path. Recent Report Template Portal work did not touch this path.

---

## 10. Exact source changes

**NONE**

---

## 11. Files changed

**NONE**

---

## 12. Files intentionally NOT changed

| Area | Reason |
|------|--------|
| `OperationalDateTime.cs` | Still the approved normalizer |
| `SampleCollectionDateRules.cs` | Invoice floor and drift ceiling intact |
| `SampleCollectionManager.CollectSample` | Still calls both |
| Sample Collection Angular component | Still sends timezone-less wall-clock |
| Receiving, invoice, CreatedOn, tokens, Gate 6 | Out of scope; not part of a proven collection regression |
| Web.config / PROD config | IST + drift 5 already correct |
| Database | No schema or data issue found that requires a change |

---

## 13. Build result

**NOT RUN.** No source change. The existing Release test assembly executed the focused tests successfully.

---

## 14. Existing regression suite result

Focused DateTime suites: **21/21 PASS**.

Full Masters suite: **NOT RUN** (not required when no fix is made; it also writes `ZoryaLMS`).

`SampleCollectionInvoiceStatusTests` / `RecentSampleListingTests`: **NOT RUN** (they call `CollectSample` and persist to `ZoryaLMS`).

---

## 15. Database status

**NO CHANGE.** No migration, no SQL, no production data update.

---

## 16. Configuration status

**NO CHANGE.**

| Key | Value observed |
|-----|----------------|
| `OperationalDateTime:FacilityTimeZoneId` | `India Standard Time` |
| `OperationalDateTime:AllowedClockDriftMinutes` | `5` |
| Gate 6 `ReportTemplate:UseDeclarativeRendererForProductionPrint` | Not modified |

---

## 17. PROD deployment status

**NOT PERFORMED.** Not authorized by this task.

---

## 18. Known limitations

1. A browser whose OS timezone is not India Standard Time defaults the picker to **that OS clock**. Those digits are stored as facility wall-clock and are **not** converted to IST first. That matches the approved `datetime-local` / Unspecified contract. It is not the previously fixed UTC-shift defect. Lab PCs set to IST send IST digits.
2. End-to-end save/read against `ZoryaLMS` was not executed, so no customer or test sample row was created. Persistence behavior is taken from the column type and the direct assignment in `CollectSample`.
3. An explicit `Z` or offset on the wire is still converted once into facility time. The UI does not send that form.

---

## 19. Answers

| Q | Answer |
|---|--------|
| Q1 Facility wall-clock normalization | **Yes.** `ToFacilityWallClock` + `GetFacilityNow` |
| Q2 Angular timezone-less format | **Yes.** `yyyy-MM-ddTHH:mm:ss` |
| Q3 API interprets correctly | **Yes.** Unspecified digits kept |
| Q4 Invoice comparison | **Yes.** |
| Q5 FacilityNow + drift | **Yes.** Drift = 5 |
| Q6 Facility TZ vs machine TZ | **Yes** for the server bound. Client default uses the browser clock digits, then the server treats them as facility wall-clock |
| Q7 Other browser timezone | Digits are **not** shifted to UTC. They follow the browser clock. They are not re-converted into IST |
| Q8 UTC/offset double conversion | Timezone-less values are **not** converted. `Z` is converted **once** |
| Q9 Read-back | **Yes** at the mapping layer (SQL `datetime`, no Kind rewrite). Live row not written |
| Q10 Helper bypassed | **No.** `CollectSample` still calls `OperationalDateTime` and `SampleCollectionDateRules` |
| Q11 Tests still protect it | **Yes.** 21 focused tests passed, including the UTC-host false-future case |

---

SAMPLE COLLECTION DATETIME REGRESSION REVIEW:
PASS — NO REGRESSION FOUND.
NO SOURCE CHANGE REQUIRED.
