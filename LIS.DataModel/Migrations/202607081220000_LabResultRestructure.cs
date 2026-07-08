namespace LIS.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class LabResultRestructure : DbMigration
    {
        public override void Up()
        {
            Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestResults') AND name = N'EquipmentId' AND is_nullable = 0
)
    ALTER TABLE dbo.TestResults ALTER COLUMN EquipmentId INT NULL;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestResultDetails') AND name = N'HISParamCode'
)
    ALTER TABLE dbo.TestResultDetails ADD HISParamCode NVARCHAR(50) NULL;

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestResultDetails') AND name = N'LISParamValue'
) AND NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestResultDetails') AND name = N'ParamValue'
)
    EXEC sp_rename 'dbo.TestResultDetails.LISParamValue', 'ParamValue', 'COLUMN';

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestResultDetails') AND name = N'LISParamUnit'
) AND NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestResultDetails') AND name = N'ParamUnit'
)
    EXEC sp_rename 'dbo.TestResultDetails.LISParamUnit', 'ParamUnit', 'COLUMN';

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestMappingMaster') AND name = N'HISTestCode'
) AND NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestMappingMaster') AND name = N'HISParamCode'
)
    EXEC sp_rename 'dbo.TestMappingMaster.HISTestCode', 'HISParamCode', 'COLUMN';

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestMappingMaster') AND name = N'HISTestCodeDescription'
) AND NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestMappingMaster') AND name = N'HISParamDescription'
)
    EXEC sp_rename 'dbo.TestMappingMaster.HISTestCodeDescription', 'HISParamDescription', 'COLUMN';

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.HISParameterMaster') AND name = N'HisTestId' AND is_nullable = 0
)
    ALTER TABLE dbo.HISParameterMaster ALTER COLUMN HisTestId INT NULL;

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

INSERT INTO dbo.TestParameterMappingMaster (HisTestId, HisParameterId, IsActive, CreatedBy, CreatedOn)
SELECT p.HisTestId, p.Id, 1, N'migration', GETDATE()
FROM dbo.HISParameterMaster p
WHERE p.HisTestId IS NOT NULL AND p.HisTestId > 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.TestParameterMappingMaster m
      WHERE m.HisTestId = p.HisTestId AND m.HisParameterId = p.Id
  );
");
        }

        public override void Down()
        {
            Sql(@"
IF OBJECT_ID(N'dbo.TestParameterMappingMaster', N'U') IS NOT NULL
    DROP TABLE dbo.TestParameterMappingMaster;

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.HISParameterMaster') AND name = N'HisTestId' AND is_nullable = 1
)
    ALTER TABLE dbo.HISParameterMaster ALTER COLUMN HisTestId INT NOT NULL;

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestMappingMaster') AND name = N'HISParamDescription'
) AND NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestMappingMaster') AND name = N'HISTestCodeDescription'
)
    EXEC sp_rename 'dbo.TestMappingMaster.HISParamDescription', 'HISTestCodeDescription', 'COLUMN';

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestMappingMaster') AND name = N'HISParamCode'
) AND NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestMappingMaster') AND name = N'HISTestCode'
)
    EXEC sp_rename 'dbo.TestMappingMaster.HISParamCode', 'HISTestCode', 'COLUMN';

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestResultDetails') AND name = N'ParamUnit'
) AND NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestResultDetails') AND name = N'LISParamUnit'
)
    EXEC sp_rename 'dbo.TestResultDetails.ParamUnit', 'LISParamUnit', 'COLUMN';

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestResultDetails') AND name = N'ParamValue'
) AND NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestResultDetails') AND name = N'LISParamValue'
)
    EXEC sp_rename 'dbo.TestResultDetails.ParamValue', 'LISParamValue', 'COLUMN';

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestResultDetails') AND name = N'HISParamCode'
)
    ALTER TABLE dbo.TestResultDetails DROP COLUMN HISParamCode;

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestResults') AND name = N'EquipmentId' AND is_nullable = 1
)
    ALTER TABLE dbo.TestResults ALTER COLUMN EquipmentId INT NOT NULL;
");
        }
    }
}
