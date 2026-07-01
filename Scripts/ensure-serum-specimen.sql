-- Standard SERUM specimen used by test master UAT and regression tests.
USE ZoryaLMS;
GO

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM HISSpecimenMaster WHERE Code = N'SERUM')
BEGIN
    INSERT INTO HISSpecimenMaster (Code, Name, IsActive, CreatedBy, CreatedOn)
    VALUES (N'SERUM', N'Serum', 1, N'system-seed', GETDATE());
    PRINT 'Inserted SERUM specimen.';
END
ELSE
BEGIN
    UPDATE HISSpecimenMaster SET Name = N'Serum', IsActive = 1 WHERE Code = N'SERUM';
    PRINT 'SERUM specimen already present.';
END
GO
