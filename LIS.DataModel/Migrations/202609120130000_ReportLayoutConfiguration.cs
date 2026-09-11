namespace LIS.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;

    /// <summary>
    /// Ensures ReportLayoutConfiguration table + Diagnostic/Radiology default rows.
    /// Idempotent: safe when Scripts/add-report-layout-configuration.sql already ran.
    /// Does not recreate Notification* tables (those belong to prior notification migrations).
    /// </summary>
    public partial class ReportLayoutConfiguration : DbMigration
    {
        public override void Up()
        {
            Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ReportLayoutConfiguration')
BEGIN
    CREATE TABLE dbo.ReportLayoutConfiguration
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReportLayoutConfiguration PRIMARY KEY,
        ReportType NVARCHAR(40) NOT NULL,
        PageSize NVARCHAR(20) NOT NULL CONSTRAINT DF_ReportLayout_PageSize DEFAULT (N'A4'),
        Orientation NVARCHAR(20) NOT NULL CONSTRAINT DF_ReportLayout_Orientation DEFAULT (N'Portrait'),
        HeaderHeightMm DECIMAL(18,2) NOT NULL,
        FooterHeightMm DECIMAL(18,2) NOT NULL,
        LeftMarginMm DECIMAL(18,2) NOT NULL,
        RightMarginMm DECIMAL(18,2) NOT NULL,
        DoctorSignatureEnabled BIT NOT NULL CONSTRAINT DF_ReportLayout_DocSigEnabled DEFAULT (1),
        DoctorSignatureHorizontal NVARCHAR(20) NULL CONSTRAINT DF_ReportLayout_DocSigH DEFAULT (N'Right'),
        DoctorSignatureVertical NVARCHAR(20) NULL CONSTRAINT DF_ReportLayout_DocSigV DEFAULT (N'Bottom'),
        DoctorSignatureWidthMm DECIMAL(18,2) NOT NULL CONSTRAINT DF_ReportLayout_DocSigW DEFAULT (50),
        DoctorSignatureHeightMm DECIMAL(18,2) NOT NULL CONSTRAINT DF_ReportLayout_DocSigHt DEFAULT (14),
        TechnicianSignatureEnabled BIT NOT NULL CONSTRAINT DF_ReportLayout_TechSigEnabled DEFAULT (0),
        TechnicianSignatureHorizontal NVARCHAR(20) NULL CONSTRAINT DF_ReportLayout_TechSigH DEFAULT (N'Left'),
        TechnicianSignatureVertical NVARCHAR(20) NULL CONSTRAINT DF_ReportLayout_TechSigV DEFAULT (N'Bottom'),
        TechnicianSignatureWidthMm DECIMAL(18,2) NOT NULL CONSTRAINT DF_ReportLayout_TechSigW DEFAULT (50),
        TechnicianSignatureHeightMm DECIMAL(18,2) NOT NULL CONSTRAINT DF_ReportLayout_TechSigHt DEFAULT (14),
        IsActive BIT NOT NULL CONSTRAINT DF_ReportLayout_IsActive DEFAULT (1),
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_ReportLayout_CreatedOn DEFAULT (GETDATE()),
        CreatedBy NVARCHAR(80) NULL,
        ModifiedOn DATETIME NOT NULL CONSTRAINT DF_ReportLayout_ModifiedOn DEFAULT (GETDATE()),
        ModifiedBy NVARCHAR(80) NULL
    );

    CREATE UNIQUE INDEX UX_ReportLayoutConfiguration_ReportType
        ON dbo.ReportLayoutConfiguration (ReportType);
END

IF NOT EXISTS (SELECT 1 FROM dbo.ReportLayoutConfiguration WHERE ReportType = N'Diagnostic')
BEGIN
    INSERT INTO dbo.ReportLayoutConfiguration
    (ReportType, PageSize, Orientation, HeaderHeightMm, FooterHeightMm, LeftMarginMm, RightMarginMm,
     DoctorSignatureEnabled, DoctorSignatureHorizontal, DoctorSignatureVertical,
     DoctorSignatureWidthMm, DoctorSignatureHeightMm,
     TechnicianSignatureEnabled, TechnicianSignatureHorizontal, TechnicianSignatureVertical,
     TechnicianSignatureWidthMm, TechnicianSignatureHeightMm,
     IsActive, CreatedBy, ModifiedBy)
    VALUES
    (N'Diagnostic', N'A4', N'Portrait', 50, 50, 12, 12,
     1, N'Right', N'Bottom', 50, 14,
     0, N'Left', N'Bottom', 50, 14,
     1, N'system', N'system');
END

IF NOT EXISTS (SELECT 1 FROM dbo.ReportLayoutConfiguration WHERE ReportType = N'Radiology')
BEGIN
    INSERT INTO dbo.ReportLayoutConfiguration
    (ReportType, PageSize, Orientation, HeaderHeightMm, FooterHeightMm, LeftMarginMm, RightMarginMm,
     DoctorSignatureEnabled, DoctorSignatureHorizontal, DoctorSignatureVertical,
     DoctorSignatureWidthMm, DoctorSignatureHeightMm,
     TechnicianSignatureEnabled, TechnicianSignatureHorizontal, TechnicianSignatureVertical,
     TechnicianSignatureWidthMm, TechnicianSignatureHeightMm,
     IsActive, CreatedBy, ModifiedBy)
    VALUES
    (N'Radiology', N'A4', N'Portrait', 40, 50, 10, 10,
     1, N'Left', N'Bottom', 50, 14,
     0, N'Left', N'Bottom', 50, 14,
     1, N'system', N'system');
END
");
        }

        public override void Down()
        {
            Sql(@"
IF OBJECT_ID(N'dbo.ReportLayoutConfiguration', N'U') IS NOT NULL
    DROP TABLE dbo.ReportLayoutConfiguration;
");
        }
    }
}
