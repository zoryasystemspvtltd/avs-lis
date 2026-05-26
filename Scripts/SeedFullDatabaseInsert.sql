-- =============================================================================
-- FULL DATABASE INSERT — populates ALL operational tables with demo rows
-- Database: AVSLIS | Safe to re-run (skips existing keys)
-- Run: sqlcmd -S .\SQLEXPRESS -d AVSLIS -i SeedFullDatabaseInsert.sql
-- =============================================================================
USE AVSLIS;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @User NVARCHAR(80) = N'db-seed';
DECLARE @Now DATETIME = GETDATE();
DECLARE @FarFuture DATETIME = DATEFROMPARTS(2099, 12, 31);

BEGIN TRANSACTION;

-- ---------------------------------------------------------------------------
-- A. Ensure baseline masters exist (from SeedSampleData codes)
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM Department WHERE Code = N'CHEM')
    INSERT INTO Department (Code, Name) VALUES (N'CHEM', N'Clinical Chemistry');
IF NOT EXISTS (SELECT 1 FROM Department WHERE Code = N'HEMA')
    INSERT INTO Department (Code, Name) VALUES (N'HEMA', N'Hematology');

IF NOT EXISTS (SELECT 1 FROM HISSpecimenMaster WHERE Code = N'SER')
    INSERT INTO HISSpecimenMaster (Code, Name, IsActive, CreatedBy, CreatedOn) VALUES (N'SER', N'Serum', 1, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM HISSpecimenMaster WHERE Code = N'EDT')
    INSERT INTO HISSpecimenMaster (Code, Name, IsActive, CreatedBy, CreatedOn) VALUES (N'EDT', N'EDTA Whole Blood', 1, @User, @Now);

IF NOT EXISTS (SELECT 1 FROM UnitMaster WHERE Code = N'MGDL')
    INSERT INTO UnitMaster (Code, Name, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn) VALUES (N'MGDL', N'mg/dL', 1, @User, @Now, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM MethodMaster WHERE Code = N'PHO')
    INSERT INTO MethodMaster (Code, Name, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn) VALUES (N'PHO', N'Photometry', 1, @User, @Now, @User, @Now);

-- ---------------------------------------------------------------------------
-- B. HIS Tests (8 standard panel tests)
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'GLU')
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'GLU', N'Fasting Blood Glucose', N'SER', N'Serum', N'CHEM', 1, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'CBC')
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'CBC', N'Complete Blood Count', N'EDT', N'EDTA Whole Blood', N'HEMA', 1, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'LIPID')
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'LIPID', N'Lipid Profile', N'SER', N'Serum', N'CHEM', 1, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'HBA1C')
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'HBA1C', N'HbA1c', N'EDT', N'EDTA Whole Blood', N'CHEM', 1, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'TSH')
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'TSH', N'TSH', N'SER', N'Serum', N'CHEM', 1, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'CREAT')
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'CREAT', N'Serum Creatinine', N'SER', N'Serum', N'CHEM', 1, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'LFT')
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'LFT', N'Liver Function Test', N'SER', N'Serum', N'CHEM', 1, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'URINE')
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'URINE', N'Urine Routine', N'SER', N'Urine', N'CHEM', 1, @User, @Now);

-- ---------------------------------------------------------------------------
-- C. HIS Parameters + reference ranges (insert into tables)
-- ---------------------------------------------------------------------------
;WITH ParamSeed AS (
    SELECT * FROM (VALUES
        (N'GLU',   N'GLU-VAL',  N'Fasting Glucose',     N'mg/dL', 70,  110),
        (N'CBC',   N'CBC-HGB',  N'Haemoglobin',         N'g/dL',  12,  17),
        (N'CBC',   N'CBC-WBC',  N'WBC Count',           N'10^3/uL', 4, 11),
        (N'LIPID', N'LIP-CHOL', N'Total Cholesterol',   N'mg/dL', 0,   200),
        (N'LIPID', N'LIP-TG',   N'Triglycerides',       N'mg/dL', 0,   150),
        (N'HBA1C', N'HBA-VAL',  N'HbA1c',               N'%',     4,   6),
        (N'TSH',   N'TSH-VAL',  N'TSH',                 N'mIU/L', 0.4, 4.5),
        (N'CREAT', N'CRE-VAL',  N'Creatinine',           N'mg/dL', 0.6, 1.2),
        (N'LFT',   N'LFT-ALT',  N'ALT',                 N'U/L',   0,   40),
        (N'LFT',   N'LFT-AST',  N'AST',                 N'U/L',   0,   40),
        (N'URINE', N'URN-PH',   N'Urine pH',            N'',      5,   8)
    ) AS v(TestCode, ParamCode, ParamName, Unit, MinVal, MaxVal)
)
INSERT INTO HISParameterMaster (HISTestCode, HISParamCode, HISParamDescription, HISParamUnit, HISParamMethod, LISParamCode, CreatedOn, HisTestId)
SELECT v.TestCode, v.ParamCode, v.ParamName, v.Unit, N'Photometry', v.ParamCode, @Now, t.Id
FROM ParamSeed v
INNER JOIN HISTestMaster t ON t.HISTestCode = v.TestCode
WHERE NOT EXISTS (
    SELECT 1 FROM HISParameterMaster p
    WHERE p.HisTestId = t.Id AND p.HISParamCode = v.ParamCode
);

