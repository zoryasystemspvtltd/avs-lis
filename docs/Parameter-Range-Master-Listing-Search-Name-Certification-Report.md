# Parameter Range Master — Listing Search & Parameter Name Certification Report

**Module:** Parameter Range Master Listing  
**Scope:** Issue 1 (listing search) + Issue 2 (Parameter column display) only  
**Date:** 2026-09-11  
**Environment:** ZoryaLMS / AVILIS (IIS `localhost:8081` API, `localhost:8080` Portal)

---

## 1. Executive Summary

Two listing-only defects on Parameter Range Master were investigated and fixed with minimal changes:

| Issue | Root cause | Fix |
|-------|------------|-----|
| Search appeared broken | Backend filtered Range Code + Parameter **Name** only; UI showed Parameter **Code**, so typing the visible code returned no rows | Include `HisParamCode` in listing `SearchText` filter |
| Parameter column showed Code | Angular schema bound `hisParamCode` under heading “Parameter” | Bind `hisParamDescription` (already enriched by API) |

No schema changes, no CRUD/evaluation logic changes, no N+1 introduced.

**Final Verdict: APPROVED FOR PROD**

---

## 2. Issue 1 Root Cause — Search

`HisParameterRangeCrudManager.Get` already received `ListOptions.SearchText` correctly from Angular (`ApiOption` header). Filtering matched only:

- `HISRangeCode`
- `HisParamDescription`

The listing Parameter column displayed `hisParamCode`. Users searching by the value they see (parameter code) got empty results → perceived broken search.

Angular search plumbing (`SearchText`) was correct; the defect was incomplete server-side match fields.

---

## 3. Issue 2 Root Cause — Parameter Name

API `Enrich()` already sets both `HisParamCode` and `HisParamDescription` from Parameter Master (`HisParameterId`) in one dictionary lookup (no per-row API calls).

Angular `master-schemas.ts` → `hisParameterRange` bound:

```text
{ heading: 'Parameter', fieldName: 'hisParamCode' }
```

So the grid showed Code despite Name being present in the response.

---

## 4. Existing Implementation Analysis

```text
/his-parameter-ranges
  → MasterListComponent (masterKey=hisParameterRange)
  → ListModuleComponent
  → MasterService.getItems('HisParameterRangeMaster', ApiOption)
  → GET /api/HisParameterRangeMaster/
  → HisParameterRangeCrudManager.Get
       → load ranges → Enrich(parameters) → SearchText filter → page/sort
  → list cell fieldValue(item, fieldName)
```

Parameter relationship: `HISParameterRangMaster.HisParameterId` → `HISParameterMaster` (navigation JSON-ignored; NotMapped code/description enriched in memory).

---

## 5. Changes Made

1. **Backend** — `HisParameterRangeCrudManager.Get` search also matches `HisParamCode` (case-insensitive partial), alongside existing range code + parameter name.
2. **Frontend** — Parameter column `fieldName`: `hisParamCode` → `hisParamDescription`.
3. **Tests** — Added `HisParameterRange_Listing_Search_By_Code_And_Name_And_Enriches_Description`.

---

## 6. API Impact

| Endpoint | Impact |
|----------|--------|
| `GET api/HisParameterRangeMaster/` (listing/search) | Search matches Parameter Code in addition to prior fields |
| Get by ID / Create / Update / Delete | Unchanged |
| Response DTO shape | Unchanged (`hisParamCode` + `hisParamDescription` still returned) |

Backward compatible: no properties renamed/removed.

---

## 7. Database Impact

- **Schema:** none  
- **Data:** none (counts post-deploy: Ranges=47, Parameters=50)  
- **Migrations:** none  

---

## 8. UI Impact

- Listing Parameter column shows Parameter **Name**  
- Search behaviour improved for code/name  
- Layout, theme, CRUD forms, permissions, navigation: unchanged  

---

## 9. Security / RBAC Validation

| Check | Result |
|-------|--------|
| Authenticated listing | 200 with admin token |
| Unauthenticated listing (`accesskey` only) | **401** |
| Menu/RBAC model | Unchanged |

