# Comprehensive Recent Changes — E2E Regression & Business Validation Certification Report

**Project:** ZoryaLMS / AVILIS  
**Exercise type:** QA blocker removal + authenticated evidence generation (no application functionality changes)  
**Date:** 2026-09-12 (blocker-removal re-run)  
**Environment:** Portal `http://localhost:8080` · API `http://localhost:8081` · DB `ZoryaLMS` on `.\SQLEXPRESS`  
**Deployed roots:** `I:\Projects\PROD\AVILIS\API` · `I:\Projects\PROD\AVILIS\PORTAL`

---

## 1. Executive Summary

Previous validation-only pass reported **38 PASS / 0 FAIL / 67 BLOCKED / 13 SKIPPED** and **NOT APPROVED**, primarily due to toolchain and authenticated-execution blockers — **not** because defects were proven.

This follow-up **removed executable blockers where possible** (ADO.NET SqlClient harnesses, authenticated OAuth tokens, controlled SampleNo invoices, report/print/RBAC/layout/collection/receiving probes) and **did not modify application code** to force green results.

| Metric | Previous pass | This re-run (executed evidence) |
|--------|--------------:|--------------------------------:|
| Planned matrix (historical) | 118 | 118 (reassessed) |
| PASS (this re-run harnesses + node + same-day Karma) | 38 | **84** (see §13) |
| FAIL | 0 | **0** |
| BLOCKED (material remaining) | 67 | **18** (see §35) |
| SKIPPED | 13 | **4** |
| Critical / High / Medium defects | 0 / 0 / 0 | **0 / 0 / 0** |
| Low observations | 1 | **2** (OBS-01, OBS-02) |

**QA Verdict:** **NOT APPROVED FOR PRODUCTION**

**BA Verdict:** **NOT APPROVED — residual material gaps remain**

Rationale: SampleNo persistence, Report Layout RBAC, anonymous API security, Print All/Specific/ownership, layout Admin save, collection/receiving smoke, OperationalDateTime future-reject, Enter-key automated suite, and mock-provider configuration now have **reproducible PASS evidence**. Material gaps remain: full `LIS.Masters.Tests` (no MSBuild/VSTest), Technician signature binary on disk (0 users), Doctor-approve → paid/partial notification E2E (not executed by design this pass), Receptionist identity, and browser Enter/Tab live UAT.

---

## 2. Previous QA Status

From prior report (same day, validation-only):

- 118 planned · 38 PASS · 0 FAIL · 67 BLOCKED · 13 SKIPPED  
- Verdict: **NOT APPROVED FOR FULL PRODUCTION SIGN-OFF**  
- Root cause of BLOCKED: MSBuild/VSTest missing, sqlcmd ODBC broken, authenticated E2E not run, SampleNo table resolution incomplete.

This re-run treats prior BLOCKED as **candidates to re-execute**, not as defects.

---

## 3. Environment

| Item | Value | Status |
|------|-------|--------|
| Portal | `http://localhost:8080` | HTTP **200** |
| API | `http://localhost:8081` | HTTP **200** |
| DB | `ZoryaLMS` / `.\SQLEXPRESS` | ADO.NET **PASS** |
| Access key | `DXI800` (existing) | Used for TOKEN/API |
| Facility TZ (deployed Web.config) | `India Standard Time` | **PASS** |
| `Notification:UseMockProviders` | `true` | **PASS** |
| `NotificationConfiguration.IsEnabled` | `1` | OBS-01 (mock required) |

---

## 4. Toolchain Validation (Stage 1)

