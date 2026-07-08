-- =============================================================================
-- QA Certification Seed Dataset (repeatable, idempotent)
-- Database: ZoryaLMS
-- Prefix: QA-CERT-* / qa-cert-*@zorya.co.in
--
-- Provides fixed entities for certification runs:
--   1 Laboratory Patient, 1 Radiology Patient
--   1 Test Profile, 1 Analyzer Test, 1 Manual Test
--   1 Pending / Collected / Received sample (laboratory workflow)
--   1 Doctor User, 1 Technician User
--
-- Re-run safe: inserts missing rows; resets workflow states on known samples.
-- Default password for cert users: zorKol@1 (same hash as admin bootstrap)
-- =============================================================================
USE ZoryaLMS;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @User NVARCHAR(80) = N'qa-cert-seed';
DECLARE @Now DATETIME = GETDATE();
DECLARE @FarFuture DATETIME = DATEFROMPARTS(2099, 12, 31);

DECLARE @DoctorUserId NVARCHAR(128) = N'c0ffee01-0001-4001-8001-000000000001';
DECLARE @TechUserId NVARCHAR(128) = N'c0ffee01-0001-4001-8001-000000000002';
DECLARE @RoleTech NVARCHAR(128) = N'2d051b02-9289-45ef-8a07-b902b0fea88f';
DECLARE @RoleDoc NVARCHAR(128) = N'a20db0ae-7b61-4c17-af97-ba35c6067e97';
DECLARE @PwdHash NVARCHAR(MAX) = N'AC9998VfILNY62YpjhEaYfqb5bYJbiCMRPMESaxqVQR2QeOKmxxKPXuCe35ZJKewog==';

BEGIN TRANSACTION;

-- ---------------------------------------------------------------------------
-- 1. Departments (Laboratory + Diagnostic routing)
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM Department WHERE Code = N'QA-LAB')
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Department') AND name = N'ProcessingCategory')
        INSERT INTO Department (Code, Name, ProcessingCategory) VALUES (N'QA-LAB', N'QA Laboratory', N'Laboratory');
    ELSE
        INSERT INTO Department (Code, Name) VALUES (N'QA-LAB', N'QA Laboratory');
END
ELSE IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Department') AND name = N'ProcessingCategory')
    UPDATE Department SET Name = N'QA Laboratory', ProcessingCategory = N'Laboratory' WHERE Code = N'QA-LAB';

IF NOT EXISTS (SELECT 1 FROM Department WHERE Code = N'QA-RAD')
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Department') AND name = N'ProcessingCategory')
        INSERT INTO Department (Code, Name, ProcessingCategory) VALUES (N'QA-RAD', N'QA Radiology', N'Diagnostic');
    ELSE
        INSERT INTO Department (Code, Name) VALUES (N'QA-RAD', N'QA Radiology');
END
ELSE IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Department') AND name = N'ProcessingCategory')
    UPDATE Department SET Name = N'QA Radiology', ProcessingCategory = N'Diagnostic' WHERE Code = N'QA-RAD';

-- ---------------------------------------------------------------------------
-- 2. Specimens + equipment
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM HISSpecimenMaster WHERE Code = N'QA-EDT')
    INSERT INTO HISSpecimenMaster (Code, Name, IsActive, CreatedBy, CreatedOn)
    VALUES (N'QA-EDT', N'QA EDTA Whole Blood', 1, @User, @Now);

IF NOT EXISTS (SELECT 1 FROM HISSpecimenMaster WHERE Code = N'QA-SER')
    INSERT INTO HISSpecimenMaster (Code, Name, IsActive, CreatedBy, CreatedOn)
    VALUES (N'QA-SER', N'QA Serum', 1, @User, @Now);

DECLARE @EquipId INT;
IF NOT EXISTS (SELECT 1 FROM EquipmentMaster WHERE AccessKey = N'QA-CERT-EQ-KEY')
BEGIN
    INSERT INTO EquipmentMaster (Name, Model, AccessKey, IsActive, CreatedBy, CreatedOn)
    VALUES (N'QA Cert Analyzer', N'QA-CERT-A1', N'QA-CERT-EQ-KEY', 1, @User, @Now);
    SET @EquipId = SCOPE_IDENTITY();
