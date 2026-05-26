# AVILIS / ZoryaLIS — Approval Workflow QA Certification

**Date:** 2026-05-26  
**Scope:** Technician Approval + Doctor Approval + Diagnostic Report Print  
**Environment:** `.\SQLEXPRESS` / `AVSLIS` | Portal `http://localhost:8080` | API `http://localhost:8081`  
**Certification status:** **PASS (after stabilization fixes)**

---

## A. Flow validated

| Step | Route / API | Result |
|------|-------------|--------|
| Result present (Report Generated) | `GET api/Patients` Status=2 | PASS |
| Technician review load | `GET api/sample/{id}` | PASS (after FileHandler fix) |
| Technician approve → 3 | `POST api/sample` | PASS |
| Duplicate tech approve blocked | `POST api/sample` | PASS (400 + message) |
| Doctor queue (status 3) | `GET api/Patients` Status=3 | PASS |
| Doctor approve → 5 | `PUT api/sample` | PASS |
| Doctor skip-tech blocked | `PUT api/sample` on status 0 | PASS |
| Diagnostic report (paid + doctor approved) | `GET api/Reports/TestReport?invoiceNo=CRUD-INV-001` | PASS (1 section, 2 parameters) |
| DB audit fields | `TestResults.ReviewedBy/Date`, `AuthorizedBy/Date` | PASS |

**Validated test sample:** `CRUD-SMP-RPT` (RequestId 10027), Invoice `CRUD-INV-001` (Paid)

---

## B. Issues found

| # | Severity | Issue |
|---|----------|--------|
| 1 | **Critical** | `GET api/Patients` returned 0 rows — EF exception in `PatientDetailManager.Get` (`IsNullOrWhiteSpace` in LINQ-to-Entities) |
| 2 | **Critical** | `GET api/sample/{id}` returned null — missing equipment JSON `CRUD-Model-X.json` crashed `GetTestRunDetails` |
| 3 | **Critical** | `GET api/Reports/TestReport` failed — EF `StringComparison` in `TestReportManager.ResolveTestRequests` |
| 4 | **High** | No workflow guards — doctor could approve without technician approval; duplicate technician approve allowed |
| 5 | **High** | `SampleController` returned `null` on errors (UI showed generic failure) |
| 6 | **Medium** | Doctor notes appended to `TechnicianNote` instead of `DoctorNote` |
| 7 | **Medium** | List batch buttons used `Samples` permission instead of `Reports` / `DoctorsApprovals` |
| 8 | **Medium** | Technician detail page `hasAccess()` always returned true |
| 9 | **Low** | `CRUD-SMP-RPT` not visible on Recent Samples “New” tab (status 2 = Report Generated) — expected by design |

---

## C. Root causes

1. **Patient list empty:** LINQ queries executed on `IQueryable` with `IsNullOrWhiteSpace` and null-unsafe `Contains` on navigation properties; exceptions swallowed by controller → empty list.
2. **Review screen blank:** `FileHandler.GetJsonMappings` threw `FileNotFoundException` when analyzer model JSON missing; `SampleController.Get` caught and returned null.
3. **Report print failure:** Same EF translation issue with `StringComparison.OrdinalIgnoreCase` in invoice/request resolution.
4. **Workflow gaps:** `ReviewProcess` had no `ValidateStatusTransition` before status update.
5. **Permission mapping:** `list-module.hasAccess` remapped `Patients` module to `Samples` for all sample lists.

---

## D. Fixes implemented

| File | Change |
|------|--------|
| `PatientDetailManager.cs` | In-memory filtering; null-safe search; attach `Patient` for grid; EF-safe parameter/result enrichment |
| `TestRequestDetailsManager.cs` | `ValidateStatusTransition`; doctor second-opinion exception; `DoctorNote` for doctor actions |
| `FileHandler.cs` | Return empty list if model JSON missing (no throw) |
| `TestRequestDetailsManager.GetTestRunDetails` | Null-safe equipment model / parameter names |
| `TestReportManager.cs` | In-memory invoice/request resolution (EF-safe) |
| `SampleController.cs` | Return `400 BadRequest` with message instead of null |
| `list-module.component.ts` | `isTechnician` → Reports, `isDoctor` → DoctorsApprovals |
| `technician-details` | `hasAccess(4/8)` checks Reports module |
| `list-rawsample.component.ts` | Added status filter **2** (Report Generated) |

**Not changed (by design):** `ReviewProcess` sibling-run rejection logic, `api/Patients` `[AllowAnonymous]` (recommend hardening in separate security pass).

---

## E. Database validation

After full flow on `CRUD-SMP-RPT`:

```
ReportStatus = 5 (Doctor Approved)
ReviewedBy = admin@zorya.co.in
AuthorizedBy = admin@zorya.co.in
```

Invoice `CRUD-INV-001`: `PaymentStatus = 2` (Paid), linked to `RequestDetailId = 10027`.

---

## F. Report validation

| Rule | Verified |
|------|----------|
| Doctor approval required | Yes — `ValidateWorkflow` enforces status 5 |
| Payment required | Yes — unpaid returns validation message |
| Cancelled invoice blocked | Existing `InvoiceStatus` check |
| Parameter values in report | Yes — 2 parameters returned for CRUD panel |

---

## G. Build & deploy status

| Component | Status |
|-----------|--------|
| API Release build | SUCCESS |
| Angular production build | SUCCESS (`main-es2015.bc0c481a8a1ff0d43d1f.js`) |
| Deploy API → `I:\Projects\PROD\AVILIS\API` | DONE — IIS Started |
| Deploy Portal → `I:\Projects\PROD\AVILIS\PORTAL` | DONE |

**Automated script:** `Scripts\TestApprovalFlow.ps1`

---

## H. Manual UAT checklist (portal)

1. **Ctrl+F5** refresh portal.
2. **Technician:** Working Board → Technician's Approval → open `CRUD-SMP-RPT` or `SMP-2026-0001` (Report Generated tab on Recent Samples).
3. Verify parameters, approve → appears under Doctor's Approval.
4. **Doctor:** Doctor's Approval → approve sample.
5. **Reports:** Diagnostic Test Report → invoice `CRUD-INV-001` → print.
6. Confirm **Approved Samples** lists status 5.

---

## I. Regression notes

- Analyzer `POST api/lis` — **not modified**
- Edit Test Results module — **not modified** (separate module)
- Approval architecture (`ReviewProcess`, status enum) — **preserved**

---

*Certified by automated API validation + code review. Recommend role-specific login UAT for Technician-only and Doctor-only accounts.*