| Tool | Status | Evidence |
|------|--------|----------|
| .NET SDK | Present | `dotnet` 8.0.425 |
| Node / npm | Present | node v16.20.2 / npm 8.19.4 |
| MSBuild (VS) | **BLOCKED** | No VS2022 Community/BuildTools MSBuild on disk |
| VSTest | **BLOCKED** | `vstest.console.exe` not found |
| Prebuilt `LIS.Masters.Tests.dll` | Present | `LIS.Masters.Tests\bin\Release\` |
| sqlcmd | **BLOCKED** | ODBC “Data source name not found”; **mitigated** via ADO.NET SqlClient harnesses |
| Angular CLI / Karma | Available in portal | Enter-key 11/11 same-day; full `ng test` still blocked by unrelated broken specs |

**Mitigation (not a product change):**  
- `Scripts/QaComprehensiveRegression.ps1`  
- `Scripts/QaStage34ReportsNotifications.ps1`

---

## 5. Authentication Validation

| Identity | Result | Notes |
|----------|--------|-------|
| Admin `admin@zorya.co.in` | **PASS** | Bearer token issued |
| Technician `qa-cert-tech@zorya.co.in` | **PASS** | Bearer token issued |
| Doctor `qa-cert-doctor@zorya.co.in` | **PASS** | Bearer token issued |
| Receptionist QA user | **SKIPPED** | TOKEN 400 — user not available / invalid for this env |

No authentication bypass, anonymous endpoints, or RBAC weakening was introduced.

---

## 6. Database Validation

| Check | Result |
|-------|--------|
| Connectivity | **PASS** |
| SampleNo persistence table | **`TestRequestDetails`** (not a `Sample` table) |
| Multi-specimen SampleNo rows | **PASS** (see §14) |
| Report Layout rows | Diagnostic + Radiology present |
| Admin RLC module map | 1 |
| Non-Admin RLC maps | 0 |
| Menu `SETUP_REPORT_LAYOUT_CONFIGURATION` | present |
| NotificationAudit | 14 rows (historical; ProviderName channel label `Sms`) |
| Doctor signature paths | count **2** |
| Technician signature paths | count **0** → **BLOCKED** for tech-sig print proof |

---

## 7. API Validation

| Check | Result |
|-------|--------|
| Host `/` | 200 |
| Authenticated Report Layout GET | Admin **PASS**; Tech/Doctor **denied** |
| Authenticated TestReport Print All/Specific | **PASS** |
| Print ownership mismatch | **PASS** (HTTP 400) |
| SampleCollection PendingQueue | **PASS** (tech + ApiOption) |
| SampleReceiving Queue | **PASS** |
| Collect / Receive (QA seed) | **PASS** |
| Future collection datetime | **PASS** (rejected) |
| TestResultEdit search | **PASS** |

---

## 8. Portal Validation

| Check | Result |
|-------|--------|
| Portal HTTP 200 | **PASS** |
| Interactive Admin menu click / visual print | **BLOCKED** (no browser automation) |
| Enter-key live Tab/Shift+Tab UAT | **BLOCKED** |

---

## 9. Stage 1 Results — Environment / Toolchain Blockers

**Exit criteria:**

1. Toolchain — partial: Node/dotnet OK; MSBuild/VSTest **BLOCKED**  
2. Build — not re-built this pass (no app code change; deployed host healthy)  
3. Test-runner — Masters suite **BLOCKED**; Angular Enter-key historical **PASS**; Node semantics **10/10 PASS**  
4. Authentication — Admin/Tech/Doctor **PASS**; Receptionist **SKIPPED**  
5. Database — **PASS** (ADO.NET)  
6. API — **PASS** (200 + auth matrix)  
7. Portal — **PASS** (200)  
8. Remaining blockers — MSBuild/VSTest, Receptionist user, browser UAT, tech signature binaries, Doctor-approve→notify E2E

---

## 10. Stage 2 Results — Authenticated Backend E2E

### 2.1 Patient / Visit
- Existing patient used for invoice creation (`PatientMaster` page 1).  
- Full new-patient + VisitId architecture deep-dive: **not re-architected**; VisitId architecture unchanged.  
- Controlled invoices created and cancelled after SampleNo checks.

### 2.2 Invoice / SampleNo — **PASS**

Persisted in **`TestRequestDetails`**:

| Scenario | Evidence |
|----------|----------|
| Multi-specimen | `INV-20260912-0007` → `202609120007SER` (GLU/SER) + `202609120007EDT` (HBA1C/EDT) |
| Format | No `INV`, no hyphen |
| Same specimen reuse | Two SER lines → unique SampleNo count = 1 (`202609120008SER`) |
| Cleanup | Multi-specimen invoice cancelled after verify |

`HISRequestNo` / `HISRequestId` remain on `TestRequestDetails` (unchanged architecture).

### 2.3 OperationalDateTime — **PARTIAL PASS**
- Deployed facility TZ = India Standard Time — **PASS**  
- Future collection beyond allowed drift — **PASS** (rejected)  
- Full Collection≥Invoice / Receiving≥Collection matrix across all migrated fields — **BLOCKED** (Masters unit suite + broader negative matrix not re-run without VSTest)

### Workflow smoke (QA seed)
| Step | Result |
|------|--------|
| Collection queue | PASS |
| Collect `QA-CERT-SMP-PEND` | PASS |
| Receiving queue | PASS |
| Receive `QA-CERT-SMP-COLL` | PASS |
| Results entry → Tech approve → Doctor approve → Notification generate | **BLOCKED** (not executed; avoid uncontrolled approve / real business notify path) |

---

## 11. Stage 3 Results — Report / RBAC / Signature / Enter-key

### 3.1 Diagnostic Report — **PASS** (API evidence)
- Order: `INV-20260823-0002` (includes LFT among approved content)  
- Print All: `contentGroups=4` (departmentGroups + profileGroups) — **PASS**  
- Print Specific (`testRequestDetailId=1891`): `specific=2` ≤ all — **PASS**  
- Ownership negative (foreign detail `1886` on this invoice): HTTP **400** — **PASS**  
- Layout enrichment on report DTO: headerMm=50, doctorSig=True, techSig=True — **PASS**  
- Visual multi-page/stationery browser print: **BLOCKED**

### 3.2 Reference Range — **PASS** (pattern scan)
- Report JSON scanned for verbose `Both ##.## - ##.## Year` fallback — **not found** — **PASS**  
- Full male/female/age applicability matrix via Masters tests: **BLOCKED** (VSTest)