END
ELSE
    SET @EquipId = (SELECT TOP 1 Id FROM EquipmentMaster WHERE AccessKey = N'QA-CERT-EQ-KEY');

-- ---------------------------------------------------------------------------
-- 3. Tests: analyzer (QA-CERT-ANLZ), manual (QA-CERT-MAN), radiology (QA-CERT-MRI)
-- ---------------------------------------------------------------------------
DECLARE @AnlzId INT, @ManId INT, @RadId INT;

IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'QA-CERT-ANLZ')
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'QA-CERT-ANLZ', N'QA Cert CBC (Analyzer)', N'QA-EDT', N'QA EDTA Whole Blood', N'QA-LAB', 1, @User, @Now);
SET @AnlzId = (SELECT TOP 1 Id FROM HISTestMaster WHERE HISTestCode = N'QA-CERT-ANLZ');

IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'QA-CERT-MAN')
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'QA-CERT-MAN', N'QA Cert Glucose (Manual)', N'QA-SER', N'QA Serum', N'QA-LAB', 1, @User, @Now);
SET @ManId = (SELECT TOP 1 Id FROM HISTestMaster WHERE HISTestCode = N'QA-CERT-MAN');

IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'QA-CERT-MRI')
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'QA-CERT-MRI', N'QA Cert MRI Brain', N'QA-SER', N'QA Serum', N'QA-RAD', 1, @User, @Now);
SET @RadId = (SELECT TOP 1 Id FROM HISTestMaster WHERE HISTestCode = N'QA-CERT-MRI');

-- Parameters
IF @AnlzId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM HISParameterMaster WHERE HISTestCode = N'QA-CERT-ANLZ' AND HISParamCode = N'QCERT-HB')
    INSERT INTO HISParameterMaster (HISTestCode, HISParamCode, HISParamDescription, HISParamUnit, HISParamMethod, LISParamCode, CreatedOn, HisTestId)
    VALUES (N'QA-CERT-ANLZ', N'QCERT-HB', N'Hemoglobin', N'g/dL', N'Photometry', N'QCERT-HB', @Now, @AnlzId);

IF @ManId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM HISParameterMaster WHERE HISTestCode = N'QA-CERT-MAN' AND HISParamCode = N'QCERT-GLU')
    INSERT INTO HISParameterMaster (HISTestCode, HISParamCode, HISParamDescription, HISParamUnit, HISParamMethod, LISParamCode, CreatedOn, HisTestId)
    VALUES (N'QA-CERT-MAN', N'QCERT-GLU', N'Glucose', N'mg/dL', N'Enzymatic', N'QCERT-GLU', @Now, @ManId);

DECLARE @pHb INT = (SELECT TOP 1 Id FROM HISParameterMaster WHERE HISTestCode = N'QA-CERT-ANLZ' AND HISParamCode = N'QCERT-HB');
DECLARE @pGlu INT = (SELECT TOP 1 Id FROM HISParameterMaster WHERE HISTestCode = N'QA-CERT-MAN' AND HISParamCode = N'QCERT-GLU');

IF @pHb IS NOT NULL AND NOT EXISTS (SELECT 1 FROM HISParameterRangMaster WHERE HisParameterId = @pHb AND Gender = N'Male')
    INSERT INTO HISParameterRangMaster (HISRangeCode, HISRangeValue, Gender, AgeFrom, AgeTo, AgeType, MinValue, MaxValue, CreatedOn, HisParameterId)
    VALUES (N'QCERT-HB', N'13 - 17', N'Male', 18, 120, N'Years', 13, 17, @Now, @pHb);

IF @pGlu IS NOT NULL AND NOT EXISTS (SELECT 1 FROM HISParameterRangMaster WHERE HisParameterId = @pGlu AND Gender = N'Male')
    INSERT INTO HISParameterRangMaster (HISRangeCode, HISRangeValue, Gender, AgeFrom, AgeTo, AgeType, MinValue, MaxValue, CreatedOn, HisParameterId)
    VALUES (N'QCERT-GLU', N'70 - 110', N'Male', 18, 120, N'Years', 70, 110, @Now, @pGlu);

