namespace Lis.Api.Migrations
{
    using System.Data.Entity.Migrations;

    /// <summary>
    /// RBAC seed: Report Layout Configuration under Setup for Administrator.
    /// Idempotent — safe when Scripts/add-report-layout-configuration-rbac.sql already ran.
    /// No identity schema change (reuses prior RoleMenuPermission model snapshot).
    /// </summary>
    public partial class ReportLayoutConfigurationRbac : DbMigration
    {
        public override void Up()
        {
            Sql(@"
DECLARE @AppId INT = (SELECT TOP 1 Id FROM ClientApplication WHERE AccessKey = N'DXI800');
IF @AppId IS NULL
    SET @AppId = (SELECT TOP 1 Id FROM ClientApplication ORDER BY Id);

DECLARE @AdminRole NVARCHAR(128) = (SELECT TOP 1 Id FROM AspNetRoles WHERE Name = N'Administrator');

IF @AppId IS NOT NULL AND @AdminRole IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM dbo.UserModules
        WHERE Name = N'ReportLayoutConfiguration' AND ApplicationId = @AppId)
    BEGIN
        INSERT INTO dbo.UserModules (Name, Url, [Order], ApplicationId, IsSyatem)
        VALUES (N'ReportLayoutConfiguration', N'/report-layout-configuration', 56, @AppId, 1);
    END
    ELSE
    BEGIN
        UPDATE dbo.UserModules
        SET Url = N'/report-layout-configuration', [Order] = 56
        WHERE Name = N'ReportLayoutConfiguration' AND ApplicationId = @AppId
          AND (Url <> N'/report-layout-configuration' OR [Order] <> 56);
    END

    DECLARE @ModuleId BIGINT = (
        SELECT TOP 1 Id FROM dbo.UserModules
        WHERE Name = N'ReportLayoutConfiguration' AND ApplicationId = @AppId);

    IF @ModuleId IS NOT NULL
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM dbo.RoleModuleMappings
            WHERE ModuleId = @ModuleId AND RoleId = @AdminRole AND ApplicationId = @AppId)
        BEGIN
            INSERT INTO dbo.RoleModuleMappings
                (CanAdd, CanEdit, CanAuthorize, CanDelete, CanView, CanReject, ModuleId, RoleId, ApplicationId)
            VALUES (1, 1, 1, 1, 1, 1, @ModuleId, @AdminRole, @AppId);
        END
        ELSE
        BEGIN
            UPDATE dbo.RoleModuleMappings
            SET CanAdd = 1, CanEdit = 1, CanAuthorize = 1, CanDelete = 1, CanView = 1, CanReject = 1
            WHERE ModuleId = @ModuleId AND RoleId = @AdminRole AND ApplicationId = @AppId
              AND (CanAdd = 0 OR CanEdit = 0 OR CanAuthorize = 0 OR CanDelete = 0 OR CanView = 0 OR CanReject = 0);
        END

        IF OBJECT_ID(N'dbo.RoleMenuPermission', N'U') IS NOT NULL
        BEGIN
            IF NOT EXISTS (
                SELECT 1 FROM dbo.RoleMenuPermission
                WHERE RoleId = @AdminRole AND ApplicationId = @AppId AND ModuleId = @ModuleId
                  AND MenuKey = N'SETUP_REPORT_LAYOUT_CONFIGURATION')
            BEGIN
                INSERT INTO dbo.RoleMenuPermission
                    (RoleId, ModuleId, MenuKey, CanView, CanAdd, CanEdit, CanDelete, CanAuthorize, CanReject,
                     IsActive, ApplicationId, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
                VALUES
                    (@AdminRole, @ModuleId, N'SETUP_REPORT_LAYOUT_CONFIGURATION',
                     1, 1, 1, 1, 1, 1,
                     1, @AppId, N'ef-migration', GETUTCDATE(), N'ef-migration', GETUTCDATE());
            END
            ELSE
            BEGIN
                UPDATE dbo.RoleMenuPermission
                SET CanView = 1, CanAdd = 1, CanEdit = 1, CanDelete = 1, CanAuthorize = 1, CanReject = 1,
                    IsActive = 1, ModifiedBy = N'ef-migration', ModifiedOn = GETUTCDATE()
                WHERE RoleId = @AdminRole AND ApplicationId = @AppId AND ModuleId = @ModuleId
                  AND MenuKey = N'SETUP_REPORT_LAYOUT_CONFIGURATION'
                  AND (CanView = 0 OR CanAdd = 0 OR CanEdit = 0 OR CanDelete = 0
                       OR CanAuthorize = 0 OR CanReject = 0 OR IsActive = 0);
            END

            DELETE FROM dbo.RoleMenuPermission
            WHERE RoleId = @AdminRole AND ApplicationId = @AppId AND ModuleId = @ModuleId
              AND MenuKey = N'setup.reportLayoutConfiguration';
        END
    END
END
");
        }

        public override void Down()
        {
            Sql(@"
DECLARE @AppId INT = (SELECT TOP 1 Id FROM ClientApplication WHERE AccessKey = N'DXI800');
IF @AppId IS NULL
    SET @AppId = (SELECT TOP 1 Id FROM ClientApplication ORDER BY Id);

DECLARE @AdminRole NVARCHAR(128) = (SELECT TOP 1 Id FROM AspNetRoles WHERE Name = N'Administrator');
DECLARE @ModuleId BIGINT = (
    SELECT TOP 1 Id FROM dbo.UserModules
    WHERE Name = N'ReportLayoutConfiguration' AND ApplicationId = @AppId);

IF @ModuleId IS NOT NULL AND @AdminRole IS NOT NULL
BEGIN
    IF OBJECT_ID(N'dbo.RoleMenuPermission', N'U') IS NOT NULL
        DELETE FROM dbo.RoleMenuPermission
        WHERE ModuleId = @ModuleId AND RoleId = @AdminRole AND ApplicationId = @AppId
          AND MenuKey IN (N'SETUP_REPORT_LAYOUT_CONFIGURATION', N'setup.reportLayoutConfiguration');

    DELETE FROM dbo.RoleModuleMappings
    WHERE ModuleId = @ModuleId AND RoleId = @AdminRole AND ApplicationId = @AppId;

    IF NOT EXISTS (SELECT 1 FROM dbo.RoleModuleMappings WHERE ModuleId = @ModuleId)
        DELETE FROM dbo.UserModules WHERE Id = @ModuleId;
END
");
        }
    }
}
