-- =============================================================================
-- AVS-LIS / AVILIS — Full CRUD test data (all operational tables)
-- Database: AVSLIS
-- Prefix: CRUD-*  (safe to identify / delete test rows)
-- Idempotent: skips rows that already exist (matched by Code / SampleNo / InvoiceNo)
--
-- Recommended order:
--   1. SeedSampleData.sql      (baseline demo masters + invoices)
--   2. SeedMasterModules.sql   (module permissions — once per DB)
--   3. SeedAllCrudTestData.sql (this file)
-- =============================================================================
USE AVSLIS;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @User NVARCHAR(80) = N'crud-seed';
DECLARE @Now DATETIME = GETDATE();
DECLARE @FarFuture DATETIME = DATEFROMPARTS(2099, 12, 31);

BEGIN TRANSACTION;

-- ===========================================================================
-- 1. SETUP MASTERS (lookup tables)
-- ===========================================================================
IF NOT EXISTS (SELECT 1 FROM Department WHERE Code = N'CRUD-D1')
    INSERT INTO Department (Code, Name) VALUES (N'CRUD-D1', N'CRUD Test Department');

IF NOT EXISTS (SELECT 1 FROM HISSpecimenMaster WHERE Code = N'CRUD-SP')
    INSERT INTO HISSpecimenMaster (Code, Name, IsActive, CreatedBy, CreatedOn)
    VALUES (N'CRUD-SP', N'CRUD Serum', 1, @User, @Now);

IF NOT EXISTS (SELECT 1 FROM UnitMaster WHERE Code = N'CRUD-UT')
    INSERT INTO UnitMaster (Code, Name, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'CRUD-UT', N'CRUD mg/dL', 1, @User, @Now, @User, @Now);

IF NOT EXISTS (SELECT 1 FROM MethodMaster WHERE Code = N'CRUD-MT')
    INSERT INTO MethodMaster (Code, Name, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'CRUD-MT', N'CRUD Photometry', 1, @User, @Now, @User, @Now);

IF NOT EXISTS (SELECT 1 FROM SampleTypeMaster WHERE Code = N'CRUD-ST')
    INSERT INTO SampleTypeMaster (Code, Name, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'CRUD-ST', N'CRUD Routine', 1, @User, @Now, @User, @Now);

IF NOT EXISTS (SELECT 1 FROM ContainerMaster WHERE Code = N'CRUD-CT')
    INSERT INTO ContainerMaster (Code, Name, Color, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'CRUD-CT', N'CRUD Gold Tube', N'Gold', 1, @User, @Now, @User, @Now);

IF NOT EXISTS (SELECT 1 FROM TestGroupMaster WHERE Code = N'CRUD-TG')
    INSERT INTO TestGroupMaster (Code, Name, SpecimenTag, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'CRUD-TG', N'CRUD Biochemistry', N'CRUD-SP', 1, @User, @Now, @User, @Now);

IF NOT EXISTS (SELECT 1 FROM TestCategoryMaster WHERE Code = N'CRUD-TC')
    INSERT INTO TestCategoryMaster (Code, Name, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'CRUD-TC', N'CRUD Category', 1, @User, @Now, @User, @Now);

IF NOT EXISTS (SELECT 1 FROM ReferralDoctorMaster WHERE Code = N'CRUD-DR')
    INSERT INTO ReferralDoctorMaster (Code, Name, Phone, Email, Address, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'CRUD-DR', N'Dr. CRUD Test', N'9999900001', N'crud.dr@test.local', N'CRUD Lab', 1, @User, @Now, @User, @Now);

IF NOT EXISTS (SELECT 1 FROM CorporateMaster WHERE Code = N'CRUD-CO')
    INSERT INTO CorporateMaster (Code, Name, Address, Phone, ContactPerson, DefaultDiscountPercent, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'CRUD-CO', N'CRUD Corporate Client', N'CRUD Address', N'02200000001', N'CRUD Contact', 5.00, 1, @User, @Now, @User, @Now);

-- ===========================================================================
-- 2. EQUIPMENT + HEARTBEAT + TEST MAPPING
-- ===========================================================================
DECLARE @EquipId INT;

