# ZoryaLMS — Diagnostic + Radiology Report Engine  
# Current-State Discovery (READ-ONLY)

**Document type:** Architecture / data / UI / printing discovery  
**Date:** 2026-09-12  
**Repository:** `I:\Projects\LIS\avs-lis`  
**Exercise rules:** No code, schema, config, template, UI, or deployment changes. Defects documented only.

**Evidence legend used throughout:**

| Tag | Meaning |
|-----|---------|
| **CONFIRMED FROM CODE** | Established by source inspection |
| **CONFIRMED FROM DATABASE** | Established by entity/migration/table mapping |
| **CONFIRMED FROM CONFIGURATION** | Established by Web.config / app settings |
| **INFERRED** | Reasonable implication; not fully proven end-to-end |
| **UNKNOWN** | Cannot be answered from repository alone |

---

## 1. Executive Summary

ZoryaLMS currently produces **Diagnostic (laboratory)** and **Radiology** clinical reports through two **separate** pipelines that share:

- Sale Invoice / payment gates  
- Patient demographics  
- Report Layout Configuration (stationery clearance + signature placement)  
- Browser HTML print (no server-side PDF engine)  
- User-level signature image storage (`AspNetUsers.DoctorSignaturePath`)

They **do not** share:

- Result models (parameter grid vs narrative Findings/Impression)  
- Approval status models (`ReportStatusType` vs `RadiologyReportStatus`)  
- Print managers (`TestReportManager` vs `RadiologyReportManager`)  
- Angular print components  

**CONFIRMED FROM CODE:** There is **no** third-party report designer, **no** RDLC/Crystal/FastReport/DevExpress/QuestPDF/iText report engine, **no** reusable Report Viewer component, and **no** stored PDF snapshot of printed reports. Layout of clinical content is **hard-coded Angular HTML/CSS**; only physical margins/signature geometry are database-configurable.

This discovery is intended as factual input for a later Report Template / Report Designer architecture phase — **not** a design proposal.

---

## 2. Solution Architecture

### 2.1 Solution

**CONFIRMED FROM CODE:** `avs-lis.sln`

| Project | Purpose | Tech | Reporting relevance |
|---------|---------|------|---------------------|
| `LIS.DtoModel` | Entities, DTOs, enums, interfaces | .NET Framework 4.8 | Report DTOs, layout entity, manager interfaces |
| `LIS.DataAccess` (`LIS.DataModel`) | EF6 DbContext, migrations | .NET 4.8 | Tables including `ReportLayoutConfiguration` |
| `LIS.BusinessLogic` | Domain managers | .NET 4.8 | `TestReportManager`, `RadiologyReportManager`, `ReportLayoutConfigurationManager` |
| `LIS.Com.Businesslogic` | Instrument/TCP/barcode COM | .NET 4.8 | Indirect (results inflow) |
| `LIS.Logger` | Logging | .NET 4.8 | Used by managers |
| `Lis.Api` | ASP.NET Web API 2 + Identity | .NET 4.8 | Report controllers, signature storage, Web.config |
| `Lis.Api.Client` | Client helpers | .NET 4.8 | Low |
| `Lis.Api.Client.Test` | Client tests | .NET 4.8 | Low |
| `LIS.Masters.Tests` | MSTest integration/unit | .NET 4.8 | Print/layout/signature/RBAC tests |
| `LisTCPIPConsole` | Console host | .NET 4.8 | Instrument |
| `BarcodePrint` | Barcode printing | .NET 4.8 | Labels, not clinical reports |
| `Test.COM` | COM tests | .NET 4.8 | Low |

**Not in `.sln`:** Angular SPA `web/Lis.Web` (deployed separately).

### 2.2 Versions & libraries

| Item | Value | Evidence |
|------|-------|----------|
| .NET | **4.8** | `*.csproj` `TargetFrameworkVersion` |
| Angular | **~10.0.5** | `web/Lis.Web/package.json` `@angular/core` |
| Angular CDK | **^10.1.0** | package.json (present; **no report DragDrop usage found**) |
| Print helper | `ngx-print` ^1.3.0 | package.json; diagnostic/radiology clinical print uses custom `iframe` + `window.print` |
| PDF NuGets | **None found** | packages.config / csproj scan |
| Reporting engines (RDLC/Crystal/etc.) | **None found** | repo search |

---

## 3. Diagnostic Report Architecture

### 3.1 Lifecycle (CONFIRMED FROM CODE + domain entities)

```
PatientDetail
  → PatientVisit (optional; VisitId resolved for header)
  → SaleInvoice + SaleInvoiceDetail
  → TestRequestDetail (per lab test; SampleNo; ReportStatus)
  → Collection / Receiving (CollectedBy, ReceivedBy, dates)
  → TestResult + TestResultDetails (values)
  → Technician approval (ReportStatus → TechnicianApproved; TestResult.ReviewedBy)
  → Doctor approval (ReportStatus → DoctorApproved; TestResult.AuthorizedBy)
  → TestReportManager.GetDiagnosticTestReport
  → OperationalReportsController enrich (approver, technician, branding, layout)
  → Angular TestReportComponent HTML
  → Browser print (iframe / window.print)
```

