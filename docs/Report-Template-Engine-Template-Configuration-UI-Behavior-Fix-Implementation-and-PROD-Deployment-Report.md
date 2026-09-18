# Report Template Configuration UI Behavior Fix — Implementation & PROD Portal Deployment Report

**Date:** 2026-09-18  
**Project:** ZoryaLMS / AVILIS  
**Scope:** Portal UI behavior correction only + **PORTAL-ONLY** PROD deploy  
**Stamp:** `phase-template-ui-behavior-20260918-234306`

---

## 1. Root cause / UX requirement

After the tab refinement shipped, **Custom Template tabs remained interactive while Template Mode = System Default**. Admins could switch Generic / Test Specific / Profile Specific even though Custom mode was not selected.

Additionally, **Create Custom / Create Generic / Create Specific** buttons stayed visible as duplicate active controls while their corresponding in-section create panels were open.

**Required behavior (this fix):**

| Mode | Tabs |
|------|------|
| System Default | Visible + **disabled** (no select / no API / no mode change) |
| Custom | Visible + **enabled** |

| Create action | While panel open | After Cancel |
|---------------|------------------|--------------|
| Create Custom / Generic / Specific | Button **hidden** (`*ngIf`) | Button **returns** |

Mode must continue to load from persisted workspace/API (not hard-coded on refresh).

---

## 2. Files changed

| File | Change |
|------|--------|
| `web/Lis.Web/src/app/setup/report-template-configuration/report-template-configuration.component.ts` | `customTabsEnabled` getter (`mode === 'Custom'`); `selectCustomTab` / `openCreateGeneric` / `openCreateSpecific` no-op when disabled |
| `web/Lis.Web/src/app/setup/report-template-configuration/report-template-configuration.component.html` | Tabs always rendered with `[disabled]` / `.disabled-tab`; Create buttons `*ngIf="createPanel!==…"` |
| `web/Lis.Web/src/app/setup/report-template-configuration/report-template-configuration.component.spec.ts` | Expanded focused behavior tests (disabled tabs, no mutation, create hide/cancel) |
| `docs/Report-Template-Engine-Template-Configuration-UI-Behavior-Fix-Implementation-and-PROD-Deployment-Report.md` | This report |

**Not changed:** API, DB, migrations, Gate 6, resolver, renderer, RBAC, feature flag, report DTOs, Diagnostic/Radiology workflows.

---

## 3. System Default tab behavior

- Tabs **remain in the DOM** and **visible**.
- `[disabled]="!customTabsEnabled || loading"` + `disabled-tab` class.
- `selectCustomTab()` returns immediately when `!customTabsEnabled`.
- No mode change, no activate/deactivate, no targets/create API from tab click.

**Unit coverage:** PASS (System Default disables tabs; select/open create are no-ops; mutation counters stay 0).

**PROD live System Default disabled-tabs check:** **SKIPPED** — customer persisted mode is **Custom**; mode was not changed for testing.

---

## 4. Custom tab behavior

- When `mode === 'Custom'`, tabs are enabled.
- Admin can select Generic / Test Specific / Profile Specific.
- Default local tab remains Generic (UI-only; not persisted to DB).
- Existing list/filter/pagination behavior unchanged.

**PROD:** Custom mode observed (UI + API). Tabs enabled. Switch Test / Profile / Generic **PASS**.

---

## 5. Create button behavior

Preferred approach implemented: **hide** the corresponding Create button while its panel is open.

| Control | Open condition | Hide rule |
|---------|----------------|-----------|
| Create Custom | `createPanel==='system'` | `*ngIf="createPanel!=='system'"` |
| + Create Generic | `createPanel==='generic'` | `*ngIf="createPanel!=='generic'"` |
| + Create Specific | `createPanel==='specific'` | `*ngIf="createPanel!=='specific'"` |

Cancel → `cancelCreate()` → `createPanel='none'` → button reappears.

Panels remain **in-section** (no modal, no `scrollIntoView`, no move to page bottom).

**Create & Design** flow unchanged (existing create API → designer route; no auto-activate; no auto mode change).

---

## 6. Tests executed

| Suite | Command / filter | Result |
|-------|------------------|--------|
| Report Template Phase 1–4 | `vstest` `FullyQualifiedName~ReportTemplateEnginePhase` | **PASS — 63/63** |
| Masters suite | `Scripts\run-masters-tests.bat` | **PASS — 298 passed / 3 skipped** (Total 301) |
| Angular RTC specs | `ng test --include=**/report-template-configuration*.spec.ts` | **PASS — 22/22** (`TOTAL: 22 SUCCESS`, `NG_TEST_EXIT=0`) |

Angular focused cases covered (among others):

1–4. System Default → tabs conceptually disabled; select no-op; no API mutation  
5–8. Custom → tabs enabled; Generic / Test / Profile selectable without mutation APIs  
9–14. Create Generic / Specific / Custom panel open → cancel clears panel (button can reappear)  
15. Mode from workspace payload (not hard-coded)  
16–17. Create & Design path / activation APIs not invoked by tab or cancel flows  

Note: unrelated `help.component.spec.ts` was temporarily quarantined for Karma compile only, then restored.

---

## 7. Build result

| Item | Value |
|------|--------|
| Command | `npx ng build --prod --configuration=production` with `NODE_OPTIONS=--openssl-legacy-provider` |
| Result | **PASS** (`BUILD_EXIT=0`) |
| Bundle | `main-es2015.3fbf6ca83eafc813d396.js` |
| Hash / time | `888833cfd22de0d35520` · 2026-09-18T18:06:25Z |
| Budget warnings | Pre-existing (test-report CSS, sale-invoice CSS, initial budget) — not blockers |

