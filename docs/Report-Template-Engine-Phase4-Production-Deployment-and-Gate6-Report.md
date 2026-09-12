# Report Template Engine — Phase 4 Production Deployment & Gate 6 Report

**Project:** ZoryaLMS / AVILIS  
**Environment:** PRODUCTION (`ZORYA-NUC1` · `AVILIS_API` :8081 · `AVILIS_PORTAL` :8080)  
**Deployment date/time:** 2026-09-12 (local IST evening; Gate 6 activated after successful redeploy)  
**Operator:** Cursor agent under explicit user Gate 6 authorization  

---

## 1. Deployment date/time

| Event | Time (local) | Result |
|-------|--------------|--------|
| Pre-deploy tests (70/70) | ~23:25 | **PASS** |
| API + Portal build | ~23:25–23:26 | **PASS** |
| Backup created | 23:27 | **PASS** |
| First API+Portal deploy (flag `false`) | ~23:28 | API **FAIL** (startup 500) |
| Emergency API rollback | ~23:30 | API **PASS** (200) |
| Dual-ctor DI fix + retest 70/70 | ~23:32 | **PASS** |
| API redeploy (flag `false`) | ~23:33 | **PASS** |
| Pre-Gate-6 print baseline | ~23:35+ | **PASS** |
| Gate 6 flag → `true` + recycle | post-UAT baseline | **PASS** |
| Post-Gate-6 UAT | immediately after | **PASS** (API-level) |

---

## 2. Source commit / version

| Item | Value |
|------|-------|
| Git branch | `sougata` |
| Commit | `db78ed5` (Phase 4 changes were **uncommitted** working tree on top of this commit) |
| Deploy-blocking fix applied during deploy | Removed second public `ReportTemplateManager` constructor (SimpleInjector one-constructor rule); Phase 1 test updated to pass `IReportRenderer` |

---

## 3. API artifact / version

| Artifact | Path | Notes |
|----------|------|-------|
| Build output | `web/Lis.Api/bin` (Release) | Same release as Portal |
| Deployed to | `I:\Projects\PROD\AVILIS\API\bin` | Root `Web.config` preserved / patched |
| `LIS.BusinessLogic.dll` | len **502784** · 2026-09-12 23:32:05 | Contains `ReportProductionPresentationAdapter` |
| `Lis.Api.dll` / `LIS.DtoModel.dll` | Matching Release build | Phase 4 presentation DTOs + flag reader |

---

## 4. Portal artifact / version

| Artifact | Path | Notes |
|----------|------|-------|
| Build | `web/Lis.Web/dist/DxI800` | `ng build --prod --configuration=production` |
| Deployed to | `I:\Projects\PROD\AVILIS\PORTAL` | |
| Main bundle | `main-es2015.6b3ec0f91ecb806ea0e5.js` | Contains `report-template-configuration`, `SETUP_REPORT_TEMPLATE_CONFIGURATION`, `useDeclarativePresentation` |

---

## 5. API deployment result

| Step | Status |
|------|--------|
| First deploy attempt | **FAIL** — SimpleInjector: `ReportTemplateManager` had 2 public constructors → TargetInvocationException → HTTP 500 |
| Emergency rollback to `_backup\phase4-gate6-20260912-232707` | **PASS** — API HTTP 200 restored |
| Minimal source fix + rebuild + 70/70 retest | **PASS** |
| Redeploy fixed API (flag still `false`) | **PASS** — HTTP 200; adapter present in deployed DLL |
| Final API health after Gate 6 | **PASS** — HTTP 200 |

---

## 6. Portal deployment result

| Step | Status |
|------|--------|
| Portal robocopy from `dist/DxI800` | **PASS** (deployed with first attempt; retained after API rollback/fix) |
| Portal HTTP `/` and `/login` | **PASS** — 200 |
| SPA route `/report-template-configuration` | **PASS** — 200 (serves SPA shell) |
| `/lis` API bridge | Present (`AVILIS_PORTAL/lis` → API physical path) |

---

## 7. Database verification

| Check | Status |
|-------|--------|
| Phase 4 schema change | **NO DB CHANGE** |
| Phase 3 tables present | **PASS** — `ReportTemplate`, `ReportTemplateVersion`, `ReportTemplateAssignment`, `ReportTemplateModeSetting` |
| Mode settings | Diagnostic=`Custom`, Radiology=`SystemDefault` |
| RBAC module `ReportTemplateConfiguration` | **PASS** — UserModules Id=20, RoleModuleMappings=1, RoleMenuPermission `SETUP_REPORT_TEMPLATE_CONFIGURATION`=1 |
| Ad-hoc PROD DB modification during deploy | **NOT EXECUTED** / not required |

