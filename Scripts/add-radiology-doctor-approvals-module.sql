-- Adds Radiology Doctor Approvals module for dedicated approval queue.
USE ZoryaLMS;
GO

SET NOCOUNT ON;

DECLARE @AppId INT = (SELECT TOP 1 Id FROM ClientApplication WHERE AccessKey = N'DXI800');
IF @AppId IS NULL
BEGIN
    RAISERROR('ClientApplication DXI800 not found.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM UserModules WHERE Name = N'RadiologyDoctorApprovals' AND ApplicationId = @AppId)
BEGIN
    INSERT INTO UserModules (Name, Url, [Order], ApplicationId, IsSyatem)
    VALUES (N'RadiologyDoctorApprovals', N'/radiology-doctor-approvals', 24, @AppId, 0);
END

DECLARE @ModuleId INT = (SELECT TOP 1 Id FROM UserModules WHERE Name = N'RadiologyDoctorApprovals' AND ApplicationId = @AppId);
DECLARE @RoleAdmin NVARCHAR(128) = (SELECT TOP 1 Id FROM AspNetRoles WHERE Name = N'Administrator');

IF @ModuleId IS NOT NULL AND @RoleAdmin IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM RoleModuleMappings WHERE ModuleId = @ModuleId AND RoleId = @RoleAdmin AND ApplicationId = @AppId)
BEGIN
    INSERT INTO RoleModuleMappings (CanAdd, CanEdit, CanAuthorize, CanDelete, CanView, CanReject, ModuleId, RoleId, ApplicationId)
    VALUES (1, 1, 1, 1, 1, 1, @ModuleId, @RoleAdmin, @AppId);
END

PRINT 'RadiologyDoctorApprovals module ready.';
GO