### 3.3 Report Layout Configuration — **PASS** (API + RBAC)
- Admin GET Diagnostic/Radiology — **PASS**  
- Admin POST save Diagnostic (tech signature enabled) — **PASS**  
- Tech POST denied — **PASS**  
- Header/Footer **content** not modified; clearance/signature geometry only.

### 3.4 Doctor Signature — **PARTIAL PASS**
- Users with `DoctorSignaturePath`: **2** — **PASS** (source exists)  
- Visual report signature rendering in browser: **BLOCKED**

### 3.5 Technician Signature — **BLOCKED**
- Technician users with signature path: **0**  
- Exact blocker: no Technician identity currently has uploaded signature binary/path in DB.  
- Attempted: SQL count on `AspNetUsers.DoctorSignaturePath` for role Technician.  
- Required to unblock: upload signature via existing User Create/Edit for a Technician QA user, then re-fetch TestReport.

### 3.6 Enter-key Navigation — **PARTIAL PASS**
- Karma automated: **11/11 PASS** (same-day cert `Diagnostic-Report-Entry-Enter-Key-Navigation-Certification-Report.md`)  
- Live browser Tab/Shift+Tab: **BLOCKED** (no browser driver)

### 3.7 RBAC — **PASS** (API + DB + Node)
- Admin module + menu present; non-Admin maps = 0  
- Tech/Doctor denied Report Layout API  
- Tech has SampleCollection; lacks ReportLayoutConfiguration  
- Doctor has DoctorsApprovals; lacks ReportLayoutConfiguration  
- `qa-menu-permission-semantics.js`: **10/10 PASS**

---

## 12. Stage 4 Results — Notification + Final Regression

### 4.1 Notification safety — **PASS** (config)
- `IsEnabled=1` documented (OBS-01)  
- Deployed `Notification:UseMockProviders=true` — **PASS**  
- No real SMS/WhatsApp send invoked in this exercise  
- Secure download invalid token: HTTP **400**, opaque body — **PASS** (comprehensive harness)

