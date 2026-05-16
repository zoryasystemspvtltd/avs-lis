-- =============================================================================
-- AVS-LIS sample / demo data — masters, rates, patients, sale invoices
-- Database: AVSLIS
-- Safe to re-run: skips rows that already exist (matched by Code / InvoiceNo / SampleNo)
-- =============================================================================
USE AVSLIS;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @User NVARCHAR(80) = N'seed-script';
DECLARE @Now DATETIME = GETDATE();
DECLARE @FarFuture DATETIME = DATEFROMPARTS(2099, 12, 31);

BEGIN TRANSACTION;

-- ---------------------------------------------------------------------------
-- Setup masters
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM Department WHERE Code = N'CHEM')
    INSERT INTO Department (Code, Name) VALUES (N'CHEM', N'Clinical Chemistry');
IF NOT EXISTS (SELECT 1 FROM Department WHERE Code = N'HEMA')
    INSERT INTO Department (Code, Name) VALUES (N'HEMA', N'Hematology');
IF NOT EXISTS (SELECT 1 FROM Department WHERE Code = N'IMM')
    INSERT INTO Department (Code, Name) VALUES (N'IMM', N'Immunology / Serology');
IF NOT EXISTS (SELECT 1 FROM Department WHERE Code = N'URI')
    INSERT INTO Department (Code, Name) VALUES (N'URI', N'Clinical Pathology');
IF NOT EXISTS (SELECT 1 FROM Department WHERE Code = N'MIC')
    INSERT INTO Department (Code, Name) VALUES (N'MIC', N'Microbiology');

IF NOT EXISTS (SELECT 1 FROM HISSpecimenMaster WHERE Code = N'SER')
    INSERT INTO HISSpecimenMaster (Code, Name, IsActive, CreatedBy, CreatedOn)
    VALUES (N'SER', N'Serum', 1, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM HISSpecimenMaster WHERE Code = N'EDT')
    INSERT INTO HISSpecimenMaster (Code, Name, IsActive, CreatedBy, CreatedOn)
    VALUES (N'EDT', N'EDTA Whole Blood', 1, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM HISSpecimenMaster WHERE Code = N'URI')
    INSERT INTO HISSpecimenMaster (Code, Name, IsActive, CreatedBy, CreatedOn)
    VALUES (N'URI', N'Urine', 1, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM HISSpecimenMaster WHERE Code = N'FLU')
    INSERT INTO HISSpecimenMaster (Code, Name, IsActive, CreatedBy, CreatedOn)
    VALUES (N'FLU', N'Fluoride Plasma', 1, @User, @Now);

IF NOT EXISTS (SELECT 1 FROM UnitMaster WHERE Code = N'MGDL')
    INSERT INTO UnitMaster (Code, Name, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'MGDL', N'mg/dL', 1, @User, @Now, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM UnitMaster WHERE Code = N'GDL')
    INSERT INTO UnitMaster (Code, Name, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'GDL', N'g/dL', 1, @User, @Now, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM UnitMaster WHERE Code = N'MIUL')
    INSERT INTO UnitMaster (Code, Name, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'MIUL', N'mIU/L', 1, @User, @Now, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM UnitMaster WHERE Code = N'PERCENT')
    INSERT INTO UnitMaster (Code, Name, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'PERCENT', N'%', 1, @User, @Now, @User, @Now);

IF NOT EXISTS (SELECT 1 FROM MethodMaster WHERE Code = N'PHO')
    INSERT INTO MethodMaster (Code, Name, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'PHO', N'Photometry', 1, @User, @Now, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM MethodMaster WHERE Code = N'CLIA')
    INSERT INTO MethodMaster (Code, Name, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'CLIA', N'Chemiluminescence', 1, @User, @Now, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM MethodMaster WHERE Code = N'IMP')
    INSERT INTO MethodMaster (Code, Name, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'IMP', N'Impedance', 1, @User, @Now, @User, @Now);