-- Analyzer mapping (parameter-level; manual test intentionally unmapped)
IF @EquipId IS NOT NULL AND @pHb IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM TestMappingMaster WHERE HISParamCode = N'QCERT-HB' AND EquipmentId = @EquipId)
    INSERT INTO TestMappingMaster (HISParamCode, HISParamDescription, SpecimenCode, SpecimenName, LISTestCode, LISTestCodeDescription, IsActive, CreatedBy, CreatedOn, GroupName, EquipmentId)
    VALUES (N'QCERT-HB', N'Hemoglobin', N'QA-EDT', N'QA EDTA Whole Blood', N'QCERT-ANLZ', N'QA Cert CBC (Analyzer)', 1, @User, @Now, N'QA-CERT', @EquipId);

-- Rates
IF @AnlzId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TestRateMaster WHERE TestId = @AnlzId AND IsActive = 1 AND RateType = 0)
    INSERT INTO TestRateMaster (TestId, Rate, EmergencyRate, DiscountPercent, TaxPercent, RateType, EffectiveStart, EffectiveEnd, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (@AnlzId, 400.00, 550.00, 0, 0, 0, @Now, @FarFuture, 1, @User, @Now, @User, @Now);

IF @ManId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TestRateMaster WHERE TestId = @ManId AND IsActive = 1 AND RateType = 0)
    INSERT INTO TestRateMaster (TestId, Rate, EmergencyRate, DiscountPercent, TaxPercent, RateType, EffectiveStart, EffectiveEnd, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (@ManId, 150.00, 220.00, 0, 0, 0, @Now, @FarFuture, 1, @User, @Now, @User, @Now);

IF @RadId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TestRateMaster WHERE TestId = @RadId AND IsActive = 1 AND RateType = 0)
    INSERT INTO TestRateMaster (TestId, Rate, EmergencyRate, DiscountPercent, TaxPercent, RateType, EffectiveStart, EffectiveEnd, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (@RadId, 4500.00, 5500.00, 0, 0, 0, @Now, @FarFuture, 1, @User, @Now, @User, @Now);

-- ---------------------------------------------------------------------------
-- 4. Test profile (laboratory tests only)
-- ---------------------------------------------------------------------------
DECLARE @ProfileId INT;
IF NOT EXISTS (SELECT 1 FROM TestProfileMaster WHERE Code = N'QA-CERT-PROF')
BEGIN
    INSERT INTO TestProfileMaster (Code, Name, PackageRate, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'QA-CERT-PROF', N'QA Cert Wellness Panel', 499.00, 1, @User, @Now, @User, @Now);
    SET @ProfileId = SCOPE_IDENTITY();
END
ELSE
    SET @ProfileId = (SELECT TOP 1 Id FROM TestProfileMaster WHERE Code = N'QA-CERT-PROF');

IF @ProfileId IS NOT NULL AND @AnlzId IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM TestProfileDetail WHERE TestProfileId = @ProfileId AND TestId = @AnlzId)
    INSERT INTO TestProfileDetail (TestProfileId, TestId, Quantity) VALUES (@ProfileId, @AnlzId, 1);

IF @ProfileId IS NOT NULL AND @ManId IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM TestProfileDetail WHERE TestProfileId = @ProfileId AND TestId = @ManId)
    INSERT INTO TestProfileDetail (TestProfileId, TestId, Quantity) VALUES (@ProfileId, @ManId, 1);

-- ---------------------------------------------------------------------------
-- 5. Patients
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM PatientDetails WHERE HisPatientId = N'QA-CERT-LAB-PAT')
    INSERT INTO PatientDetails (HisPatientId, Name, Age, Gender, Phone, IsActive, DateOfBirth, MRNo, VisitId, CreatedBy, CreatedOn)
    VALUES (N'QA-CERT-LAB-PAT', N'QA Cert Lab Patient', 40.0, N'Male', N'9900001001', 1, '1986-01-15', N'QA-CERT-LAB-PAT', N'V-QA-LAB-001', @User, @Now);