### 4.2 Notification business rules — **BLOCKED / PARTIAL**
| Rule | Status |
|------|--------|
| Paid report → notify + secure link | **BLOCKED** (Doctor Approve not triggered) |
| Partial/outstanding → notify without link | **BLOCKED** |
| Disabled → zero behavior | **BLOCKED** (IsEnabled=1 locally) |
| Missing phone skip | **BLOCKED** |
| Duplicate prevention | **BLOCKED** |
| Failure does not block approval | **BLOCKED** |
| Retry / success no-retry | **BLOCKED** |
| Invalid/expired/used token | Invalid **PASS**; expired/used **BLOCKED** (no controlled tokens generated this pass) |

Historical `NotificationAudit` exists (14 rows). ProviderName column shows channel label `Sms` — **not** used alone as proof of mock vs live; mock gate is Web.config `UseMockProviders=true`.

---

## 13. Complete Regression Matrix (reassessed)

| ID | Area | Result | Evidence source |
|----|------|--------|-----------------|
| A | SampleNo | **PASS** | QaComprehensiveRegression |
| B | OperationalDateTime | **PARTIAL** | TZ + future reject PASS; full suite BLOCKED |
| C | Notifications | **PARTIAL** | Mock/config/invalid download PASS; paid/partial E2E BLOCKED |
| D | Dashboard | **PASS** | Admin Registration metrics; anonymous 401 |
| E | Diagnostic print layout | **PARTIAL** | API layout enrichment PASS; visual BLOCKED |
| F | Report Layout Config | **PASS** | Admin GET/POST; Tech deny |
| G | Doctor Signature layout | **PARTIAL** | Config flags PASS; visual BLOCKED |
| H | Technician signature upload | **BLOCKED** | 0 tech paths |
| I | Tech signature in layout | **PARTIAL** | Config enabled PASS; print with tech identity BLOCKED |
| J | Individual Diagnostic print | **PASS** | Print All / Specific / ownership |
| K | Enter-key | **PARTIAL** | Karma PASS; browser BLOCKED |
| L | Report Layout RBAC | **PASS** | DB + API + Node 10/10 |
| M | API security | **PASS** | Anonymous 401 matrix + secure download |
| N | Parameter range | **PARTIAL** | Verbose pattern absent; Masters suite BLOCKED |
| O | Full patient→report E2E | **PARTIAL** | Invoice/SampleNo/Collect/Receive PASS; approve/notify BLOCKED |
| P | Same-specimen order | **PASS** | |
| Q | Multi-specimen order | **PASS** | |
| R | Print ownership/security | **PASS** | HTTP 400 |
| S | Full Masters tests | **BLOCKED** | No VSTest |
| T | Relevant Angular tests | **PARTIAL** | Enter-key only; full suite not run |

### Executed assertion tallies (this re-run)

| Suite | PASS | FAIL | BLOCKED | SKIPPED |
|-------|-----:|-----:|--------:|--------:|
| `QaComprehensiveRegression.ps1` | 43 | 0 | 3 | 1 |
| `QaStage34ReportsNotifications.ps1` | 20 | 0 | 3 | 1 |
| `qa-menu-permission-semantics.js` | 10 | 0 | 0 | 0 |
| Enter-key Karma (same-day) | 11 | 0 | 0 | 0 |
| **Combined (incl. overlap)** | **84** | **0** | **6 harness + material gaps in §35** | **2+** |

Artifacts:  
- `docs/_qa_comprehensive_run.json` / `_qa_comprehensive_console.txt`  
- `docs/_qa_stage34_run.json` / `_qa_stage34_console.txt`

---

## 14. SampleNo Evidence

```
INV-20260912-0007
  GLU  / SER → 202609120007SER
  HBA1C/ EDT → 202609120007EDT
INV-20260912-0008 (same specimen)
  unique SampleNo count = 1 → 202609120008SER
```

Table: `TestRequestDetails` columns `SampleNo`, `HISRequestNo`, `HISRequestId`, `SpecimenCode`.

---

## 15. OperationalDateTime Evidence

- Web.config `OperationalDateTime:FacilityTimeZoneId=India Standard Time`  
- Future collection (+2 days) on QA pending seed → **rejected**  
- Broader rule suite: **BLOCKED** without VSTest

---

## 16–19. Diagnostic / Print Evidence

