/*
  RBAC registration only for Report Layout Configuration.
  Prefer EF migration for teammates after pull:
    Update-Database -ProjectName Lis.Api -StartUpProjectName Lis.Api -ConfigurationTypeName Lis.Api.Migrations.Configuration
  Migration Id: 202609120131000_ReportLayoutConfigurationRbac

  This script remains an ops/emergency idempotent helper.
*/
USE ZoryaLMS;
GO

SET NOCOUNT ON;

DECLARE @AppId INT = (SELECT TOP 1 Id FROM ClientApplication WHERE AccessKey = N'DXI800');
IF @AppId IS NULL
    SET @AppId = (SELECT TOP 1 Id FROM ClientApplication ORDER BY Id);

IF @AppId IS NULL
BEGIN
    RAISERROR('ClientApplication not found.', 16, 1);
    RETURN;
END

DECLARE @AdminRole NVARCHAR(128) = (SELECT TOP 1 Id FROM AspNetRoles WHERE Name = N'Administrator');
IF @AdminRole IS NULL
BEGIN
    RAISERROR('Administrator role not found.', 16, 1);
    RETURN;
END

/* ---- UserModules (no duplicate) ---- */
IF NOT EXISTS (
    SELECT 1 FROM dbo.UserModules
    WHERE Name = N'ReportLayoutConfiguration' AND ApplicationId = @AppId)
BEGIN
    INSERT INTO dbo.UserModules (Name, Url, [Order], ApplicationId, IsSyatem)
    VALUES (N'ReportLayoutConfiguration', N'/report-layout-configuration', 56, @AppId, 1);
    PRINT 'Inserted UserModules.ReportLayoutConfiguration';
END
ELSE
BEGIN
    UPDATE dbo.UserModules
    SET Url = N'/report-layout-configuration',
        [Order] = 56
    WHERE Name = N'ReportLayoutConfiguration'
      AND ApplicationId = @AppId
      AND (Url <> N'/report-layout-configuration' OR [Order] <> 56);
    PRINT 'UserModules.ReportLayoutConfiguration already present (url/order ensured).';
END

DECLARE @ModuleId BIGINT = (
    SELECT TOP 1 Id FROM dbo.UserModules
    WHERE Name = N'ReportLayoutConfiguration' AND ApplicationId = @AppId);

IF @ModuleId IS NULL
BEGIN
    RAISERROR('ReportLayoutConfiguration module id missing after upsert.', 16, 1);
    RETURN;
END

/* ---- Administrator RoleModuleMappings (full bits; upsert) ---- */
IF NOT EXISTS (
    SELECT 1 FROM dbo.RoleModuleMappings
    WHERE ModuleId = @ModuleId AND RoleId = @AdminRole AND ApplicationId = @AppId)
BEGIN
    INSERT INTO dbo.RoleModuleMappings
        (CanAdd, CanEdit, CanAuthorize, CanDelete, CanView, CanReject, ModuleId, RoleId, ApplicationId)
    VALUES (1, 1, 1, 1, 1, 1, @ModuleId, @AdminRole, @AppId);
    PRINT 'Inserted Administrator RoleModuleMappings for ReportLayoutConfiguration';
END
ELSE
BEGIN
    UPDATE dbo.RoleModuleMappings
    SET CanAdd = 1, CanEdit = 1, CanAuthorize = 1, CanDelete = 1, CanView = 1, CanReject = 1
    WHERE ModuleId = @ModuleId AND RoleId = @AdminRole AND ApplicationId = @AppId
      AND (CanAdd = 0 OR CanEdit = 0 OR CanAuthorize = 0 OR CanDelete = 0 OR CanView = 0 OR CanReject = 0);
    PRINT 'Administrator RoleModuleMappings for ReportLayoutConfiguration ensured (full Can*).';
END