### 3.2 Core classes

| Layer | Class | File | Key methods |
|-------|-------|------|-------------|
| Interface | `ITestReportManager` | `LIS.DtoModel/Interfaces/ITestReportManager.cs` | `GetDiagnosticTestReport`, `GetPrintableTestOptions`, `GetPrintableLabNumbers` |
| Business | `TestReportManager` | `LIS.Businesslogic/TestReportManager.cs` | Same + `ValidateWorkflow`, `BuildSection`, `BuildDepartmentGroups`, `BuildProfileGroupedSections` |
| API | `OperationalReportsController` | `web/Lis.Api/Controllers/Api/OperationalReportsController.cs` | `GetTestReport*`, enrichers |
| Exception | `TestReportValidationException` | `LIS.Businesslogic/TestReportValidationException.cs` | → HTTP 400 |
| Angular | `TestReportComponent` | `web/Lis.Web/src/app/reports/test-report/` | search / printMode / print |
| Service | `ReportService` | `web/Lis.Web/src/app/_services/report.service.ts` | HTTP wrappers |

### 3.3 Important business rules (print)

**CONFIRMED FROM CODE** (`TestReportManager.GetDiagnosticTestReport`):

1. LabNo or InvoiceNo required  
2. Invoice must exist  
3. Invoice not Cancelled  
4. `PaymentStatus == Paid`  
5. Test requests must exist for invoice  
6. **Print All:** every request passes `ValidateWorkflow` (DoctorApproved + results)  
7. **Print Specific:** ownership of `testRequestDetailId`, then `ValidateWorkflow` on **selected only**  
8. Sections built only for DoctorApproved runs with parameter values  

---

## 4. Radiology Report Architecture

### 4.1 Lifecycle (CONFIRMED FROM CODE)

```
SaleInvoice (Diagnostic ProcessingCategory tests)
  → RadiologyRequestDetail (AccessionNo, Pending)
  → RadiologyResultDetail narrative save (Draft / UnderReview)
  → Doctor AuthorizeReport (Authorized or Released; requires digital signature flag)
  → RadiologyReportManager.GetRadiologyReportForPrint
  → OperationalReportsController enrich (approver, layout)
  → Angular RadiologyReportPrintComponent HTML
  → Browser print
```

**CONFIRMED FROM CODE:** No separate Examination entity/table. Exam identity = `HISTestName` + `Modality` + `Department` on `RadiologyRequestDetail`.

### 4.2 Status model

**CONFIRMED FROM CODE** (`DTOEnums` / manager):  
`Pending → Draft → UnderReview → Authorized | Released`

### 4.3 Core classes

| Layer | Class | File |
|-------|-------|------|
| Interface | `IRadiologyReportManager` | `LIS.DtoModel/Interfaces/IRadiologyReportManager.cs` |
| Business | `RadiologyReportManager` | `LIS.Businesslogic/RadiologyReportManager.cs` |
| Workflow API | `RadiologyReportController` | `web/Lis.Api/Controllers/Api/RadiologyReportController.cs` |
| Print API | `OperationalReportsController` | `GetRadiologyReport`, `GetRadiologyPrintAccessions` |
| Angular print | `RadiologyReportPrintComponent` | `web/Lis.Web/src/app/reports/radiology-report-print/` |
| Angular entry/approval | `radiology-report-entry`, `radiology-doctor-approval`, `radiology-approved-reports` | `web/Lis.Web/src/app/LIS/radiology/` |

### 4.4 Print gates (Radiology)

**CONFIRMED FROM CODE** (`GetRadiologyReportForPrint`):

- Status must be Authorized or Released  
- Invoice found via `HISRequestNo`  
- Invoice not Cancelled  
- Payment Paid  
- Findings and Impression non-empty  

### 4.5 Diagnostic vs Radiology differences

| Concern | Diagnostic | Radiology |
|---------|------------|-----------|
| Request table | `TestRequestDetails` | `RadiologyRequestDetail` |
| Result shape | Parameter rows | Narrative text fields |
| Approval enum | `ReportStatusType` (DoctorApproved=5) | `RadiologyReportStatus` |
| Print manager | `TestReportManager` | `RadiologyReportManager` |
| Individual print | `testRequestDetailId` | One accession per print (no multi-exam “Print All” sibling filter) |
| Technician signature on print | Yes (`ReviewedBy`) | No (doctor/radiologist only) |
| Branding Web.config `Report:*` | Applied (`EnrichReportBranding`) | **CONFIRMED not applied** on radiology enrich path |
| Layout ReportType key | `Diagnostic` | `Radiology` |

---

## 5. Report Data Model

### 5.1 Diagnostic-related entities