IF NOT EXISTS (SELECT 1 FROM EquipmentMaster WHERE AccessKey = N'CRUD-EQ-KEY-001')
BEGIN
    INSERT INTO EquipmentMaster (Name, Model, AccessKey, IsActive, CreatedBy, CreatedOn)
    VALUES (N'CRUD Analyzer 01', N'CRUD-Model-X', N'CRUD-EQ-KEY-001', 1, @User, @Now);
    SET @EquipId = SCOPE_IDENTITY();
END
ELSE
    SET @EquipId = (SELECT TOP 1 Id FROM EquipmentMaster WHERE AccessKey = N'CRUD-EQ-KEY-001');

IF NOT EXISTS (SELECT 1 FROM EquipmentHeartBeat WHERE AccessKey = N'CRUD-EQ-KEY-001')
    INSERT INTO EquipmentHeartBeat (AccessKey, IsAlive, CreatedBy, CreatedOn)
    VALUES (N'CRUD-EQ-KEY-001', 1, @User, @Now);

-- ===========================================================================
-- 3. HIS TEST + PARAMETERS + RANGES
-- ===========================================================================
DECLARE @CrudTestId INT;

IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'CRUD-TST')
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'CRUD-TST', N'CRUD Panel (Glucose + Creatinine)', N'CRUD-SP', N'CRUD Serum', N'CRUD-D1', 1, @User, @Now);

SET @CrudTestId = (SELECT TOP 1 Id FROM HISTestMaster WHERE HISTestCode = N'CRUD-TST');

-- Parameters
IF @CrudTestId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM HISParameterMaster WHERE HISTestCode = N'CRUD-TST' AND HISParamCode = N'CRUD-P1')
    INSERT INTO HISParameterMaster (HISTestCode, HISParamCode, HISParamDescription, HISParamUnit, HISParamMethod, LISParamCode, CreatedOn, HisTestId)
    VALUES (N'CRUD-TST', N'CRUD-P1', N'CRUD Glucose', N'mg/dL', N'Photometry', N'CRUD-P1', @Now, @CrudTestId);

IF @CrudTestId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM HISParameterMaster WHERE HISTestCode = N'CRUD-TST' AND HISParamCode = N'CRUD-P2')
    INSERT INTO HISParameterMaster (HISTestCode, HISParamCode, HISParamDescription, HISParamUnit, HISParamMethod, LISParamCode, CreatedOn, HisTestId)
    VALUES (N'CRUD-TST', N'CRUD-P2', N'CRUD Creatinine', N'mg/dL', N'Photometry', N'CRUD-P2', @Now, @CrudTestId);

DECLARE @Param1Id INT = (SELECT TOP 1 Id FROM HISParameterMaster WHERE HISTestCode = N'CRUD-TST' AND HISParamCode = N'CRUD-P1');
DECLARE @Param2Id INT = (SELECT TOP 1 Id FROM HISParameterMaster WHERE HISTestCode = N'CRUD-TST' AND HISParamCode = N'CRUD-P2');

IF @Param1Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM HISParameterRangMaster WHERE HisParameterId = @Param1Id AND Gender = N'Male')
    INSERT INTO HISParameterRangMaster (HISRangeCode, HISRangeValue, Gender, AgeFrom, AgeTo, AgeType, MinValue, MaxValue, CreatedOn, HisParameterId)
    VALUES (N'CRUD-P1', N'70 - 110', N'Male', 18, 120, N'Years', 70, 110, @Now, @Param1Id);

IF @Param2Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM HISParameterRangMaster WHERE HisParameterId = @Param2Id AND Gender = N'Male')
    INSERT INTO HISParameterRangMaster (HISRangeCode, HISRangeValue, Gender, AgeFrom, AgeTo, AgeType, MinValue, MaxValue, CreatedOn, HisParameterId)
    VALUES (N'CRUD-P2', N'0.6 - 1.2', N'Male', 18, 120, N'Years', 0.6, 1.2, @Now, @Param2Id);

-- Test mapping (analyzer)
IF @EquipId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TestMappingMaster WHERE HISTestCode = N'CRUD-TST' AND EquipmentId = @EquipId)
    INSERT INTO TestMappingMaster (HISTestCode, HISTestCodeDescription, SpecimenCode, SpecimenName, LISTestCode, LISTestCodeDescription, IsActive, CreatedBy, CreatedOn, GroupName, EquipmentId)
    VALUES (N'CRUD-TST', N'CRUD Panel', N'CRUD-SP', N'CRUD Serum', N'CRUD-LIS', N'CRUD LIS Panel', 1, @User, @Now, N'CRUD', @EquipId);