/* ---- Administrator RoleMenuPermission (enterprise key; no duplicate) ---- */
IF OBJECT_ID(N'dbo.RoleMenuPermission', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM dbo.RoleMenuPermission
        WHERE RoleId = @AdminRole
          AND ApplicationId = @AppId
          AND ModuleId = @ModuleId
          AND MenuKey = N'SETUP_REPORT_LAYOUT_CONFIGURATION')
    BEGIN
        INSERT INTO dbo.RoleMenuPermission
            (RoleId, ModuleId, MenuKey, CanView, CanAdd, CanEdit, CanDelete, CanAuthorize, CanReject,
             IsActive, ApplicationId, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
        VALUES
            (@AdminRole, @ModuleId, N'SETUP_REPORT_LAYOUT_CONFIGURATION',
             1, 1, 1, 1, 1, 1,
             1, @AppId, N'rbac-seed', GETUTCDATE(), N'rbac-seed', GETUTCDATE());
        PRINT 'Inserted Administrator RoleMenuPermission SETUP_REPORT_LAYOUT_CONFIGURATION';
    END
    ELSE
    BEGIN
        UPDATE dbo.RoleMenuPermission
        SET CanView = 1, CanAdd = 1, CanEdit = 1, CanDelete = 1, CanAuthorize = 1, CanReject = 1,
            IsActive = 1, ModifiedBy = N'rbac-seed', ModifiedOn = GETUTCDATE()
        WHERE RoleId = @AdminRole
          AND ApplicationId = @AppId
          AND ModuleId = @ModuleId
          AND MenuKey = N'SETUP_REPORT_LAYOUT_CONFIGURATION'
          AND (
                CanView = 0 OR CanAdd = 0 OR CanEdit = 0 OR CanDelete = 0
             OR CanAuthorize = 0 OR CanReject = 0 OR IsActive = 0
              );
        PRINT 'Administrator RoleMenuPermission SETUP_REPORT_LAYOUT_CONFIGURATION already present.';
    END

    /* Remove legacy duplicate key if both exist (keep enterprise key only). */
    DELETE FROM dbo.RoleMenuPermission
    WHERE RoleId = @AdminRole
      AND ApplicationId = @AppId
      AND ModuleId = @ModuleId
      AND MenuKey = N'setup.reportLayoutConfiguration';
END
ELSE
BEGIN
    PRINT 'RoleMenuPermission table missing — module mapping only (menu overlay not seeded).';
END

/* ---- Ensure non-Administrator roles do NOT receive this module via this script ---- */
/* (Do not delete existing intentional grants; only report.) */
SELECT
    r.Name AS RoleName,
    COUNT(*) AS MappingCount
FROM dbo.RoleModuleMappings rm
INNER JOIN dbo.AspNetRoles r ON r.Id = rm.RoleId
WHERE rm.ModuleId = @ModuleId
  AND r.Name <> N'Administrator'
GROUP BY r.Name;

PRINT 'Report Layout Configuration RBAC registration complete.';
GO

/* ---- Verification ---- */
SET NOCOUNT ON;
SELECT 'UserModules' AS CheckName, COUNT(*) AS Cnt
FROM dbo.UserModules WHERE Name = N'ReportLayoutConfiguration';

SELECT 'AdminModuleMap' AS CheckName, COUNT(*) AS Cnt
FROM dbo.RoleModuleMappings rm
INNER JOIN dbo.AspNetRoles r ON r.Id = rm.RoleId
INNER JOIN dbo.UserModules um ON um.Id = rm.ModuleId
WHERE um.Name = N'ReportLayoutConfiguration'
  AND r.Name = N'Administrator'
  AND rm.CanView = 1 AND rm.CanEdit = 1 AND rm.CanAdd = 1
  AND rm.CanDelete = 1 AND rm.CanAuthorize = 1 AND rm.CanReject = 1;

SELECT 'AdminMenuKey' AS CheckName, COUNT(*) AS Cnt
FROM dbo.RoleMenuPermission p
INNER JOIN dbo.AspNetRoles r ON r.Id = p.RoleId
WHERE r.Name = N'Administrator'
  AND p.MenuKey = N'SETUP_REPORT_LAYOUT_CONFIGURATION'
  AND p.IsActive = 1 AND p.CanView = 1;

SELECT 'DuplicateModules' AS CheckName, COUNT(*) AS Cnt
FROM (
    SELECT ApplicationId, COUNT(*) AS C
    FROM dbo.UserModules
    WHERE Name = N'ReportLayoutConfiguration'
    GROUP BY ApplicationId
    HAVING COUNT(*) > 1
) d;

SELECT 'NonAdminModuleMaps' AS CheckName, COUNT(*) AS Cnt
FROM dbo.RoleModuleMappings rm
INNER JOIN dbo.AspNetRoles r ON r.Id = rm.RoleId
INNER JOIN dbo.UserModules um ON um.Id = rm.ModuleId
WHERE um.Name = N'ReportLayoutConfiguration'
  AND r.Name <> N'Administrator';
GO