| Entity | Table | PK | Important FKs / fields used by reports |
|--------|-------|----|----------------------------------------|
| `PatientDetail` | `PatientDetails` | `Id` | Name, Age, Gender, MRNo, VisitId, Phone |
| `PatientVisit` | `PatientVisit` | `PatientVisitId` | `PatientId`; VisitId resolution |
| `SaleInvoice` | `SaleInvoice` | `Id` | `InvoiceNo`, `PatientId`, `PaymentStatus`, `InvoiceStatus`, dates, referral/corporate |
| `SaleInvoiceDetail` | `SaleInvoiceDetail` | `Id` | `SaleInvoiceId`, `TestId`, `TestProfileId`, `RequestDetailId` |
| `TestRequestDetail` | `TestRequestDetails` | `Id` | `SampleNo`, test codes/names, specimen, department, `HISRequestNo`, `ReportStatus`, collection/receive dates, `PatientId` |
| `TestResult` | `TestResults` | `Id` | `TestRequestId`, `AuthorizedBy`, `ReviewedBy`, dates, notes |
| `TestResultDetails` | `TestResultDetails` | `Id` | `TestResultId`, param codes/values/units |
| `HisTestMaster` | `HISTestMaster` | `Id` | Test master / department |
| `HISParameterMaster` | `HISParameterMaster` | `Id` | Parameter master, comments |
| `HISParameterRangMaster` | `HISParameterRangMaster` | `Id` | Gender/age ranges |
| `ReportLayoutConfiguration` | `ReportLayoutConfiguration` | `Id` | Clearance + signature geometry |
| `ApplicationUser` | `AspNetUsers` | `Id` | `DoctorSignaturePath`, `Qualification`, `DoctorDesignation` |

**CONFIRMED FROM DATABASE/ENTITY:** There is **no** dedicated `Sample` table; sample identity lives on `TestRequestDetails.SampleNo`.

### 5.2 Radiology entities

| Entity | Table | PK | Report fields |
|--------|-------|----|---------------|
| `RadiologyRequestDetail` | `RadiologyRequestDetail` | `Id` | AccessionNo, HISRequestNo, Modality, test name/code, Department, ReportStatus, PatientId |
| `RadiologyResultDetail` | `RadiologyResultDetail` | `Id` | `RadiologyRequestId` (unique), ClinicalHistory, Findings, Impression, Recommendation, AuthorizedBy/On, DigitalSignature |

### 5.3 Secure download (lab notifications)

| Entity | Table | Role |
|--------|-------|------|
| `SecureLinkToken` | `SecureLinkToken` | Token lifecycle for anonymous download |
| `NotificationAudit` | `NotificationAudit` | Send audit; may reference invoice/patient |

---

## 6. Report Data Contract

### 6.1 Delivery format

**CONFIRMED FROM CODE:** Report generation returns **strongly typed C# DTOs** serialized as **JSON** over Web API. Angular binds JSON to HTML templates. No DataTables / anonymous objects / stored-procedure result sets observed on the print path.

### 6.2 Diagnostic DTO (actual properties)

**Source:** `LIS.DtoModel/Models/Reports/DiagnosticTestReportDto.cs`

**Header:** LabNo, InvoiceNo, PatientName, PatientId, MRNo, VisitId, Age, Gender, ReferralDoctor, Corporate, CollectionDate, ReceivedDate, ReportDate, Status, ApprovedBy*, ReviewedBy*, branding fields (LabName, Tagline, CentreName, LogoUrl, LicenseName, Address, Email, ContactNumbers, PharmacyContact, AppointmentContact).

**Section:** TestRequestDetailId, TestCode, TestName, Specimen, SampleNo, Department, Comment, DoctorApprovalComment, Parameters[].

**Parameter:** ParameterCode, ParameterName, ResultValue, Unit, ReferenceRange, Flag, IsAbnormal, SectionName.

**Groups:** DepartmentGroups[{DepartmentName, Sections}], ProfileGroups[{ProfileName, ProfileCode, Sections}], flat Sections.

**Layout:** `ReportLayoutConfigurationDto` (see §9).

### 6.3 Radiology DTO (actual properties)

**Source:** `DiagnosticRadiologyReportDto.cs`

**Header:** AccessionNo, InvoiceNo, PatientName, PatientId, MRNo, VisitId, Age, Gender, TestName, Modality, Department, ReportStatus, ReportDate, AuthorizedBy/On, DigitalSignature, AuthorizedByName/Designation/SignatureImage.

**Body:** ClinicalHistory, Findings, Impression, Recommendation, Layout.

**Do not invent fields:** Image collections, measurement grids, and technique templates are **not** present on this DTO (**CONFIRMED FROM CODE**).

---

## 7. Current Template / Layout Architecture

**CONFIRMED FROM CODE:**

| Aspect | Mechanism |
|--------|-----------|
| Template type | Angular HTML components |
| Styles | Component CSS + **injected print CSS string** (`getDiagnosticPrintStyles` / radiology equivalent) |
| Page size / margins | CSS `@page` driven by Layout DTO mm values |
| Clinical structure | Hard-coded HTML tables/sections |
| Rendering engine | Browser layout engine |
| Server PDF | None |

