# Enterprise Operational Date/Time Standardization — Certification Report

**Project:** ZoryaLMS (AVILIS)  
**Date:** 2026-08-07  
**Phase:** Operational Date/Time Standardization (post Sample Collection Phase 1)  
**Verdict:** **APPROVED FOR PROD**

---

## 1. Executive Summary

Phase 1 introduced `OperationalDateTime` for Sample Collection. This phase completed the **business operational** timestamp standard across Sale Invoice, Patient Visit, Sample Receiving, Lab Result Entry, Technician/Doctor Approval, Radiology authorization/accession day, and report date fallback — without redesigning architecture, UI, RBAC, notifications, or schema.

**Infrastructure timestamps** (`CreatedOn`/`ModifiedOn`, OAuth, notification retry/expiry, secure tokens, heartbeat, logging) were **explicitly left on `DateTime.Now` / `DateTime.UtcNow`**.

---

## 2. Scope of Migration

| Module | Migrated operational fields | Left unchanged |
|--------|----------------------------|----------------|
| Sale Invoice | `InvoiceDate` default + normalize; `INV-yyyyMMdd` day; rate `asOf` day; auto request collection/received stamps | `CreatedOn`/`ModifiedOn`; parameter `CreatedOn` UtcNow |
| Patient Visit | `VisitDateTime` | `CreatedOn`/`ModifiedOn` |
| Sample Collection | Confirmed (Phase 1) | — |
| Sample Receiving | Normalize `ReceivedDateTime`; ≥ collection; ≤ facility now + drift | — |
| Lab Result Entry | `ResultDate` (manual + control) | Detail/result `CreatedOn` |
| Technician / Doctor Approval | `ReviewDate` / `AuthorizationDate` + note clocks | — |
| Radiology | Accession day; `AuthorizedOn` | `CreatedOn`/`ModifiedOn` |
| Report print | `ReportDate` fallback | — |
| HIS External API | Collection/received stamps on import | — |

---

## 3. Modules Explicitly Excluded (Infrastructure)

- Audit: `CreatedOn`, `ModifiedOn`
- Notification retry / `NextRetryOn` / template effective window
- Secure download token expiry
- OAuth / JWT
- Equipment heartbeat
- Logging / IIS diagnostics

---

## 4. Enterprise Standard Compliance

- Facility TZ: `OperationalDateTime:FacilityTimeZoneId` = `India Standard Time`
- Drift: `OperationalDateTime:AllowedClockDriftMinutes` = `5`
- Angular receiving payload normalized to wall-clock `yyyy-MM-ddTHH:mm:ss` (no `Z`)
- Chronology: Invoice ≤ Collection ≤ Receiving (validated); approvals stamped with facility now after prior stages in normal workflow

---

## 5. Unit / Regression Tests

| Suite | Result |
|-------|--------|
| SampleCollectionDateTimeRulesTests | 11/11 PASS |
| OperationalDateTimeStandardizationTests | 10/10 PASS |
| SampleCollectionInvoiceStatusTests + RecentSampleListing | 8/8 PASS |
| TestResultEditManagerTests | 5 PASS, 3 skipped (pre-existing) |
| **Total executed** | **34 PASS / 3 skipped** |

---

## 6. Build & Deployment

| Item | Result |
|------|--------|
| API Release + Portal production | SUCCESS |
| Deploy `I:\Projects\PROD\AVILIS\API` | Deployed |
| Deploy `I:\Projects\PROD\AVILIS\PORTAL` | Deployed |
| Health | API **200** · Portal **200** |
| Schema / UI theme | Unchanged |

---

## 7. Remaining Risks

- Historical rows may still contain host-TZ-skewed operational values (read-only legacy data).
- Dashboard overdue cutoffs still use host `DateTime.Now` (classified infrastructure/SLA; out of this migration scope per Rule 5/exclusion of dashboard framework redesign).
- Invoice auto-create still pre-stamps collection/received (existing behaviour); clock source only changed to facility now.

---

## 8. Production Readiness Checklist

- [x] Operational business timestamps use `OperationalDateTime` where in scope  
- [x] Infrastructure timestamps unchanged  
- [x] No DB schema changes  
- [x] No UI layout/theme changes  
- [x] Auth/RBAC/notification/report frameworks not redesigned  
- [x] Unit/regression tests pass  
- [x] IIS deploy healthy  

---

## Final Verdict

**APPROVED FOR PROD**

*Certified by: Solution Architecture / .NET / QA — Operational Date/Time Standardization*