IF NOT EXISTS (SELECT 1 FROM PatientDetails WHERE HisPatientId = N'QA-CERT-RAD-PAT')
    INSERT INTO PatientDetails (HisPatientId, Name, Age, Gender, Phone, IsActive, DateOfBirth, MRNo, VisitId, CreatedBy, CreatedOn)
    VALUES (N'QA-CERT-RAD-PAT', N'QA Cert Radiology Patient', 45.0, N'Female', N'9900001002', 1, '1981-06-20', N'QA-CERT-RAD-PAT', N'V-QA-RAD-001', @User, @Now);

DECLARE @LabPatId BIGINT = (SELECT TOP 1 Id FROM PatientDetails WHERE HisPatientId = N'QA-CERT-LAB-PAT');
DECLARE @RadPatId BIGINT = (SELECT TOP 1 Id FROM PatientDetails WHERE HisPatientId = N'QA-CERT-RAD-PAT');

-- ---------------------------------------------------------------------------
-- 6. Laboratory test requests (three workflow states)
-- ---------------------------------------------------------------------------
IF @LabPatId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TestRequestDetails WHERE SampleNo = N'QA-CERT-SMP-PEND')
    INSERT INTO TestRequestDetails (
        SampleNo, HISTestCode, HISTestName, SampleCollectionDate, SampleReceivedDate,
        SpecimenCode, SpecimenName, CreatedBy, CreatedOn, ReportStatus,
        PatientId, DepartmentId, Department, MRNo, HISRequestId, HISRequestNo
    )
    SELECT N'QA-CERT-SMP-PEND', t.HISTestCode, t.HISTestCodeDescription, @Now, @Now,
           t.HISSpecimenCode, t.HISSpecimenName, @User, @Now, 0,
           @LabPatId, t.DepartmentCode, d.Name, N'QA-CERT-LAB-PAT', N'QA-CERT-INV-PEND', N'QA-CERT-INV-PEND'
    FROM HISTestMaster t LEFT JOIN Department d ON d.Code = t.DepartmentCode WHERE t.HISTestCode = N'QA-CERT-MAN';

IF @LabPatId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TestRequestDetails WHERE SampleNo = N'QA-CERT-SMP-COLL')
    INSERT INTO TestRequestDetails (
        SampleNo, HISTestCode, HISTestName, SampleCollectionDate, SampleReceivedDate,
        SpecimenCode, SpecimenName, CreatedBy, CreatedOn, ReportStatus,
        PatientId, DepartmentId, Department, MRNo, HISRequestId, HISRequestNo,
        CollectedBy, CollectedRemarks
    )
    SELECT N'QA-CERT-SMP-COLL', t.HISTestCode, t.HISTestCodeDescription, DATEADD(HOUR, -2, @Now), @Now,
           t.HISSpecimenCode, t.HISSpecimenName, @User, @Now, 0,
           @LabPatId, t.DepartmentCode, d.Name, N'QA-CERT-LAB-PAT', N'QA-CERT-INV-COLL', N'QA-CERT-INV-COLL',
           N'qa-cert-tech@zorya.co.in', N'QA cert collected sample'
    FROM HISTestMaster t LEFT JOIN Department d ON d.Code = t.DepartmentCode WHERE t.HISTestCode = N'QA-CERT-ANLZ';

IF @LabPatId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TestRequestDetails WHERE SampleNo = N'QA-CERT-SMP-RECV')
    INSERT INTO TestRequestDetails (
        SampleNo, HISTestCode, HISTestName, SampleCollectionDate, SampleReceivedDate,
        SpecimenCode, SpecimenName, CreatedBy, CreatedOn, ReportStatus,
        PatientId, DepartmentId, Department, MRNo, HISRequestId, HISRequestNo,
        CollectedBy, ReceivedBy, CollectedRemarks, ReceivedRemarks
    )
    SELECT N'QA-CERT-SMP-RECV', t.HISTestCode, t.HISTestCodeDescription, DATEADD(HOUR, -3, @Now), DATEADD(HOUR, -1, @Now),
           t.HISSpecimenCode, t.HISSpecimenName, @User, @Now, 0,
           @LabPatId, t.DepartmentCode, d.Name, N'QA-CERT-LAB-PAT', N'QA-CERT-INV-RECV', N'QA-CERT-INV-RECV',
           N'qa-cert-tech@zorya.co.in', N'qa-cert-tech@zorya.co.in', N'QA cert collected', N'QA cert received'
    FROM HISTestMaster t LEFT JOIN Department d ON d.Code = t.DepartmentCode WHERE t.HISTestCode = N'QA-CERT-MAN';

