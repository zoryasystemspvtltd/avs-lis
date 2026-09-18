# Report Template Engine — PROD Template Button Diagnostic

**Date:** 2026-09-18  
**Project:** ZoryaLMS / AVILIS  
**Environment:** PROD Portal `http://localhost:8080`  
**Feature:** Report Template Configuration create buttons  
**Scope:** Read-only forensic diagnosis (no remediation)

---

## 1. Actual PROD behavior

Admin can open **Setup → Report Template Configuration**. Page shows System Default, Preview, Create Custom from System Default, Custom Generic, Create Generic, and Generic rows.

Admin reports that:

1. **Create Custom from System Default** — click does nothing (no designer, no obvious dialog).  
2. **Create Generic** — click does nothing.

### Intended behavior (from source — not invented)

Neither button navigates or opens a Bootstrap modal.

Both call `openCreate()`, which:

1. Sets `showCreate = true`.  
2. Resets create form fields (`createFrom='SystemDefault'`, `createCategory='CustomGeneric'`, …).  
3. Issues **GET** `/api/ReportTemplateConfiguration/targets` (for Specific target dropdown).  
4. Reveals an **inline** panel titled **“Create Template”** further down the same page (`*ngIf="showCreate"`).

Only after the Admin fills a name and clicks **Create** inside that panel does `createTemplate()` **POST** `/designer/create` and then `router.navigate` to `/report-template-configuration/design/:id`.

---

## 2. Create Custom from System Default — reproduction result

| Observation | Result |
|-------------|--------|
| Button present | Yes |
| Disabled | **No** (`disabled=false`) |
| Route change on click | **None** (URL stays `/report-template-configuration`) |
| Modal | **None** |
| Handler effect | **Create Template** panel inserted in DOM |
| Panel in viewport after click | **No** |
| Panel position | `getBoundingClientRect().top ≈ 3292` px (viewport height 768) |
| Page scroll after click | **None** (`avilis-page-body.scrollTop` remains 0) |
| Network on click | `GET /lis/api/ReportTemplateConfiguration/targets` → **200** |
| Console / page errors | **None** |

**User-visible effect at top of page:** none → reported as “does nothing.”  
**Actual effect:** create panel rendered **far below the fold** inside `.avilis-page-body`.

---

## 3. Create Generic — reproduction result

| Observation | Result |
|-------------|--------|
| Button present | Yes (Custom mode) |
| Disabled | No `[disabled]` binding |
| Route change | **None** |
| Handler | Same `openCreate()` (+ sets `createCategory='CustomGeneric'`) |
| Create panel after click | **Yes** (same panel) |
| In viewport | **No** (`top ≈ 3259` vs viewport 768) |
| Network | `GET .../targets` → **200** |
| Console errors | **None** |

**Same defect class as button A.**

---

## 4. Browser console findings

Playwright Chrome (headless) against live PROD Portal after Admin login + hard navigation:

- **pageerror:** none  
- **console error:** none  
- No Angular exception, TypeError, DI error, or template parse error observed on either click.

---

## 5. Network findings

| Click | Request generated? | URL | Method | Status |
|-------|--------------------|-----|--------|--------|
| Create Custom from System Default | Yes | `/lis/api/ReportTemplateConfiguration/targets` | GET | 200 |
| Create Generic | Yes | `/lis/api/ReportTemplateConfiguration/targets` | GET | 200 |
| Immediate create POST | **No** | — | — | — |

No 4xx/5xx on these clicks. No mutating create call until the secondary **Create** button in the panel (not exercised; would mutate PROD).

---

## 6. Angular HTML evidence

File: `web/Lis.Web/src/app/setup/report-template-configuration/report-template-configuration.component.html`

**A — Create Custom from System Default**

```html
<button type="button" class="btn btn-primary btn-sm" ...
        (click)="openCreate()" [disabled]="loading">
  Create Custom from System Default
</button>
```

- `type="button"` (not submit)  
- No `routerLink`  
- Disabled only when `loading`  

**B — Create Generic**

```html
<button type="button" class="btn btn-primary btn-xs"
        (click)="openCreate(); createCategory='CustomGeneric'">Create Generic</button>
```

**Create UI (not a modal; below lists)**

```html
<div class="panel panel-primary" *ngIf="showCreate" style="margin-top:16px;">
  <div class="panel-heading">Create Template</div>
  ...
</div>
```

DOM order on the page:

1. System Default well (button A)  
2. Custom Generic heading + button B + **54-row table** (~2359 px tall in PROD)  
3. Test / Profile Specific + 8-row table  
4. **Create Template panel** (`showCreate`)  
5. Sample Preview  

---

## 7. TypeScript handler evidence

File: `report-template-configuration.component.ts`

```typescript
openCreate() {
  this.showCreate = true;
  this.createName = '';
  this.createFrom = 'SystemDefault';
  this.createCategory = 'CustomGeneric';
  this.targetId = null;
  this.service.targets('').subscribe(rows => this.targets = rows || []);
}
```

| Question | Answer |
|----------|--------|
| Exact handler | `openCreate()` |
| Executes? | **Yes** (panel appears; targets GET fires) |
| Early exit? | No |
| API on click? | Read-only `targets()` only |
| Navigate on click? | **No** |
| Opens designer on click? | **No** (only after `createTemplate()` success) |
| Depends on selected template? | No |
| Depends on ReportType/Mode? | Button A visible whenever `workspace` loaded; button B only if `mode==='Custom'` |

