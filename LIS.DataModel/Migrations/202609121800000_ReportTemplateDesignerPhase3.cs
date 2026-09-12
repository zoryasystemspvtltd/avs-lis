namespace LIS.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ReportTemplateDesignerPhase3 : DbMigration
    {
        public override void Up()
        {
            Sql(@"
IF COL_LENGTH('dbo.ReportTemplate', 'TemplateCategory') IS NULL
    ALTER TABLE dbo.ReportTemplate ADD TemplateCategory NVARCHAR(40) NULL;
IF COL_LENGTH('dbo.ReportTemplate', 'TargetTestId') IS NULL
    ALTER TABLE dbo.ReportTemplate ADD TargetTestId INT NULL;
IF COL_LENGTH('dbo.ReportTemplate', 'TargetProfileId') IS NULL
    ALTER TABLE dbo.ReportTemplate ADD TargetProfileId INT NULL;
IF COL_LENGTH('dbo.ReportTemplateAssignment', 'ProfileId') IS NULL
    ALTER TABLE dbo.ReportTemplateAssignment ADD ProfileId INT NULL;
IF OBJECT_ID(N'dbo.ReportTemplateModeSetting', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReportTemplateModeSetting (
        ReportType NVARCHAR(40) NOT NULL PRIMARY KEY,
        Mode NVARCHAR(20) NOT NULL,
        ModifiedBy NVARCHAR(80) NULL,
        ModifiedOn DATETIME NULL
    );
END
IF NOT EXISTS (SELECT 1 FROM dbo.ReportTemplateModeSetting WHERE ReportType = N'Diagnostic')
    INSERT INTO dbo.ReportTemplateModeSetting (ReportType, Mode, ModifiedBy, ModifiedOn)
    VALUES (N'Diagnostic', N'SystemDefault', N'ef-migration', GETUTCDATE());
IF NOT EXISTS (SELECT 1 FROM dbo.ReportTemplateModeSetting WHERE ReportType = N'Radiology')
    INSERT INTO dbo.ReportTemplateModeSetting (ReportType, Mode, ModifiedBy, ModifiedOn)
    VALUES (N'Radiology', N'SystemDefault', N'ef-migration', GETUTCDATE());
UPDATE dbo.ReportTemplate SET TemplateCategory = N'SystemDefault'
WHERE IsSystemDefault = 1 AND (TemplateCategory IS NULL OR TemplateCategory = N'');
");
        }

        public override void Down()
        {
        }
    }
}