**Key files:**

- `web/Lis.Web/src/app/reports/test-report/test-report.component.html`  
- `web/Lis.Web/src/app/reports/test-report/test-report.component.ts` (`executePrint`, `getDiagnosticPrintStyles`)  
- `web/Lis.Web/src/app/reports/radiology-report-print/radiology-report-print.component.html`  
- `web/Lis.Web/src/app/reports/radiology-report-print/radiology-report-print.component.ts`

---

## 8. Pre-Printed Stationery

**CONFIRMED FROM CODE:**

- Reports intentionally leave blank header/footer bands for letterhead.  
- Clearances are **configurable** via `ReportLayoutConfiguration` (`HeaderHeightMm`, `FooterHeightMm`, `LeftMarginMm`, `RightMarginMm`).  
- Defaults (**CONFIRMED FROM CODE** in layout manager tests/defaults): Diagnostic header/footer typically 50mm; Radiology header often 40mm, footer 50mm; side margins commonly 10–12mm.  
- Header/Footer **graphic content is not stored** in DB — only clearance.  
- Angular print CSS applies clearance as top/bottom spacing / `@page` margins.

---

## 9. Report Layout Configuration

### 9.1 Persistence

**CONFIRMED FROM DATABASE:** Table `ReportLayoutConfiguration` — one active conceptual row per `ReportType` (`Diagnostic` | `Radiology`).

**Entity:** `LIS.DtoModel/Models/Reports/ReportLayoutConfiguration.cs`  
**Manager:** `LIS.Businesslogic/ReportLayoutConfigurationManager.cs` (`GetByReportType`, `GetDefaults`, `Save`, `ResetToDefault`)  
**API:** `web/Lis.Api/Controllers/Api/ReportLayoutConfigurationController.cs`  
**UI:** `web/Lis.Web/src/app/setup/report-layout-configuration/`  
**RBAC module:** `ReportLayoutConfiguration`; menu key `SETUP_REPORT_LAYOUT_CONFIGURATION`

### 9.2 Fields

PageSize, Orientation, Header/Footer/Left/Right mm, Doctor signature enable/horizontal/vertical/width/height, Technician signature enable/horizontal/vertical/width/height, IsActive, audit Created/Modified.

### 9.3 Runtime path

```
Setup UI → POST api/ReportLayoutConfiguration
  → ReportLayoutConfigurationManager.Save
  → dbo.ReportLayoutConfiguration
  → Print GET attaches Layout via EnrichReportLayout / EnrichRadiologyLayout
  → Angular resolvePrintLayout → CSS variables / signature zone sizing
```

**CONFIRMED FROM CODE:** Layout config is **directly consumed by the print renderer** (client CSS), not merely displayed in Setup.

---

## 10. Doctor Signature

| Concern | Finding | Evidence |
|---------|---------|----------|
| Capture | User Create/Edit multipart upload | `UsersController.PostDoctorSignature` |
| Storage field | `AspNetUsers.DoctorSignaturePath` | `IdentityModels.ApplicationUser` |
| Files | `uploads/doctors` under API site | `DoctorSignatureStorage.RelativeFolder` |
| Validation | PNG/JPG, max 2 MB | `DoctorSignatureStorage` |
| Scope | **User-level** (not report-level template) | CONFIRMED FROM CODE |
| Diagnostic consumption | Header ApprovedBy → resolve user → data-URI image | `EnrichLabApprover` |
| Radiology consumption | AuthorizedBy → resolve user → signature image | `EnrichRadiologyApprover` |
| Layout | Position/size from Report Layout Configuration | Angular print CSS |

---

## 11. Technician Signature

| Concern | Finding |
|---------|---------|
| Capture | **Same** User Create/Edit + `DoctorSignaturePath` when user has Technician role |
| Storage | Shared field/folder with Doctor mechanism |
| Diagnostic print | Header `ReviewedBy` → `EnrichLabTechnician` |
| Radiology print | **Not applied** (no technician reviewer on radiology print DTO) |
| Layout | Technician signature panel in Report Layout Configuration (Diagnostic) |

**CONFIRMED FROM CODE:** Same underlying storage mechanism as Doctor Signature.

---

## 12. Diagnostic Parameter Model

**CONFIRMED FROM CODE** (`BuildSection` / `MapParameter` / `TestResultRangeEvaluator`):

```
HISTestMaster
  → HISParameterMaster (+ optional ranges HISParameterRangMaster)
  → TestResult / TestValues / TestResultDetails (runtime values)
  → DiagnosticTestReportParameter rows
```

| Concern | Behavior |
|---------|----------|
| Ordering | Driven by approved run values / booking order; department grouping uses invoice booking order |
| Groups | Optional `SectionName` from analyzer mapping GroupName when ≥2 groups |
| Profiles | Invoice profile lines → `ProfileGroups` (secondary UI path) |
| Reference ranges | Gender/age applicable range; avoids verbose catalog “Both … Year” strings |
| Flags | `L` / `H`, `IsAbnormal` |
| Comments | Parameter master comments aggregated to section Comment; DoctorNote → DoctorApprovalComment |