;WITH RangeSeed AS (
    SELECT p.Id AS ParamId, p.HISParamCode, p.HISParamUnit,
           CASE WHEN p.HISParamUnit = N'' THEN N'5 - 8' ELSE
                CAST(CAST(r.MinVal AS NVARCHAR(20)) AS NVARCHAR(20)) + N' - ' + CAST(CAST(r.MaxVal AS NVARCHAR(20)) AS NVARCHAR(20))
           END AS RangeText,
           r.MinVal, r.MaxVal
    FROM HISParameterMaster p
    INNER JOIN (VALUES
        (N'GLU-VAL',  70,  110),
        (N'CBC-HGB',  12,  17),
        (N'CBC-WBC',  4,   11),
        (N'LIP-CHOL', 0,   200),
        (N'LIP-TG',   0,   150),
        (N'HBA-VAL',  4,   6),
        (N'TSH-VAL',  0.4, 4.5),
        (N'CRE-VAL',  0.6, 1.2),
        (N'LFT-ALT',  0,   40),
        (N'LFT-AST',  0,   40),
        (N'URN-PH',   5,   8)
    ) AS r(ParamCode, MinVal, MaxVal) ON r.ParamCode = p.HISParamCode
)
INSERT INTO HISParameterRangMaster (HISRangeCode, HISRangeValue, Gender, AgeFrom, AgeTo, AgeType, MinValue, MaxValue, CreatedOn, HisParameterId)
SELECT s.HISParamCode, s.RangeText, N'Male', 18, 120, N'Years', s.MinVal, s.MaxVal, @Now, s.ParamId
FROM RangeSeed s
WHERE NOT EXISTS (SELECT 1 FROM HISParameterRangMaster r WHERE r.HisParameterId = s.ParamId AND r.Gender = N'Male');

-- ---------------------------------------------------------------------------
-- D. Equipment + test mappings
-- ---------------------------------------------------------------------------
DECLARE @EquipId INT = (SELECT TOP 1 Id FROM EquipmentMaster WHERE IsActive = 1 ORDER BY Id);
IF @EquipId IS NULL
BEGIN
    INSERT INTO EquipmentMaster (Name, Model, AccessKey, IsActive, CreatedBy, CreatedOn)
    VALUES (N'Demo Analyzer', N'Demo-01', N'DEMO-EQ-KEY-001', 1, @User, @Now);
    SET @EquipId = SCOPE_IDENTITY();
END

INSERT INTO TestMappingMaster (HISTestCode, HISTestCodeDescription, SpecimenCode, SpecimenName, LISTestCode, LISTestCodeDescription, IsActive, CreatedBy, CreatedOn, GroupName, EquipmentId)
SELECT t.HISTestCode, t.HISTestCodeDescription, t.HISSpecimenCode, t.HISSpecimenName,
       t.HISTestCode, t.HISTestCodeDescription, 1, @User, @Now, N'DEMO', @EquipId
FROM HISTestMaster t
WHERE t.HISTestCode IN (N'GLU', N'CBC', N'LIPID', N'HBA1C', N'TSH', N'CREAT', N'LFT', N'URINE')
  AND NOT EXISTS (SELECT 1 FROM TestMappingMaster m WHERE m.HISTestCode = t.HISTestCode AND m.EquipmentId = @EquipId);

-- ---------------------------------------------------------------------------
-- E. Test rates
-- ---------------------------------------------------------------------------
INSERT INTO TestRateMaster (TestId, Rate, EmergencyRate, DiscountPercent, TaxPercent, RateType, EffectiveStart, EffectiveEnd, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
SELECT t.Id, r.Rate, r.EmergencyRate, 0, 5.00, 0, @Now, @FarFuture, 1, @User, @Now, @User, @Now
FROM HISTestMaster t
INNER JOIN (VALUES
    (N'GLU',   120.00, 180.00),
    (N'CBC',   350.00, 500.00),
    (N'LIPID', 750.00, 950.00),
    (N'HBA1C', 450.00, 650.00),
    (N'TSH',   400.00, 550.00),
    (N'CREAT', 180.00, 260.00),
    (N'LFT',   900.00, 1200.00),
    (N'URINE', 150.00, 220.00)
) AS r(Code, Rate, EmergencyRate) ON r.Code = t.HISTestCode
WHERE NOT EXISTS (SELECT 1 FROM TestRateMaster tr WHERE tr.TestId = t.Id AND tr.RateType = 0 AND tr.IsActive = 1);

-- ---------------------------------------------------------------------------
-- F. Patients
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM PatientDetails WHERE HisPatientId = N'PAT-1001')
    INSERT INTO PatientDetails (HisPatientId, Name, Age, Gender, Phone, IsActive, DateOfBirth, CreatedBy, CreatedOn)
    VALUES (N'PAT-1001', N'Rajesh Kumar', 42, N'Male', N'9811001001', 1, '1983-04-12', @User, @Now);
