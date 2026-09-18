# Report Template Configuration UI Redesign — PROD Portal Deployment Report

**Deployment timestamp:** 2026-09-18 21:22:26 IST (approx.)  
**Environment:** PROD AVILIS (`http://localhost:8080`)  
**Scope:** **PORTAL ONLY**

---

## 1. Deployment date/time

| Field | Value |
|-------|--------|
| Backup/deploy stamp | `20260918-212226` |
| Deployed bundle file time | 2026-09-18 21:15:48 |
| Verification window | 2026-09-18 ~21:22–21:55 IST |

---

## 2. Environment

| Component | Path / URL |
|-----------|------------|
| Portal URL | `http://localhost:8080` |
| API (untouched) | `I:\Projects\PROD\AVILIS\API` |
| DB | Not modified |

---

## 3. Portal source/build path

`i:\Projects\LIS\avs-lis\web\Lis.Web\dist\DxI800`

Pre-check confirmed expected bundle and UI strings present.

---

## 4. PROD Portal path

`I:\Projects\PROD\AVILIS\PORTAL`

---

## 5. Backup path

`I:\Projects\PROD\AVILIS\_backup\phase-template-ui-redesign-20260918-212226`

Previous bundle preserved in backup: `main-es2015.5be29c72310d0fdb4ca5.js`

**Rollback status:** Backup exists; **rollback not required** (no material regression observed).

---

## 6. Bundle deployed

| Item | Value | Status |
|------|-------|--------|
| Expected | `main-es2015.5476a2e0172520f0c5b4.js` | **PASS** |
| On disk after deploy | Present (1,894,785 bytes) | **PASS** |
| `index.html` reference | `main-es2015.5476a2e0172520f0c5b4.js` | **PASS** |

---

## 7. IIS / application pool verification

| Check | Result | Status |
|-------|--------|--------|
| `AVILIS_PORTAL_POOL` recycled | Yes → **Started** | **PASS** |
| `AVILIS_API_POOL` | **Started** (not recycled for this deploy) | **PASS** |
| Portal HTTP | Loads new index/bundle | **PASS** |

---

## 8. Browser / cache verification

Fresh Chrome (Playwright) hard reload + navigation.

| Check | Result | Status |
|-------|--------|--------|
| Loaded script | `http://localhost:8080/main-es2015.5476a2e0172520f0c5b4.js` | **PASS** |
| Strings: Create Custom / Generic / Specific / Create & Design / Test Specific / Profile Specific | Present in UI | **PASS** |

---

## 9. Admin UI verification

Admin login → Setup → Report Template Configuration.

| Check | Status | Evidence |
|-------|--------|----------|
| Page loads | **PASS** | Title + redesigned sections visible |
| Report Type Diagnostic | **PASS** | Present |
| Report Type Radiology | **PASS** | Present |
| Template Mode control | **PASS** | System Default + Custom buttons |
| Persisted mode displayed | **PASS** | UI primary = Custom; API `workspace.mode=Custom` (not reset by deploy) |
| Console / page errors | **PASS** | None on RTC page |

**Mode safety:** Did **not** click mode switch during verification. Deploy did **not** reset persisted Custom mode.

---

## 10. Create Custom verification

| Check | Status | Evidence |
|-------|--------|----------|
| Panel opens in System Default section | **PASS** | Panel y≈314 near button y≈272; in viewport |
| Visible without bottom-of-page scroll | **PASS** | `near=true`, `inView=true` |
| No navigation on click | **PASS** | Remained on configuration route |
| Cancelled without create | **PASS** | No template mutation |

---

## 11. Create Generic verification

| Check | Status | Evidence |
|-------|--------|----------|
| Panel under Generic heading | **PASS** | Panel immediately below Create Generic |
| Visible without hunting below Generic table | **PASS** | `near=true`, `inView=true` |
| Cancelled without create | **PASS** | |

---

## 12. Create Specific verification

| Check | Status | Evidence |
|-------|--------|----------|
| Panel under Specific section | **PASS** | In viewport near Create Specific |
| Target Type (Test/Profile) | **PASS** | Visible |
| Target select | **PASS** | Visible |
| Cancelled without create | **PASS** | |

---

## 13. Generic / Test / Profile list verification