---

## 8. PROD backup path

```
I:\Projects\PROD\AVILIS\_backup\phase-template-ui-behavior-20260918-234306
```

Backup contains prior Portal assets including the previous main bundle (pre-behavior-fix).

---

## 9. Deployed bundle

| Item | Value |
|------|--------|
| Source | `i:\Projects\LIS\avs-lis\web\Lis.Web\dist\DxI800` |
| Target | `I:\Projects\PROD\AVILIS\PORTAL` |
| Deployed main | `main-es2015.3fbf6ca83eafc813d396.js` (1,901,137 bytes; 2026-09-18 23:36:25) |
| Live browser | `http://localhost:8080/main-es2015.3fbf6ca83eafc813d396.js` → **PASS** |

---

## 10. IIS Portal status

| Pool | Action | State |
|------|--------|-------|
| `AVILIS_PORTAL_POOL` | Recycled as required for Portal deploy | **Started** |
| `AVILIS_API_POOL` | **Not recycled / not touched** | **Started** |

IIS site configuration preserved. Portal-only file sync.

---

## 11. PROD verification

Non-mutating Admin Playwright + follow-up RBAC/report smoke. **No PROD template records created.** Persisted mode left as **Custom**.

| Check | Status |
|-------|--------|
| Bundle `3fbf6ca83eafc813d396` loaded | **PASS** |
| Tabs Generic / Test Specific / Profile Specific visible | **PASS** |
| Persisted mode UI = Custom | **PASS** |
| Persisted mode API = Custom (`workspace?reportType=Diagnostic`) | **PASS** |
| Custom → tabs enabled | **PASS** |
| Switch Test Specific | **PASS** |
| Switch Profile Specific | **PASS** |
| Create Custom → panel → button hidden → Cancel → button returns | **PASS** |
| Create Generic → panel → button hidden → Cancel → button returns | **PASS** |
| Create Specific → panel → button hidden → Cancel → button returns | **PASS** |
| System Default → tabs disabled (live PROD) | **SKIPPED** (would require changing customer mode) |
| System Default disabled behavior | **PASS** via Angular unit tests |
| RBAC technician blocked (`/?denied=1`) | **PASS** |
| Diagnostic report smoke | **PASS** |
| Radiology report smoke | **PASS** |
| Create & Design submit (actual create) | **SKIPPED** (no PROD template data) |
| Mode switch / activate / deactivate | **SKIPPED** (must not alter customer data/mode) |
| Search / filters / pagination deep fuzz | **SKIPPED** (unchanged implementation; not re-fuzzed) |

---

## 12. API status

| Item | Status |
|------|--------|
| API deployment | **NOT PERFORMED** |
| `I:\Projects\PROD\AVILIS\API\bin\Lis.Api.dll` | Unchanged — **2026-09-12 23:52:05** |
| API application pool | Not recycled |

---

## 13. DB status

| Item | Status |
|------|--------|
| Database change | **NO CHANGE** |
| Migration | **NOT RUN** |
| Deployment SQL | **NOT RUN** |
| Template data | **NO TEST DATA CREATED** |

---

## 14. Gate 6 status

**UNCHANGED**

```
<add key="ReportTemplate:UseDeclarativeRendererForProductionPrint" value="true" />
```

Verified in `I:\Projects\PROD\AVILIS\API\Web.config` after Portal deploy. Resolver / declarative renderer **not** modified.

---

## 15. Skipped checks

| Check | Status | Reason |
|-------|--------|--------|
| Live PROD System Default disabled tabs | **SKIPPED** | Customer mode is Custom; do not change mode for test |
| Actual Create & Design in PROD | **SKIPPED** | Would create template records |
| Activate / Deactivate in PROD | **SKIPPED** | Forbidden mutation |
| Mode persist logout/login cycle after switch | **SKIPPED** | Would require mode mutation |

These are recorded as **SKIPPED**, not PASS.

---

## 16. Known issues

1. Full Karma suite still blocked by pre-existing broken `help.component.spec.ts` (out of scope; quarantined only for RTC specs).  
2. First combined PROD smoke timed out on technician re-login (`#login-email`); RBAC/report checks re-run separately → **PASS**.  
3. Create Specific panel markup exists under both Test and Profile tab bodies (only one tab visible); shared `createPanel` state — pre-existing pattern, unchanged intentionally.

---

## Explicit safety confirmations

| Item | Status |
|------|--------|
| API deployment | **NOT PERFORMED** |
| Database | **NO CHANGE** |
| Migration | **NOT RUN** |
| Deployment SQL | **NOT RUN** |
| Gate 6 | **UNCHANGED** (`true`) |
| Template data | **NO TEST DATA CREATED** |
| Resolver | **UNCHANGED** |
| Renderer | **UNCHANGED** |
| Rollback | **Not required** (backup retained) |

---

## Final acceptance

| Criterion | Result |
|-----------|--------|
| System Default → tabs visible + disabled (code + unit) | **PASS** |
| Custom → tabs enabled (PROD) | **PASS** |
| Create buttons hide while panel open / return on Cancel (PROD) | **PASS** |
| Mode from persisted workspace/API | **PASS** (Custom retained) |
| Portal deployed with new bundle | **PASS** |
| API / DB / Gate 6 untouched | **PASS** |

**STOP.** No further UI redesign; no API/DB/Gate 6/template-data changes.
