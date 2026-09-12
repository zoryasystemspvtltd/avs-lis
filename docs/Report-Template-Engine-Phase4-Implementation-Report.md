# Report Template Engine — Phase 4 Implementation Report

**Date:** 2026-09-12  
**Status:** Implementation complete through Gates 1–3 (+ local verification). **Gate 6 NOT activated.**

## Summary

Phase 4 production presentation wiring was implemented per the approved Final Architecture plan.

- Production flag remains **OFF** (`Web.config` + fail-closed reader).
- System Default / builtin → Existing Angular.
- Declarative only when flag ON + Custom + successful integrity-checked render.
- Whole-report switch only; presentation failures → Angular without 400/500.
- NO DB CHANGE REQUIRED.

## Gates

| Gate | Status |
|------|--------|
| 1 Implementation | Done |
| 2 Automated tests | 61 ReportTemplate* tests PASS |
| 3 Build | DtoModel, BusinessLogic, Api, Masters.Tests, Angular prod SUCCESS |
| 4 Deployment verification | Artifacts built; config key `false`. Site IIS deploy is ops-owned |
| 5 Browser/E2E | Code-path verified for flag OFF → Angular; live browser UAT pending on target env |
| 6 Production activation | **NOT DONE** — flag stays false |

## Flag

`ReportTemplate:UseDeclarativeRendererForProductionPrint` = `false` in `web/Lis.Api/Web.config`.  
Reader: `ReportTemplateEngineFeatureFlags` (fail-closed).

## Key files

- `ReportProductionPresentationAdapter.cs`
- `OperationalReportsController` post-enrich Apply*Presentation
- Angular `test-report` / `radiology-report-print` declarative bind
- RBAC: HTTP QAuthorize tests + `report-template-configuration-rbac.spec.ts`