| Test | Result | Detail |
|------|--------|--------|
| Print All | PASS | invoice `INV-20260823-0002`, contentGroups=4 |
| Print Specific | PASS | detail 1891 → contentGroups=2 |
| Ownership negative | PASS | badId 1886 → HTTP 400 |
| Verbose Both/Year range | PASS | pattern not in report JSON |

---

## 20. Reference Range Evidence

Pattern scan on live TestReport JSON: **PASS** (no verbose Both/Year fallback string).  
Full applicability matrix: **BLOCKED** (Masters.Tests).

---

## 21–23. Signature / Layout Evidence

| Item | Result |
|------|--------|
| Doctor users with signature path | PASS (2) |
| Technician users with signature path | **BLOCKED** (0) |
| Layout Admin save + Tech deny | PASS |
| Report DTO layout enrichment | PASS |

---

## 24. RBAC Evidence

Admin allowed; Tech/Doctor denied API; DB non-Admin maps=0; Node overlay semantics 10/10. Interactive Setup menu click: **BLOCKED**.

---

## 25. Enter-Key Evidence

Karma 11/11 PASS (same-day). Browser live UAT: **BLOCKED**.

---

## 26. Notification Dry-Run Evidence

- Mock providers **true**  
- IsEnabled **1** (OBS-01)  
- No Doctor Approve / send invoked this pass  
- Invalid secure download: PASS  

---

## 27. Secure Download Security Evidence

`GET /api/report/download/invalid-token-*` → **400** opaque rejection — **PASS**.  
Expired/used/wrong-context tokens: **BLOCKED** (no controlled token lifecycle this pass).

---

## 28. API Security Evidence

Anonymous GET denied (401/403) for SaleInvoice, BarCode, HisTest, Department, TestRate, Quality, ReportLayout, NotificationConfiguration, Dashboard/Registration — **PASS**.

---

## 29. Full E2E Evidence

| Step | Status |
|------|--------|
| Patient (existing) | PASS |
| Invoice | PASS |
| SampleNo | PASS |
| Collection | PASS (QA seed) |
| Receiving | PASS (QA seed) |
| Result entry write | BLOCKED |
| Technician approval | BLOCKED |
| Doctor approval | BLOCKED |
| Report print | PASS (existing approved order) |
| Notification | BLOCKED |

---

## 30. Database Verification

| Entity | Verified |
|--------|----------|
| Patient | via PatientMaster API id used |
| Invoice / HISRequestNo | SaleInvoice create + TestRequestDetails |
| SampleNo | TestRequestDetails |
| Collection / Receiving | CollectedBy / ReceivedBy on QA seed rows |
| Report layout | ReportLayoutConfiguration via API save |
| Signatures | AspNetUsers.DoctorSignaturePath counts |
| Notification audit | table present; rowcount 14 (no PHI logged here) |

---

## 31. Build / Deployment Verification

- **No application code changes** in this blocker-removal exercise.  
- Runtime hosts return HTTP 200 for API and Portal.  
- Deployed Web.config confirms mock providers + IST facility TZ.  
- No production deploy performed.

---

## 32. Defects Found

**None** (Critical/High/Medium) from executed tests.

---

## 33. Defects Fixed

**None** — no in-scope application defect was conclusively reproduced requiring a fix.

Harness-only fixes (QA tooling, not product):
- `Scripts/QaComprehensiveRegression.ps1` (existing)  
- `Scripts/QaStage34ReportsNotifications.ps1` (invoiceNo + report DTO contentGroups; SqlClient; QA seed collect/receive)

---

## 34. Unrelated Observations — NOT FIXED

| ID | Severity | Observation |
|----|----------|-------------|
| OBS-01 | Low / Ops | `NotificationConfiguration.IsEnabled=1` locally; ensure mock providers remain true before any non-mock gateway credentials are introduced. |
| OBS-02 | Low / Env | Receptionist QA identity TOKEN fails (400); create/repair `qa-cert-reception` if Receptionist RBAC matrix is required. |
| OBS-03 | Low / Env | sqlcmd ODBC broken on this machine; ADO.NET harness is the supported local path. |
| OBS-04 | Low / Data | Duplicate/ambiguous `QA-CERT-SMP-PEND` rows exist (status 0 and 7); seed hygiene recommended for future certs. |