IF NOT EXISTS (SELECT 1 FROM SampleTypeMaster WHERE Code = N'ROUT')
    INSERT INTO SampleTypeMaster (Code, Name, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'ROUT', N'Routine', 1, @User, @Now, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM SampleTypeMaster WHERE Code = N'STAT')
    INSERT INTO SampleTypeMaster (Code, Name, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'STAT', N'STAT / Emergency', 1, @User, @Now, @User, @Now);

IF NOT EXISTS (SELECT 1 FROM ContainerMaster WHERE Code = N'SST')
    INSERT INTO ContainerMaster (Code, Name, Color, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'SST', N'Serum Separator Tube', N'Gold', 1, @User, @Now, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM ContainerMaster WHERE Code = N'LAV')
    INSERT INTO ContainerMaster (Code, Name, Color, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'LAV', N'EDTA Tube', N'Lavender', 1, @User, @Now, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM ContainerMaster WHERE Code = N'URN')
    INSERT INTO ContainerMaster (Code, Name, Color, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'URN', N'Urine Container', N'Yellow', 1, @User, @Now, @User, @Now);

IF NOT EXISTS (SELECT 1 FROM TestGroupMaster WHERE Code = N'BIOC')
    INSERT INTO TestGroupMaster (Code, Name, SpecimenTag, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'BIOC', N'Biochemistry', N'SER', 1, @User, @Now, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM TestGroupMaster WHERE Code = N'HEMA')
    INSERT INTO TestGroupMaster (Code, Name, SpecimenTag, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'HEMA', N'Hematology', N'EDT', 1, @User, @Now, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM TestGroupMaster WHERE Code = N'ENDO')
    INSERT INTO TestGroupMaster (Code, Name, SpecimenTag, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'ENDO', N'Endocrinology', N'SER', 1, @User, @Now, @User, @Now);

IF NOT EXISTS (SELECT 1 FROM TestCategoryMaster WHERE Code = N'ROUT')
    INSERT INTO TestCategoryMaster (Code, Name, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'ROUT', N'Routine Tests', 1, @User, @Now, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM TestCategoryMaster WHERE Code = N'SPEC')
    INSERT INTO TestCategoryMaster (Code, Name, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'SPEC', N'Special Tests', 1, @User, @Now, @User, @Now);

IF NOT EXISTS (SELECT 1 FROM ReferralDoctorMaster WHERE Code = N'DR001')
    INSERT INTO ReferralDoctorMaster (Code, Name, Phone, Email, Address, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'DR001', N'Dr. Anil Mehta', N'9876500101', N'anil.mehta@example.com', N'Mumbai', 1, @User, @Now, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM ReferralDoctorMaster WHERE Code = N'DR002')
    INSERT INTO ReferralDoctorMaster (Code, Name, Phone, Email, Address, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'DR002', N'Dr. Priya Nair', N'9876500102', N'priya.nair@example.com', N'Pune', 1, @User, @Now, @User, @Now);

IF NOT EXISTS (SELECT 1 FROM CorporateMaster WHERE Code = N'CORP01')
    INSERT INTO CorporateMaster (Code, Name, Address, Phone, ContactPerson, DefaultDiscountPercent, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'CORP01', N'Zorya Corporate Health', N'Andheri East, Mumbai', N'02240001001', N'Rahul Shah', 10.00, 1, @User, @Now, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM CorporateMaster WHERE Code = N'CORP02')
    INSERT INTO CorporateMaster (Code, Name, Address, Phone, ContactPerson, DefaultDiscountPercent, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'CORP02', N'City Diagnostics Pvt Ltd', N'Banjara Hills, Hyderabad', N'04040001002', N'Sneha Reddy', 5.00, 1, @User, @Now, @User, @Now);

