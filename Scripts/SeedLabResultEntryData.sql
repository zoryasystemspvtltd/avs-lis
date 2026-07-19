-- =============================================================================
-- AVS-LIS / AVILIS - Lab Result Entry sample data
-- Database: ZoryaLMS
-- Prefix: LRE-*  (safe to identify / delete test rows)
-- Idempotent: skips rows that already exist (matched by Code / SampleNo)
--
-- Purpose: realistic samples in editable states (ReportGenerated / Technician
--          Approved) with analyzer results so they appear in the Lab Result
--          Entry screen and can be searched, opened and edited end-to-end.
-- =============================================================================
USE ZoryaLMS;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @User NVARCHAR(80) = N'lre-seed';
DECLARE @Now DATETIME = GETDATE();

BEGIN TRANSACTION;

-- ===========================================================================
-- 1. MASTERS: department, specimen, equipment
-- ===========================================================================
IF NOT EXISTS (SELECT 1 FROM Department WHERE Code = N'LRE-BIO')
    INSERT INTO Department (Code, Name) VALUES (N'LRE-BIO', N'Biochemistry (LRE)');

IF NOT EXISTS (SELECT 1 FROM HISSpecimenMaster WHERE Code = N'LRE-SER')
    INSERT INTO HISSpecimenMaster (Code, Name, IsActive, CreatedBy, CreatedOn)
    VALUES (N'LRE-SER', N'Serum (LRE)', 1, @User, @Now);

IF NOT EXISTS (SELECT 1 FROM HISSpecimenMaster WHERE Code = N'LRE-EDTA')
    INSERT INTO HISSpecimenMaster (Code, Name, IsActive, CreatedBy, CreatedOn)
    VALUES (N'LRE-EDTA', N'EDTA Whole Blood (LRE)', 1, @User, @Now);

-- Equipment master is NOT seeded (managed manually via the Equipments UI).
-- Reuse an existing equipment for downstream mappings.
DECLARE @EquipId INT;
SET @EquipId = (SELECT TOP 1 Id FROM EquipmentMaster WHERE AccessKey = N'LRE-EQ-KEY-001');
IF @EquipId IS NULL
    SET @EquipId = (SELECT TOP 1 Id FROM EquipmentMaster WHERE IsActive = 1 ORDER BY Id);

-- ===========================================================================
-- 2. TEST A: Lipid Profile (LRE-LIPID) + 4 parameters + ranges
-- ===========================================================================
DECLARE @LipidId INT;
IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'LRE-LIPID')
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'LRE-LIPID', N'Lipid Profile', N'LRE-SER', N'Serum (LRE)', N'LRE-BIO', 1, @User, @Now);
SET @LipidId = (SELECT TOP 1 Id FROM HISTestMaster WHERE HISTestCode = N'LRE-LIPID');

IF @LipidId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM HISParameterMaster WHERE HISTestCode = N'LRE-LIPID' AND HISParamCode = N'LRE-CHOL')
    INSERT INTO HISParameterMaster (HISTestCode, HISParamCode, HISParamDescription, HISParamUnit, HISParamMethod, LISParamCode, CreatedOn, HisTestId)
    VALUES (N'LRE-LIPID', N'LRE-CHOL', N'Total Cholesterol', N'mg/dL', N'Enzymatic', N'LRE-CHOL', @Now, @LipidId);

IF @LipidId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM HISParameterMaster WHERE HISTestCode = N'LRE-LIPID' AND HISParamCode = N'LRE-TRIG')
    INSERT INTO HISParameterMaster (HISTestCode, HISParamCode, HISParamDescription, HISParamUnit, HISParamMethod, LISParamCode, CreatedOn, HisTestId)
    VALUES (N'LRE-LIPID', N'LRE-TRIG', N'Triglycerides', N'mg/dL', N'Enzymatic', N'LRE-TRIG', @Now, @LipidId);

IF @LipidId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM HISParameterMaster WHERE HISTestCode = N'LRE-LIPID' AND HISParamCode = N'LRE-HDL')
    INSERT INTO HISParameterMaster (HISTestCode, HISParamCode, HISParamDescription, HISParamUnit, HISParamMethod, LISParamCode, CreatedOn, HisTestId)
    VALUES (N'LRE-LIPID', N'LRE-HDL', N'HDL Cholesterol', N'mg/dL', N'Direct', N'LRE-HDL', @Now, @LipidId);