-- ===========================================================================
-- 4. TEST PROFILE + RATES
-- ===========================================================================
DECLARE @ProfileId INT;

IF NOT EXISTS (SELECT 1 FROM TestProfileMaster WHERE Code = N'CRUD-PRF')
BEGIN
    INSERT INTO TestProfileMaster (Code, Name, PackageRate, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'CRUD-PRF', N'CRUD Wellness Panel', 599.00, 1, @User, @Now, @User, @Now);
    SET @ProfileId = SCOPE_IDENTITY();

    IF @CrudTestId IS NOT NULL
        INSERT INTO TestProfileDetail (TestProfileId, TestId, Quantity)
        SELECT @ProfileId, @CrudTestId, 1
        WHERE NOT EXISTS (SELECT 1 FROM TestProfileDetail d WHERE d.TestProfileId = @ProfileId AND d.TestId = @CrudTestId);
END

IF @CrudTestId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TestRateMaster WHERE TestId = @CrudTestId AND RateType = 0 AND IsActive = 1)
    INSERT INTO TestRateMaster (TestId, Rate, EmergencyRate, DiscountPercent, TaxPercent, RateType, EffectiveStart, EffectiveEnd, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (@CrudTestId, 499.00, 699.00, 0, 5.00, 0, @Now, @FarFuture, 1, @User, @Now, @User, @Now);

DECLARE @CorpId INT = (SELECT TOP 1 Id FROM CorporateMaster WHERE Code = N'CRUD-CO');
IF @CrudTestId IS NOT NULL AND @CorpId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM TestRateMaster WHERE TestId = @CrudTestId AND RateType = 1 AND CorporateId = @CorpId)
    INSERT INTO TestRateMaster (TestId, Rate, EmergencyRate, DiscountPercent, TaxPercent, RateType, CorporateId, EffectiveStart, EffectiveEnd, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (@CrudTestId, 449.00, 699.00, 10.00, 5.00, 1, @CorpId, @Now, @FarFuture, 1, @User, @Now, @User, @Now);

-- ===========================================================================
-- 5. PATIENT
-- ===========================================================================
DECLARE @PatientId BIGINT;

IF NOT EXISTS (SELECT 1 FROM PatientDetails WHERE HisPatientId = N'CRUD-PAT-001')
    INSERT INTO PatientDetails (HisPatientId, Name, Age, Gender, Phone, IsActive, DateOfBirth, CreatedBy, CreatedOn)
    VALUES (N'CRUD-PAT-001', N'CRUD Test Patient', 40.0, N'Male', N'9811999900', 1, '1985-01-15', @User, @Now);

SET @PatientId = (SELECT TOP 1 Id FROM PatientDetails WHERE HisPatientId = N'CRUD-PAT-001');

-- ===========================================================================
-- 6. TEST REQUESTS (multiple approval states for workflow / edit testing)
-- ReportStatus: 0=New, 2=ReportGenerated, 3=TechnicianApproved, 5=DoctorApproved
-- ===========================================================================
DECLARE @ReqNew BIGINT, @ReqRpt BIGINT, @ReqTech BIGINT, @ReqDoc BIGINT;

IF @PatientId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TestRequestDetails WHERE SampleNo = N'CRUD-SMP-NEW')
    INSERT INTO TestRequestDetails (SampleNo, HISTestCode, HISTestName, SampleCollectionDate, SampleReceivedDate, SpecimenCode, SpecimenName, CreatedBy, CreatedOn, ReportStatus, PatientId, DepartmentId, Department, MRNo, HISRequestId, HISRequestNo)
    SELECT N'CRUD-SMP-NEW', t.HISTestCode, t.HISTestCodeDescription, DATEADD(HOUR, -6, @Now), DATEADD(HOUR, -5, @Now), t.HISSpecimenCode, t.HISSpecimenName, @User, @Now, 0, @PatientId, t.DepartmentCode, d.Name, N'CRUD-PAT-001', N'CRUD-REQ-NEW', N'CRUD-INV-NEW'
    FROM HISTestMaster t LEFT JOIN Department d ON d.Code = t.DepartmentCode WHERE t.HISTestCode = N'CRUD-TST';