IF NOT EXISTS (SELECT 1 FROM PatientDetails WHERE HisPatientId = N'PAT-1002')
    INSERT INTO PatientDetails (HisPatientId, Name, Age, Gender, Phone, IsActive, DateOfBirth, CreatedBy, CreatedOn)
    VALUES (N'PAT-1002', N'Sunita Desai', 35, N'Female', N'9811001002', 1, '1990-08-25', @User, @Now);
IF NOT EXISTS (SELECT 1 FROM PatientDetails WHERE HisPatientId = N'PAT-1003')
    INSERT INTO PatientDetails (HisPatientId, Name, Age, Gender, Phone, IsActive, DateOfBirth, CreatedBy, CreatedOn)
    VALUES (N'PAT-1003', N'Mohammed Farooq', 58, N'Male', N'9811001003', 1, '1967-01-30', @User, @Now);

-- ---------------------------------------------------------------------------
-- G. Test requests (samples)
-- ---------------------------------------------------------------------------
INSERT INTO TestRequestDetails (
    SampleNo, HISTestCode, HISTestName, SampleCollectionDate, SampleReceivedDate,
    SpecimenCode, SpecimenName, CreatedBy, CreatedOn, ReportStatus,
    PatientId, DepartmentId, Department, MRNo, HISRequestId, HISRequestNo
)
SELECT r.SampleNo, t.HISTestCode, t.HISTestCodeDescription,
       DATEADD(HOUR, -4, @Now), DATEADD(HOUR, -3, @Now),
       t.HISSpecimenCode, t.HISSpecimenName, @User, @Now, r.ReportStatus,
       p.Id, t.DepartmentCode, d.Name, p.HisPatientId, N'REQ-' + RIGHT(r.SampleNo, 4), N'HR-' + RIGHT(r.SampleNo, 4)
FROM (VALUES
    (N'SMP-2026-0001', N'PAT-1001', N'GLU',   2),
    (N'SMP-2026-0002', N'PAT-1001', N'LIPID', 2),
    (N'SMP-2026-0003', N'PAT-1002', N'CBC',   3),
    (N'SMP-2026-0004', N'PAT-1002', N'TSH',   0),
    (N'SMP-2026-0005', N'PAT-1003', N'HBA1C', 0)
) AS r(SampleNo, HisPatientId, TestCode, ReportStatus)
INNER JOIN PatientDetails p ON p.HisPatientId = r.HisPatientId
INNER JOIN HISTestMaster t ON t.HISTestCode = r.TestCode
LEFT JOIN Department d ON d.Code = t.DepartmentCode
WHERE NOT EXISTS (SELECT 1 FROM TestRequestDetails tr WHERE tr.SampleNo = r.SampleNo);

-- ---------------------------------------------------------------------------
-- H. Test results + result details (INSERT into result tables)
-- ---------------------------------------------------------------------------
DECLARE @ReqId BIGINT, @PatId BIGINT, @TrId BIGINT, @TestCode NVARCHAR(20), @SampleNo NVARCHAR(30);

DECLARE result_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT trd.Id, trd.PatientId, trd.HISTestCode, trd.SampleNo
    FROM TestRequestDetails trd
    WHERE trd.SampleNo IN (N'SMP-2026-0001', N'SMP-2026-0002', N'SMP-2026-0003')
      AND NOT EXISTS (SELECT 1 FROM TestResults r WHERE r.TestRequestId = trd.Id);