IF @LipidId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM HISParameterMaster WHERE HISTestCode = N'LRE-LIPID' AND HISParamCode = N'LRE-LDL')
    INSERT INTO HISParameterMaster (HISTestCode, HISParamCode, HISParamDescription, HISParamUnit, HISParamMethod, LISParamCode, CreatedOn, HisTestId)
    VALUES (N'LRE-LIPID', N'LRE-LDL', N'LDL Cholesterol', N'mg/dL', N'Calculated', N'LRE-LDL', @Now, @LipidId);

-- Ranges (Male + Female) so reference range + flags resolve for both patients
DECLARE @pChol INT = (SELECT TOP 1 Id FROM HISParameterMaster WHERE HISTestCode = N'LRE-LIPID' AND HISParamCode = N'LRE-CHOL');
DECLARE @pTrig INT = (SELECT TOP 1 Id FROM HISParameterMaster WHERE HISTestCode = N'LRE-LIPID' AND HISParamCode = N'LRE-TRIG');
DECLARE @pHdl  INT = (SELECT TOP 1 Id FROM HISParameterMaster WHERE HISTestCode = N'LRE-LIPID' AND HISParamCode = N'LRE-HDL');
DECLARE @pLdl  INT = (SELECT TOP 1 Id FROM HISParameterMaster WHERE HISTestCode = N'LRE-LIPID' AND HISParamCode = N'LRE-LDL');

DECLARE @g TABLE (Gender NVARCHAR(10));
INSERT INTO @g VALUES (N'Male'), (N'Female');

INSERT INTO HISParameterRangMaster (HISRangeCode, HISRangeValue, Gender, AgeFrom, AgeTo, AgeType, MinValue, MaxValue, CreatedOn, HisParameterId)
SELECT N'LRE-CHOL', N'125 - 200', g.Gender, 18, 120, N'Years', 125, 200, @Now, @pChol FROM @g g
WHERE @pChol IS NOT NULL AND NOT EXISTS (SELECT 1 FROM HISParameterRangMaster WHERE HisParameterId = @pChol AND Gender = g.Gender);

INSERT INTO HISParameterRangMaster (HISRangeCode, HISRangeValue, Gender, AgeFrom, AgeTo, AgeType, MinValue, MaxValue, CreatedOn, HisParameterId)
SELECT N'LRE-TRIG', N'0 - 150', g.Gender, 18, 120, N'Years', 0, 150, @Now, @pTrig FROM @g g
WHERE @pTrig IS NOT NULL AND NOT EXISTS (SELECT 1 FROM HISParameterRangMaster WHERE HisParameterId = @pTrig AND Gender = g.Gender);

INSERT INTO HISParameterRangMaster (HISRangeCode, HISRangeValue, Gender, AgeFrom, AgeTo, AgeType, MinValue, MaxValue, CreatedOn, HisParameterId)
SELECT N'LRE-HDL', N'40 - 60', g.Gender, 18, 120, N'Years', 40, 60, @Now, @pHdl FROM @g g
WHERE @pHdl IS NOT NULL AND NOT EXISTS (SELECT 1 FROM HISParameterRangMaster WHERE HisParameterId = @pHdl AND Gender = g.Gender);

INSERT INTO HISParameterRangMaster (HISRangeCode, HISRangeValue, Gender, AgeFrom, AgeTo, AgeType, MinValue, MaxValue, CreatedOn, HisParameterId)
SELECT N'LRE-LDL', N'0 - 100', g.Gender, 18, 120, N'Years', 0, 100, @Now, @pLdl FROM @g g
WHERE @pLdl IS NOT NULL AND NOT EXISTS (SELECT 1 FROM HISParameterRangMaster WHERE HisParameterId = @pLdl AND Gender = g.Gender);

-- ===========================================================================
-- 3. TEST B: Complete Blood Count (LRE-CBC) + 3 parameters + ranges
-- ===========================================================================
DECLARE @CbcId INT;
IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'LRE-CBC')
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'LRE-CBC', N'Complete Blood Count', N'LRE-EDTA', N'EDTA Whole Blood (LRE)', N'LRE-BIO', 1, @User, @Now);
SET @CbcId = (SELECT TOP 1 Id FROM HISTestMaster WHERE HISTestCode = N'LRE-CBC');

