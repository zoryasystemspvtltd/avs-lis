-- =============================================================================
-- Diagnostic Report DEMO data — multi-test invoice + wellness package
-- Use for print/UAT: Reports → Diagnostic Report
-- Safe to re-run (CRUD / DIAG prefixes)
-- =============================================================================
USE AVSLIS;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @User NVARCHAR(80) = N'diag-demo-seed';
DECLARE @Now DATETIME = GETDATE();
DECLARE @EquipId INT = (SELECT TOP 1 Id FROM EquipmentMaster WHERE IsActive = 1 ORDER BY Id);

IF @EquipId IS NULL
BEGIN
    INSERT INTO EquipmentMaster (Name, Model, AccessKey, IsActive, CreatedBy, CreatedOn)
    VALUES (N'Demo Analyzer', N'DXI800', N'DEMO-EQ-KEY', 1, @User, @Now);
    SET @EquipId = SCOPE_IDENTITY();
END

-- Patient
IF NOT EXISTS (SELECT 1 FROM PatientDetails WHERE HisPatientId = N'DIAG-DEMO-001')
    INSERT INTO PatientDetails (HisPatientId, Name, Age, Gender, Phone, IsActive, DateOfBirth, CreatedBy, CreatedOn)
    VALUES (N'DIAG-DEMO-001', N'Diagnostic Demo Patient', 45, N'Male', N'9811998800', 1, '1980-06-15', @User, @Now);

DECLARE @PatId BIGINT = (SELECT Id FROM PatientDetails WHERE HisPatientId = N'DIAG-DEMO-001');

-- Tests (use existing GLU, CBC, LIPID)
DECLARE @T_GLU INT = (SELECT Id FROM HISTestMaster WHERE HISTestCode = N'GLU');
DECLARE @T_CBC INT = (SELECT Id FROM HISTestMaster WHERE HISTestCode = N'CBC');
DECLARE @T_LIP INT = (SELECT Id FROM HISTestMaster WHERE HISTestCode = N'LIPID');

-- ---------------------------------------------------------------------------
-- Multi-test invoice: 3 individual tests, all doctor-approved
-- Lab No = INV-DIAG-MULTI-001 (HISRequestNo on requests)
-- ---------------------------------------------------------------------------
DECLARE @InvMulti NVARCHAR(30) = N'INV-DIAG-MULTI-001';
DECLARE @LabMulti NVARCHAR(30) = N'INV-DIAG-MULTI-001';

-- Helper: create request + result if missing
DECLARE @Tests TABLE (Code NVARCHAR(20), SampleSuffix NVARCHAR(10));
INSERT INTO @Tests VALUES (N'GLU', N'-GLU'), (N'CBC', N'-CBC'), (N'LIPID', N'-LIP');

DECLARE @Code NVARCHAR(20), @Suffix NVARCHAR(10), @TId INT, @ReqId BIGINT, @Sample NVARCHAR(40), @TrId BIGINT, @InvId BIGINT;