Rendering is **database-driven values + hard-coded HTML table columns** (not template-driven).

---

## 13. Repeating Structures

| Structure | How repetition is expressed |
|-----------|-----------------------------|
| Parameters | `*ngFor` over `section.parameters` |
| Tests/sections | `*ngFor` over `departmentGroups[].sections` (primary) |
| Departments | `*ngFor` over `departmentGroups` |
| Profiles | Legacy `profileGroups` when departments empty |
| Radiology narrative | **Non-repeating** fixed sections (History/Findings/Impression/Recommendation) |

**CONFIRMED FROM CODE:** “Repeat for each parameter/test” is implemented as **Angular structural directives over DTO arrays**, not a designer “repeating band” abstraction.

---

## 14. Multi-Test Orders

**CONFIRMED FROM CODE:**

- One `SaleInvoice` → many `SaleInvoiceDetail` lines → many `TestRequestDetail` rows sharing `HISRequestNo` ≈ invoice number.  
- SampleNo format: invoice digits + specimen code (`Helper.BuildSampleNo`) — stored on `TestRequestDetails`.  
- Report grouping: department (primary) and/or profile.  
- Identifiers for print: LabNo/InvoiceNo + optional `TestRequestDetailId`.

---

## 15. Individual Report Printing

**CONFIRMED FROM CODE** (current implementation after partial-print change):

| Layer | Detail |
|-------|--------|
| UI | Diagnostic Report page radios: All Reports / Specific Report; dropdown of printable tests |
| Options API | `GET api/Reports/TestReportPrintOptions` |
| Print API | `GET api/Reports/TestReport?labNo=&testRequestDetailId=` |
| Manager | Ownership check → validate selected only → build sections for that request |
| Security | QAuthorize Reports CanView; ownership; payment; DoctorApproved+results |
| Output | Same HTML print path, fewer sections |

Cert/history docs: `docs/Diagnostic-Report-Partial-Individual-Print-Certification-Report.md`, earlier individual-print cert.

---

## 16. Print All

**CONFIRMED FROM CODE:**

- Triggered when `testRequestDetailId` omitted.  
- Requires **every** applicable `TestRequestDetail` on the invoice to pass `ValidateWorkflow`.  
- UI disables All Reports when `CanPrintAll == false`.  
- **Must not** be treated as “print whatever is approved.”

Radiology: print is per accession; “Print All tests on one order” is a Diagnostic concept.

---

## 17. Payment Gate

| Layer | Enforced? | Source |
|-------|-----------|--------|
| Business (Diagnostic) | **Yes** | `TestReportManager` Paid check |
| Business (Radiology) | **Yes** | `GetRadiologyReportForPrint` Paid check |
| Print options | **Yes** | `GetPrintableTestOptions` |
| Lab number list | Filters unpaid | `GetPrintableLabNumbers` |
| Secure download | **Yes** | `NotificationSecureDownloadManager` |
| Frontend | Soft (list/options); **not** authoritative | Angular |

**CONFIRMED FROM CODE:** Payment is a **hard business-layer gate**. Frontend alone cannot be trusted.

Payment field: `SaleInvoice.PaymentStatus` (`PaymentStatusType.Paid = 2`).

---

## 18. Approval Gate

### Diagnostic

- Prerequisite workflow: TechnicianApproved before DoctorApproved (`TestRequestDetailsManager.ValidateStatusTransition`).  
- **Print requires** `ReportStatus == DoctorApproved` plus result values (`ValidateWorkflow` / `IsRequestPrintable`).  
- Approver identity: `TestResult.AuthorizedBy`; reviewer: `TestResult.ReviewedBy`.

### Radiology

- Print requires `Authorized` or `Released`.  
- Authorize requires digital signature flag on authorize request.  
- Approver: `RadiologyResultDetail.AuthorizedBy`.

---

## 19. Report Generation vs Printing Pipeline

**CONFIRMED FROM CODE:**

```
Database entities
  → Business managers assemble DTOs
  → Web API JSON
  → Angular binds DTO to HTML template
  → Injected print CSS (layout config)
  → Hidden iframe or window.print()
  → OS/browser printer / PDF printer driver (user-controlled)
```

There is **no** separate “generate PDF artifact then print” server step for clinical Diagnostic/Radiology reports.

---

## 20. PDF Generation

| Question | Answer | Confidence |
|----------|--------|------------|
| Server PDF library? | **None found** | High |
| Deterministic PDF bytes stored? | **No** | High |
| Preview vs print vs download same renderer? | Preview = on-screen HTML; Print = same HTML via print CSS; Secure download = **JSON DTO** (lab) | High |

---

## 21. Report Viewer

**NO REUSABLE REPORT VIEWER FOUND.**

Existing behavior:

- Report pages embed HTML preview inline  
- Print via ad-hoc iframe duplication in Diagnostic and Radiology print components  
- `YouFrameComponent` exists but is not a clinical report/PDF viewer  