-- ---------------------------------------------------------------------------
-- Test master (HIS tests)
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'GLU')
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'GLU', N'Fasting Blood Glucose', N'SER', N'Serum', N'CHEM', 1, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'HBA1C')
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'HBA1C', N'HbA1c', N'EDT', N'EDTA Whole Blood', N'CHEM', 1, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'LIPID')
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'LIPID', N'Lipid Profile', N'SER', N'Serum', N'CHEM', 1, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'CBC')
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'CBC', N'Complete Blood Count', N'EDT', N'EDTA Whole Blood', N'HEMA', 1, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'TSH')
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'TSH', N'Thyroid Stimulating Hormone (TSH)', N'SER', N'Serum', N'IMM', 1, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'URINE')
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'URINE', N'Urine Routine Examination', N'URI', N'Urine', N'URI', 1, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'CREAT')
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'CREAT', N'Serum Creatinine', N'SER', N'Serum', N'CHEM', 1, @User, @Now);
IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'LFT')
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'LFT', N'Liver Function Test Panel', N'SER', N'Serum', N'CHEM', 1, @User, @Now);

-- ---------------------------------------------------------------------------
-- Test profile (package)
-- ---------------------------------------------------------------------------
DECLARE @ProfileId INT;
IF NOT EXISTS (SELECT 1 FROM TestProfileMaster WHERE Code = N'DIAB')
BEGIN
    INSERT INTO TestProfileMaster (Code, Name, PackageRate, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'DIAB', N'Diabetes Care Panel', 899.00, 1, @User, @Now, @User, @Now);
    SET @ProfileId = SCOPE_IDENTITY();

    INSERT INTO TestProfileDetail (TestProfileId, TestId, Quantity)
    SELECT @ProfileId, t.Id, 1
    FROM HISTestMaster t
    WHERE t.HISTestCode IN (N'GLU', N'HBA1C')
      AND NOT EXISTS (SELECT 1 FROM TestProfileDetail d WHERE d.TestProfileId = @ProfileId AND d.TestId = t.Id);
END

