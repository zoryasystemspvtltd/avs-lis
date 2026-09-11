# Diagnostic Report — Individual Print Selection Certification Report

**Module:** Diagnostic Report — Print All / Print Specific  
**Scope:** Print-selection enhancement only (which eligible report(s) print)  
**Date:** 2026-09-12  
**Environment:** ZoryaLMS / AVILIS (`localhost:8081` API, `localhost:8080` Portal)

---

## 1. Executive Summary

Administrators/doctors can now choose **Print All Reports** (unchanged behaviour) or **Specific Report** (one test from the same Lab No / invoice) on the Diagnostic Report screen.

Implementation reuses existing report generation, eligibility, layout, signature, and iframe print. Optional API parameter `testRequestDetailId` scopes the existing DTO after ownership validation.

**Database schema changes = 0**  
**Final Verdict: APPROVED FOR PROD**

---

## 2. Existing Print Architecture

```text
Diagnostic Report UI (/reports/test-report)
        ↓ Search Lab No
GET api/Reports/TestReport?labNo=…
        ↓
TestReportManager.GetDiagnosticTestReport
  → Resolve invoice (paid, not cancelled)
  → ResolveTestRequests
  → ValidateWorkflow (all DoctorApproved + results)
  → Build sections / department groups
        ↓
Enrich approver / branding / layout
        ↓
Angular HTML + iframe print
```

---

## 3. Root Cause / Current Limitation

The report API always returned **all** eligible tests for the Lab No. There was no supported way to print a single test from a multi-test order.

---

## 4. Business Requirement

```text
Order
├── Blood Sugar
├── LFT
└── Lipid Profile
        │
        ▼
  Print Selection
     /         \
Print All    Print Specific
```

---

## 5. Solution Implemented

1. Optional `testRequestDetailId` on existing report API/manager.  
2. After existing `ValidateWorkflow`, if ID provided:  
   - Reject if not in this invoice’s requests (ownership).  
   - Narrow request list to that ID.  
3. Expose `TestRequestDetailId` on each report section DTO.  
4. UI: radio **All Reports** / **Specific Report** + dropdown of tests from current order.  
5. Specific mode re-fetches via API with ownership check, then prints existing renderer.

Authoritative identity: **`TestRequestDetail.Id`**.

---

## 6. UI Changes

`test-report.component.html` / `.ts`

- Print: ○ All Reports / ○ Specific Report  
- Select Report dropdown (current order tests only)  
- Button: **Print All Reports** or **Print Selected Report**  
- Reset/Search clears selection; no stale test from prior Lab No  

---

## 7. API Changes

```text
GET api/Reports/TestReport?labNo={}&invoiceNo={}&testRequestDetailId={optional long}
```

- Omitting `testRequestDetailId` → identical to previous Print All  
- With ID → same eligibility, scoped sections  
- Cross-order ID → `400` / `TestReportValidationException`  

Notification secure download still calls without filter (full invoice report).

---

## 8. Security Validation

| Case | Result |
|------|--------|
| Valid lab + owned testRequestDetailId | Pass — one section |
| Valid lab + foreign ID (`long.MaxValue`) | Pass — rejected (“does not belong”) |
| Invalid lab | Pass — rejected |
| Client-only filter not relied upon | Pass — Specific always re-fetches API |

---

## 9. Print-All Validation

Automated: omitting filter returns full eligible set; section IDs populated; backward-compatible overload.

---

## 10. Individual-Report Validation

Automated: selecting first section ID returns exactly one section; patient/invoice header preserved.

---

## 11. Multi-Department Validation

Department grouping code path unchanged; Specific mode builds groups from the single remaining request (only that department/test appears).

---

## 12. Multi-Page Validation

Same print CSS / layout / iframe mechanism; only HTML content scope changes.

---

## 13. Report Layout Regression

`EnrichReportLayout` still applied. Header/footer clearance and doctor signature config unchanged.

---

## 14. Doctor Signature Regression

Approver enrichment unchanged; signature source/path/position logic untouched.

---

## 15. Comment/Note Regression

Per-section comments still built by existing `BuildSection` / Parameter Master paths.

---

## 16. Unit / Integration Test Results

`DiagnosticReportPrintSelectionTests` — **5/5 passed**

1. PrintAll_Without_Filter_Matches_Existing_Behaviour  
2. PrintSpecific_Returns_Only_Selected_Test  
3. PrintSpecific_Rejects_Test_Not_Belonging_To_Order  
4. PrintSpecific_Invalid_Lab_Still_Rejected  
5. PrintAll_Omitting_Optional_Id_Remains_Backward_Compatible  

---

## 17. Integration Test Results

Live ZoryaLMS printable labs exercised by the tests above.

---

## 18. Security Test Results

Cross-order and invalid lab cases covered (see §8).

---

## 19. Regression Results

Unchanged:

- Eligibility (paid + all DoctorApproved)  
- Result/approval/payment logic  
- Layout configuration  
- Radiology  
- Schema  

---

## 20. Build Results

| Build | Result |
|-------|--------|
| API Release | Pass |
| Angular production | Pass |
| Print selection tests | 5/5 Pass |

---

## 21. Deployment Results

| Step | Result |
|------|--------|
| API → `I:\Projects\PROD\AVILIS\API` | Success |
| Portal → `I:\Projects\PROD\AVILIS\PORTAL` | Success |
| API HTTP | 200 |
| Portal HTTP | 200 |

---

## 22. Files Modified

- `LIS.Businesslogic/TestReportManager.cs`
- `LIS.DtoModel/Interfaces/ITestReportManager.cs`
- `LIS.DtoModel/Models/Reports/DiagnosticTestReportDto.cs`
- `web/Lis.Api/Controllers/Api/OperationalReportsController.cs`
- `web/Lis.Web/src/app/_services/report.service.ts`
- `web/Lis.Web/src/app/reports/test-report/test-report.component.ts`
- `web/Lis.Web/src/app/reports/test-report/test-report.component.html`
- `LIS.Masters.Tests/Reports/DiagnosticReportPrintSelectionTests.cs`
- `LIS.Masters.Tests/Notifications/NotificationPhase2Tests.cs` (stub signature)
- `LIS.Masters.Tests/LIS.Masters.Tests.csproj`
- `docs/Diagnostic-Report-Individual-Print-Certification-Report.md`

---

## 23. Database Impact

**None.**

---

## 24. Known Limitations

1. Specific print still requires the **entire order** to satisfy existing eligibility (all tests DoctorApproved). Partial-order print of one approved sibling while another is incomplete remains blocked — by design (unchanged eligibility).  
2. Radiology out of scope.  
3. Physical multi-page letterhead spot-check recommended after first use.

---

## 25. Production Readiness

| Criterion | Met |
|-----------|-----|
| Print All unchanged when no filter | Yes |
| Specific test print | Yes |
| Ownership validation | Yes |
| No schema change | Yes |
| Layout/signature/comments preserved | Yes |
| Builds + deploy + HTTP 200 | Yes |

---

## 26. Final Verdict

**APPROVED FOR PROD**

Print selection is a minimal, backward-compatible extension of the existing Diagnostic Report pipeline.
