namespace LIS.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class PatientMrVisitPrefix : DbMigration
    {
        public override void Up()
        {
            // Each Sql() is a separate batch. SQL Server validates column names at parse time,
            // so ADD COLUMN and CREATE INDEX on new columns must not share one batch.
            Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.PatientDetails') AND name = 'PatientPrefix'
)
    ALTER TABLE dbo.PatientDetails ADD PatientPrefix NVARCHAR(20) NULL;
");

            Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.PatientDetails') AND name = 'MRNo'
)
    ALTER TABLE dbo.PatientDetails ADD MRNo NVARCHAR(30) NULL;
");

            Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.PatientDetails') AND name = 'VisitId'
)
    ALTER TABLE dbo.PatientDetails ADD VisitId NVARCHAR(30) NULL;
");

            Sql(@"
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PatientDetails_MRNo' AND object_id = OBJECT_ID(N'dbo.PatientDetails'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_PatientDetails_MRNo
        ON dbo.PatientDetails (MRNo)
        WHERE IsActive = 1 AND MRNo IS NOT NULL AND MRNo <> '';
");

            Sql(@"
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PatientDetails_VisitId' AND object_id = OBJECT_ID(N'dbo.PatientDetails'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_PatientDetails_VisitId
        ON dbo.PatientDetails (VisitId)
        WHERE IsActive = 1 AND VisitId IS NOT NULL AND VisitId <> '';
");

            Sql(@"
SET QUOTED_IDENTIFIER ON;
IF EXISTS (
    SELECT 1 FROM sys.indexes i
    CROSS APPLY (SELECT filter_definition FROM sys.indexes WHERE object_id = i.object_id AND index_id = i.index_id) fd
    WHERE i.name = 'UX_PatientDetails_MRNo' AND i.object_id = OBJECT_ID(N'dbo.PatientDetails')
      AND (fd.filter_definition IS NULL OR fd.filter_definition NOT LIKE '%IsActive%')
)
BEGIN
    DROP INDEX UX_PatientDetails_MRNo ON dbo.PatientDetails;
    CREATE UNIQUE NONCLUSTERED INDEX UX_PatientDetails_MRNo
        ON dbo.PatientDetails (MRNo)
        WHERE IsActive = 1 AND MRNo IS NOT NULL AND MRNo <> '';
END
");

            Sql(@"
SET QUOTED_IDENTIFIER ON;
IF EXISTS (
    SELECT 1 FROM sys.indexes i
    CROSS APPLY (SELECT filter_definition FROM sys.indexes WHERE object_id = i.object_id AND index_id = i.index_id) fd
    WHERE i.name = 'UX_PatientDetails_VisitId' AND i.object_id = OBJECT_ID(N'dbo.PatientDetails')
      AND (fd.filter_definition IS NULL OR fd.filter_definition NOT LIKE '%IsActive%')
)
BEGIN
    DROP INDEX UX_PatientDetails_VisitId ON dbo.PatientDetails;
    CREATE UNIQUE NONCLUSTERED INDEX UX_PatientDetails_VisitId
        ON dbo.PatientDetails (VisitId)
        WHERE IsActive = 1 AND VisitId IS NOT NULL AND VisitId <> '';
END
");
        }

        public override void Down()
        {
            Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PatientDetails_VisitId' AND object_id = OBJECT_ID(N'dbo.PatientDetails'))
    DROP INDEX UX_PatientDetails_VisitId ON dbo.PatientDetails;
");

            Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PatientDetails_MRNo' AND object_id = OBJECT_ID(N'dbo.PatientDetails'))
    DROP INDEX UX_PatientDetails_MRNo ON dbo.PatientDetails;
");

            Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.PatientDetails') AND name = 'VisitId')
    ALTER TABLE dbo.PatientDetails DROP COLUMN VisitId;
");

            Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.PatientDetails') AND name = 'MRNo')
    ALTER TABLE dbo.PatientDetails DROP COLUMN MRNo;
");

            Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.PatientDetails') AND name = 'PatientPrefix')
    ALTER TABLE dbo.PatientDetails DROP COLUMN PatientPrefix;
");
        }
    }
}
