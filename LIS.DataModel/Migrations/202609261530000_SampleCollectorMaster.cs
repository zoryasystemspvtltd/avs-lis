namespace LIS.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class SampleCollectorMaster : DbMigration
    {
        public override void Up()
        {
            Sql(@"
IF OBJECT_ID(N'dbo.SampleCollectorMaster', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SampleCollectorMaster (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SampleCollectorMaster PRIMARY KEY,
        Code NVARCHAR(20) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Phone NVARCHAR(15) NULL,
        IsActive BIT NOT NULL,
        CreatedBy NVARCHAR(MAX) NULL,
        CreatedOn DATETIME NOT NULL,
        ModifiedBy NVARCHAR(MAX) NULL,
        ModifiedOn DATETIME NOT NULL
    );
END

IF COL_LENGTH('dbo.SaleInvoice', 'CollectorId') IS NULL
    ALTER TABLE dbo.SaleInvoice ADD CollectorId INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SaleInvoice_CollectorId' AND object_id = OBJECT_ID(N'dbo.SaleInvoice'))
    CREATE INDEX IX_SaleInvoice_CollectorId ON dbo.SaleInvoice(CollectorId);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SaleInvoice_SampleCollectorMaster')
    ALTER TABLE dbo.SaleInvoice ADD CONSTRAINT FK_SaleInvoice_SampleCollectorMaster
        FOREIGN KEY (CollectorId) REFERENCES dbo.SampleCollectorMaster(Id);

IF OBJECT_ID(N'dbo.RoleMenuPermission', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenuPermission WHERE MenuKey = N'MASTER_SAMPLE_COLLECTOR')
BEGIN
    INSERT INTO dbo.RoleMenuPermission
        (RoleId, ModuleId, MenuKey, CanView, CanAdd, CanEdit, CanDelete, CanAuthorize, CanReject, IsActive, ApplicationId, CreatedBy, CreatedOn)
    SELECT RoleId, ModuleId, N'MASTER_SAMPLE_COLLECTOR', CanView, CanAdd, CanEdit, CanDelete, CanAuthorize, CanReject, IsActive, ApplicationId, N'step2-collector', GETDATE()
    FROM dbo.RoleMenuPermission
    WHERE MenuKey = N'MASTER_REFERRAL_DOCTOR';
END
");
        }

        public override void Down()
        {
        }
    }
}