IF @CbcId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM HISParameterMaster WHERE HISTestCode = N'LRE-CBC' AND HISParamCode = N'LRE-HB')
    INSERT INTO HISParameterMaster (HISTestCode, HISParamCode, HISParamDescription, HISParamUnit, HISParamMethod, LISParamCode, CreatedOn, HisTestId)
    VALUES (N'LRE-CBC', N'LRE-HB', N'Hemoglobin', N'g/dL', N'Photometry', N'LRE-HB', @Now, @CbcId);

IF @CbcId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM HISParameterMaster WHERE HISTestCode = N'LRE-CBC' AND HISParamCode = N'LRE-WBC')
    INSERT INTO HISParameterMaster (HISTestCode, HISParamCode, HISParamDescription, HISParamUnit, HISParamMethod, LISParamCode, CreatedOn, HisTestId)
    VALUES (N'LRE-CBC', N'LRE-WBC', N'WBC Count', N'x10^3/uL', N'Impedance', N'LRE-WBC', @Now, @CbcId);

IF @CbcId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM HISParameterMaster WHERE HISTestCode = N'LRE-CBC' AND HISParamCode = N'LRE-PLT')
    INSERT INTO HISParameterMaster (HISTestCode, HISParamCode, HISParamDescription, HISParamUnit, HISParamMethod, LISParamCode, CreatedOn, HisTestId)
    VALUES (N'LRE-CBC', N'LRE-PLT', N'Platelet Count', N'x10^3/uL', N'Impedance', N'LRE-PLT', @Now, @CbcId);

DECLARE @pHb  INT = (SELECT TOP 1 Id FROM HISParameterMaster WHERE HISTestCode = N'LRE-CBC' AND HISParamCode = N'LRE-HB');
DECLARE @pWbc INT = (SELECT TOP 1 Id FROM HISParameterMaster WHERE HISTestCode = N'LRE-CBC' AND HISParamCode = N'LRE-WBC');
DECLARE @pPlt INT = (SELECT TOP 1 Id FROM HISParameterMaster WHERE HISTestCode = N'LRE-CBC' AND HISParamCode = N'LRE-PLT');

-- Hemoglobin: gender-specific reference ranges
IF @pHb IS NOT NULL AND NOT EXISTS (SELECT 1 FROM HISParameterRangMaster WHERE HisParameterId = @pHb AND Gender = N'Male')
    INSERT INTO HISParameterRangMaster (HISRangeCode, HISRangeValue, Gender, AgeFrom, AgeTo, AgeType, MinValue, MaxValue, CreatedOn, HisParameterId)
    VALUES (N'LRE-HB', N'13 - 17', N'Male', 18, 120, N'Years', 13, 17, @Now, @pHb);
IF @pHb IS NOT NULL AND NOT EXISTS (SELECT 1 FROM HISParameterRangMaster WHERE HisParameterId = @pHb AND Gender = N'Female')
    INSERT INTO HISParameterRangMaster (HISRangeCode, HISRangeValue, Gender, AgeFrom, AgeTo, AgeType, MinValue, MaxValue, CreatedOn, HisParameterId)
    VALUES (N'LRE-HB', N'12 - 15', N'Female', 18, 120, N'Years', 12, 15, @Now, @pHb);

INSERT INTO HISParameterRangMaster (HISRangeCode, HISRangeValue, Gender, AgeFrom, AgeTo, AgeType, MinValue, MaxValue, CreatedOn, HisParameterId)
SELECT N'LRE-WBC', N'4 - 11', g.Gender, 18, 120, N'Years', 4, 11, @Now, @pWbc FROM @g g
WHERE @pWbc IS NOT NULL AND NOT EXISTS (SELECT 1 FROM HISParameterRangMaster WHERE HisParameterId = @pWbc AND Gender = g.Gender);

INSERT INTO HISParameterRangMaster (HISRangeCode, HISRangeValue, Gender, AgeFrom, AgeTo, AgeType, MinValue, MaxValue, CreatedOn, HisParameterId)
SELECT N'LRE-PLT', N'150 - 410', g.Gender, 18, 120, N'Years', 150, 410, @Now, @pPlt FROM @g g
WHERE @pPlt IS NOT NULL AND NOT EXISTS (SELECT 1 FROM HISParameterRangMaster WHERE HisParameterId = @pPlt AND Gender = g.Gender);