IF @PatientId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TestRequestDetails WHERE SampleNo = N'CRUD-SMP-RPT')
    INSERT INTO TestRequestDetails (SampleNo, HISTestCode, HISTestName, SampleCollectionDate, SampleReceivedDate, SpecimenCode, SpecimenName, CreatedBy, CreatedOn, ReportStatus, PatientId, DepartmentId, Department, MRNo, HISRequestId, HISRequestNo)
    SELECT N'CRUD-SMP-RPT', t.HISTestCode, t.HISTestCodeDescription, DATEADD(HOUR, -4, @Now), DATEADD(HOUR, -3, @Now), t.HISSpecimenCode, t.HISSpecimenName, @User, @Now, 2, @PatientId, t.DepartmentCode, d.Name, N'CRUD-PAT-001', N'CRUD-REQ-RPT', N'CRUD-INV-RPT'
    FROM HISTestMaster t LEFT JOIN Department d ON d.Code = t.DepartmentCode WHERE t.HISTestCode = N'CRUD-TST';

IF @PatientId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TestRequestDetails WHERE SampleNo = N'CRUD-SMP-TECH')
    INSERT INTO TestRequestDetails (SampleNo, HISTestCode, HISTestName, SampleCollectionDate, SampleReceivedDate, SpecimenCode, SpecimenName, CreatedBy, CreatedOn, ReportStatus, PatientId, DepartmentId, Department, MRNo, HISRequestId, HISRequestNo)
    SELECT N'CRUD-SMP-TECH', t.HISTestCode, t.HISTestCodeDescription, DATEADD(HOUR, -2, @Now), DATEADD(HOUR, -1, @Now), t.HISSpecimenCode, t.HISSpecimenName, @User, @Now, 3, @PatientId, t.DepartmentCode, d.Name, N'CRUD-PAT-001', N'CRUD-REQ-TECH', N'CRUD-INV-TECH'
    FROM HISTestMaster t LEFT JOIN Department d ON d.Code = t.DepartmentCode WHERE t.HISTestCode = N'CRUD-TST';

IF @PatientId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TestRequestDetails WHERE SampleNo = N'CRUD-SMP-DOC')
    INSERT INTO TestRequestDetails (SampleNo, HISTestCode, HISTestName, SampleCollectionDate, SampleReceivedDate, SpecimenCode, SpecimenName, CreatedBy, CreatedOn, ReportStatus, PatientId, DepartmentId, Department, MRNo, HISRequestId, HISRequestNo)
    SELECT N'CRUD-SMP-DOC', t.HISTestCode, t.HISTestCodeDescription, DATEADD(HOUR, -1, @Now), @Now, t.HISSpecimenCode, t.HISSpecimenName, @User, @Now, 5, @PatientId, t.DepartmentCode, d.Name, N'CRUD-PAT-001', N'CRUD-REQ-DOC', N'CRUD-INV-DOC'
    FROM HISTestMaster t LEFT JOIN Department d ON d.Code = t.DepartmentCode WHERE t.HISTestCode = N'CRUD-TST';

SET @ReqNew  = (SELECT TOP 1 Id FROM TestRequestDetails WHERE SampleNo = N'CRUD-SMP-NEW');
SET @ReqRpt  = (SELECT TOP 1 Id FROM TestRequestDetails WHERE SampleNo = N'CRUD-SMP-RPT');
SET @ReqTech = (SELECT TOP 1 Id FROM TestRequestDetails WHERE SampleNo = N'CRUD-SMP-TECH');
SET @ReqDoc  = (SELECT TOP 1 Id FROM TestRequestDetails WHERE SampleNo = N'CRUD-SMP-DOC');

-- TestParameters (booked parameter lines)
IF @ReqNew IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TestParameters WHERE TestRequestDetailsId = @ReqNew AND HISParamCode = N'CRUD-P1')
    INSERT INTO TestParameters (HISParamCode, HISParamName, HISTestCode, CreatedBy, CreatedOn, TestRequestDetailsId)
    VALUES (N'CRUD-P1', N'CRUD Glucose', N'CRUD-TST', @User, @Now, @ReqNew);

-- ===========================================================================
-- 7. SALE INVOICE (confirmed + paid)
-- ===========================================================================
DECLARE @RefDrId INT = (SELECT TOP 1 Id FROM ReferralDoctorMaster WHERE Code = N'CRUD-DR');
DECLARE @InvId BIGINT;