**Explicit:** **NO DB CHANGE REQUIRED. NO DB CHANGE PERFORMED.**

---

## 8. Menu verification

| Check | Status | Evidence |
|-------|--------|----------|
| Admin template list API | **PASS** | `GET /api/ReportTemplateConfiguration?reportType=Diagnostic` → 200 |
| Admin workspace API | **PASS** | `GET .../workspace?reportType=Diagnostic` → mode=Custom, systemDefault present (`Built-in Diagnostic Report`) |
| Radiology mode API | **PASS** | mode=`SystemDefault` |
| Portal bundle menu key / route | **PASS** | strings present in deployed `main-es2015.*.js` |
| Portal route URL loads | **PASS** | `http://localhost:8080/report-template-configuration` → 200 |
| Live Admin UI click “Setup → Report Template Configuration” | **NOT EXECUTED** | No interactive browser session; API + SPA + RBAC seed verified |

---

## 9. Feature flag before activation

```
ReportTemplate:UseDeclarativeRendererForProductionPrint=false
```

(Key was **added** to PROD `Web.config` during deploy; previously absent → fail-closed equivalent.)

---

## 10. Gate 6 approval

**APPROVED by user** in the deployment authorization prompt for this task  
(“The user has now explicitly approved Gate 6.”)

---

## 11. Feature flag after activation

```
ReportTemplate:UseDeclarativeRendererForProductionPrint=true
```

Location: `I:\Projects\PROD\AVILIS\API\Web.config` (appSettings)  
**Not** DB-backed. Source repo `web/Lis.Api/Web.config` remains **`false`** (fail-closed default for non-prod checkouts).

---

## 12. IIS recycle result

| Action | Status |
|--------|--------|
| `AVILIS_API_POOL` recycle after flag=`true` | **PASS** |
| API HTTP 200 after recycle | **PASS** |
| Runtime behavior confirms flag ON | **PASS** — Diagnostic Custom print returned `PresentationMode=Declarative` |

---

## 13. Diagnostic UAT results

| Scenario | Status | Evidence |
|----------|--------|----------|
| Flag OFF Print All → Existing / FlagOff | **PASS** | `mode=Existing reason=FlagOff hasHtml=False` |
| Flag OFF Print Specific → Existing / FlagOff | **PASS** | same |
| Gate 6 Print All → Declarative CustomGeneric | **PASS** | `mode=Declarative source=CustomGeneric hasHtml=True templateId=153` · HTML contains `rte-report` · len≈8298 |
| Gate 6 Print Specific → Declarative | **PASS** | Declarative + HTML (same CustomGeneric when Specific resolves to generic) |
| Payment gate unpaid → 400 | **PASS** | unpaid invoice rejected |
| Template workspace / System Default visible | **PASS** | API workspace |
| Browser visual print chrome | **NOT EXECUTED** | API presentation verified only |

---

## 14. Radiology UAT results

| Scenario | Status | Evidence |
|----------|--------|----------|
| Mode remains SystemDefault | **PASS** | configuration API |
| Gate 6 print → Existing / SystemDefault | **PASS** | `mode=Existing reason=SystemDefault hasHtml=False` (architecture: System Default never declarative) |
| Accessions endpoint reachable | **PASS** | returns data |
| Interactive browser radiology print | **NOT EXECUTED** | |

---

## 15. Fallback test result

| Scenario | Status | Evidence |
|----------|--------|----------|
| System Default under flag ON → Angular/Existing | **PASS** | Radiology live path |
| Flag OFF → Existing | **PASS** | pre-activation Diagnostic |
| Soft-skip / empty HTML / exception → Angular | **PASS** (automated suite) | Covered by Phase 4 unit/integration tests (70/70); not re-broken in PROD templates |
| Presentation failure ≠ HTTP 500 for eligible Diagnostic | **PASS** | Eligible Diagnostic returned 200 with Declarative |

Intentional corruption of PROD custom templates: **NOT EXECUTED** (unsafe).

---

## 16. RBAC result

| Check | Status |
|-------|--------|
| Anonymous template API | **PASS** → 401 |
| Technician template API | **PASS** → 403 |
| Admin template API | **PASS** → 200 |
| Menu/module seed present | **PASS** |
| Parallel auth introduced | **PASS** (none) |

