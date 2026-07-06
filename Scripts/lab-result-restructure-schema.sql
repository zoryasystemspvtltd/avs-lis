-- Lab Result restructuring: TestResult, TestResultDetails, Analyzer Parameter Mapping,
-- Parameter Master independence, Test Parameter Mapping.
-- Idempotent: safe to run multiple times.

-- TestResults.EquipmentId nullable (manual entry without analyzer)
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestResults') AND name = N'EquipmentId' AND is_nullable = 0
)
BEGIN
    ALTER TABLE dbo.TestResults ALTER COLUMN EquipmentId INT NULL;
END
GO

-- TestResultDetails: add HISParamCode
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestResultDetails') AND name = N'HISParamCode'
)
BEGIN
    ALTER TABLE dbo.TestResultDetails ADD HISParamCode NVARCHAR(50) NULL;
END
GO

-- TestResultDetails: rename LISParamValue -> ParamValue
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestResultDetails') AND name = N'LISParamValue'
) AND NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestResultDetails') AND name = N'ParamValue'
)
BEGIN
    EXEC sp_rename 'dbo.TestResultDetails.LISParamValue', 'ParamValue', 'COLUMN';
END
GO

-- TestResultDetails: rename LISParamUnit -> ParamUnit
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestResultDetails') AND name = N'LISParamUnit'
) AND NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestResultDetails') AND name = N'ParamUnit'
)
BEGIN
    EXEC sp_rename 'dbo.TestResultDetails.LISParamUnit', 'ParamUnit', 'COLUMN';
END
GO

-- Analyzer Parameter Mapping: rename HISTestCode -> HISParamCode on TestMappingMaster
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestMappingMaster') AND name = N'HISTestCode'
) AND NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestMappingMaster') AND name = N'HISParamCode'
)
BEGIN
    EXEC sp_rename 'dbo.TestMappingMaster.HISTestCode', 'HISParamCode', 'COLUMN';
END
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestMappingMaster') AND name = N'HISTestCodeDescription'
) AND NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestMappingMaster') AND name = N'HISParamDescription'
)
BEGIN
    EXEC sp_rename 'dbo.TestMappingMaster.HISTestCodeDescription', 'HISParamDescription', 'COLUMN';
END
GO

-- Parameter Master: HisTestId optional
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.HISParameterMaster') AND name = N'HisTestId' AND is_nullable = 0
)
BEGIN
    ALTER TABLE dbo.HISParameterMaster ALTER COLUMN HisTestId INT NULL;
END
GO

-- Test Parameter Mapping master table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'TestParameterMappingMaster')
BEGIN
    CREATE TABLE dbo.TestParameterMappingMaster (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        HisTestId INT NOT NULL,
        HisParameterId INT NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_TestParameterMappingMaster_IsActive DEFAULT(1),
        CreatedBy NVARCHAR(100) NULL,
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_TestParameterMappingMaster_CreatedOn DEFAULT(GETDATE()),
        CONSTRAINT FK_TestParameterMappingMaster_HisTest FOREIGN KEY (HisTestId) REFERENCES dbo.HISTestMaster(Id),
        CONSTRAINT FK_TestParameterMappingMaster_HisParameter FOREIGN KEY (HisParameterId) REFERENCES dbo.HISParameterMaster(Id)
    );
    CREATE UNIQUE INDEX UX_TestParameterMappingMaster_Test_Param
        ON dbo.TestParameterMappingMaster(HisTestId, HisParameterId);
END
GO

-- Backfill TestParameterMapping from legacy HISParameterMaster.HisTestId links
INSERT INTO dbo.TestParameterMappingMaster (HisTestId, HisParameterId, IsActive, CreatedBy, CreatedOn)
SELECT p.HisTestId, p.Id, 1, 'migration', GETDATE()
FROM dbo.HISParameterMaster p
WHERE p.HisTestId IS NOT NULL AND p.HisTestId > 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.TestParameterMappingMaster m
      WHERE m.HisTestId = p.HisTestId AND m.HisParameterId = p.Id
  );
GO