---

## 22. Existing Designer / Drag-Drop Technologies

| Technology | Present? | Used for reports? |
|------------|----------|-------------------|
| Angular CDK DragDrop | CDK package present | **No report usage found** |
| Canvas / SVG editor | Not found for reports | — |
| Form builder visual | No | Reactive `FormBuilder` only for ordinary forms |
| TinyMCE | Present in app | Radiology entry uses plain textareas |
| Report designer package | **Not found** | — |

---

## 23. Existing Template-Like Mechanisms

| Mechanism | What it configures | Clinical content layout? |
|-----------|--------------------|--------------------------|
| Report Layout Configuration | Stationery clearances + signature geometry | **No** |
| Web.config `Report:*` branding | Lab name/logo/contacts (Diagnostic enrich) | Partial branding only |
| Notification templates | SMS/WhatsApp text versions | Not clinical PDF |
| Hard-coded Angular HTML | Full clinical report structure | **Yes** |

---

## 24. Security / RBAC

| Capability | Module | Notes |
|------------|--------|-------|
| Diagnostic print | `Reports` CanView | `api/Reports/TestReport*` |
| Radiology print | `RadiologyReports` CanView | `api/Reports/RadiologyReport*` |
| Radiology entry | `RadiologyReportEntry` | queues/save |
| Radiology doctor approval | `RadiologyDoctorApprovals` | authorize |
| Layout configuration | `ReportLayoutConfiguration` | Admin Setup menu |
| Signature upload | `Users` CanEdit | role must be Doctor or Technician |
| Secure download | AllowAnonymous + token validation | Opaque errors; paid gate inside manager |

Menu keys include `REPORT_DIAGNOSTIC`, `REPORT_RADIOLOGY`, `SETUP_REPORT_LAYOUT_CONFIGURATION` (`MenuCatalog` / role-permission catalog).

---

## 25. Audit / Versioning

| Area | Audit fields | Versioning |
|------|--------------|------------|
| TestRequestDetail | CreatedBy/On | No version |
| TestResult | CreatedBy/On; Authorized/Reviewed stamps | No version |
| Radiology request/result | Created/Modified + Authorized | No version |
| ReportLayoutConfiguration | Created/Modified | **No version history** (upsert per ReportType) |
| NotificationTemplate | Version field | Template only, not clinical report |

**CONFIRMED FROM CODE:** No clinical report template version entity.

---

## 26. Report History / Reproducibility

**CONFIRMED FROM CODE / INFERRED behavior:**

- Printed output is **not** stored as an immutable PDF/HTML snapshot.  
- Re-print **regenerates** from current DB clinical data + **current** layout configuration + **current** user signature files.  
- Changing layout/signature/user demographics after the original print can change subsequent reprints.

**Impact for future designer:** Historical visual immutability is **not** currently guaranteed.

---

## 27. Current Diagnostic Report Examples (structure)

**CONFIRMED FROM CODE (template structure):**

1. **Simple single-parameter test:** One department group → one section → one parameter row.  
2. **Multi-parameter test:** One section → many parameter rows (+ optional SectionName subgroups).  
3. **Profile/panel:** May appear under ProfileGroups (legacy) or as multiple sections under department.  
4. **Multi-test order:** Multiple sections; Print All vs Specific filters.  
5. **Comments/notes:** Section Comment + DoctorApprovalComment.  
6. **Reference ranges:** Parameter ReferenceRange + Flag.  
7. **Doctor signature:** Footer/signature zone when enabled + image available.  
8. **Technician signature:** Separate zone when enabled + ReviewedBy identity.

Artifact sample (if present): `TestResults/diagnostic-report-sample.json` (**INFERRED** as fixture/artifact).

---

## 28. Current Radiology Report Examples

**CONFIRMED FROM CODE:** Single narrative layout for all modalities currently supported by the print DTO:

- Patient/accession header  
- TestName / Modality / Department  
- Clinical History  
- Findings  
- Impression  
- Recommendation  
- Authorizer signature  

**UNKNOWN from code alone:** Whether distinct X-Ray vs USG vs CT vs MRI **visual formats** exist beyond Modality field text — **no modality-specific templates found**.

Images: **not** on print DTO (**CONFIRMED FROM CODE**).

---

## 29. Configuration / Defaults

**CONFIRMED FROM CONFIGURATION** (`web/Lis.Api/Web.config` appSettings — typical keys):

- `Report:LabName`, `Report:Tagline`, `Report:CentreName`, `Report:LogoUrl`, `Report:LicenseName`, `Report:Address`, `Report:Email`, `Report:ContactNumbers`, `Report:PharmacyContact`, `Report:AppointmentContact`  
- `OperationalDateTime:FacilityTimeZoneId`, `OperationalDateTime:AllowedClockDriftMinutes`  
- Notification keys (secure link base URL, mock providers) — lab notification download path  

Layout defaults: code in `ReportLayoutConfigurationManager.GetDefaults` (Diagnostic vs Radiology).

