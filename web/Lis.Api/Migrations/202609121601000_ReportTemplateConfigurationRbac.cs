namespace Lis.Api.Migrations
{
    using System.Data.Entity.Migrations;

    /// <summary>
    /// RBAC: Report Template Configuration (Admin Setup). Phase 1 management only.
    /// </summary>
    public partial class ReportTemplateConfigurationRbac : DbMigration
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
        WHERE Name = N'ReportTemplateConfiguration' AND ApplicationId = @AppId)
    BEGIN
        INSERT INTO dbo.UserModules (Name, Url, [Order], ApplicationId, IsSyatem)
        VALUES (N'ReportTemplateConfiguration', N'/report-template-configuration', 57, @AppId, 1);
    END
    ELSE
    BEGIN
        UPDATE dbo.UserModules
        SET Url = N'/report-template-configuration', [Order] = 57
        WHERE Name = N'ReportTemplateConfiguration' AND ApplicationId = @AppId;
    END

    DECLARE @ModuleId BIGINT = (
        SELECT TOP 1 Id FROM dbo.UserModules
        WHERE Name = N'ReportTemplateConfiguration' AND ApplicationId = @AppId);

    IF @ModuleId IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM dbo.RoleModuleMappings
        WHERE RoleId = @AdminRole AND ModuleId = @ModuleId)
    BEGIN
        INSERT INTO dbo.RoleModuleMappings
            (RoleId, ModuleId, CanAdd, CanEdit, CanDelete, CanView, CanAuthorize, CanReject)
        VALUES (@AdminRole, @ModuleId, 1, 1, 1, 1, 1, 1);
    END

    IF OBJECT_ID(N'dbo.RoleMenuPermission', N'U') IS NOT NULL
       AND @ModuleId IS NOT NULL
       AND NOT EXISTS (
        SELECT 1 FROM dbo.RoleMenuPermission
        WHERE RoleId = @AdminRole
          AND ApplicationId = @AppId
          AND ModuleId = @ModuleId
          AND MenuKey = N'SETUP_REPORT_TEMPLATE_CONFIGURATION')
    BEGIN
        INSERT INTO dbo.RoleMenuPermission
            (RoleId, ModuleId, MenuKey, CanView, CanAdd, CanEdit, CanDelete, CanAuthorize, CanReject,
             IsActive, ApplicationId, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
        VALUES
            (@AdminRole, @ModuleId, N'SETUP_REPORT_TEMPLATE_CONFIGURATION',
             1, 1, 1, 1, 1, 1,
             1, @AppId, N'ef-migration', GETUTCDATE(), N'ef-migration', GETUTCDATE());
    END
END
");
        }

        public override void Down()
        {
            Sql(@"
DELETE FROM dbo.RoleMenuPermission WHERE MenuKey = N'SETUP_REPORT_TEMPLATE_CONFIGURATION';
DELETE rm FROM dbo.RoleModuleMappings rm
INNER JOIN dbo.UserModules um ON um.Id = rm.ModuleId
WHERE um.Name = N'ReportTemplateConfiguration';
DELETE FROM dbo.UserModules WHERE Name = N'ReportTemplateConfiguration';
");
        }
    }
}
