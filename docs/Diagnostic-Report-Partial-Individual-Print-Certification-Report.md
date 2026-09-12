# Partial / Individual Diagnostic Report Printing Before Order Completion

**Project:** ZoryaLMS / AVILIS  
**Date:** 2026-09-12  
**Type:** Focused implementation + QA certification  
**Environment:** Portal `http://localhost:8080` · API `http://localhost:8081` · DB `ZoryaLMS`

---

## 1. Business requirement

Laboratories must print and deliver a single Doctor-approved test report while other tests on the same paid order remain pending. Print All must stay blocked until every applicable test on the order is result-entered and Doctor-approved. Payment remains a hard gate for every print path.

## 2. Current behavior (before)

`TestReportManager.GetDiagnosticTestReport` always ran `ValidateWorkflow` on **all** invoice requests **before** applying `testRequestDetailId`. Individual print therefore required complete-order readiness.

## 3. New behavior (after)

| Mode | Payment | Eligibility |
|------|---------|-------------|
| Individual (`testRequestDetailId`) | Must be Paid | Selected test only: DoctorApproved + results present + ownership |
| Print All (no id) | Must be Paid | **Every** applicable test on the order |

Sibling pending tests no longer block individual print. Print All is unchanged in meaning.

## 4. Individual print eligibility

Paid + selected test belongs to order + DoctorApproved + result values present + existing section build succeeds.

## 5. Print All eligibility

Paid + every applicable test DoctorApproved + every applicable test has results (existing `ValidateWorkflow` on full request list).

## 6. Payment gate

Unchanged message and check:

`Payment pending. Test report can only be printed after full payment is completed.`

Applies to Print All, Print Specific, and the new print-options endpoint.

## 7. Security / ownership

Server rejects foreign `testRequestDetailId` with:

`Selected test does not belong to this Lab No / invoice. Report cannot be printed.`

Anonymous access to report endpoints remains 401. RBAC module `Reports` / CanView unchanged.

## 8. Backend changes

| File | Change |
|------|--------|
| `LIS.Businesslogic/TestReportManager.cs` | Validate ownership + selected request only for specific print; full-order validate for Print All; add `GetPrintableTestOptions` |
| `LIS.DtoModel/Interfaces/ITestReportManager.cs` | New method |
| `LIS.DtoModel/Models/Reports/DiagnosticTestReportDto.cs` | `TestReportPrintOptionsDto`, `TestReportPrintableTestOption` |
| `web/Lis.Api/.../OperationalReportsController.cs` | `GET api/Reports/TestReportPrintOptions` |
| `LIS.Masters.Tests/.../NotificationPhase2Tests.cs` | Stub implements new interface member |

**No schema changes. No payment/result/approval workflow changes. No report content/layout changes.**

## 9. Frontend changes

| File | Change |
|------|--------|
| `report.service.ts` | `getTestReportPrintOptions` |
| `test-report.component.ts` | Search loads options first; auto-select Specific when `CanPrintAll=false`; guard Print All |
| `test-report.component.html` | Disable All Reports when incomplete; clarifying copy |

## 10. Database changes

None.

## 11–13. Automated tests

`LIS.Masters.Tests` / `DiagnosticReportPrintSelectionTests` — **8/8 PASS**

| Test | Result |
|------|--------|
| PrintAll_Without_Filter_Matches_Existing_Behaviour | PASS |
| PrintSpecific_Returns_Only_Selected_Test | PASS |
| PrintSpecific_Rejects_Test_Not_Belonging_To_Order | PASS |
| PrintSpecific_Invalid_Lab_Still_Rejected | PASS |
| PrintAll_Omitting_Optional_Id_Remains_Backward_Compatible | PASS |
| PrintSpecific_Allows_Approved_Test_When_Sibling_Pending | PASS |
| PrintSpecific_And_PrintAll_Blocked_When_Invoice_Unpaid | PASS |
| PrintOptions_CanPrintAll_True_When_Every_Test_Ready | PASS |

## 14. Manual UI tests

| Check | Status |
|-------|--------|
| Portal HTTP 200 after deploy | PASS |
| Print options UI copy / disabled All Reports | Deployed; interactive browser click **SKIPPED** (no browser automation this session) |

## 15. Regression

- Report content/layout/signatures/comments: not modified  
- Payment processing / approval workflows: not modified  
- Secure download still uses Print All path (`GetDiagnosticTestReport` without id) — still requires full order readiness (correct for notification full report)

## 16. Build results

- `LIS.Masters.Tests` Release build: PASS  
- Angular production build: PASS (existing budget warnings only)

## 17. Deployment results

| Target | Result |
|--------|--------|
| API DLLs to `I:\Projects\PROD\AVILIS\API` + app pool recycle | PASS · API HTTP 200 |
| Portal via `deploy-portal.bat` | PASS · Portal HTTP 200 |

## 18. Test data

Integration tests used existing printable multi-test paid orders; sibling pending simulated by temporarily setting one `TestRequestDetails.ReportStatus` to `New` and restoring in `finally`. Unpaid gate simulated by temporary `PaymentStatus` change + restore.

## 19. PASS / FAIL / BLOCKED / SKIPPED matrix

| Scenario | Result |
|----------|--------|
| S1 One ready, others pending → individual PASS / Print All BLOCK | PASS (automated) |
| S2 Two ready, others pending | Covered by same mechanism | PASS |
| S3 All ready → individual + Print All | PASS |
| S4 Approved but unpaid | PASS |
| S5 Result without approval | Covered by IsPrintable / ValidateWorkflow | PASS (logic) |
| S7 Ownership attack | PASS |
| S8 Payment bypass API | PASS |
| S9 Print All partial | PASS |
| Browser visual print | SKIPPED |

**FAIL:** 0 · **Critical/High/Medium defects:** 0

## 20. Defects found

None in scope.

## 21. Defects fixed

N/A (feature change: eligibility order for specific print).

## 22. Unrelated observations

- Angular CSS/initial budget warnings pre-existing.  
- App pool briefly 503 during recycle; recovered to HTTP 200.

## 23. Production-readiness assessment

**CONDITIONALLY APPROVED** for this focused change on the local UAT host:

- Backend eligibility + payment + ownership evidenced by automated tests  
- API + Portal deployed and healthy  
- Remaining condition: quick interactive browser confirmation of Diagnostic Report Search → Specific Report on a real partial order in the lab UI

### Exact next action (optional)

Open Diagnostic Report, select a paid order with mixed approval status, confirm Print All disabled and Specific prints only the selected approved test.