---

## 10. Performance Considerations

- Enrich still uses one Parameter Master dictionary load (existing pattern).  
- Search remains in-memory after Enrich (existing pattern; same as before).  
- No N+1 / no per-row Parameter API calls.

---

## 11. Unit / Manager Test Results

| Test | Result |
|------|--------|
| `HisParameterRange_Listing_Search_By_Code_And_Name_And_Enriches_Description` | **Passed** |
| `HisParameter_And_Range_Crud_With_Test_FK` | **Passed** |
| `HisParameterRange_Edit_Through_Interface_Preserves_Code_And_Updates` | **Passed** |

Covered: exact/partial code, name, case-insensitive, no-hit, empty search, pagination, enriched description.

---

## 12. API / Integration Test Results

| Check | Result |
|-------|--------|
| List (page size 5) | total=47, items=5 |
| Sample enrichment | `CBC-HGB` / name=`Haemoglobin` |
| Search by Parameter Code | total=1, hit=1 |
| Search by partial Parameter Name (`Haemog`) | total=1 |
| Unauthorized GET | 401 |

---

## 13. Regression Test Results (scope-guard)

Unchanged by design (not re-executed end-to-end workflows beyond listing/CRUD manager tests):

- Parameter Range Create/Edit/Delete validation path (CRUD tests passed)  
- Parameter Master, Test Mapping, Result Entry, Reference Range Evaluation, Diagnostic Report, Report Print  
- Patient / Sample / Approval / Radiology / Notification / Dashboard / OperationalDateTime  

**Documented out-of-scope observation (not fixed):** listing still loads all ranges into memory before filter/page (pre-existing).

---

## 14. Build Results

| Build | Result |
|-------|--------|
| API Release (`Lis.Api`) | Succeeded |
| Angular production | Succeeded |
| `LIS.Masters.Tests` Release | Succeeded |

---

## 15. Deployment Results

| Target | Result |
|--------|--------|
| API → `I:\Projects\PROD\AVILIS\API` | Deployed; site Started |
| Portal → `I:\Projects\PROD\AVILIS\PORTAL` | Deployed |
| API HTTP | **200** |
| Portal HTTP | **200** |
| Portal bundle Parameter bind | `heading:"Parameter",fieldName:"hisParamDescription"` confirmed |

---

## 16. Files Modified

| File | Change |
|------|--------|
| `LIS.Businesslogic/SetupDomainManagers.cs` | Search includes `HisParamCode` |
| `web/Lis.Web/src/app/masters/master-schemas.ts` | Parameter column → `hisParamDescription` |
| `LIS.Masters.Tests/Masters/HisParameterAndMappingTests.cs` | Listing search/name certification test |
| `docs/Parameter-Range-Master-Listing-Search-Name-Certification-Report.md` | This report |

---

## 17. Defects Found

1. Listing search omitted Parameter Code (displayed field).  
2. Listing Parameter column bound to Code instead of Name.  

---

## 18. Defects Fixed

Both defects above — fixed.

---

## 19. Remaining Issues

None for the two reported listing issues.

Out-of-scope (documented only): in-memory full-table listing load remains as before.

---

## 20. Production Readiness Assessment

| Criterion | Status |
|-----------|--------|
| Search by Parameter Code | Pass |
| Search by Parameter Name | Pass |
| Partial / case-insensitive search | Pass |
| Empty / clear search | Pass |
| Pagination with search | Pass |
| Parameter column shows Name | Pass |
| CRUD still functional (manager tests) | Pass |
| No N+1 | Pass |
| RBAC intact (401 unauth) | Pass |
| No DB schema change | Pass |
| Builds + IIS deploy + HTTP 200 | Pass |
| No Critical/High/Medium open for this scope | Pass |

---

## 21. Final Verdict

**APPROVED FOR PROD**

Hard-refresh the portal and open **Parameter Range Master** (`/his-parameter-ranges`) to confirm Parameter names and search by code/name.