IF NOT EXISTS (SELECT 1 FROM SaleInvoice WHERE InvoiceNo = N'CRUD-INV-001')
BEGIN
    INSERT INTO SaleInvoice (
        InvoiceNo, InvoiceDate, InvoiceStatus, PaymentStatus, RequestDetailId, PatientId,
        GrossAmount, DiscountAmount, TaxAmount, NetAmount, PaidAmount, DueAmount,
        RefDoctorName, ReferralDoctorId, CorporateId, Notes,
        CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive
    )
    VALUES (
        N'CRUD-INV-001', @Now, 2, 2, @ReqRpt, @PatientId,
        499.00, 0, 24.95, 523.95, 523.95, 0,
        N'Dr. CRUD Test', @RefDrId, @CorpId, N'CRUD test invoice — paid',
        @Now, @User, @Now, @User, 1
    );
    SET @InvId = SCOPE_IDENTITY();

    IF @InvId IS NOT NULL AND @CrudTestId IS NOT NULL AND @ReqRpt IS NOT NULL
        INSERT INTO SaleInvoiceDetail (SaleInvoiceId, TestId, Rate, Quantity, Amount, DiscountAmount, TaxAmount, NetAmount, RequestDetailId, SampleNo, CreatedBy, CreatedOn, IsActive)
        VALUES (@InvId, @CrudTestId, 499.00, 1, 499.00, 0, 24.95, 523.95, @ReqRpt, N'CRUD-SMP-RPT', @User, @Now, 1);
END

-- ===========================================================================
-- 8. TEST RESULTS + DETAILS (for edit / report / approval flows)
-- ===========================================================================
DECLARE @TrId BIGINT;

-- Report Generated — editable by technician
IF @ReqRpt IS NOT NULL AND @EquipId IS NOT NULL AND @PatientId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM TestResults WHERE TestRequestId = @ReqRpt)
BEGIN
    INSERT INTO TestResults (SampleNo, HISTestCode, LISTestCode, SpecimenCode, SpecimenName, ResultDate, SampleCollectionDate, SampleReceivedDate, CreatedBy, CreatedOn, PatientId, TestRequestId, EquipmentId)
    SELECT trd.SampleNo, trd.HISTestCode, N'CRUD-LIS', trd.SpecimenCode, trd.SpecimenName, @Now, trd.SampleCollectionDate, trd.SampleReceivedDate, @User, @Now, @PatientId, @ReqRpt, @EquipId
    FROM TestRequestDetails trd WHERE trd.Id = @ReqRpt;
    SET @TrId = SCOPE_IDENTITY();

    INSERT INTO TestResultDetails (LISParamCode, LISParamValue, LISParamUnit, CreatedBy, CreatedOn, TestResultId)
    VALUES (N'CRUD-P1', N'95.0', N'mg/dL', @User, @Now, @TrId),
           (N'CRUD-P2', N'0.9', N'mg/dL', @User, @Now, @TrId);
END

-- Technician Approved
IF @ReqTech IS NOT NULL AND @EquipId IS NOT NULL AND @PatientId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM TestResults WHERE TestRequestId = @ReqTech)
BEGIN
    INSERT INTO TestResults (SampleNo, HISTestCode, LISTestCode, SpecimenCode, SpecimenName, ResultDate, SampleCollectionDate, SampleReceivedDate, ReviewDate, ReviewedBy, CreatedBy, CreatedOn, PatientId, TestRequestId, EquipmentId)
    SELECT trd.SampleNo, trd.HISTestCode, N'CRUD-LIS', trd.SpecimenCode, trd.SpecimenName, @Now, trd.SampleCollectionDate, trd.SampleReceivedDate, @Now, @User, @User, @Now, @PatientId, @ReqTech, @EquipId
    FROM TestRequestDetails trd WHERE trd.Id = @ReqTech;
    SET @TrId = SCOPE_IDENTITY();

    INSERT INTO TestResultDetails (LISParamCode, LISParamValue, LISParamUnit, CreatedBy, CreatedOn, TestResultId)
    VALUES (N'CRUD-P1', N'102.0', N'mg/dL', @User, @Now, @TrId),
           (N'CRUD-P2', N'1.0', N'mg/dL', @User, @Now, @TrId);
