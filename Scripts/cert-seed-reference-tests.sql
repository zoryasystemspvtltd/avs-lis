-- Reference certification tests: Laboratory (CBC001) + Diagnostic (MRI101)
-- Idempotent seed for UAT / Sale Invoice demo scenarios

IF NOT EXISTS (SELECT 1 FROM Department WHERE Code = N'HEMA')
BEGIN
    INSERT INTO Department (Code, Name, ProcessingCategory)
    VALUES (N'HEMA', N'Hematology', N'Laboratory');
END
ELSE IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Department') AND name = N'ProcessingCategory'
)
BEGIN
    UPDATE Department SET ProcessingCategory = N'Laboratory' WHERE Code = N'HEMA';
END
GO

IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'CBC001')
BEGIN
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'CBC001', N'CBC', N'BLOOD', N'BLOOD', N'HEMA', 1, N'cert-seed', GETDATE());
END
ELSE
BEGIN
    UPDATE HISTestMaster
    SET HISTestCodeDescription = N'CBC',
        DepartmentCode = N'HEMA',
        IsActive = 1
    WHERE HISTestCode = N'CBC001';
END
GO

IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'MRI101')
BEGIN
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'MRI101', N'MRI Brain', N'BLOOD', N'BLOOD', N'DEP00011', 1, N'cert-seed', GETDATE());
END
ELSE
BEGIN
    UPDATE HISTestMaster
    SET HISTestCodeDescription = N'MRI Brain',
        DepartmentCode = N'DEP00011',
        IsActive = 1
    WHERE HISTestCode = N'MRI101';
END
GO

DECLARE @cbcId INT = (SELECT Id FROM HISTestMaster WHERE HISTestCode = N'CBC001');
IF @cbcId IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM TestRateMaster WHERE TestId = @cbcId AND IsActive = 1
      AND Rate = 350 AND EffectiveStart <= CAST(GETDATE() AS date) AND EffectiveEnd >= CAST(GETDATE() AS date)
)
BEGIN
    UPDATE TestRateMaster SET IsActive = 0 WHERE TestId = @cbcId AND IsActive = 1;
    INSERT INTO TestRateMaster (TestId, Rate, EmergencyRate, DiscountPercent, TaxPercent, RateType, EffectiveStart, EffectiveEnd, IsActive, CreatedBy, CreatedOn, ModifiedOn, ModifiedBy)
    VALUES (@cbcId, 350, 0, 0, 0, 0, DATEADD(year, -1, GETDATE()), DATEADD(year, 1, GETDATE()), 1, N'cert-seed', GETDATE(), GETDATE(), N'cert-seed');
END
GO

DECLARE @mriId INT = (SELECT Id FROM HISTestMaster WHERE HISTestCode = N'MRI101');
IF @mriId IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM TestRateMaster WHERE TestId = @mriId AND IsActive = 1
      AND Rate = 4500 AND EffectiveStart <= CAST(GETDATE() AS date) AND EffectiveEnd >= CAST(GETDATE() AS date)
)
BEGIN
    UPDATE TestRateMaster SET IsActive = 0 WHERE TestId = @mriId AND IsActive = 1;
    INSERT INTO TestRateMaster (TestId, Rate, EmergencyRate, DiscountPercent, TaxPercent, RateType, EffectiveStart, EffectiveEnd, IsActive, CreatedBy, CreatedOn, ModifiedOn, ModifiedBy)
    VALUES (@mriId, 4500, 0, 0, 0, 0, DATEADD(year, -1, GETDATE()), DATEADD(year, 1, GETDATE()), 1, N'cert-seed', GETDATE(), GETDATE(), N'cert-seed');
END
GO
