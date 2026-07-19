namespace LIS.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class TestParameterMappingSequence : DbMigration
    {
        public override void Up()
        {
            Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestParameterMappingMaster') AND name = N'Sequence'
)
    ALTER TABLE dbo.TestParameterMappingMaster ADD [Sequence] INT NULL;
");

            Sql(@"
;WITH MappingSequence AS (
    SELECT Id,
           ROW_NUMBER() OVER (PARTITION BY HisTestId ORDER BY Id) AS SequenceNumber
    FROM dbo.TestParameterMappingMaster
)
UPDATE mapping
SET [Sequence] = numbered.SequenceNumber
FROM dbo.TestParameterMappingMaster mapping
INNER JOIN MappingSequence numbered ON numbered.Id = mapping.Id
WHERE mapping.[Sequence] IS NULL OR mapping.[Sequence] < 1;
");

            Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestParameterMappingMaster')
      AND name = N'Sequence'
      AND is_nullable = 1
)
    ALTER TABLE dbo.TestParameterMappingMaster ALTER COLUMN [Sequence] INT NOT NULL;
");

            Sql(@"
IF NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id
       AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.TestParameterMappingMaster')
      AND c.name = N'Sequence'
)
    ALTER TABLE dbo.TestParameterMappingMaster
        ADD CONSTRAINT DF_TestParameterMappingMaster_Sequence DEFAULT(1) FOR [Sequence];
");
        }

        public override void Down()
        {
            Sql(@"
DECLARE @constraintName NVARCHAR(128);
SELECT @constraintName = dc.name
FROM sys.default_constraints dc
INNER JOIN sys.columns c
    ON c.object_id = dc.parent_object_id
   AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID(N'dbo.TestParameterMappingMaster')
  AND c.name = N'Sequence';

IF @constraintName IS NOT NULL
    EXEC(N'ALTER TABLE dbo.TestParameterMappingMaster DROP CONSTRAINT [' + @constraintName + N']');

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestParameterMappingMaster') AND name = N'Sequence'
)
    ALTER TABLE dbo.TestParameterMappingMaster DROP COLUMN [Sequence];
");
        }
    }
}