| Check | Status | Notes |
|-------|--------|-------|
| Generic list (Name / Status / Actions) | **PASS** | Table present; pagination `ngb-pagination` count=1 |
| Search/filter controls present | **PASS** | UI present (smoke; not exhaustively fuzzed) |
| Test Specific section | **PASS** | Heading + rows (API: 10 Test Specific) |
| Profile Specific section | **PASS** | Heading + empty state (API: 0 Profile Specific) |
| Sections discoverable | **PASS** | DOM order: Generic → Specific Templates → Test Specific → Profile Specific (CSS shows uppercase labels) |

Note: An automated case-sensitive heading check falsely labeled `SPECIFIC_SECTIONS_DOM=FAIL` while evidence JSON already listed `TEST SPECIFIC` / `PROFILE SPECIFIC`. Treated as **PASS** based on that evidence + Admin body text checks.

---

## 14. Mode persistence verification

| Check | Status | Evidence |
|-------|--------|----------|
| UI reflects API mode | **PASS** | Custom selected in UI |
| API mode after Portal deploy | **PASS** | `mode=Custom` |
| Deploy did not reset mode | **PASS** | Remained Custom |
| Hard-coded SystemDefault on every load | **PASS** (not observed) | Reads workspace |

Logout/login mode round-trip with mode **change** intentionally **SKIPPED** (must not alter customer mode for testing).

---

## 15. RBAC verification

| Actor | Check | Status | Evidence |
|-------|-------|--------|----------|
| Admin | RTC accessible | **PASS** | Full UI |
| Technician `qa-cert-tech@zorya.co.in` | Route blocked | **PASS** | Redirect `/?denied=1`; menu link count=0; no Create Custom UI |
| Technician API | Workspace denied | **PASS** | HTTP **403** |

---

## 16. Diagnostic / Radiology smoke verification

| Check | Status | Evidence |
|-------|--------|----------|
| `/reports/test-report` loads | **PASS** | Page content matches report UI |
| `/reports/radiology-report` loads | **PASS** | Page content matches radiology report UI |
| Gate 6 unchanged | **PASS** | See §19 |

No Print All/Specific POST executed (non-mutating smoke of page availability).

---

## 17. API deployment status

**NOT PERFORMED**

`I:\Projects\PROD\AVILIS\API\bin\Lis.Api.dll` LastWriteTime remains **2026-09-12 23:52:05** (pre-dates this Portal deploy).

---

## 18. DB status

**NONE** — no migration, no SQL, no template record mutation during deployment/smoke (create panels cancelled).

---

## 19. Gate 6 status

**UNCHANGED**

```
<add key="ReportTemplate:UseDeclarativeRendererForProductionPrint" value="true" />
```

Verified before and after Portal deploy.

---

## 20. Skipped tests

| Item | Status | Reason |
|------|--------|--------|
| Create & Design (actual PROD create) | **SKIPPED** | Avoid creating PROD template data; panel + Create & Design button verified only |
| Mode switch persist logout/login cycle | **SKIPPED** | Must not alter customer Template Mode for testing |
| Activate/Deactivate templates | **SKIPPED** | Forbidden by deployment instructions |
| Exhaustive search/filter fuzzing | **SKIPPED** | Controls present; not fully exercised |

---

## 21. Observed issues

1. Verifier script case-sensitivity false FAIL on Specific section headings (uppercase CSS) — **not a product defect**.  
2. Technician welcome text still lists multiple role names in header chrome, but RTC route is denied (`/?denied=1`) — **RBAC boundary holds**.  
3. Specific Templates section still sits below the paginated Generic list (by design); Create Specific panel opens **in-section** and is visible without scrolling the full Generic history.

---

## 22. Rollback status

**Not invoked.** Backup retained at path in §5 for emergency restore if needed later.

---

## FINAL REQUIRED SUMMARY

**PORTAL DEPLOYMENT:** **PASS**

**API DEPLOYMENT:** **NOT PERFORMED**

**DATABASE CHANGE:** **NONE**

**GATE 6:** **UNCHANGED** (`true`)

**UI PROD VERIFICATION:**

| Item | Result |
|------|--------|
| Create Custom | **PASS** |
| Create Generic | **PASS** |
| Create Specific | **PASS** |
| Generic list | **PASS** |
| Test Specific | **PASS** |
| Profile Specific | **PASS** |
| Mode behavior | **PASS** (read persisted Custom; deploy did not reset) |
| RBAC | **PASS** |
| Diagnostic/Radiology smoke | **PASS** |

**Create & Design end-to-end create:** **SKIPPED** (intentional)

---

## Stop

Deployment and verification complete. No further UX/API/DB/Gate 6 changes made.