-- ===========================================================================
-- 4. PATIENTS
-- ===========================================================================
IF NOT EXISTS (SELECT 1 FROM PatientDetails WHERE HisPatientId = N'LRE-PAT-001')
    INSERT INTO PatientDetails (HisPatientId, Name, Age, Gender, Phone, IsActive, DateOfBirth, CreatedBy, CreatedOn)
    VALUES (N'LRE-PAT-001', N'Rajesh Kumar', 45.0, N'Male', N'9820011111', 1, '1981-03-12', @User, @Now);

IF NOT EXISTS (SELECT 1 FROM PatientDetails WHERE HisPatientId = N'LRE-PAT-002')
    INSERT INTO PatientDetails (HisPatientId, Name, Age, Gender, Phone, IsActive, DateOfBirth, CreatedBy, CreatedOn)
    VALUES (N'LRE-PAT-002', N'Priya Sharma', 32.0, N'Female', N'9820022222', 1, '1994-07-25', @User, @Now);

DECLARE @Pat1 BIGINT = (SELECT TOP 1 Id FROM PatientDetails WHERE HisPatientId = N'LRE-PAT-001');
DECLARE @Pat2 BIGINT = (SELECT TOP 1 Id FROM PatientDetails WHERE HisPatientId = N'LRE-PAT-002');

-- ===========================================================================
-- 5. TEST REQUESTS (editable statuses: 2=ReportGenerated, 3=TechnicianApproved)
-- ===========================================================================
-- helper to insert one request if missing
-- Sample 1001: Rajesh - Lipid Profile - ReportGenerated (normal values)
IF @Pat1 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TestRequestDetails WHERE SampleNo = N'LRE-SMP-1001')
    INSERT INTO TestRequestDetails (SampleNo, HISTestCode, HISTestName, SampleCollectionDate, SampleReceivedDate, SpecimenCode, SpecimenName, CreatedBy, CreatedOn, ReportStatus, PatientId, DepartmentId, Department, MRNo, HISRequestId, HISRequestNo)
    SELECT N'LRE-SMP-1001', t.HISTestCode, t.HISTestCodeDescription, DATEADD(HOUR, -5, @Now), DATEADD(HOUR, -4, @Now), t.HISSpecimenCode, t.HISSpecimenName, @User, @Now, 2, @Pat1, t.DepartmentCode, d.Name, N'LRE-PAT-001', N'LRE-REQ-1001', N'LRE-INV-1001'
    FROM HISTestMaster t LEFT JOIN Department d ON d.Code = t.DepartmentCode WHERE t.HISTestCode = N'LRE-LIPID';

-- Sample 1002: Rajesh - CBC - TechnicianApproved (low Hb abnormal)
IF @Pat1 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TestRequestDetails WHERE SampleNo = N'LRE-SMP-1002')
    INSERT INTO TestRequestDetails (SampleNo, HISTestCode, HISTestName, SampleCollectionDate, SampleReceivedDate, SpecimenCode, SpecimenName, CreatedBy, CreatedOn, ReportStatus, PatientId, DepartmentId, Department, MRNo, HISRequestId, HISRequestNo)
    SELECT N'LRE-SMP-1002', t.HISTestCode, t.HISTestCodeDescription, DATEADD(HOUR, -3, @Now), DATEADD(HOUR, -2, @Now), t.HISSpecimenCode, t.HISSpecimenName, @User, @Now, 3, @Pat1, t.DepartmentCode, d.Name, N'LRE-PAT-001', N'LRE-REQ-1002', N'LRE-INV-1002'
    FROM HISTestMaster t LEFT JOIN Department d ON d.Code = t.DepartmentCode WHERE t.HISTestCode = N'LRE-CBC';

-- Sample 1003: Priya - Lipid Profile - ReportGenerated (high LDL + Trig abnormal)
IF @Pat2 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TestRequestDetails WHERE SampleNo = N'LRE-SMP-1003')
    INSERT INTO TestRequestDetails (SampleNo, HISTestCode, HISTestName, SampleCollectionDate, SampleReceivedDate, SpecimenCode, SpecimenName, CreatedBy, CreatedOn, ReportStatus, PatientId, DepartmentId, Department, MRNo, HISRequestId, HISRequestNo)
    SELECT N'LRE-SMP-1003', t.HISTestCode, t.HISTestCodeDescription, DATEADD(HOUR, -2, @Now), DATEADD(HOUR, -1, @Now), t.HISSpecimenCode, t.HISSpecimenName, @User, @Now, 2, @Pat2, t.DepartmentCode, d.Name, N'LRE-PAT-002', N'LRE-REQ-1003', N'LRE-INV-1003'
    FROM HISTestMaster t LEFT JOIN Department d ON d.Code = t.DepartmentCode WHERE t.HISTestCode = N'LRE-LIPID';

