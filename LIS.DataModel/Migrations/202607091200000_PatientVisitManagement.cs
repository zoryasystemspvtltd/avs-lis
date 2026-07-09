namespace LIS.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class PatientVisitManagement : DbMigration
    {
        public override void Up()
        {
            Sql(@"
IF OBJECT_ID(N'dbo.PatientVisit', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PatientVisit (
        PatientVisitId BIGINT IDENTITY(1,1) NOT NULL,
        PatientId BIGINT NOT NULL,
        VisitId NVARCHAR(30) NOT NULL,
        VisitDateTime DATETIME NOT NULL,
        SaleInvoiceId BIGINT NULL,
        VisitStatus INT NOT NULL CONSTRAINT DF_PatientVisit_VisitStatus DEFAULT(0),
        CreatedBy NVARCHAR(80) NULL,
        CreatedOn DATETIME NOT NULL,
        ModifiedBy NVARCHAR(80) NULL,
        ModifiedOn DATETIME NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_PatientVisit_IsActive DEFAULT(1),
        CONSTRAINT PK_PatientVisit PRIMARY KEY CLUSTERED (PatientVisitId),
        CONSTRAINT FK_PatientVisit_PatientDetails FOREIGN KEY (PatientId) REFERENCES dbo.PatientDetails(Id),
        CONSTRAINT FK_PatientVisit_SaleInvoice FOREIGN KEY (SaleInvoiceId) REFERENCES dbo.SaleInvoice(Id)
    );
END
");

            Sql(@"
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PatientVisit_VisitId' AND object_id = OBJECT_ID(N'dbo.PatientVisit'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_PatientVisit_VisitId
        ON dbo.PatientVisit (VisitId)
        WHERE IsActive = 1 AND VisitId IS NOT NULL AND VisitId <> '';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PatientVisit_PatientId' AND object_id = OBJECT_ID(N'dbo.PatientVisit'))
    CREATE NONCLUSTERED INDEX IX_PatientVisit_PatientId
        ON dbo.PatientVisit (PatientId, VisitDateTime DESC)
        INCLUDE (VisitId, SaleInvoiceId, VisitStatus, IsActive);
");

            Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.SaleInvoice') AND name = N'PatientVisitId'
)
    ALTER TABLE dbo.SaleInvoice ADD PatientVisitId BIGINT NULL;
");

            Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SaleInvoice_PatientVisit'
)
    ALTER TABLE dbo.SaleInvoice
        ADD CONSTRAINT FK_SaleInvoice_PatientVisit
        FOREIGN KEY (PatientVisitId) REFERENCES dbo.PatientVisit(PatientVisitId);
");

            Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TestRequestDetails') AND name = N'PatientVisitId'
)
    ALTER TABLE dbo.TestRequestDetails ADD PatientVisitId BIGINT NULL;
");

            Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TestRequestDetails_PatientVisit'
)
    ALTER TABLE dbo.TestRequestDetails
        ADD CONSTRAINT FK_TestRequestDetails_PatientVisit
        FOREIGN KEY (PatientVisitId) REFERENCES dbo.PatientVisit(PatientVisitId);
");

            Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.RadiologyRequestDetail') AND name = N'PatientVisitId'
)
    ALTER TABLE dbo.RadiologyRequestDetail ADD PatientVisitId BIGINT NULL;
");

            Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RadiologyRequestDetail_PatientVisit'
)
    ALTER TABLE dbo.RadiologyRequestDetail
        ADD CONSTRAINT FK_RadiologyRequestDetail_PatientVisit
        FOREIGN KEY (PatientVisitId) REFERENCES dbo.PatientVisit(PatientVisitId);
");

            Sql(@"
INSERT INTO dbo.PatientVisit (
    PatientId, VisitId, VisitDateTime, SaleInvoiceId, VisitStatus,
    CreatedBy, CreatedOn, ModifiedBy, ModifiedOn, IsActive
)
SELECT
    p.Id,
    p.VisitId,
    ISNULL(p.CreatedOn, GETDATE()),
    NULL,
    2,
    ISNULL(NULLIF(LTRIM(RTRIM(p.CreatedBy)), ''), 'migration'),
    ISNULL(p.CreatedOn, GETDATE()),
    ISNULL(NULLIF(LTRIM(RTRIM(p.CreatedBy)), ''), 'migration'),
    ISNULL(p.CreatedOn, GETDATE()),
    1
FROM dbo.PatientDetails p
WHERE p.VisitId IS NOT NULL
  AND LTRIM(RTRIM(p.VisitId)) <> ''
  AND NOT EXISTS (
      SELECT 1 FROM dbo.PatientVisit v
      WHERE v.PatientId = p.Id
        AND v.VisitId = p.VisitId
  );
");
        }

        public override void Down()
        {
            Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RadiologyRequestDetail_PatientVisit')
    ALTER TABLE dbo.RadiologyRequestDetail DROP CONSTRAINT FK_RadiologyRequestDetail_PatientVisit;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.RadiologyRequestDetail') AND name = N'PatientVisitId')
    ALTER TABLE dbo.RadiologyRequestDetail DROP COLUMN PatientVisitId;
");

            Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TestRequestDetails_PatientVisit')
    ALTER TABLE dbo.TestRequestDetails DROP CONSTRAINT FK_TestRequestDetails_PatientVisit;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TestRequestDetails') AND name = N'PatientVisitId')
    ALTER TABLE dbo.TestRequestDetails DROP COLUMN PatientVisitId;
");

            Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SaleInvoice_PatientVisit')
    ALTER TABLE dbo.SaleInvoice DROP CONSTRAINT FK_SaleInvoice_PatientVisit;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.SaleInvoice') AND name = N'PatientVisitId')
    ALTER TABLE dbo.SaleInvoice DROP COLUMN PatientVisitId;
");

            Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PatientVisit_PatientId' AND object_id = OBJECT_ID(N'dbo.PatientVisit'))
    DROP INDEX IX_PatientVisit_PatientId ON dbo.PatientVisit;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PatientVisit_VisitId' AND object_id = OBJECT_ID(N'dbo.PatientVisit'))
    DROP INDEX UX_PatientVisit_VisitId ON dbo.PatientVisit;
IF OBJECT_ID(N'dbo.PatientVisit', N'U') IS NOT NULL
    DROP TABLE dbo.PatientVisit;
");
        }
    }
}
