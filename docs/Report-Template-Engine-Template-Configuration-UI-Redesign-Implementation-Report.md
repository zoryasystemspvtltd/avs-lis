# Report Template Configuration — UI Redesign Implementation Report

**Date:** 2026-09-18  
**Project:** ZoryaLMS / AVILIS  
**Task:** Report Template Configuration UI/UX redesign (specification-driven)

---

## 1. Scope

What changed:

- Redesigned the **Report Template Configuration** Angular page for compactness, discoverability, and in-section Create panels (fixes below-the-fold Create defect).
- Split Generic / Test Specific / Profile Specific lists with empty states, client search/filters, and pagination (15 rows).
- Added confirmation dialogs for Template Mode switch and Activate/Deactivate.
- Clarified designer Save messaging and Back label (designer architecture unchanged).

What did **not** change:

- Template resolution / Gate 6 / declarative renderer / print workflows  
- RBAC / menus / routes / QAuthorize  
- Database schema / migrations  
- API business logic (reused existing workspace, create, activate, mode, targets APIs)  
- PROD deployment (not authorized in this task)

---

## 2. Files changed

| File | Change |
|------|--------|
| `web/Lis.Web/src/app/setup/report-template-configuration/report-template-configuration.component.ts` | Redesigned component logic (create panels, filters, pagination, confirms) |
| `web/Lis.Web/src/app/setup/report-template-configuration/report-template-configuration.component.html` | New page structure per approved layout |
| `web/Lis.Web/src/app/setup/report-template-configuration/report-template-designer.component.ts` | Save success message; Activate confirm text (same activate API) |
| `web/Lis.Web/src/app/setup/report-template-configuration/report-template-designer.component.html` | “Back to Template Configuration” label |
| `docs/Report-Template-Engine-Template-Configuration-UI-Redesign-Implementation-Report.md` | This report |

---

## 3. UX changes (map to requirements)

| Requirement | Implementation |
|-------------|----------------|
| Fix Create below-fold | Separate inline panels: `createPanel = system \| generic \| specific` placed **inside** each section immediately under the Create button |
| Not scroll-only fix | No `scrollIntoView` as the solution; panel placement changed |
| Report Type | Existing Diagnostic/Radiology select + `reload()` |
| Template Mode two-state | Button group reflecting **persisted** `workspace.mode` / API `setMode` |
| Default System Default | Unchanged backend `EnsureModeRow` seeds SystemDefault only when missing; UI reads API (no hard-code on every load) |
| Persist Custom / System Default | Existing `POST .../mode` — confirmation then persist |
| Create Custom in System Default section | `openCreateSystem()` → panel under System Default |
| Create Generic in Generic section | `openCreateGeneric()` → panel under Generic heading |
| Create Specific in Specific section | `openCreateSpecific()` → panel with Test/Profile radios + target select |
| Create & Design | Primary button label; still `createCustom` then navigate `design/:id` |
| No auto-activate | Unchanged create/save paths |
| Cancel | `cancelCreate()` clears panel state; no API |
| Generic / Test / Profile lists | Split tables; Active vs Draft / Inactive |
| Specific empty state | Always show sections with empty copy |
| Search / filters | Client-side on workspace payload |
| Pagination | `ngb-pagination`, page size **15** |
| Activate confirm | `confirm()` with Generic / Test / Profile messaging; mode not changed |
| Mode switch confirm | Required before `setMode` |
| Designer Save UX | Message: saved successfully; Draft / Inactive; saving does not activate |
| Theme | Existing Bootstrap / AVILIS controls; compact section cards |

---

## 4. Existing functionality preserved

| Area | Status |
|------|--------|
| Resolver precedence (Test → Profile → Generic → System Default) | Untouched |
| Declarative renderer / Gate 6 flag | Untouched |
| Designer structure (palette/canvas/props) | Untouched (messages/labels only) |
| RBAC Menu → Route → QAuthorize | Untouched |
| Diagnostic / Radiology print workflows | Untouched |
| Mode persistence | Untouched backend; UI now confirms before calling existing API |

---

## 5. Database

**NO DB CHANGE REQUIRED**

UI redesign uses existing `ReportTemplate*` tables and `ReportTemplateModeSetting` via existing APIs.

---

## 6. Tests

| Suite | Result | Notes |
|-------|--------|-------|
| `LIS.Masters.Tests` full (`Scripts/run-masters-tests.bat`) | **PASS** — 298 passed, 3 skipped, 0 failed | Includes report-layout, masters, and report-template coverage |
| `FullyQualifiedName~ReportTemplateEnginePhase` | **PASS** — **63/63** | Phase 1–4 engine tests |
| Angular `report-template-configuration-rbac.spec.ts` | **PASS** — **6/6** | Ran with temporary quarantine of unrelated broken `help.component.spec.ts` (restored after; not fixed) |
| Full Karma suite | **NOT RUN** | Blocked by same pre-existing `help.component.spec.ts` defect |

---

## 7. Build

| Artifact | Result |
|----------|--------|
| API `Lis.Api` Release | **PASS** |
| Angular `ng build --prod --configuration=production` | **PASS** (with `NODE_OPTIONS=--openssl-legacy-provider` on Node 24) |
| Output bundle | `main-es2015.5476a2e0172520f0c5b4.js` |
| Bundle strings verified | Create Custom / Generic / Specific / Create & Design / Test Specific / Profile Specific present |

First Angular attempt without OpenSSL legacy provider failed (Node 24 / webpack). Retry with legacy provider succeeded. Documented as environment note, not a product defect.

---

## 8. Deployment

**NOT PERFORMED**

Deployment was not explicitly authorized. Artifacts are built locally under `web/Lis.Web/dist/DxI800` and API `bin`.

---

## 9. PROD verification

**NOT PERFORMED** (no Portal/API deploy).

Recommended after authorized Portal deploy:

1. Hard refresh Admin → Report Template Configuration  
2. Confirm Create Custom / Generic / Specific panels open **in-section** (visible without hunting below Generic table)  
3. Confirm mode persists across logout/login  
4. Confirm create remains Draft / Inactive until Activate  
5. Smoke Diagnostic/Radiology print unchanged  

---

## 10. Known issues

1. **Full Karma suite** still blocked by pre-existing broken `help.component.spec.ts` (missing `about.component`). Out of scope; RBAC spec verified in isolation.  
2. **Target labels** for existing Specific rows still come from API `targetLabel` (often `Test #id`); create picker uses `/targets` labels. Enriching list labels is optional future UX, not required for this redesign.  
3. **Workspace payload** still includes `definitionJson` per row from existing API — unchanged; pagination mitigates UI length.  
4. **PROD not deployed** — redesign exists in source + local dist only until authorized Portal deploy.

---

## Mode persistence stop-condition (design verification)

Backend already:

- Seeds `SystemDefault` only when no `ReportTemplateModeSetting` row exists  
- Persists Admin Custom / SystemDefault via `SetMode`  
- Does not reset mode on app restart / deploy  

UI now:

- Loads mode from workspace API (not hard-coded each load)  
- Requires confirmation before calling `setMode`  

Full logout/login PROD verification deferred until Portal deploy is authorized.

---

## Explicit statements

- No Gate 6 / feature-flag changes  
- No DB / migration / SQL changes  
- No PROD deployment in this task  
- No parallel authorization or new modal framework (native `confirm()` + inline panels)