DECLARE tc CURSOR LOCAL FAST_FORWARD FOR SELECT Code, SampleSuffix FROM @Tests;
OPEN tc;
FETCH NEXT FROM tc INTO @Code, @Suffix;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @TId = Id FROM HISTestMaster WHERE HISTestCode = @Code;
    SET @Sample = @LabMulti + @Suffix;

    IF NOT EXISTS (SELECT 1 FROM TestRequestDetails WHERE SampleNo = @Sample)
    BEGIN
        INSERT INTO TestRequestDetails (
            SampleNo, HISTestCode, HISTestName, SampleCollectionDate, SampleReceivedDate,
            SpecimenCode, SpecimenName, CreatedBy, CreatedOn, ReportStatus,
            PatientId, DepartmentId, Department, MRNo, HISRequestId, HISRequestNo
        )
        SELECT @Sample, t.HISTestCode, t.HISTestCodeDescription, DATEADD(HOUR, -8, @Now), DATEADD(HOUR, -7, @Now),
            t.HISSpecimenCode, t.HISSpecimenName, @User, @Now, 5,
            @PatId, t.DepartmentCode, d.Name, N'DIAG-DEMO-001', N'REQ' + @Suffix, @LabMulti
        FROM HISTestMaster t
        LEFT JOIN Department d ON d.Code = t.DepartmentCode
        WHERE t.Id = @TId;
    END
    ELSE
        UPDATE TestRequestDetails SET ReportStatus = 5, HISRequestNo = @LabMulti WHERE SampleNo = @Sample;

    SET @ReqId = (SELECT Id FROM TestRequestDetails WHERE SampleNo = @Sample);

    IF NOT EXISTS (SELECT 1 FROM TestResults WHERE TestRequestId = @ReqId)
    BEGIN
        INSERT INTO TestResults (
            SampleNo, HISTestCode, LISTestCode, SpecimenCode, SpecimenName,
            ResultDate, SampleCollectionDate, SampleReceivedDate,
            ReviewDate, ReviewedBy, AuthorizationDate, AuthorizedBy,
            CreatedBy, CreatedOn, PatientId, TestRequestId, EquipmentId
        )
        SELECT trd.SampleNo, trd.HISTestCode, trd.HISTestCode, trd.SpecimenCode, trd.SpecimenName,
            @Now, trd.SampleCollectionDate, trd.SampleReceivedDate,
            @Now, @User, @Now, @User, @User, @Now, @PatId, @ReqId, @EquipId
        FROM TestRequestDetails trd WHERE trd.Id = @ReqId;
        SET @TrId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        SET @TrId = (SELECT TOP 1 Id FROM TestResults WHERE TestRequestId = @ReqId);
        UPDATE TestResults SET ReviewedBy = @User, ReviewDate = @Now, AuthorizedBy = @User, AuthorizationDate = @Now
        WHERE Id = @TrId;
    END

    -- Insert parameters per test
    IF @Code = N'GLU' AND NOT EXISTS (SELECT 1 FROM TestResultDetails WHERE TestResultId = @TrId)
        INSERT INTO TestResultDetails (LISParamCode, LISParamValue, LISParamUnit, CreatedBy, CreatedOn, TestResultId)
        VALUES (N'GLU-VAL', N'105.0', N'mg/dL', @User, @Now, @TrId);

    IF @Code = N'CBC' AND NOT EXISTS (SELECT 1 FROM TestResultDetails WHERE TestResultId = @TrId)
        INSERT INTO TestResultDetails (LISParamCode, LISParamValue, LISParamUnit, CreatedBy, CreatedOn, TestResultId)
        VALUES (N'CBC-HGB', N'13.8', N'g/dL', @User, @Now, @TrId),
               (N'CBC-WBC', N'8.2', N'10^3/uL', @User, @Now, @TrId);

    IF @Code = N'LIPID' AND NOT EXISTS (SELECT 1 FROM TestResultDetails WHERE TestResultId = @TrId)
        INSERT INTO TestResultDetails (LISParamCode, LISParamValue, LISParamUnit, CreatedBy, CreatedOn, TestResultId)
        VALUES (N'LIP-CHOL', N'210.0', N'mg/dL', @User, @Now, @TrId),
               (N'LIP-TG', N'145.0', N'mg/dL', @User, @Now, @TrId);

    FETCH NEXT FROM tc INTO @Code, @Suffix;
END
CLOSE tc;
DEALLOCATE tc;

-- Sale invoice (paid)
IF NOT EXISTS (SELECT 1 FROM SaleInvoice WHERE InvoiceNo = @InvMulti)
BEGIN
    INSERT INTO SaleInvoice (
        InvoiceNo, InvoiceDate, InvoiceStatus, PaymentStatus, PatientId,
        GrossAmount, DiscountAmount, TaxAmount, NetAmount, PaidAmount, DueAmount,
        Notes, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive
    )
    VALUES (@InvMulti, @Now, 2, 2, @PatId, 1220.00, 0, 61.00, 1281.00, 1281.00, 0,
        N'Demo: multi-test diagnostic print', @Now, @User, @Now, @User, 1);
    SET @InvId = SCOPE_IDENTITY();

    INSERT INTO SaleInvoiceDetail (SaleInvoiceId, TestId, Rate, Quantity, Amount, DiscountAmount, TaxAmount, NetAmount, RequestDetailId, SampleNo, CreatedBy, CreatedOn, IsActive)
    SELECT @InvId, t.Id, r.Rate, 1, r.Rate, 0, r.Rate * 0.05, r.Rate * 1.05, tr.Id, tr.SampleNo, @User, @Now, 1
    FROM (VALUES (N'GLU', 120.00), (N'CBC', 350.00), (N'LIPID', 750.00)) AS r(Code, Rate)
    INNER JOIN HISTestMaster t ON t.HISTestCode = r.Code
    INNER JOIN TestRequestDetails tr ON tr.SampleNo = @LabMulti + CASE r.Code WHEN N'LIPID' THEN N'-LIP' ELSE N'-' + r.Code END;
END

-- ---------------------------------------------------------------------------
-- Package invoice: Executive Wellness (WELL profile) — single lab no, multiple tests
-- ---------------------------------------------------------------------------
DECLARE @InvPkg NVARCHAR(30) = N'INV-DIAG-PKG-001';
DECLARE @ProfId INT = (SELECT TOP 1 Id FROM TestProfileMaster WHERE Code = N'WELL');