OPEN result_cursor;
FETCH NEXT FROM result_cursor INTO @ReqId, @PatId, @TestCode, @SampleNo;
WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO TestResults (
        SampleNo, HISTestCode, LISTestCode, SpecimenCode, SpecimenName,
        ResultDate, SampleCollectionDate, SampleReceivedDate,
        CreatedBy, CreatedOn, PatientId, TestRequestId, EquipmentId
    )
    SELECT trd.SampleNo, trd.HISTestCode, trd.HISTestCode, trd.SpecimenCode, trd.SpecimenName,
           @Now, trd.SampleCollectionDate, trd.SampleReceivedDate,
           @User, @Now, @PatId, @ReqId, @EquipId
    FROM TestRequestDetails trd WHERE trd.Id = @ReqId;

    SET @TrId = SCOPE_IDENTITY();

    INSERT INTO TestResultDetails (LISParamCode, LISParamValue, LISParamUnit, CreatedBy, CreatedOn, TestResultId)
    SELECT p.LISParamCode,
           CASE p.HISParamCode
               WHEN N'GLU-VAL'  THEN N'92.0'
               WHEN N'CBC-HGB'  THEN N'14.2'
               WHEN N'CBC-WBC'  THEN N'7.5'
               WHEN N'LIP-CHOL' THEN N'185.0'
               WHEN N'LIP-TG'   THEN N'120.0'
               ELSE N'10.0'
           END,
           p.HISParamUnit, @User, @Now, @TrId
    FROM HISParameterMaster p
    WHERE p.HISTestCode = @TestCode;

    FETCH NEXT FROM result_cursor INTO @ReqId, @PatId, @TestCode, @SampleNo;
END
CLOSE result_cursor;
DEALLOCATE result_cursor;

-- ---------------------------------------------------------------------------
-- I. Sale invoice
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM SaleInvoice WHERE InvoiceNo = N'INV-DEMO-0001')
BEGIN
    DECLARE @Pat1 BIGINT = (SELECT TOP 1 Id FROM PatientDetails WHERE HisPatientId = N'PAT-1001');
    DECLARE @Req1 BIGINT = (SELECT TOP 1 Id FROM TestRequestDetails WHERE SampleNo = N'SMP-2026-0001');
    DECLARE @T1 INT = (SELECT TOP 1 Id FROM HISTestMaster WHERE HISTestCode = N'GLU');
    DECLARE @Inv1 BIGINT;

    INSERT INTO SaleInvoice (
        InvoiceNo, InvoiceDate, InvoiceStatus, PaymentStatus, RequestDetailId, PatientId,
        GrossAmount, DiscountAmount, TaxAmount, NetAmount, PaidAmount, DueAmount,
        Notes, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive
    )
    VALUES (N'INV-DEMO-0001', DATEADD(DAY, -1, @Now), 2, 2, @Req1, @Pat1,
            120.00, 0, 6.00, 126.00, 126.00, 0,
            N'Demo invoice — glucose', @Now, @User, @Now, @User, 1);
    SET @Inv1 = SCOPE_IDENTITY();

    INSERT INTO SaleInvoiceDetail (SaleInvoiceId, TestId, Rate, Quantity, Amount, DiscountAmount, TaxAmount, NetAmount, RequestDetailId, SampleNo, CreatedBy, CreatedOn, IsActive)
    VALUES (@Inv1, @T1, 120.00, 1, 120.00, 0, 6.00, 126.00, @Req1, N'SMP-2026-0001', @User, @Now, 1);
END

-- ---------------------------------------------------------------------------
-- J. Control QC results
-- ---------------------------------------------------------------------------
IF @EquipId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ControlResults WHERE SampleNo = N'QC-DEMO-001')
BEGIN
    DECLARE @CtrlId BIGINT;
    INSERT INTO ControlResults (SampleNo, ResultDate, CreatedBy, CreatedOn, EquipmentId)
    VALUES (N'QC-DEMO-001', @Now, @User, @Now, @EquipId);
    SET @CtrlId = SCOPE_IDENTITY();

    INSERT INTO ControlResultDetails (LISParamCode, LISParamValue, LISParamUnit, CreatedBy, CreatedOn, ControlResultId)
    VALUES (N'GLU-VAL', N'100.0', N'mg/dL', @User, @Now, @CtrlId);
END

COMMIT TRANSACTION;

PRINT '=== Full database INSERT completed ===';
SELECT 'Department' AS Tbl, COUNT(*) AS Rows FROM Department
UNION ALL SELECT 'HISTestMaster', COUNT(*) FROM HISTestMaster
UNION ALL SELECT 'HISParameterMaster', COUNT(*) FROM HISParameterMaster
UNION ALL SELECT 'HISParameterRangMaster', COUNT(*) FROM HISParameterRangMaster
UNION ALL SELECT 'PatientDetails', COUNT(*) FROM PatientDetails
UNION ALL SELECT 'TestRequestDetails', COUNT(*) FROM TestRequestDetails
UNION ALL SELECT 'TestResults', COUNT(*) FROM TestResults
UNION ALL SELECT 'TestResultDetails', COUNT(*) FROM TestResultDetails
UNION ALL SELECT 'SaleInvoice', COUNT(*) FROM SaleInvoice
UNION ALL SELECT 'ControlResults', COUNT(*) FROM ControlResults;
GO