---

## 17. Security result

| Check | Status |
|-------|--------|
| Validator / allow-list unchanged | **PASS** (no security weakening in deploy fix) |
| Flag only via Web.config | **PASS** |
| No PHI logged in this session’s verification output | **PASS** (UAT logged modes/ids/lengths only) |
| Client trusts server Presentation HTML only | **PASS** (existing Angular bind) |

---

## 18. Browser / E2E result

| Layer | Status |
|-------|--------|
| Authenticated API end-to-end (template + print) | **PASS** |
| Portal HTTP / SPA shell | **PASS** |
| Interactive Admin UI menu click + visual print | **NOT EXECUTED** |

---

## 19. Warnings

1. First deploy failed on SimpleInjector dual-constructor — fixed with minimal ctor change; required emergency API rollback.  
2. Angular production build still emits pre-existing CSS/budget **warnings** (non-blocking).  
3. `TestReportLabNumbers` / `RadiologyPrintAccessions` responses appear to concatenate multi-row fields when consumed naively in PowerShell — used discrete invoice/`radiologyRequestId` parse for UAT; treat as **RISK/observe** (may be client aggregation quirk).  
4. Diagnostic mode was already **Custom** in PROD DB before Gate 6 — enabling the flag immediately exercises Custom Generic declarative path (expected for Gate 6).

---

## 20. Unrelated blockers

| Item | Status |
|------|--------|
| Full Karma suite (`help.component.spec.ts`) | **UNRELATED** — not fixed |
| Interactive browser UAT | **NOT EXECUTED** — not a code blocker |

---

## 21. Rollback readiness

| Asset | Location |
|-------|----------|
| Pre-deploy backup | `I:\Projects\PROD\AVILIS\_backup\phase4-gate6-20260912-232707` (API-bin + PORTAL + Web.config) |
| Proven rollback | Emergency API bin restore already **PASS** during this session |
| Soft rollback (preferred for Gate 6) | Set flag `false` in PROD Web.config → recycle `AVILIS_API_POOL` → Existing Angular |

Post-activation soft-rollback **not exercised** (UAT succeeded; would re-disable declarative printing).

---

## 22. Final production status

| Item | Status |
|------|--------|
| API | **DEPLOYED** |
| Portal | **DEPLOYED** |
| Template Configuration menu (API + SPA + RBAC) | **VISIBLE / ACCESSIBLE TO ADMIN** (API-proven; UI click **NOT EXECUTED**) |
| Feature flag | **`true`** |
| Gate 6 | **APPROVED / ACTIVATED** |
| Diagnostic declarative production printing | **WORKING** (API-proven) |
| Radiology production printing | **WORKING** via Existing Angular (System Default) |
| Existing Angular fallback | **WORKING** |
| Database | **NO DB CHANGE** |
| RBAC | **PASS** |
| Security | **PASS** |
| Browser visual UAT | **NOT EXECUTED** |

---

## Decision matrix (requested categories only)

| Area | Result |
|------|--------|
| Pre-deploy source check | **PASS** |
| Automated tests 70/70 | **PASS** |
| Production builds | **PASS** |
| DB verification | **PASS** (no change) |
| Backup | **PASS** |
| API deploy (final) | **PASS** |
| Portal deploy | **PASS** |
| Menu (Admin API/SPA/RBAC) | **PASS** |
| Legacy print flag OFF | **PASS** |
| Gate 6 flag ON | **PASS** |
| IIS recycle | **PASS** |
| Diagnostic declarative UAT | **PASS** |
| Radiology SystemDefault UAT | **PASS** |
| Payment gate | **PASS** |
| Interactive browser UI | **NOT EXECUTED** |
| Multi-server sync | **NOT APPLICABLE** (single node `ZORYA-NUC1`) |

---

## Final statement

**Gate 6 — APPROVED / ACTIVATED.**

API and Portal Phase 4 artifacts are deployed to PROD.  
`ReportTemplate:UseDeclarativeRendererForProductionPrint=true` is live after IIS recycle.  
Diagnostic Custom templates print declaratively; Radiology System Default remains on Existing Angular; unpaid payment gate still returns 400; Admin template configuration APIs work; **NO DB CHANGE**.

Interactive browser menu click / visual print confirmation remains **NOT EXECUTED** and should be spot-checked by an Admin in the lab UI (hard refresh recommended).