---

## 30. Database Relationship Map

### Diagnostic (CONFIRMED FROM CODE)

```
PatientDetails
  ├─ PatientVisit
  └─ SaleInvoice
        └─ SaleInvoiceDetail ── HisTestMaster / TestProfile
              └─ TestRequestDetails (HISRequestNo ≈ InvoiceNo)
                    ├─ SampleNo / Specimen / ReportStatus
                    └─ TestResults
                          └─ TestResultDetails (parameters)
HISParameterMaster ── HISParameterRangMaster
AspNetUsers.DoctorSignaturePath  (approver/reviewer users)
ReportLayoutConfiguration (Diagnostic)
```

### Radiology (CONFIRMED FROM CODE)

```
PatientDetails
  └─ SaleInvoice
        └─ RadiologyRequestDetail (AccessionNo, HISRequestNo)
              └─ RadiologyResultDetail (1:1 narrative + AuthorizedBy)
ReportLayoutConfiguration (Radiology)
AspNetUsers.DoctorSignaturePath (authorizer)
```

---

## 31. API Map (report-related)

### `api/Reports` (`OperationalReportsController`)

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET | `TestReportLabNumbers` | Reports CanView | Printable lab dropdown |
| GET | `TestReportPrintOptions` | Reports CanView | Per-test printable + CanPrintAll |
| GET | `TestReport` | Reports CanView | Diagnostic report DTO (optional `testRequestDetailId`) |
| GET | `RadiologyPrintAccessions` | RadiologyReports CanView | Accession dropdown |
| GET | `RadiologyReport` | RadiologyReports CanView | Radiology print DTO |
| GET | `SaleInvoiceRegister` / `TestBookingRegister` / FDD sample & radiology stats | Reports or RadiologyReports | Operational registers (not clinical letterhead print) |

### `api/ReportLayoutConfiguration`

| Method | Route | Auth |
|--------|-------|------|
| GET | `{reportType}` | ReportLayoutConfiguration |
| GET | `defaults/{reportType}` | same |
| POST | `` | CanEdit |
| POST | `reset/{reportType}` | CanEdit |

### `api/RadiologyReport` (workflow)

Pending/Doctor/Approved queues, Get, Create, Save, Authorize — modules RadiologyReportEntry / RadiologyDoctorApprovals.

### `api/report/download/{token}`

AllowAnonymous secure lab report JSON download.

### Users signature

`POST/GET api/Users/{id}/DoctorSignature`, `GET CurrentDoctorSignature`.

---

## 32. Frontend Component Map

```
reports/
  test-report/                 → /reports/test-report
  radiology-report-print/      → /reports/radiology-report
  sale-invoice-register/
  test-booking-register/
  fdd-report/                  → operational FDD routes
setup/
  report-layout-configuration/ → /report-layout-configuration
LIS/radiology/
  radiology-report-entry/
  radiology-doctor-approval/
  radiology-approved-reports/
  radiology-report-header/
LIS/samples/edit-test-results/ → result entry (feeds diagnostic print)
administration/users/
  users-create / users-edit    → signature upload
_services/
  report.service.ts
  report-layout-configuration.service.ts
  user.service.ts
  sample-workflow.service.ts
```

---

## 33. Test Coverage

| Area | Tests (examples) |
|------|------------------|
| Print All / Specific / unpaid / sibling pending | `LIS.Masters.Tests/Reports/DiagnosticReportPrintSelectionTests.cs` |
| Layout manager | `ReportLayoutConfigurationManagerTests.cs` |
| Reference ranges | `TestResultRangeEvaluatorTests.cs` |
| Layout RBAC | `ReportLayoutConfigurationRbacTests.cs`, Angular `report-layout-configuration-rbac.spec.ts` |
| Signature validation | `UserSignatureValidationTests.cs` |
| Radiology workflow (limited) | `RadiologyReportManagerTests.cs` |
| Invoice → radiology request | `SaleInvoiceRegressionTests.cs` |

**CONFIRMED gaps:** No automated tests found for radiology print payment gate / Angular print components / PDF (N/A).

---

## 34. Technical Constraints / Debt (relevant to future designer)

| Limitation | Evidence |
|------------|----------|
| Clinical layout hard-coded in Angular HTML | test-report / radiology-report-print templates |
| No template abstraction or storage | No template entity beyond layout clearance |
| No server PDF / immutable snapshot | No PDF libs; regenerate on print |
| Dual pipelines (lab vs radiology) with partial shared layout only | Separate managers/DTOs |
| Signature path shared field name `DoctorSignaturePath` for technicians | Identity model + UsersController |
| Branding Web.config applied to Diagnostic enrich only | OperationalReportsController |
| Preview/print tightly coupled to Angular SPA | iframe print in components |
| CDK present but unused for designer | package.json + no DragDrop usage |

---

## 35. Future Design Constraints (must respect; not a design)

Any future Report Designer **must** account for (non-exhaustive):

