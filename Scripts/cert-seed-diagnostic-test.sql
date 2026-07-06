-- QA certification seed: one diagnostic test with active rate (idempotent)
IF NOT EXISTS (SELECT 1 FROM HISTestMaster WHERE HISTestCode = N'XR-CHEST-UAT')
BEGIN
    INSERT INTO HISTestMaster (HISTestCode, HISTestCodeDescription, HISSpecimenCode, HISSpecimenName, DepartmentCode, IsActive, CreatedBy, CreatedOn)
    VALUES (N'XR-CHEST-UAT', N'X-Ray Chest (UAT)', N'SERUM', N'SERUM', N'DEP00011', 1, N'cert-seed', GETDATE());
END
GO

DECLARE @testId INT = (SELECT Id FROM HISTestMaster WHERE HISTestCode = N'XR-CHEST-UAT');
IF @testId IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM TestRateMaster WHERE TestId = @testId AND IsActive = 1
      AND EffectiveStart <= CAST(GETDATE() AS date) AND EffectiveEnd >= CAST(GETDATE() AS date)
)
BEGIN
    INSERT INTO TestRateMaster (TestId, Rate, EmergencyRate, DiscountPercent, TaxPercent, RateType, EffectiveStart, EffectiveEnd, IsActive, CreatedBy, CreatedOn, ModifiedOn, ModifiedBy)
    VALUES (@testId, 500, 0, 0, 0, 0, DATEADD(year, -1, GETDATE()), DATEADD(year, 1, GETDATE()), 1, N'cert-seed', GETDATE(), GETDATE(), N'cert-seed');
END
GO