END

-- Doctor Approved — admin-only edit in Edit Test Results
IF @ReqDoc IS NOT NULL AND @EquipId IS NOT NULL AND @PatientId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM TestResults WHERE TestRequestId = @ReqDoc)
BEGIN
    INSERT INTO TestResults (SampleNo, HISTestCode, LISTestCode, SpecimenCode, SpecimenName, ResultDate, SampleCollectionDate, SampleReceivedDate, AuthorizationDate, AuthorizedBy, ReviewDate, ReviewedBy, CreatedBy, CreatedOn, PatientId, TestRequestId, EquipmentId)
    SELECT trd.SampleNo, trd.HISTestCode, N'CRUD-LIS', trd.SpecimenCode, trd.SpecimenName, @Now, trd.SampleCollectionDate, trd.SampleReceivedDate, @Now, @User, @Now, @User, @User, @Now, @PatientId, @ReqDoc, @EquipId
    FROM TestRequestDetails trd WHERE trd.Id = @ReqDoc;
    SET @TrId = SCOPE_IDENTITY();

    INSERT INTO TestResultDetails (LISParamCode, LISParamValue, LISParamUnit, CreatedBy, CreatedOn, TestResultId)
    VALUES (N'CRUD-P1', N'88.0', N'mg/dL', @User, @Now, @TrId),
           (N'CRUD-P2', N'0.8', N'mg/dL', @User, @Now, @TrId);
END

-- ===========================================================================
-- 9. CONTROL RESULTS (QC)
-- ===========================================================================
DECLARE @CtrlId BIGINT;

IF @EquipId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ControlResults WHERE SampleNo = N'CRUD-QC-001')
BEGIN
    INSERT INTO ControlResults (SampleNo, ResultDate, CreatedBy, CreatedOn, EquipmentId)
    VALUES (N'CRUD-QC-001', @Now, @User, @Now, @EquipId);
    SET @CtrlId = SCOPE_IDENTITY();

    INSERT INTO ControlResultDetails (LISParamCode, LISParamValue, LISParamUnit, CreatedBy, CreatedOn, ControlResultId)
    VALUES (N'CRUD-P1', N'100.0', N'mg/dL', @User, @Now, @CtrlId),
           (N'CRUD-P2', N'1.0', N'mg/dL', @User, @Now, @CtrlId);
END

-- ===========================================================================
-- 10. Supplement GLU parameters (if SeedSampleData ran but params missing)
-- ===========================================================================
DECLARE @GluId INT = (SELECT TOP 1 Id FROM HISTestMaster WHERE HISTestCode = N'GLU');

IF @GluId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM HISParameterMaster WHERE HISTestCode = N'GLU' AND HISParamCode = N'GLU-VAL')
BEGIN
    INSERT INTO HISParameterMaster (HISTestCode, HISParamCode, HISParamDescription, HISParamUnit, HISParamMethod, LISParamCode, CreatedOn, HisTestId)
    VALUES (N'GLU', N'GLU-VAL', N'Fasting Glucose', N'mg/dL', N'Photometry', N'GLU-VAL', @Now, @GluId);

    DECLARE @GluParamId INT = SCOPE_IDENTITY();
    IF NOT EXISTS (SELECT 1 FROM HISParameterRangMaster WHERE HisParameterId = @GluParamId)
        INSERT INTO HISParameterRangMaster (HISRangeCode, HISRangeValue, Gender, AgeFrom, AgeTo, AgeType, MinValue, MaxValue, CreatedOn, HisParameterId)
        VALUES (N'GLU-VAL', N'70 - 110', N'Male', 18, 120, N'Years', 70, 110, @Now, @GluParamId);
END

COMMIT TRANSACTION;

PRINT '=== CRUD test data seed completed ===';
PRINT 'Use VerifyCrudSeedData.sql to validate row counts.';
PRINT '';
PRINT 'Key CRUD identifiers:';
PRINT '  Test: CRUD-TST | Patient: CRUD-PAT-001 | Equipment key: CRUD-EQ-KEY-001';
PRINT '  Samples: CRUD-SMP-NEW (status 0), CRUD-SMP-RPT (2), CRUD-SMP-TECH (3), CRUD-SMP-DOC (5)';
PRINT '  Invoice: CRUD-INV-001 | QC: CRUD-QC-001';
GO
