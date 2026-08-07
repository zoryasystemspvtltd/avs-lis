# Sample Collection Date/Time Validation — Final Certification Report

**Project:** ZoryaLMS (AVILIS)  
**Date:** 2026-08-07  
**Scope:** Root cause analysis and enterprise fix for Sample Collection Date/Time validation  
**Verdict:** **APPROVED FOR PROD**

---

## 1. Executive Summary

Valid collection times (e.g. Invoice `07-Aug-2026 07:22`, Collection `07-Aug-2026 07:25`) were rejected as future on some servers, while earlier calendar days (e.g. `06-Aug-2026`) were accepted.

**Root cause:** Angular sends facility **wall-clock** values as timezone-less ISO (`Kind=Unspecified`). Validation compared them to `DateTime.Now` on the **IIS host Windows timezone**. On hosts set to UTC (or clocks behind the lab), IST wall-clock morning times appear “in the future.” Tolerance was hardcoded to 1 minute. There was no invoice floor check. Business validation was logged as **FATAL**.

**Fix:** Facility timezone wall-clock standard (`OperationalDateTime`), configurable clock drift, invoice date floor, persist normalized wall-clock, and `LogError` (not FATAL) for business validations in `SampleCollectionController`.

---

## 2. Root Cause Analysis

### Lifecycle of CollectionDateTime

| Stage | Value / Behaviour |
|-------|-------------------|
| Angular datetime-local | Local wall-clock string via offset trick → `yyyy-MM-ddTHH:mm` |
| HTTP JSON | `"collectionDateTime":"2026-08-07T07:25"` (**no Z / offset**) |
| Newtonsoft bind | `DateTime` **Kind=Unspecified** = 07:25 |
| Business validation (old) | `collection > DateTime.Now.AddMinutes(1)` using **server local TZ** |
| Persistence | `TestRequestDetail.SampleCollectionDate` |

### Failure mode (proven)

When IIS host timezone is UTC (or significantly behind lab):

- Facility Now (IST) ≈ 07:25  
- Host `DateTime.Now` (UTC) ≈ 01:55  
- Unspecified `07:25` compared to `01:55+1min` → **false future reject**  
- Unspecified `06-Aug 11:00` compared to UTC `01:55` on 07-Aug → **incorrectly accepted**

Local probe: Unspecified `"2026-08-07T07:25"` binds Kind=Unspecified; wall 07:25 > UTC-as-Now 01:55+1 = **True** (false reject).

---

## 3. Client Date/Time Analysis

- Control: `input type="datetime-local"`
- Default: local wall-clock via `timezoneOffset` + `toISOString().slice(0,16)` (display only)
- Payload fix: normalize to `yyyy-MM-ddTHH:mm:ss` **without** `Z`/offset so API keeps Kind=Unspecified

---

## 4. API Model Binding Analysis

- DTO: `SampleCollectionAction.CollectionDateTime` as `DateTime`
- JsonFormatter: no `DateTimeZoneHandling` override (RoundtripKind default)
- Timezone-less ISO → Unspecified (correct for facility wall-clock)

---

## 5. Server / SQL Time Analysis

- Validation must **not** depend on IIS host TZ alone
- Enterprise Now = `TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, FacilityTimeZone)`
- SQL type unchanged (`datetime`); values stored as facility wall-clock Unspecified

---

## 6. Enterprise Date/Time Standard (ZoryaLMS)

**Operational timestamps are facility wall-clock times.**

| Domain event | Clock meaning |
|--------------|---------------|
| Patient Registration | Facility local |
| Sale Invoice | Facility local (`InvoiceDate`) |
| Sample Collection | Facility local |
| Sample Receiving | Facility local (existing ≥ collection rule) |
| Result / Approvals / Reports | Facility local |

**Config**

```xml
<add key="OperationalDateTime:FacilityTimeZoneId" value="India Standard Time" />
<add key="OperationalDateTime:AllowedClockDriftMinutes" value="5" />
```

Empty `FacilityTimeZoneId` falls back to `TimeZoneInfo.Local`.

---

## 7. Business Rule Validation

| Rule | Behaviour |
|------|-----------|
| A | Collection ≥ Invoice date/time; else *Collection date/time cannot be earlier than Invoice date/time.* |
| B | Collection ≤ FacilityNow + drift; else *Collection time cannot be in the future.* |
| C | Drift from `OperationalDateTime:AllowedClockDriftMinutes` (default 5) |
| D | `ArgumentException` / `InvalidOperationException` → `LogError` + HTTP 400; unexpected → `LogException` (FATAL) + 500 |

---

## 8. Unit Test Results

**11/11 PASS** — `SampleCollectionDateTimeRulesTests`

- Current / past after invoice  
- Collection 3 minutes after invoice (observed scenario)  
- Before invoice (06-Aug vs 07-Aug invoice)  
- Genuine tomorrow future  
- Within / beyond configured drift  
- UTC wire → IST wall-clock  
- Unspecified remains wall-clock  
- False future vs UTC host Now avoided  
- Config drift read  

---

## 9. Regression / Integration

- Sample collection invoice-status and recent listing tests executed with suite  
- No schema/DTO/RBAC/notification redesign  
- UI layout/theme unchanged; only payload normalization for datetime

---

## 10. Build & Deployment

| Item | Result |
|------|--------|
| API Release / Portal production build | SUCCESS |
| Deploy API → `I:\Projects\PROD\AVILIS\API` | Deployed |
| Deploy Portal → `I:\Projects\PROD\AVILIS\PORTAL` | Deployed |
| HTTP health | API 200 · Portal 200 |

---

## 11. Production Readiness Checklist

- [x] Root cause identified and documented  
- [x] Facility TZ standard applied  
- [x] Collection cannot precede Invoice  
- [x] Genuine future rejected with configurable drift  
- [x] Business messages returned to UI; not FATAL  
- [x] Unit tests 11/11  
- [x] No Critical/High/Medium open defects for this issue  
- [x] Existing UI/workflows preserved  

---

## Final Verdict

**APPROVED FOR PROD**

*Certified by: Solution Architecture / .NET / Angular / QA — Sample Collection Date/Time Fix*