IF @ProfId IS NOT NULL
BEGIN
    DECLARE prof_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT t.HISTestCode, t.Id
        FROM TestProfileDetail pd
        INNER JOIN HISTestMaster t ON t.Id = pd.TestId
        WHERE pd.TestProfileId = @ProfId;

    OPEN prof_cursor;
    FETCH NEXT FROM prof_cursor INTO @Code, @TId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Sample = @InvPkg + N'-' + @Code;
        IF NOT EXISTS (SELECT 1 FROM TestRequestDetails WHERE SampleNo = @Sample)
        BEGIN
            INSERT INTO TestRequestDetails (
                SampleNo, HISTestCode, HISTestName, SampleCollectionDate, SampleReceivedDate,
                SpecimenCode, SpecimenName, CreatedBy, CreatedOn, ReportStatus,
                PatientId, DepartmentId, Department, MRNo, HISRequestId, HISRequestNo
            )
            SELECT @Sample, t.HISTestCode, t.HISTestCodeDescription, DATEADD(HOUR, -6, @Now), DATEADD(HOUR, -5, @Now),
                t.HISSpecimenCode, t.HISSpecimenName, @User, @Now, 5,
                @PatId, t.DepartmentCode, d.Name, N'DIAG-DEMO-001', N'PKG' + LEFT(@Code, 4), @InvPkg
            FROM HISTestMaster t
            LEFT JOIN Department d ON d.Code = t.DepartmentCode
            WHERE t.Id = @TId;
        END
        ELSE
            UPDATE TestRequestDetails SET ReportStatus = 5, HISRequestNo = @InvPkg WHERE SampleNo = @Sample;

        SET @ReqId = (SELECT Id FROM TestRequestDetails WHERE SampleNo = @Sample);

        IF NOT EXISTS (SELECT 1 FROM TestResults WHERE TestRequestId = @ReqId)
        BEGIN
            INSERT INTO TestResults (
                SampleNo, HISTestCode, LISTestCode, SpecimenCode, SpecimenName,
                ResultDate, SampleCollectionDate, SampleReceivedDate,
                ReviewDate, ReviewedBy, AuthorizationDate, AuthorizedBy,
                CreatedBy, CreatedOn, PatientId, TestRequestId, EquipmentId
            )
            SELECT trd.SampleNo, trd.HISTestCode, trd.HISTestCode, trd.SpecimenCode, trd.SpecimenName,
                @Now, trd.SampleCollectionDate, trd.SampleReceivedDate,
                @Now, @User, @Now, @User, @User, @Now, @PatId, @ReqId, @EquipId
            FROM TestRequestDetails trd WHERE trd.Id = @ReqId;
            SET @TrId = SCOPE_IDENTITY();

            INSERT INTO TestResultDetails (LISParamCode, LISParamValue, LISParamUnit, CreatedBy, CreatedOn, TestResultId)
            SELECT TOP 1 p.LISParamCode, N'10.0', ISNULL(p.HISParamUnit, N''), @User, @Now, @TrId
            FROM HISParameterMaster p WHERE p.HisTestId = @TId;
        END

        FETCH NEXT FROM prof_cursor INTO @Code, @TId;
    END
    CLOSE prof_cursor;
    DEALLOCATE prof_cursor;

    IF NOT EXISTS (SELECT 1 FROM SaleInvoice WHERE InvoiceNo = @InvPkg)
    BEGIN
        INSERT INTO SaleInvoice (
            InvoiceNo, InvoiceDate, InvoiceStatus, PaymentStatus, PatientId,
            GrossAmount, DiscountAmount, TaxAmount, NetAmount, PaidAmount, DueAmount,
            Notes, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive
        )
        VALUES (@InvPkg, @Now, 2, 2, @PatId, 2499.00, 0, 124.95, 2623.95, 2623.95, 0,
            N'Demo: wellness package diagnostic print', @Now, @User, @Now, @User, 1);
        SET @InvId = SCOPE_IDENTITY();

        INSERT INTO SaleInvoiceDetail (SaleInvoiceId, TestId, Rate, Quantity, Amount, DiscountAmount, TaxAmount, NetAmount, RequestDetailId, SampleNo, CreatedBy, CreatedOn, IsActive)
        SELECT @InvId, pd.TestId, 0, 1, 0, 0, 0, 0, tr.Id, tr.SampleNo, @User, @Now, 1
        FROM TestProfileDetail pd
        INNER JOIN HISTestMaster t ON t.Id = pd.TestId
        INNER JOIN TestRequestDetails tr ON tr.HISTestCode = t.HISTestCode AND tr.HISRequestNo = @InvPkg
        WHERE pd.TestProfileId = @ProfId;
    END
END

-- Ensure user's sample (INV-20260519-0002) is printable if exists
UPDATE trd SET ReportStatus = 5
FROM TestRequestDetails trd
WHERE trd.HISRequestNo = N'INV-20260519-0002'
  AND EXISTS (SELECT 1 FROM TestResults tr WHERE tr.TestRequestId = trd.Id);

UPDATE tr SET AuthorizedBy = @User, AuthorizationDate = @Now, ReviewedBy = @User, ReviewDate = @Now
FROM TestResults tr
INNER JOIN TestRequestDetails trd ON trd.Id = tr.TestRequestId
WHERE trd.HISRequestNo = N'INV-20260519-0002';

UPDATE SaleInvoice SET PaymentStatus = 2, InvoiceStatus = 2
WHERE InvoiceNo = N'INV-20260519-0002';

PRINT 'Diagnostic report demo seed completed.';
PRINT 'Multi-test Lab No: INV-DIAG-MULTI-001 (GLU + CBC + LIPID)';
PRINT 'Package Lab No:    INV-DIAG-PKG-001 (WELL profile tests)';
PRINT 'Patient:           DIAG-DEMO-001';
GO