---

## 35. Remaining BLOCKED Tests

1. Full `LIS.Masters.Tests` Release via vstest (**blocker:** MSBuild/VSTest missing)  
2. Doctor Approve → Notification paid/partial/outstanding matrix  
3. Notification retry / duplicate / missing-phone / failure-non-blocking  
4. Secure download expired / used / wrong-context tokens  
5. Technician signature upload + print rendering  
6. Browser visual print (stationery clearance / multi-page)  
7. Browser Enter + Tab/Shift+Tab live UAT  
8. Interactive Admin Setup → Report Layout menu click  
9. Full result-entry write + Tech/Doctor approval chain on new invoice  
10. Receptionist role matrix  
11. Full Angular suite (`help.component.spec.ts` and peers)  
12. IIS vs facility TZ skew interactive proof beyond future-reject  

---

## 36. Remaining SKIPPED Tests

1. Receptionist token  
2. Dedicated secure-download token table name probe (tokens may live in NotificationAudit.SecureLinkToken; lifecycle tests still BLOCKED)  
3. Any test requiring production SMS/WhatsApp (intentionally never run)  
4. Full project `ng test` (unrelated broken specs)

---

## 37. Risk Assessment

| Risk | Level | Notes |
|------|-------|-------|
| Anonymous PHI exposure (prior Critical/High class) | Low | Reconfirmed closed |
| SampleNo format regression | Low | Live DB evidence PASS |
| Report print ownership leak | Low | Negative test PASS |
| Report Layout RBAC bypass | Low | API+DB PASS |
| Notification real-traffic | Medium until ops confirm | IsEnabled=1; mitigated by UseMockProviders=true on this host |
| Tech signature missing on print | Medium for that feature | No tech path uploaded |
| Unrun Masters suite | Medium process risk | Toolchain gap, not a known product FAIL |

---

## 38. Final QA Verdict

### **NOT APPROVED FOR PRODUCTION**

Enough evidence exists to show the prior “many BLOCKED” state was largely **environment/execution**, and several formerly blocked areas now **PASS**. Material certification gaps remain (Masters suite, notification business E2E, technician signature binary, full approve chain, browser UAT). Per rules: **BLOCKED ≠ PASS**.

---

## 39. Final BA Verdict

### **NOT APPROVED**

Business workflows SampleNo, print ownership, layout RBAC, and collection/receiving smoke are supported by evidence. Sign-off still requires notification commercial rules under mock providers, technician signature readiness, and full automated Masters regression on a machine with VS Build Tools.

---

## 40. Production Recommendation

**Do not grant full production sign-off yet.**

### Exact next actions (environment — not product “fixes”)

1. Install VS Build Tools (MSBuild + VSTest) **or** run Masters suite on a build agent that already has them; attach TRX.  
2. Upload Technician signature via existing User Edit for `qa-cert-tech@zorya.co.in`; re-run Stage 3.5 print enrichment.  
3. Under **confirmed** `UseMockProviders=true`, execute controlled Doctor Approve on paid vs partial invoices; verify NotificationAudit + secure link rules.  
4. Create/repair Receptionist QA user; complete role matrix.  
5. Optional: Playwright/manual browser UAT for Enter-key + visual print.  
6. Re-issue this report; only then consider **CONDITIONALLY APPROVED** or **APPROVED FOR PRODUCTION**.

---

## Appendix — Commands executed (audit trail)

```text
dotnet --version
node --version
powershell -File Scripts/QaComprehensiveRegression.ps1
powershell -File Scripts/QaStage34ReportsNotifications.ps1
node Scripts/qa-menu-permission-semantics.js
# ADO.NET probes against ZoryaLMS (SampleNo, RBAC, NotificationAudit aggregates, QA seed)
# Authenticated TOKEN for admin / qa-cert-tech / qa-cert-doctor
# GET /api/Reports/TestReport?invoiceNo=... [&testRequestDetailId=...]
```

**Code changes made under this task:** none to application functionality. QA harness script updates only.

---

*End of blocker-removal certification update.*
