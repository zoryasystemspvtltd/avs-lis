-- Additive Report Layout Configuration (Phase 1: one active layout per ReportType).
-- Prefer EF migration for teammates after pull:
--   Update-Database -ProjectName LIS.DataAccess -StartUpProjectName Lis.Api -ConfigurationTypeName LIS.DataAccess.Migrations.Configuration
-- Migration Id: 202609120130000_ReportLayoutConfiguration
-- This script remains an ops/emergency idempotent helper.
USE ZoryaLMS;
GO

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ReportLayoutConfiguration')
BEGIN
    CREATE TABLE dbo.ReportLayoutConfiguration
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReportLayoutConfiguration PRIMARY KEY,
        ReportType NVARCHAR(40) NOT NULL,
        PageSize NVARCHAR(20) NOT NULL CONSTRAINT DF_ReportLayout_PageSize DEFAULT (N'A4'),
        Orientation NVARCHAR(20) NOT NULL CONSTRAINT DF_ReportLayout_Orientation DEFAULT (N'Portrait'),
        HeaderHeightMm DECIMAL(9,2) NOT NULL,
        FooterHeightMm DECIMAL(9,2) NOT NULL,
        LeftMarginMm DECIMAL(9,2) NOT NULL,
        RightMarginMm DECIMAL(9,2) NOT NULL,
        DoctorSignatureEnabled BIT NOT NULL CONSTRAINT DF_ReportLayout_DocSigEnabled DEFAULT (1),
        DoctorSignatureHorizontal NVARCHAR(20) NOT NULL CONSTRAINT DF_ReportLayout_DocSigH DEFAULT (N'Right'),
        DoctorSignatureVertical NVARCHAR(20) NOT NULL CONSTRAINT DF_ReportLayout_DocSigV DEFAULT (N'Bottom'),
        DoctorSignatureWidthMm DECIMAL(9,2) NOT NULL CONSTRAINT DF_ReportLayout_DocSigW DEFAULT (50),
        DoctorSignatureHeightMm DECIMAL(9,2) NOT NULL CONSTRAINT DF_ReportLayout_DocSigHt DEFAULT (14),
        TechnicianSignatureEnabled BIT NOT NULL CONSTRAINT DF_ReportLayout_TechSigEnabled DEFAULT (0),
        TechnicianSignatureHorizontal NVARCHAR(20) NOT NULL CONSTRAINT DF_ReportLayout_TechSigH DEFAULT (N'Left'),
        TechnicianSignatureVertical NVARCHAR(20) NOT NULL CONSTRAINT DF_ReportLayout_TechSigV DEFAULT (N'Bottom'),
        TechnicianSignatureWidthMm DECIMAL(9,2) NOT NULL CONSTRAINT DF_ReportLayout_TechSigW DEFAULT (50),
        TechnicianSignatureHeightMm DECIMAL(9,2) NOT NULL CONSTRAINT DF_ReportLayout_TechSigHt DEFAULT (14),
        IsActive BIT NOT NULL CONSTRAINT DF_ReportLayout_IsActive DEFAULT (1),
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_ReportLayout_CreatedOn DEFAULT (GETDATE()),
        CreatedBy NVARCHAR(80) NULL,
        ModifiedOn DATETIME NOT NULL CONSTRAINT DF_ReportLayout_ModifiedOn DEFAULT (GETDATE()),
        ModifiedBy NVARCHAR(80) NULL
    );

    CREATE UNIQUE INDEX UX_ReportLayoutConfiguration_ReportType
        ON dbo.ReportLayoutConfiguration (ReportType);
END
GO

-- Seed defaults matching current production print CSS (idempotent).
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
GO

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
GO

-- Module + Administrator permissions only (print layout is enriched on report APIs).
-- Full Can* bits for Administrator (matches NotificationConfiguration pattern).
-- For RoleMenuPermission overlay, also run: add-report-layout-configuration-rbac.sql
DECLARE @AppId INT = (SELECT TOP 1 Id FROM ClientApplication WHERE AccessKey = N'DXI800');
IF @AppId IS NULL
    SET @AppId = (SELECT TOP 1 Id FROM ClientApplication ORDER BY Id);

IF @AppId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM UserModules WHERE Name = N'ReportLayoutConfiguration' AND ApplicationId = @AppId)
BEGIN
    INSERT INTO UserModules (Name, Url, [Order], ApplicationId, IsSyatem)
    VALUES (N'ReportLayoutConfiguration', N'/report-layout-configuration', 56, @AppId, 1);
END

DECLARE @ModuleId INT = (
    SELECT TOP 1 Id FROM UserModules
    WHERE Name = N'ReportLayoutConfiguration' AND ApplicationId = @AppId);
DECLARE @AdminRole NVARCHAR(128) = (
    SELECT TOP 1 Id FROM AspNetRoles WHERE Name = N'Administrator');

IF @ModuleId IS NOT NULL AND @AdminRole IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM RoleModuleMappings
        WHERE ModuleId = @ModuleId AND RoleId = @AdminRole AND ApplicationId = @AppId)
    BEGIN
        INSERT INTO RoleModuleMappings
            (CanAdd, CanEdit, CanAuthorize, CanDelete, CanView, CanReject, ModuleId, RoleId, ApplicationId)
        VALUES (1, 1, 1, 1, 1, 1, @ModuleId, @AdminRole, @AppId);
    END
    ELSE
    BEGIN
        UPDATE RoleModuleMappings
        SET CanAdd = 1, CanEdit = 1, CanAuthorize = 1, CanDelete = 1, CanView = 1, CanReject = 1
        WHERE ModuleId = @ModuleId AND RoleId = @AdminRole AND ApplicationId = @AppId
          AND (CanAdd = 0 OR CanEdit = 0 OR CanAuthorize = 0 OR CanDelete = 0 OR CanView = 0 OR CanReject = 0);
    END
END

PRINT 'ReportLayoutConfiguration ready.';
GO