DECLARE @ReqPend BIGINT = (SELECT TOP 1 Id FROM TestRequestDetails WHERE SampleNo = N'QA-CERT-SMP-PEND');
DECLARE @ReqColl BIGINT = (SELECT TOP 1 Id FROM TestRequestDetails WHERE SampleNo = N'QA-CERT-SMP-COLL');
DECLARE @ReqRecv BIGINT = (SELECT TOP 1 Id FROM TestRequestDetails WHERE SampleNo = N'QA-CERT-SMP-RECV');

-- Reset workflow states on every seed run (repeatable certification)
IF @ReqPend IS NOT NULL
    UPDATE TestRequestDetails SET
        ReportStatus = 0,
        CollectedBy = NULL,
        ReceivedBy = NULL,
        CollectedRemarks = NULL,
        ReceivedRemarks = NULL,
        SampleCollectionDate = @Now,
        SampleReceivedDate = @Now
    WHERE Id = @ReqPend;

IF @ReqColl IS NOT NULL
    UPDATE TestRequestDetails SET
        ReportStatus = 0,
        CollectedBy = N'qa-cert-tech@zorya.co.in',
        ReceivedBy = NULL,
        ReceivedRemarks = NULL,
        CollectedRemarks = N'QA cert collected sample',
        SampleCollectionDate = DATEADD(HOUR, -2, @Now),
        SampleReceivedDate = @Now
    WHERE Id = @ReqColl;

IF @ReqRecv IS NOT NULL
    UPDATE TestRequestDetails SET
        ReportStatus = 0,
        CollectedBy = N'qa-cert-tech@zorya.co.in',
        ReceivedBy = N'qa-cert-tech@zorya.co.in',
        CollectedRemarks = N'QA cert collected',
        ReceivedRemarks = N'QA cert received',
        SampleCollectionDate = DATEADD(HOUR, -3, @Now),
        SampleReceivedDate = DATEADD(HOUR, -1, @Now)
    WHERE Id = @ReqRecv;

-- ---------------------------------------------------------------------------
-- 7. Sale invoices (confirmed + paid; links via RequestDetailId)
-- ---------------------------------------------------------------------------
DECLARE @InvPend BIGINT, @InvColl BIGINT, @InvRecv BIGINT, @InvRad BIGINT;

IF @LabPatId IS NOT NULL AND @ReqPend IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SaleInvoice WHERE InvoiceNo = N'QA-CERT-INV-PEND')
BEGIN
    INSERT INTO SaleInvoice (
        InvoiceNo, InvoiceDate, InvoiceStatus, PaymentStatus, RequestDetailId, PatientId,
        GrossAmount, DiscountAmount, TaxAmount, NetAmount, PaidAmount, DueAmount,
        Notes, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive
    )
    VALUES (
        N'QA-CERT-INV-PEND', @Now, 2, 2, @ReqPend, @LabPatId,
        150.00, 0, 0, 150.00, 150.00, 0,
        N'QA cert pending collection sample', @Now, @User, @Now, @User, 1
    );
    SET @InvPend = SCOPE_IDENTITY();
    INSERT INTO SaleInvoiceDetail (SaleInvoiceId, TestId, Rate, Quantity, Amount, DiscountAmount, TaxAmount, NetAmount, RequestDetailId, SampleNo, CreatedBy, CreatedOn, IsActive)
    VALUES (@InvPend, @ManId, 150.00, 1, 150.00, 0, 0, 150.00, @ReqPend, N'QA-CERT-SMP-PEND', @User, @Now, 1);
END