IF NOT EXISTS (SELECT 1 FROM TestProfileMaster WHERE Code = N'WELL')
BEGIN
    INSERT INTO TestProfileMaster (Code, Name, PackageRate, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (N'WELL', N'Executive Wellness Panel', 2499.00, 1, @User, @Now, @User, @Now);
    SET @ProfileId = SCOPE_IDENTITY();

    INSERT INTO TestProfileDetail (TestProfileId, TestId, Quantity)
    SELECT @ProfileId, t.Id, 1
    FROM HISTestMaster t
    WHERE t.HISTestCode IN (N'CBC', N'LIPID', N'LFT', N'TSH', N'GLU')
      AND NOT EXISTS (SELECT 1 FROM TestProfileDetail d WHERE d.TestProfileId = @ProfileId AND d.TestId = t.Id);
END

-- ---------------------------------------------------------------------------
-- Test rates (standard — RateType 0)
-- ---------------------------------------------------------------------------
INSERT INTO TestRateMaster (TestId, Rate, EmergencyRate, DiscountPercent, TaxPercent, RateType, EffectiveStart, EffectiveEnd, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
SELECT t.Id, r.Rate, r.EmergencyRate, 0, 5.00, 0, @Now, @FarFuture, 1, @User, @Now, @User, @Now
FROM HISTestMaster t
INNER JOIN (VALUES
    (N'GLU',   120.00, 180.00),
    (N'HBA1C', 450.00, 650.00),
    (N'LIPID', 750.00, 950.00),
    (N'CBC',   350.00, 500.00),
    (N'TSH',   400.00, 550.00),
    (N'URINE', 150.00, 220.00),
    (N'CREAT', 180.00, 260.00),
    (N'LFT',   900.00, 1200.00)
) AS r(Code, Rate, EmergencyRate) ON r.Code = t.HISTestCode
WHERE NOT EXISTS (
    SELECT 1 FROM TestRateMaster tr
    WHERE tr.TestId = t.Id AND tr.RateType = 0 AND tr.IsActive = 1
);

-- Corporate rate for GLU (10% discount via lower rate)
DECLARE @CorpId INT = (SELECT TOP 1 Id FROM CorporateMaster WHERE Code = N'CORP01');
DECLARE @GluId INT = (SELECT TOP 1 Id FROM HISTestMaster WHERE HISTestCode = N'GLU');
IF @CorpId IS NOT NULL AND @GluId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM TestRateMaster WHERE TestId = @GluId AND RateType = 1 AND CorporateId = @CorpId)
    INSERT INTO TestRateMaster (TestId, Rate, EmergencyRate, DiscountPercent, TaxPercent, RateType, CorporateId, EffectiveStart, EffectiveEnd, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (@GluId, 108.00, 180.00, 10.00, 5.00, 1, @CorpId, @Now, @FarFuture, 1, @User, @Now, @User, @Now);

-- ---------------------------------------------------------------------------
-- Patients & sample requests (for billing / LIS workflow)
-- ---------------------------------------------------------------------------
DECLARE @Patients TABLE (HisId NVARCHAR(20), Name NVARCHAR(100), Age DECIMAL(5,1), Gender NVARCHAR(10), Phone NVARCHAR(15), DOB DATE);
INSERT INTO @Patients VALUES
    (N'PAT-1001', N'Rajesh Kumar', 42.0, N'Male', N'9811001001', '1983-04-12'),
    (N'PAT-1002', N'Sunita Desai', 35.0, N'Female', N'9811001002', '1990-08-25'),
    (N'PAT-1003', N'Mohammed Farooq', 58.0, N'Male', N'9811001003', '1967-01-30'),
    (N'PAT-1004', N'Lakshmi Iyer', 29.0, N'Female', N'9811001004', '1996-11-05'),
    (N'PAT-1005', N'Vikram Singh', 51.0, N'Male', N'9811001005', '1974-06-18');

INSERT INTO PatientDetails (HisPatientId, Name, Age, Gender, Phone, IsActive, DateOfBirth, CreatedBy, CreatedOn)
SELECT p.HisId, p.Name, p.Age, p.Gender, p.Phone, 1, p.DOB, @User, @Now
FROM @Patients p
WHERE NOT EXISTS (SELECT 1 FROM PatientDetails pd WHERE pd.HisPatientId = p.HisId);

-- Sample requests
DECLARE @Req TABLE (SampleNo NVARCHAR(30), HisPatientId NVARCHAR(20), TestCode NVARCHAR(20));
INSERT INTO @Req VALUES
    (N'SMP-2026-0001', N'PAT-1001', N'GLU'),
    (N'SMP-2026-0002', N'PAT-1001', N'LIPID'),
    (N'SMP-2026-0003', N'PAT-1002', N'CBC'),
    (N'SMP-2026-0004', N'PAT-1002', N'TSH'),
    (N'SMP-2026-0005', N'PAT-1003', N'HBA1C'),
    (N'SMP-2026-0006', N'PAT-1003', N'CREAT'),
    (N'SMP-2026-0007', N'PAT-1004', N'URINE'),
    (N'SMP-2026-0008', N'PAT-1005', N'LFT'),
    (N'SMP-2026-0009', N'PAT-1005', N'CBC');

INSERT INTO TestRequestDetails (
    SampleNo, HISTestCode, HISTestName, SampleCollectionDate, SampleReceivedDate,
    SpecimenCode, SpecimenName, CreatedBy, CreatedOn, ReportStatus,
    PatientId, DepartmentId, Department, MRNo, HISRequestId, HISRequestNo
)
SELECT
    r.SampleNo,
    t.HISTestCode,
    t.HISTestCodeDescription,
    DATEADD(HOUR, -4, @Now),
    DATEADD(HOUR, -3, @Now),
    t.HISSpecimenCode,
    t.HISSpecimenName,
    @User,
    @Now,
    0, -- New
    p.Id,
    t.DepartmentCode,
    d.Name,
    p.HisPatientId,
    N'REQ-' + RIGHT(r.SampleNo, 4),
    N'HR-' + RIGHT(r.SampleNo, 4)
FROM @Req r
INNER JOIN PatientDetails p ON p.HisPatientId = r.HisPatientId
INNER JOIN HISTestMaster t ON t.HISTestCode = r.TestCode
LEFT JOIN Department d ON d.Code = t.DepartmentCode
WHERE NOT EXISTS (SELECT 1 FROM TestRequestDetails tr WHERE tr.SampleNo = r.SampleNo);

-- ---------------------------------------------------------------------------
-- Sale invoices (transactions)
-- InvoiceStatus: 0=Draft, 1=Confirmed, 2=Paid, 3=Cancelled
-- PaymentStatus: 0=Unpaid, 1=Partial, 2=Paid
-- ---------------------------------------------------------------------------
DECLARE @RefDr1 INT = (SELECT TOP 1 Id FROM ReferralDoctorMaster WHERE Code = N'DR001');
DECLARE @RefDr2 INT = (SELECT TOP 1 Id FROM ReferralDoctorMaster WHERE Code = N'DR002');

-- Invoice 1 — paid walk-in
IF NOT EXISTS (SELECT 1 FROM SaleInvoice WHERE InvoiceNo = N'INV-DEMO-0001')
BEGIN
    DECLARE @Pat1 BIGINT = (SELECT TOP 1 Id FROM PatientDetails WHERE HisPatientId = N'PAT-1001');
    DECLARE @Req1 BIGINT = (SELECT TOP 1 Id FROM TestRequestDetails WHERE SampleNo = N'SMP-2026-0001');
    DECLARE @Req2 BIGINT = (SELECT TOP 1 Id FROM TestRequestDetails WHERE SampleNo = N'SMP-2026-0002');
    DECLARE @T1 INT = (SELECT TOP 1 Id FROM HISTestMaster WHERE HISTestCode = N'GLU');
    DECLARE @T2 INT = (SELECT TOP 1 Id FROM HISTestMaster WHERE HISTestCode = N'LIPID');
    DECLARE @Inv1 BIGINT;

    INSERT INTO SaleInvoice (
        InvoiceNo, InvoiceDate, InvoiceStatus, PaymentStatus, RequestDetailId, PatientId,
        GrossAmount, DiscountAmount, TaxAmount, NetAmount, PaidAmount, DueAmount,
        RefDoctorName, ReferralDoctorId, CorporateId, Notes,
        CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive
    )
    VALUES (
        N'INV-DEMO-0001', DATEADD(DAY, -2, @Now), 2, 2, @Req1, @Pat1,
        0, 0, 0, 0, 0, 0,
        N'Dr. Anil Mehta', @RefDr1, NULL, N'Demo: walk-in fasting glucose + lipid panel',
        @Now, @User, @Now, @User, 1
    );
    SET @Inv1 = SCOPE_IDENTITY();

    INSERT INTO SaleInvoiceDetail (SaleInvoiceId, TestId, Rate, Quantity, Amount, DiscountAmount, TaxAmount, NetAmount, RequestDetailId, SampleNo, CreatedBy, CreatedOn, IsActive)
    VALUES
        (@Inv1, @T1, 120.00, 1, 120.00, 0, 6.00, 126.00, @Req1, N'SMP-2026-0001', @User, @Now, 1),
        (@Inv1, @T2, 750.00, 1, 750.00, 0, 37.50, 787.50, @Req2, N'SMP-2026-0002', @User, @Now, 1);

    UPDATE SaleInvoice SET
        GrossAmount = 870.00, DiscountAmount = 0, TaxAmount = 43.50, NetAmount = 913.50,
        PaidAmount = 913.50, DueAmount = 0
    WHERE Id = @Inv1;
END

-- Invoice 2 — confirmed, partial payment (corporate)
IF NOT EXISTS (SELECT 1 FROM SaleInvoice WHERE InvoiceNo = N'INV-DEMO-0002')
BEGIN
    DECLARE @Pat2 BIGINT = (SELECT TOP 1 Id FROM PatientDetails WHERE HisPatientId = N'PAT-1002');
    DECLARE @Req3 BIGINT = (SELECT TOP 1 Id FROM TestRequestDetails WHERE SampleNo = N'SMP-2026-0003');
    DECLARE @Req4 BIGINT = (SELECT TOP 1 Id FROM TestRequestDetails WHERE SampleNo = N'SMP-2026-0004');
    DECLARE @Tc INT = (SELECT TOP 1 Id FROM HISTestMaster WHERE HISTestCode = N'CBC');
    DECLARE @Tt INT = (SELECT TOP 1 Id FROM HISTestMaster WHERE HISTestCode = N'TSH');
    DECLARE @Inv2 BIGINT;

    INSERT INTO SaleInvoice (
        InvoiceNo, InvoiceDate, InvoiceStatus, PaymentStatus, RequestDetailId, PatientId,
        GrossAmount, DiscountAmount, TaxAmount, NetAmount, PaidAmount, DueAmount,
        RefDoctorName, ReferralDoctorId, CorporateId, Notes,
        CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive
    )
    VALUES (
        N'INV-DEMO-0002', DATEADD(DAY, -1, @Now), 1, 1, @Req3, @Pat2,
        0, 0, 0, 0, 400.00, 0,
        N'Dr. Priya Nair', @RefDr2, @CorpId, N'Demo: corporate panel — partial payment',
        @Now, @User, @Now, @User, 1
    );
    SET @Inv2 = SCOPE_IDENTITY();

    INSERT INTO SaleInvoiceDetail (SaleInvoiceId, TestId, Rate, Quantity, Amount, DiscountAmount, TaxAmount, NetAmount, RequestDetailId, SampleNo, CreatedBy, CreatedOn, IsActive)
    VALUES
        (@Inv2, @Tc, 350.00, 1, 350.00, 17.50, 16.63, 349.13, @Req3, N'SMP-2026-0003', @User, @Now, 1),
        (@Inv2, @Tt, 400.00, 1, 400.00, 20.00, 19.00, 399.00, @Req4, N'SMP-2026-0004', @User, @Now, 1);

    UPDATE SaleInvoice SET
        GrossAmount = 750.00, DiscountAmount = 37.50, TaxAmount = 35.63, NetAmount = 748.13,
        PaidAmount = 400.00, DueAmount = 348.13
    WHERE Id = @Inv2;
END

-- Invoice 3 — draft unpaid
IF NOT EXISTS (SELECT 1 FROM SaleInvoice WHERE InvoiceNo = N'INV-DEMO-0003')
BEGIN
    DECLARE @Pat3 BIGINT = (SELECT TOP 1 Id FROM PatientDetails WHERE HisPatientId = N'PAT-1003');
    DECLARE @Req5 BIGINT = (SELECT TOP 1 Id FROM TestRequestDetails WHERE SampleNo = N'SMP-2026-0005');
    DECLARE @Th INT = (SELECT TOP 1 Id FROM HISTestMaster WHERE HISTestCode = N'HBA1C');
    DECLARE @Inv3 BIGINT;

    INSERT INTO SaleInvoice (
        InvoiceNo, InvoiceDate, InvoiceStatus, PaymentStatus, RequestDetailId, PatientId,
        GrossAmount, DiscountAmount, TaxAmount, NetAmount, PaidAmount, DueAmount,
        RefDoctorName, ReferralDoctorId, CorporateId, Notes,
        CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive
    )
    VALUES (
        N'INV-DEMO-0003', @Now, 0, 0, @Req5, @Pat3,
        450.00, 0, 22.50, 472.50, 0, 472.50,
        NULL, NULL, NULL, N'Demo: draft invoice — diabetes follow-up',
        @Now, @User, @Now, @User, 1
    );
    SET @Inv3 = SCOPE_IDENTITY();

    INSERT INTO SaleInvoiceDetail (SaleInvoiceId, TestId, Rate, Quantity, Amount, DiscountAmount, TaxAmount, NetAmount, RequestDetailId, SampleNo, CreatedBy, CreatedOn, IsActive)
    VALUES (@Inv3, @Th, 450.00, 1, 450.00, 0, 22.50, 472.50, @Req5, N'SMP-2026-0005', @User, @Now, 1);
END

COMMIT TRANSACTION;

PRINT 'Sample data seed completed.';
PRINT 'Departments: 5 | Specimens: 4 | Tests: 8 | Patients: 5 | Samples: 9 | Invoices: 3 (if not already present)';
GO
