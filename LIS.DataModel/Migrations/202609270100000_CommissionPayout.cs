namespace LIS.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class CommissionPayout : DbMigration
    {
        public override void Up()
        {
            Sql(@"
IF OBJECT_ID(N'dbo.CommissionPayout', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CommissionPayout (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CommissionPayout PRIMARY KEY,
        PayoutNo NVARCHAR(30) NOT NULL,
        RecipientType INT NOT NULL,
        RecipientId INT NOT NULL,
        FromDate DATETIME NOT NULL,
        ToDate DATETIME NOT NULL,
        TotalAmount DECIMAL(18,2) NOT NULL,
        CreatedBy NVARCHAR(MAX) NULL,
        CreatedOn DATETIME NOT NULL
    );
    CREATE UNIQUE INDEX UX_CommissionPayout_Recipient_From
        ON dbo.CommissionPayout(RecipientType, RecipientId, FromDate);
END

IF OBJECT_ID(N'dbo.CommissionPayoutDetail', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CommissionPayoutDetail (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CommissionPayoutDetail PRIMARY KEY,
        CommissionPayoutId BIGINT NOT NULL,
        RecipientType INT NOT NULL,
        RecipientId INT NOT NULL,
        SaleInvoiceId BIGINT NOT NULL,
        InvoiceNo NVARCHAR(50) NULL,
        InvoiceDate DATETIME NOT NULL,
        CommissionBase DECIMAL(18,2) NOT NULL,
        CommissionPercent DECIMAL(5,2) NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_CommissionPayoutDetail_CommissionPayout
            FOREIGN KEY (CommissionPayoutId) REFERENCES dbo.CommissionPayout(Id)
    );
    CREATE UNIQUE INDEX UX_CommissionPayoutDetail_Recipient_Invoice
        ON dbo.CommissionPayoutDetail(RecipientType, RecipientId, SaleInvoiceId);
END

DECLARE @AppId INT = (SELECT TOP 1 Id FROM ClientApplication WHERE AccessKey = N'DXI800');
IF @AppId IS NULL
    SET @AppId = (SELECT TOP 1 Id FROM ClientApplication ORDER BY Id);

DECLARE @AdminRole NVARCHAR(128) = (SELECT TOP 1 Id FROM AspNetRoles WHERE Name = N'Administrator');

IF @AppId IS NOT NULL AND @AdminRole IS NOT NULL
BEGIN
    DECLARE @ReportsModule BIGINT = (
        SELECT TOP 1 Id FROM dbo.UserModules WHERE Name = N'Reports' AND ApplicationId = @AppId);

    IF @ReportsModule IS NOT NULL AND OBJECT_ID(N'dbo.RoleMenuPermission', N'U') IS NOT NULL
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM dbo.RoleMenuPermission
            WHERE RoleId = @AdminRole AND ApplicationId = @AppId AND MenuKey = N'REPORT_DOCTOR_COMMISSION')
        BEGIN
            INSERT INTO dbo.RoleMenuPermission
                (RoleId, ModuleId, MenuKey, CanView, CanAdd, CanEdit, CanDelete, CanAuthorize, CanReject, IsActive, ApplicationId, CreatedBy, CreatedOn)
            VALUES
                (@AdminRole, @ReportsModule, N'REPORT_DOCTOR_COMMISSION', 1, 1, 1, 1, 1, 1, 1, @AppId, N'stage4-commission-report', GETDATE());
        END

        IF NOT EXISTS (
            SELECT 1 FROM dbo.RoleMenuPermission
            WHERE RoleId = @AdminRole AND ApplicationId = @AppId AND MenuKey = N'REPORT_COLLECTOR_COMMISSION')
        BEGIN
            INSERT INTO dbo.RoleMenuPermission
                (RoleId, ModuleId, MenuKey, CanView, CanAdd, CanEdit, CanDelete, CanAuthorize, CanReject, IsActive, ApplicationId, CreatedBy, CreatedOn)
            VALUES
                (@AdminRole, @ReportsModule, N'REPORT_COLLECTOR_COMMISSION', 1, 1, 1, 1, 1, 1, 1, @AppId, N'stage4-commission-report', GETDATE());
        END
    END

    IF NOT EXISTS (
        SELECT 1 FROM dbo.UserModules WHERE Name = N'CommissionPayout' AND ApplicationId = @AppId)
    BEGIN
        INSERT INTO dbo.UserModules (Name, Url, [Order], ApplicationId, IsSyatem)
        VALUES (N'CommissionPayout', N'/commission-payouts', 210, @AppId, 1);
    END

    DECLARE @PayoutModule BIGINT = (
        SELECT TOP 1 Id FROM dbo.UserModules WHERE Name = N'CommissionPayout' AND ApplicationId = @AppId);

    IF @PayoutModule IS NOT NULL
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM dbo.RoleModuleMappings
            WHERE ModuleId = @PayoutModule AND RoleId = @AdminRole AND ApplicationId = @AppId)
        BEGIN
            INSERT INTO dbo.RoleModuleMappings
                (CanAdd, CanEdit, CanAuthorize, CanDelete, CanView, CanReject, ModuleId, RoleId, ApplicationId)
            VALUES (1, 1, 1, 1, 1, 1, @PayoutModule, @AdminRole, @AppId);
        END

        IF OBJECT_ID(N'dbo.RoleMenuPermission', N'U') IS NOT NULL
        AND NOT EXISTS (
            SELECT 1 FROM dbo.RoleMenuPermission
            WHERE RoleId = @AdminRole AND ApplicationId = @AppId AND MenuKey = N'COMMISSION_PAYOUT')
        BEGIN
            INSERT INTO dbo.RoleMenuPermission
                (RoleId, ModuleId, MenuKey, CanView, CanAdd, CanEdit, CanDelete, CanAuthorize, CanReject, IsActive, ApplicationId, CreatedBy, CreatedOn)
            VALUES
                (@AdminRole, @PayoutModule, N'COMMISSION_PAYOUT', 1, 1, 1, 1, 1, 1, 1, @AppId, N'stage5-commission-payout', GETDATE());
        END
    END
END
");
        }

        public override void Down()
        {
        }
    }
}