IF @LabPatId IS NOT NULL AND @ReqColl IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SaleInvoice WHERE InvoiceNo = N'QA-CERT-INV-COLL')
BEGIN
    INSERT INTO SaleInvoice (
        InvoiceNo, InvoiceDate, InvoiceStatus, PaymentStatus, RequestDetailId, PatientId,
        GrossAmount, DiscountAmount, TaxAmount, NetAmount, PaidAmount, DueAmount,
        Notes, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive
    )
    VALUES (
        N'QA-CERT-INV-COLL', @Now, 2, 2, @ReqColl, @LabPatId,
        400.00, 0, 0, 400.00, 400.00, 0,
        N'QA cert collected sample (awaiting receiving)', @Now, @User, @Now, @User, 1
    );
    SET @InvColl = SCOPE_IDENTITY();
    INSERT INTO SaleInvoiceDetail (SaleInvoiceId, TestId, Rate, Quantity, Amount, DiscountAmount, TaxAmount, NetAmount, RequestDetailId, SampleNo, CreatedBy, CreatedOn, IsActive)
    VALUES (@InvColl, @AnlzId, 400.00, 1, 400.00, 0, 0, 400.00, @ReqColl, N'QA-CERT-SMP-COLL', @User, @Now, 1);
END

IF @LabPatId IS NOT NULL AND @ReqRecv IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SaleInvoice WHERE InvoiceNo = N'QA-CERT-INV-RECV')
BEGIN
    INSERT INTO SaleInvoice (
        InvoiceNo, InvoiceDate, InvoiceStatus, PaymentStatus, RequestDetailId, PatientId,
        GrossAmount, DiscountAmount, TaxAmount, NetAmount, PaidAmount, DueAmount,
        Notes, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive
    )
    VALUES (
        N'QA-CERT-INV-RECV', @Now, 2, 2, @ReqRecv, @LabPatId,
        150.00, 0, 0, 150.00, 150.00, 0,
        N'QA cert received sample', @Now, @User, @Now, @User, 1
    );
    SET @InvRecv = SCOPE_IDENTITY();
    INSERT INTO SaleInvoiceDetail (SaleInvoiceId, TestId, Rate, Quantity, Amount, DiscountAmount, TaxAmount, NetAmount, RequestDetailId, SampleNo, CreatedBy, CreatedOn, IsActive)
    VALUES (@InvRecv, @ManId, 150.00, 1, 150.00, 0, 0, 150.00, @ReqRecv, N'QA-CERT-SMP-RECV', @User, @Now, 1);
END

-- Radiology patient invoice + request (diagnostic routing)
IF @RadPatId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SaleInvoice WHERE InvoiceNo = N'QA-CERT-INV-RAD')
BEGIN
    INSERT INTO SaleInvoice (
        InvoiceNo, InvoiceDate, InvoiceStatus, PaymentStatus, RequestDetailId, PatientId,
        GrossAmount, DiscountAmount, TaxAmount, NetAmount, PaidAmount, DueAmount,
        Notes, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive
    )
    VALUES (
        N'QA-CERT-INV-RAD', @Now, 2, 2, NULL, @RadPatId,
        4500.00, 0, 0, 4500.00, 4500.00, 0,
        N'QA cert radiology patient', @Now, @User, @Now, @User, 1
    );
    SET @InvRad = SCOPE_IDENTITY();
    INSERT INTO SaleInvoiceDetail (SaleInvoiceId, TestId, Rate, Quantity, Amount, DiscountAmount, TaxAmount, NetAmount, RequestDetailId, SampleNo, CreatedBy, CreatedOn, IsActive)
    VALUES (@InvRad, @RadId, 4500.00, 1, 4500.00, 0, 0, 4500.00, NULL, NULL, @User, @Now, 1);
END

IF @RadPatId IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM RadiologyRequestDetail WHERE PatientId = @RadPatId AND HISRequestNo = N'QA-CERT-INV-RAD' AND HISTestCode = N'QA-CERT-MRI'
)
BEGIN
    INSERT INTO RadiologyRequestDetail (
        PatientId, HISRequestNo, AccessionNo, Modality, HISTestCode, HISTestName, Department,
        ReportStatus, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn
    )
    VALUES (
        @RadPatId, N'QA-CERT-INV-RAD', N'QA-CERT-INV-RAD-QA-CERT-MRI', N'MRI Brain',
        N'QA-CERT-MRI', N'QA Cert MRI Brain', N'QA Radiology', 0,
        @User, @Now, @User, @Now
    );
END

