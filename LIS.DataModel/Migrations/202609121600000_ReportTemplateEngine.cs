namespace LIS.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;

    /// <summary>
    /// Phase 1 Report Template Engine tables. Idempotent SQL — does not change clinical report rendering.
    /// </summary>
    public partial class ReportTemplateEngine : DbMigration
    {
        public override void Up()
        {
            Sql(@"
IF OBJECT_ID(N'dbo.ReportTemplate', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReportTemplate (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(120) NOT NULL,
        ReportType NVARCHAR(40) NOT NULL,
        Description NVARCHAR(500) NULL,
        BuiltInRendererKey NVARCHAR(80) NULL,
        IsSystemDefault BIT NOT NULL CONSTRAINT DF_ReportTemplate_IsSystemDefault DEFAULT(0),
        IsActive BIT NOT NULL CONSTRAINT DF_ReportTemplate_IsActive DEFAULT(1),
        CreatedBy NVARCHAR(80) NULL,
        CreatedOn DATETIME NOT NULL,
        ModifiedBy NVARCHAR(80) NULL,
        ModifiedOn DATETIME NULL
    );
    CREATE INDEX IX_ReportTemplate_ReportType ON dbo.ReportTemplate(ReportType);
END

IF OBJECT_ID(N'dbo.ReportTemplateVersion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReportTemplateVersion (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TemplateId INT NOT NULL,
        VersionNumber INT NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        DefinitionJson NVARCHAR(MAX) NOT NULL,
        SchemaVersion INT NOT NULL CONSTRAINT DF_ReportTemplateVersion_SchemaVersion DEFAULT(1),
        CreatedBy NVARCHAR(80) NULL,
        CreatedOn DATETIME NOT NULL,
        ModifiedBy NVARCHAR(80) NULL,
        ModifiedOn DATETIME NULL,
        PublishedBy NVARCHAR(80) NULL,
        PublishedOn DATETIME NULL,
        ArchivedBy NVARCHAR(80) NULL,
        ArchivedOn DATETIME NULL,
        CONSTRAINT FK_ReportTemplateVersion_Template FOREIGN KEY (TemplateId) REFERENCES dbo.ReportTemplate(Id)
    );
    CREATE UNIQUE INDEX UX_ReportTemplateVersion_Template_Version ON dbo.ReportTemplateVersion(TemplateId, VersionNumber);
    CREATE INDEX IX_ReportTemplateVersion_Status ON dbo.ReportTemplateVersion(TemplateId, Status);
END

IF OBJECT_ID(N'dbo.ReportTemplateAssignment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReportTemplateAssignment (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ReportType NVARCHAR(40) NOT NULL,
        ScopeType NVARCHAR(20) NOT NULL,
        DepartmentCode NVARCHAR(15) NULL,
        TestId INT NULL,
        TemplateId INT NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ReportTemplateAssignment_IsActive DEFAULT(1),
        CreatedBy NVARCHAR(80) NULL,
        CreatedOn DATETIME NOT NULL,
        ModifiedBy NVARCHAR(80) NULL,
        ModifiedOn DATETIME NULL,
        CONSTRAINT FK_ReportTemplateAssignment_Template FOREIGN KEY (TemplateId) REFERENCES dbo.ReportTemplate(Id)
    );
    CREATE INDEX IX_ReportTemplateAssignment_Resolve ON dbo.ReportTemplateAssignment(ReportType, ScopeType, IsActive);
END
");
        }

        public override void Down()
        {
            Sql(@"
IF OBJECT_ID(N'dbo.ReportTemplateAssignment', N'U') IS NOT NULL DROP TABLE dbo.ReportTemplateAssignment;
IF OBJECT_ID(N'dbo.ReportTemplateVersion', N'U') IS NOT NULL DROP TABLE dbo.ReportTemplateVersion;
IF OBJECT_ID(N'dbo.ReportTemplate', N'U') IS NOT NULL DROP TABLE dbo.ReportTemplate;
");
        }
    }
}