`createTemplate()` (not invoked by the failing buttons) POSTs create then navigates to designer.

---

## 8. Routing evidence

| Route | Component | Guard |
|-------|-----------|-------|
| `report-template-configuration` | `ReportTemplateConfigurationComponent` | `ROUTE_GUARDS` |
| `report-template-configuration/design/:id` | `ReportTemplateDesignerComponent` | `ROUTE_GUARDS` |

Create buttons **do not use** the design route. Design route is reached only after successful create, or via **Design** on an existing row (`design(item)` → `router.navigate([...])`).

Module: declared in `app.module.ts` with `FormsModule` present — create panel `ngModel` bindings are supported.

---

## 9. API evidence

| Step | API | Role |
|------|-----|------|
| openCreate | `GET .../targets` | Load Test/Profile options (works in PROD — 200) |
| Create (panel) | `POST .../designer/create` | Creates template (not called in this diagnosis) |
| Design | `GET` list/load designer | Separate path |

Source + prior read-only UAT confirm create/activate APIs exist. **This failure is not an API outage on the first click.**

---

## 10. Deployed bundle evidence

| Check | Result |
|-------|--------|
| Loaded script | `main-es2015.5be29c72310d0fdb4ca5.js` |
| Matches prior Portal deploy | Yes |
| Source handlers present in that build era | Yes (`openCreate` / Create Template panel / Create Generic labels present in bundle historically verified) |

**Not** classification F (stale handler missing). Bundle is current; handler is present and executes.

---

## 11. Cache verification

Reproduction used a fresh Chrome session + loaded the current hashed main bundle from PROD.

- Bundle hash matches deployed Portal.  
- Behavior is consistent with source (inline panel below lists).  
- **Not** explained by stale cache omitting handlers.

Ctrl+F5 alone would **not** fix this: current code intentionally does not scroll or modalize.

---

## 12. Specific section visibility evidence

| Check | Result |
|-------|--------|
| In DOM when Custom + workspace | **Yes** — heading `Test / Profile Specific`, `Create Specific`, 8 Design rows |
| `*ngIf` | Parent: `mode==='Custom' && workspace`. Table: `specificTemplates?.length` |
| Dependent on Generic list length? | **No** for rendering; **Yes** for viewport position (Generic table pushes it down) |
| Position from top (PROD Diagnostic) | `top ≈ 2792` px |
| Below fold at scrollTop=0? | **Yes** |
| Scroll container | `.avilis-page-body` (`clientHeight ≈ 655`, `scrollHeight ≈ 3652`) |

So Specific **is rendered** but **not visible without scrolling** the page body past the ~54 Generic rows (~2359 px table height). This matches the earlier Specific “not visible” report without requiring missing implementation.

---

## 13. Root cause for each button

### A. Create Custom from System Default

**Classification: J. Other (primary)** — UX / layout: handler succeeds but result is an inline panel outside the viewport; no auto-scroll of `.avilis-page-body`.

Secondary labels (not primary):

- Not B (handler works)  
- Not C (not disabled when reproduced)  
- Not D (no navigation intended on first click)  
- Not E (targets 200)  
- Not F (bundle current)  
- Not G (no runtime errors)  
- Not A (cache)  
- Not I (Admin authenticated; page loaded)

Related layout factor **H**: content scrolls in `.avilis-page-body`, not `window`; click does not adjust that scrollTop.

### B. Create Generic

**Same root cause as A** (shared `openCreate()` + same below-fold Create Template panel).

---

## 14. One common defect or multiple?

**One common defect** affecting both buttons:

> `openCreate()` toggles an inline Create Template panel placed **after** large Custom Generic (+ Specific) tables, inside a fixed-height scroll container (`.avilis-page-body`), **without** scrolling that container into view and **without** a modal.

Symptoms for both: click appears to do nothing.

---

## 15. Design button (source-only / safe)

`design(item)` navigates to `/report-template-configuration/design/:id` (GET load). It does **not** use `openCreate()`.

Not clicked in PROD during this diagnosis (avoid any risk of accidental saves). Expectation: Design opens the designer workflow; Create buttons only open the create form panel until Create is confirmed.

---

## 16. Recommended minimum remediation (DO NOT IMPLEMENT YET)

Smallest production-safe UX fixes (product to choose one):

1. **Preferred minimal:** After `showCreate = true`, scroll the Create Template panel into view within `.avilis-page-body` (e.g. `Element.scrollIntoView` / `ViewChild`).  
2. **Or:** Move the Create Template panel **above** the Generic/Specific tables.  
3. **Or:** Use a true modal/dialog anchored to the viewport.

Optional follow-ups (separate, larger):

- Empty-state / sticky section headers so Specific is discoverable.  
- Stop embedding full `definitionJson` on every workspace list row (payload size / scroll height contributors) — not required to fix the click illusion.

Do **not** change Gate 6, feature flag, DB, or RBAC for this issue.

---

## Explicit statement

**NO SOURCE CHANGES**  
**NO DATABASE CHANGES**  
**NO CONFIGURATION CHANGES**  
**NO DEPLOYMENT**  
**NO TEMPLATE DATA CHANGES**  
**NO GATE 6 CHANGES**

Diagnostic automation used temporary scripts under `%TEMP%` only (deleted after runs). No repo files were modified for remediation.

---

## STOP

Diagnosis complete. Awaiting review before any remediation.