-- ---------------------------------------------------------------------------
-- 8. Certification users (doctor + technician)
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = @DoctorUserId)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.AspNetUsers') AND name = N'DoctorDesignation')
        INSERT INTO AspNetUsers (
            Id, Email, EmailConfirmed, PasswordHash, SecurityStamp, PhoneNumber, PhoneNumberConfirmed,
            TwoFactorEnabled, LockoutEndDateUtc, LockoutEnabled, AccessFailedCount, UserName,
            FirstName, LastName, IsBlocked, DoctorDesignation
        )
        VALUES (
            @DoctorUserId, N'qa-cert-doctor@zorya.co.in', 1, @PwdHash,
            N'qa-cert-doctor-stamp-0001', N'9900002001', 0,
            0, NULL, 1, 0, N'qa-cert-doctor@zorya.co.in',
            N'QA', N'Cert Doctor', 0, N'Consultant Radiologist, QA Certification'
        );
    ELSE
        INSERT INTO AspNetUsers (
            Id, Email, EmailConfirmed, PasswordHash, SecurityStamp, PhoneNumber, PhoneNumberConfirmed,
            TwoFactorEnabled, LockoutEndDateUtc, LockoutEnabled, AccessFailedCount, UserName,
            FirstName, LastName, IsBlocked
        )
        VALUES (
            @DoctorUserId, N'qa-cert-doctor@zorya.co.in', 1, @PwdHash,
            N'qa-cert-doctor-stamp-0001', N'9900002001', 0,
            0, NULL, 1, 0, N'qa-cert-doctor@zorya.co.in',
            N'QA', N'Cert Doctor', 0
        );
END
ELSE IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.AspNetUsers') AND name = N'DoctorDesignation')
    UPDATE AspNetUsers SET DoctorDesignation = N'Consultant Radiologist, QA Certification'
    WHERE Id = @DoctorUserId AND (DoctorDesignation IS NULL OR DoctorDesignation = N'');

IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = @TechUserId)
    INSERT INTO AspNetUsers (
        Id, Email, EmailConfirmed, PasswordHash, SecurityStamp, PhoneNumber, PhoneNumberConfirmed,
        TwoFactorEnabled, LockoutEndDateUtc, LockoutEnabled, AccessFailedCount, UserName,
        FirstName, LastName, IsBlocked
    )
    VALUES (
        @TechUserId, N'qa-cert-tech@zorya.co.in', 1, @PwdHash,
        N'qa-cert-tech-stamp-0001', N'9900002002', 0,
        0, NULL, 1, 0, N'qa-cert-tech@zorya.co.in',
        N'QA', N'Cert Technician', 0
    );

IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @DoctorUserId AND RoleId = @RoleDoc)
    INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@DoctorUserId, @RoleDoc);

IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @TechUserId AND RoleId = @RoleTech)
    INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@TechUserId, @RoleTech);

DECLARE @AppId INT = (SELECT TOP 1 Id FROM ClientApplication WHERE AccessKey = N'DXI800');
IF @AppId IS NULL
    SET @AppId = (SELECT TOP 1 Id FROM ClientApplication WHERE AccessKey = N'DUMMY');

IF @AppId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM UserApplicationMappings WHERE UserId = @DoctorUserId AND ClientApplicationId = @AppId)
        INSERT INTO UserApplicationMappings (UserId, ClientApplicationId) VALUES (@DoctorUserId, @AppId);
    IF NOT EXISTS (SELECT 1 FROM UserApplicationMappings WHERE UserId = @TechUserId AND ClientApplicationId = @AppId)
        INSERT INTO UserApplicationMappings (UserId, ClientApplicationId) VALUES (@TechUserId, @AppId);

    DECLARE @ModuleNames TABLE (Name NVARCHAR(128), RoleId NVARCHAR(128));
    INSERT INTO @ModuleNames (Name, RoleId) VALUES
        (N'Samples', @RoleTech),
        (N'SampleCollection', @RoleTech),
        (N'SampleReceiving', @RoleTech),
        (N'DoctorsApprovals', @RoleDoc),
        (N'RadiologyReportEntry', @RoleDoc),
        (N'RadiologyReports', @RoleDoc),
        (N'RadiologyDoctorApprovals', @RoleDoc),
        (N'Reports', @RoleDoc);

    INSERT INTO RoleModuleMappings (CanAdd, CanEdit, CanAuthorize, CanDelete, CanView, CanReject, ModuleId, RoleId, ApplicationId)
    SELECT 1, 1, 1, 1, 1, 1, um.Id, m.RoleId, @AppId
    FROM @ModuleNames m
    INNER JOIN UserModules um ON um.Name = m.Name AND um.ApplicationId = @AppId
    WHERE NOT EXISTS (
        SELECT 1 FROM RoleModuleMappings rm
        WHERE rm.ModuleId = um.Id AND rm.RoleId = m.RoleId AND rm.ApplicationId = @AppId
    );
