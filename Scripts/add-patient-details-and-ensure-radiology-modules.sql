-- Ensures Transaction / Working Board permission modules exist for role assignment:
--   PatientDetails, RadiologyReportEntry, RadiologyDoctorApprovals
-- Grants Administrator full access. Safe to re-run.

SET NOCOUNT ON;

DECLARE @AppId INT = (SELECT TOP 1 Id FROM ClientApplication WHERE AccessKey = N'DXI800');
IF @AppId IS NULL
    SET @AppId = (SELECT TOP 1 Id FROM ClientApplication WHERE AccessKey = N'DUMMY');

DECLARE @RoleAdmin NVARCHAR(128) = (SELECT TOP 1 Id FROM AspNetRoles WHERE Name = N'Administrator');

IF @AppId IS NULL
BEGIN
    RAISERROR('ClientApplication DXI800/DUMMY not found.', 16, 1);
    RETURN;
END

DECLARE @Modules TABLE (Name NVARCHAR(128), Url NVARCHAR(128), [Order] INT);
INSERT INTO @Modules (Name, Url, [Order]) VALUES
    (N'PatientDetails', N'/patient-master', 12),
    (N'RadiologyReportEntry', N'/radiology-report-entry', 22),
    (N'RadiologyDoctorApprovals', N'/radiology-doctor-approvals', 24);

INSERT INTO UserModules (Name, Url, [Order], ApplicationId, IsSyatem)
SELECT m.Name, m.Url, m.[Order], @AppId, 0
FROM @Modules m
WHERE NOT EXISTS (
    SELECT 1 FROM UserModules um WHERE um.Name = m.Name AND um.ApplicationId = @AppId);

-- Keep URLs/order aligned if modules already exist
UPDATE um
SET um.Url = m.Url,
    um.[Order] = m.[Order]
FROM UserModules um
INNER JOIN @Modules m ON m.Name = um.Name
WHERE um.ApplicationId = @AppId;

IF @RoleAdmin IS NOT NULL
BEGIN
    INSERT INTO RoleModuleMappings (CanAdd, CanEdit, CanAuthorize, CanDelete, CanView, CanReject, ModuleId, RoleId, ApplicationId)
    SELECT 1, 1, 1, 1, 1, 1, um.Id, @RoleAdmin, @AppId
    FROM UserModules um
    INNER JOIN @Modules m ON m.Name = um.Name
    WHERE um.ApplicationId = @AppId
      AND NOT EXISTS (
          SELECT 1 FROM RoleModuleMappings rm
          WHERE rm.ModuleId = um.Id AND rm.RoleId = @RoleAdmin AND rm.ApplicationId = @AppId);
END

PRINT 'PatientDetails / RadiologyReportEntry / RadiologyDoctorApprovals modules ready.';
GO