1. Pre-printed stationery clearances  
2. Payment Paid hard gate (Diagnostic + Radiology)  
3. Approval gates (DoctorApproved / Authorized|Released)  
4. PHI protection / RBAC / no anonymous clinical dumps  
5. User-level signatures (existing storage)  
6. Dynamic parameter sets + gender/age ranges  
7. Multi-page browser print behavior  
8. Print All vs Individual Diagnostic semantics  
9. Separate Diagnostic vs Radiology content models  
10. Auditability expectations if templates become versioned  
11. Existing Report Layout Configuration (clearance/signature) unless explicitly superseded later  

---

## 36. Open Questions (cannot be answered from repository alone)

| Question | Why it matters | Information needed |
|----------|----------------|--------------------|
| Must historical reprints look identical forever? | Snapshot vs live regeneration | Lab/legal policy |
| Do users need arbitrary absolute positioning of every field? | Designer complexity | Product UX workshop |
| Tenant/multi-lab template packs? | Storage & RBAC model | Deployment topology |
| Export/import of templates between sites? | Serialization format | Ops requirements |
| Radiology modality-specific layouts required? | Multiple templates vs one narrative | Clinical stakeholders |
| Must output be true PDF server-side (for WhatsApp/secure link)? | Today secure download is JSON | Product + notification roadmap |
| Maximum template complexity (nested bands, conditional sections)? | Engine scope | BA workshop |
| Who may edit templates (Admin only vs Lab Manager)? | RBAC design | Role matrix workshop |

---

## 37. Reusable Existing Components

| Asset | Reuse potential |
|-------|-----------------|
| `ReportLayoutConfiguration` + manager/API/UI | Stationery + signature geometry |
| `DoctorSignatureStorage` + Users upload | Signature binary pipeline |
| `DiagnosticTestReportDto` / `DiagnosticRadiologyReportDto` | Stable data contracts |
| Payment/approval validation methods | Hard gates to keep calling |
| Print options / individual print ownership checks | Preserve in any new renderer |
| QAuthorize module pattern | Permissions for designer admin |
| Facility OperationalDateTime helper | Consistent timestamps |

---

## 38. Tightly Coupled Areas (careful treatment)

| Area | Coupling |
|------|----------|
| Angular HTML ↔ DTO property names | Rename breaks print |
| `BuildSection` ↔ DoctorApproved TestRuns | Approval semantics |
| Invoice `HISRequestNo` ↔ request linkage | Lab No resolution |
| Enrich* methods in controller | Signatures/branding/layout attachment |
| Dual Diagnostic/Radiology print CSS generators | Duplicated print mechanics |
| Secure download expects Diagnostic DTO | Notification consumers |

---

## 39. Recommended Information Needed Before Architecture Design

*(Information gathering — **not** implementation recommendations)*

1. Written product decisions on snapshot vs regenerating historical reports  
2. Lab stationery specs (exact mm, A4 only?, duplex?)  
3. Whether Report Designer must cover Radiology narrative + Diagnostic grids in one tool  
4. PDF vs browser-print primary delivery channel for patients  
5. Role who designs/publishes templates  
6. Need for conditional visibility (e.g., hide empty Recommendation)  
7. Need for multi-language templates  
8. Acceptance examples (annotated PDFs/screenshots) of target formats  
9. Performance expectations for large multi-test Print All  
10. Integration requirements with existing Individual Print / Print All / notifications  

---

## Final Summary Table

| AREA | STATUS | CONFIDENCE | KEY SOURCE |
|------|--------|------------|------------|
| Diagnostic Report | Confirmed | High | `TestReportManager`, `test-report` component |
| Radiology Report | Confirmed | High | `RadiologyReportManager`, `radiology-report-print` |
| PDF Renderer | **Not found** (browser HTML print) | High | No PDF NuGets; iframe `print()` |
| Report Viewer | **Not found** (reusable) | High | Ad-hoc page HTML only |
| Template Storage | Clearance-only DB; content hard-coded | High | `ReportLayoutConfiguration` + Angular HTML |
| Parameter Rendering | Confirmed | High | `BuildSection` / `DiagnosticTestReportParameter` |
| Signature Source | Confirmed user-level path | High | `DoctorSignatureStorage`, `AspNetUsers.DoctorSignaturePath` |
| Payment Gate | Confirmed | High | Managers’ `PaymentStatus == Paid` |
| Approval Gate | Confirmed | High | DoctorApproved / Authorized\|Released |
| Individual Print | Confirmed | High | `testRequestDetailId` + PrintOptions |
| Print All | Confirmed | High | Full-order `ValidateWorkflow` |
| RBAC | Confirmed | High | QAuthorize modules + MenuCatalog |
| Versioning | **Not found** for clinical templates | High | Layout upsert; no version table |
| Drag-Drop Infrastructure | CDK present / unused for reports | High | package.json + no DragDrop usage |
| Report History Snapshot | **Not found** | High | Live regenerate on print |

---

*End of read-only discovery. No application code, database, configuration, templates, UI, or deployments were modified while producing this document.*