END

COMMIT TRANSACTION;

-- ---------------------------------------------------------------------------
-- Manifest (for certification scripts)
-- ---------------------------------------------------------------------------
PRINT '=== QA Certification Seed Dataset ===';
PRINT 'Password (doctor + technician): zorKol@1';
PRINT '';

SELECT EntityType, EntityKey, EntityId, Detail
FROM (
    SELECT N'LaboratoryPatient' AS EntityType, N'QA-CERT-LAB-PAT' AS EntityKey, CAST(Id AS NVARCHAR(30)) AS EntityId, Name AS Detail FROM PatientDetails WHERE HisPatientId = N'QA-CERT-LAB-PAT'
    UNION ALL SELECT N'RadiologyPatient', N'QA-CERT-RAD-PAT', CAST(Id AS NVARCHAR(30)), Name FROM PatientDetails WHERE HisPatientId = N'QA-CERT-RAD-PAT'
    UNION ALL SELECT N'TestProfile', N'QA-CERT-PROF', CAST(Id AS NVARCHAR(30)), Name FROM TestProfileMaster WHERE Code = N'QA-CERT-PROF'
    UNION ALL SELECT N'AnalyzerTest', N'QA-CERT-ANLZ', CAST(Id AS NVARCHAR(30)), HISTestCodeDescription FROM HISTestMaster WHERE HISTestCode = N'QA-CERT-ANLZ'
    UNION ALL SELECT N'ManualTest', N'QA-CERT-MAN', CAST(Id AS NVARCHAR(30)), HISTestCodeDescription FROM HISTestMaster WHERE HISTestCode = N'QA-CERT-MAN'
    UNION ALL SELECT N'RadiologyTest', N'QA-CERT-MRI', CAST(Id AS NVARCHAR(30)), HISTestCodeDescription FROM HISTestMaster WHERE HISTestCode = N'QA-CERT-MRI'
    UNION ALL SELECT N'PendingSample', N'QA-CERT-SMP-PEND', CAST(Id AS NVARCHAR(30)), HISTestCode FROM TestRequestDetails WHERE SampleNo = N'QA-CERT-SMP-PEND'
    UNION ALL SELECT N'CollectedSample', N'QA-CERT-SMP-COLL', CAST(Id AS NVARCHAR(30)), HISTestCode FROM TestRequestDetails WHERE SampleNo = N'QA-CERT-SMP-COLL'
    UNION ALL SELECT N'ReceivedSample', N'QA-CERT-SMP-RECV', CAST(Id AS NVARCHAR(30)), HISTestCode FROM TestRequestDetails WHERE SampleNo = N'QA-CERT-SMP-RECV'
    UNION ALL SELECT N'DoctorUser', N'qa-cert-doctor@zorya.co.in', Id, N'QA Cert Doctor' FROM AspNetUsers WHERE UserName = N'qa-cert-doctor@zorya.co.in'
    UNION ALL SELECT N'TechnicianUser', N'qa-cert-tech@zorya.co.in', Id, N'Technician' FROM AspNetUsers WHERE UserName = N'qa-cert-tech@zorya.co.in'
    UNION ALL SELECT N'AnalyzerEquipment', N'QA-CERT-EQ-KEY', CAST(Id AS NVARCHAR(30)), Name FROM EquipmentMaster WHERE AccessKey = N'QA-CERT-EQ-KEY'
    UNION ALL SELECT N'RadiologyRequest', N'QA-CERT-INV-RAD', CAST(Id AS NVARCHAR(30)), HISTestCode FROM RadiologyRequestDetail WHERE HISRequestNo = N'QA-CERT-INV-RAD'
) m
ORDER BY EntityType;

GO
