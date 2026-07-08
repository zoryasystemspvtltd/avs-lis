-- =============================================================================
-- Role-based permission matrix for Technician and Doctor (DXI800)
-- Idempotent: replaces module mappings for these roles on each run.
-- UI menus require access bitmask 63 (all flags) per module.
-- =============================================================================
USE ZoryaLMS;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @RoleTech NVARCHAR(128) = N'2d051b02-9289-45ef-8a07-b902b0fea88f';
DECLARE @RoleDoc NVARCHAR(128) = N'a20db0ae-7b61-4c17-af97-ba35c6067e97';
DECLARE @AppId INT = (SELECT TOP 1 Id FROM ClientApplication WHERE AccessKey = N'DXI800');
IF @AppId IS NULL
    SET @AppId = (SELECT TOP 1 Id FROM ClientApplication WHERE AccessKey = N'DUMMY');

IF @AppId IS NULL
BEGIN
    RAISERROR('ClientApplication DXI800/DUMMY not found.', 16, 1);
    RETURN;
END

BEGIN TRANSACTION;

-- Technician: laboratory workflow only
DELETE FROM RoleModuleMappings WHERE RoleId = @RoleTech AND ApplicationId = @AppId;

INSERT INTO RoleModuleMappings (CanAdd, CanEdit, CanAuthorize, CanDelete, CanView, CanReject, ModuleId, RoleId, ApplicationId)
SELECT 1, 1, 1, 1, 1, 1, um.Id, @RoleTech, @AppId
FROM UserModules um
WHERE um.ApplicationId = @AppId
  AND um.Name IN (N'Samples', N'SampleCollection', N'SampleReceiving', N'Reports');

-- Doctor: approvals and reports only (no sample collection / radiology entry)
DELETE FROM RoleModuleMappings WHERE RoleId = @RoleDoc AND ApplicationId = @AppId;

INSERT INTO RoleModuleMappings (CanAdd, CanEdit, CanAuthorize, CanDelete, CanView, CanReject, ModuleId, RoleId, ApplicationId)
SELECT 1, 1, 1, 1, 1, 1, um.Id, @RoleDoc, @AppId
FROM UserModules um
WHERE um.ApplicationId = @AppId
  AND um.Name IN (N'DoctorsApprovals', N'Reports', N'RadiologyDoctorApprovals', N'RadiologyReports');

COMMIT TRANSACTION;

PRINT 'Role-based permissions applied for Technician and Doctor.';
GO