-- Sample 1004: Priya - CBC - ReportGenerated (all normal)
IF @Pat2 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TestRequestDetails WHERE SampleNo = N'LRE-SMP-1004')
    INSERT INTO TestRequestDetails (SampleNo, HISTestCode, HISTestName, SampleCollectionDate, SampleReceivedDate, SpecimenCode, SpecimenName, CreatedBy, CreatedOn, ReportStatus, PatientId, DepartmentId, Department, MRNo, HISRequestId, HISRequestNo)
    SELECT N'LRE-SMP-1004', t.HISTestCode, t.HISTestCodeDescription, DATEADD(HOUR, -2, @Now), DATEADD(HOUR, -1, @Now), t.HISSpecimenCode, t.HISSpecimenName, @User, @Now, 2, @Pat2, t.DepartmentCode, d.Name, N'LRE-PAT-002', N'LRE-REQ-1004', N'LRE-INV-1004'
    FROM HISTestMaster t LEFT JOIN Department d ON d.Code = t.DepartmentCode WHERE t.HISTestCode = N'LRE-CBC';

DECLARE @Req1 BIGINT = (SELECT TOP 1 Id FROM TestRequestDetails WHERE SampleNo = N'LRE-SMP-1001');
DECLARE @Req2 BIGINT = (SELECT TOP 1 Id FROM TestRequestDetails WHERE SampleNo = N'LRE-SMP-1002');
DECLARE @Req3 BIGINT = (SELECT TOP 1 Id FROM TestRequestDetails WHERE SampleNo = N'LRE-SMP-1003');
DECLARE @Req4 BIGINT = (SELECT TOP 1 Id FROM TestRequestDetails WHERE SampleNo = N'LRE-SMP-1004');

-- ===========================================================================
-- 6. TEST RESULTS + DETAILS (analyzer results so samples open in Lab Result Entry)
-- ===========================================================================
DECLARE @Tr BIGINT;

-- 1001 Lipid (Rajesh) - normal
IF @Req1 IS NOT NULL AND @EquipId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TestResults WHERE TestRequestId = @Req1)
BEGIN
    INSERT INTO TestResults (SampleNo, HISTestCode, LISTestCode, SpecimenCode, SpecimenName, ResultDate, SampleCollectionDate, SampleReceivedDate, CreatedBy, CreatedOn, PatientId, TestRequestId, EquipmentId)
    SELECT trd.SampleNo, trd.HISTestCode, N'LRE-LIPID', trd.SpecimenCode, trd.SpecimenName, @Now, trd.SampleCollectionDate, trd.SampleReceivedDate, @User, @Now, @Pat1, @Req1, @EquipId
    FROM TestRequestDetails trd WHERE trd.Id = @Req1;
    SET @Tr = SCOPE_IDENTITY();
    INSERT INTO TestResultDetails (LISParamCode, LISParamValue, LISParamUnit, CreatedBy, CreatedOn, TestResultId)
    VALUES (N'LRE-CHOL', N'180', N'mg/dL', @User, @Now, @Tr),
           (N'LRE-TRIG', N'120', N'mg/dL', @User, @Now, @Tr),
           (N'LRE-HDL',  N'50',  N'mg/dL', @User, @Now, @Tr),
           (N'LRE-LDL',  N'95',  N'mg/dL', @User, @Now, @Tr);
END

