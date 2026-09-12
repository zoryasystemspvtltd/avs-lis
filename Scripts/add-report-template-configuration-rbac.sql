-- Twin of Lis.Api migration 202609121601000_ReportTemplateConfigurationRbac
-- Aligns with ReportLayoutConfiguration RBAC seed (ModuleId required on RoleMenuPermission).
DECLARE @AppId INT = (SELECT TOP 1 Id FROM ClientApplication WHERE AccessKey = N'DXI800');
IF @AppId IS NULL SET @AppId = (SELECT TOP 1 Id FROM ClientApplication ORDER BY Id);
DECLARE @AdminRole NVARCHAR(128) = (SELECT TOP 1 Id FROM AspNetRoles WHERE Name = N'Administrator');

IF @AppId IS NULL OR @AdminRole IS NULL
BEGIN
    RAISERROR('ClientApplication or Administrator role missing — cannot seed ReportTemplateConfiguration RBAC.', 16, 1);
END
ELSE
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM dbo.UserModules
        WHERE Name = N'ReportTemplateConfiguration' AND ApplicationId = @AppId)
    BEGIN
        INSERT INTO dbo.UserModules (Name, Url, [Order], ApplicationId, IsSyatem)
        VALUES (N'ReportTemplateConfiguration', N'/report-template-configuration', 57, @AppId, 1);
        PRINT 'Inserted UserModules.ReportTemplateConfiguration';
    END
    ELSE
    BEGIN
        UPDATE dbo.UserModules
        SET Url = N'/report-template-configuration', [Order] = 57
        WHERE Name = N'ReportTemplateConfiguration' AND ApplicationId = @AppId;
        PRINT 'UserModules.ReportTemplateConfiguration already present (url/order ensured).';
    END

    DECLARE @ModuleId BIGINT = (
        SELECT TOP 1 Id FROM dbo.UserModules
        WHERE Name = N'ReportTemplateConfiguration' AND ApplicationId = @AppId);

    IF @ModuleId IS NULL
        RAISERROR('ReportTemplateConfiguration module id missing after upsert.', 16, 1);

    IF NOT EXISTS (
        SELECT 1 FROM dbo.RoleModuleMappings
        WHERE RoleId = @AdminRole AND ModuleId = @ModuleId)
    BEGIN
        INSERT INTO dbo.RoleModuleMappings
            (RoleId, ModuleId, CanAdd, CanEdit, CanDelete, CanView, CanAuthorize, CanReject)
        VALUES (@AdminRole, @ModuleId, 1, 1, 1, 1, 1, 1);
        PRINT 'Inserted Administrator RoleModuleMappings for ReportTemplateConfiguration';
    END
    ELSE
    BEGIN
        UPDATE dbo.RoleModuleMappings
        SET CanAdd = 1, CanEdit = 1, CanDelete = 1, CanView = 1, CanAuthorize = 1, CanReject = 1
        WHERE RoleId = @AdminRole AND ModuleId = @ModuleId
          AND (CanAdd = 0 OR CanEdit = 0 OR CanDelete = 0 OR CanView = 0 OR CanAuthorize = 0 OR CanReject = 0);
        PRINT 'Administrator RoleModuleMappings for ReportTemplateConfiguration ensured.';
    END

    IF OBJECT_ID(N'dbo.RoleMenuPermission', N'U') IS NOT NULL
    BEGIN
        IF NOT EXISTS (
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
                 1, @AppId, N'rbac-seed', GETUTCDATE(), N'rbac-seed', GETUTCDATE());
            PRINT 'Inserted Administrator RoleMenuPermission SETUP_REPORT_TEMPLATE_CONFIGURATION';
        END
        ELSE
        BEGIN
            UPDATE dbo.RoleMenuPermission
            SET CanView = 1, CanAdd = 1, CanEdit = 1, CanDelete = 1, CanAuthorize = 1, CanReject = 1,
                IsActive = 1, ModifiedBy = N'rbac-seed', ModifiedOn = GETUTCDATE()
            WHERE RoleId = @AdminRole
              AND ApplicationId = @AppId
              AND ModuleId = @ModuleId
              AND MenuKey = N'SETUP_REPORT_TEMPLATE_CONFIGURATION'
              AND (
                    CanView = 0 OR CanAdd = 0 OR CanEdit = 0 OR CanDelete = 0
                 OR CanAuthorize = 0 OR CanReject = 0 OR IsActive = 0
                  );
            PRINT 'Administrator RoleMenuPermission SETUP_REPORT_TEMPLATE_CONFIGURATION already present.';
        END
    END
    ELSE
        PRINT 'RoleMenuPermission table missing — module mapping only (menu overlay not seeded).';
END
GO

/* Verification */
SELECT 'UserModules' AS Src, Id, Name, Url, [Order], ApplicationId
FROM dbo.UserModules WHERE Name = N'ReportTemplateConfiguration';

SELECT 'RoleModuleMappings' AS Src, rm.RoleId, rm.ModuleId, rm.CanView, rm.CanAdd, rm.CanEdit
FROM dbo.RoleModuleMappings rm
INNER JOIN dbo.UserModules um ON um.Id = rm.ModuleId
WHERE um.Name = N'ReportTemplateConfiguration';

SELECT 'RoleMenuPermission' AS Src, p.RoleId, p.ModuleId, p.MenuKey, p.CanView, p.IsActive, p.ApplicationId
FROM dbo.RoleMenuPermission p
WHERE p.MenuKey = N'SETUP_REPORT_TEMPLATE_CONFIGURATION';
GO