-- 1002 CBC (Rajesh) - low Hb (abnormal), Technician Approved
IF @Req2 IS NOT NULL AND @EquipId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TestResults WHERE TestRequestId = @Req2)
BEGIN
    INSERT INTO TestResults (SampleNo, HISTestCode, LISTestCode, SpecimenCode, SpecimenName, ResultDate, SampleCollectionDate, SampleReceivedDate, ReviewDate, ReviewedBy, CreatedBy, CreatedOn, PatientId, TestRequestId, EquipmentId)
    SELECT trd.SampleNo, trd.HISTestCode, N'LRE-CBC', trd.SpecimenCode, trd.SpecimenName, @Now, trd.SampleCollectionDate, trd.SampleReceivedDate, @Now, @User, @User, @Now, @Pat1, @Req2, @EquipId
    FROM TestRequestDetails trd WHERE trd.Id = @Req2;
    SET @Tr = SCOPE_IDENTITY();
    INSERT INTO TestResultDetails (LISParamCode, LISParamValue, LISParamUnit, CreatedBy, CreatedOn, TestResultId)
    VALUES (N'LRE-HB',  N'11.5', N'g/dL',     @User, @Now, @Tr),
           (N'LRE-WBC', N'7.2',  N'x10^3/uL', @User, @Now, @Tr),
           (N'LRE-PLT', N'250',  N'x10^3/uL', @User, @Now, @Tr);
END

-- 1003 Lipid (Priya) - high Triglycerides + high LDL (abnormal)
IF @Req3 IS NOT NULL AND @EquipId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TestResults WHERE TestRequestId = @Req3)
BEGIN
    INSERT INTO TestResults (SampleNo, HISTestCode, LISTestCode, SpecimenCode, SpecimenName, ResultDate, SampleCollectionDate, SampleReceivedDate, CreatedBy, CreatedOn, PatientId, TestRequestId, EquipmentId)
    SELECT trd.SampleNo, trd.HISTestCode, N'LRE-LIPID', trd.SpecimenCode, trd.SpecimenName, @Now, trd.SampleCollectionDate, trd.SampleReceivedDate, @User, @Now, @Pat2, @Req3, @EquipId
    FROM TestRequestDetails trd WHERE trd.Id = @Req3;
    SET @Tr = SCOPE_IDENTITY();
    INSERT INTO TestResultDetails (LISParamCode, LISParamValue, LISParamUnit, CreatedBy, CreatedOn, TestResultId)
    VALUES (N'LRE-CHOL', N'240', N'mg/dL', @User, @Now, @Tr),
           (N'LRE-TRIG', N'210', N'mg/dL', @User, @Now, @Tr),
           (N'LRE-HDL',  N'42',  N'mg/dL', @User, @Now, @Tr),
           (N'LRE-LDL',  N'155', N'mg/dL', @User, @Now, @Tr);
END

-- 1004 CBC (Priya) - all normal
IF @Req4 IS NOT NULL AND @EquipId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TestResults WHERE TestRequestId = @Req4)
BEGIN
    INSERT INTO TestResults (SampleNo, HISTestCode, LISTestCode, SpecimenCode, SpecimenName, ResultDate, SampleCollectionDate, SampleReceivedDate, CreatedBy, CreatedOn, PatientId, TestRequestId, EquipmentId)
    SELECT trd.SampleNo, trd.HISTestCode, N'LRE-CBC', trd.SpecimenCode, trd.SpecimenName, @Now, trd.SampleCollectionDate, trd.SampleReceivedDate, @User, @Now, @Pat2, @Req4, @EquipId
    FROM TestRequestDetails trd WHERE trd.Id = @Req4;
    SET @Tr = SCOPE_IDENTITY();
    INSERT INTO TestResultDetails (LISParamCode, LISParamValue, LISParamUnit, CreatedBy, CreatedOn, TestResultId)
    VALUES (N'LRE-HB',  N'13.8', N'g/dL',     @User, @Now, @Tr),
           (N'LRE-WBC', N'6.5',  N'x10^3/uL', @User, @Now, @Tr),
           (N'LRE-PLT', N'300',  N'x10^3/uL', @User, @Now, @Tr);
END

COMMIT TRANSACTION;

PRINT '=== Lab Result Entry sample data seed completed ===';
PRINT 'Patients : LRE-PAT-001 (Rajesh Kumar, M/45), LRE-PAT-002 (Priya Sharma, F/32)';
PRINT 'Tests    : LRE-LIPID (Lipid Profile, 4 params), LRE-CBC (CBC, 3 params)';
PRINT 'Samples  :';
PRINT '  LRE-SMP-1001  Rajesh  Lipid  ReportGenerated(2)    normal';
PRINT '  LRE-SMP-1002  Rajesh  CBC    TechnicianApproved(3) low Hb';
PRINT '  LRE-SMP-1003  Priya   Lipid  ReportGenerated(2)    high Trig/LDL';
PRINT '  LRE-SMP-1004  Priya   CBC    ReportGenerated(2)    normal';
GO
