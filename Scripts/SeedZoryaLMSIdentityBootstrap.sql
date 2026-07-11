-- Bootstrap identity, client application, modules, and admin user for ZoryaLMS.
-- Run after Lis.Api identity migration (empty AspNet* / ClientApplication tables).
USE ZoryaLMS;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AspNetUsers')
BEGIN
    RAISERROR('Identity tables not found. Run Lis.Api EF migration first.', 16, 1);
    RETURN;
END

BEGIN TRANSACTION;

DECLARE @AdminUserId NVARCHAR(128) = N'1f6be85b-651e-46cb-ab08-7f9e48cfbf12';
DECLARE @RoleAdmin NVARCHAR(128) = N'a0db73d7-9a9b-4b65-83fd-acdbea4ecb2e';
DECLARE @RoleTech NVARCHAR(128) = N'2d051b02-9289-45ef-8a07-b902b0fea88f';
DECLARE @RoleDoc NVARCHAR(128) = N'a20db0ae-7b61-4c17-af97-ba35c6067e97';

IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Id = @RoleAdmin)
    INSERT INTO AspNetRoles (Id, Name) VALUES (@RoleAdmin, N'Administrator');
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Id = @RoleTech)
    INSERT INTO AspNetRoles (Id, Name) VALUES (@RoleTech, N'Technician');
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Id = @RoleDoc)
    INSERT INTO AspNetRoles (Id, Name) VALUES (@RoleDoc, N'Doctor');

IF NOT EXISTS (SELECT 1 FROM ClientApplication WHERE AccessKey = N'DXI800')
    INSERT INTO ClientApplication (Name, Description, AccessKey, RefreshTokenLifeTime, AllowedOrigin)
    VALUES (N'DXI800', N'ZoryaLIS Production Client', N'DXI800', 1440, N'*');

DECLARE @AppId INT = (SELECT TOP 1 Id FROM ClientApplication WHERE AccessKey = N'DXI800');

IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = @AdminUserId)
BEGIN
    INSERT INTO AspNetUsers (
        Id, Email, EmailConfirmed, PasswordHash, SecurityStamp, PhoneNumber, PhoneNumberConfirmed,
        TwoFactorEnabled, LockoutEndDateUtc, LockoutEnabled, AccessFailedCount, UserName,
        FirstName, LastName, IsBlocked)
    VALUES (
        @AdminUserId, N'admin@zorya.co.in', 1,
        N'AC9998VfILNY62YpjhEaYfqb5bYJbiCMRPMESaxqVQR2QeOKmxxKPXuCe35ZJKewog==',
        N'a67ec0c5-b9b9-477d-9877-867f7159bb41', N'0000000000', 0,
        0, NULL, 1, 0, N'admin@zorya.co.in',
        N'Administrator', N'LIS', 0);
END

IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @AdminUserId AND RoleId = @RoleAdmin)
    INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@AdminUserId, @RoleAdmin);
IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @AdminUserId AND RoleId = @RoleTech)
    INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@AdminUserId, @RoleTech);
IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @AdminUserId AND RoleId = @RoleDoc)
    INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@AdminUserId, @RoleDoc);

DECLARE @Modules TABLE (Name NVARCHAR(128), Url NVARCHAR(128), [Order] INT, IsSystem BIT);
INSERT INTO @Modules (Name, Url, [Order], IsSystem) VALUES
    (N'Applications', N'/client-application', 1, 1),
    (N'Roles', N'/roles', 2, 1),
    (N'Users', N'/users', 3, 1),
    (N'Reports', N'/reports', 4, 0),
    (N'Equipments', N'/equipments', 5, 0),
    (N'Samples', N'/samples', 6, 0),
    (N'DoctorsApprovals', N'/doctorapprovals', 7, 0),
    (N'Masters', N'/masters', 8, 0),
    (N'TestRates', N'/test-rates', 9, 0),
    (N'SaleInvoices', N'/sale-invoices', 10, 0),
    (N'HisTest', N'/test-master', 11, 0),
    (N'PatientDetails', N'/patient-master', 12, 0),
    (N'SampleCollection', N'/sample-collection', 20, 0),
    (N'SampleReceiving', N'/sample-receiving', 21, 0),
    (N'RadiologyReportEntry', N'/radiology-report-entry', 22, 0),
    (N'RadiologyReports', N'/reports/radiology', 23, 0),
    (N'RadiologyDoctorApprovals', N'/radiology-doctor-approvals', 24, 0);

INSERT INTO UserModules (Name, Url, [Order], ApplicationId, IsSyatem)
SELECT m.Name, m.Url, m.[Order], @AppId, m.IsSystem
FROM @Modules m
WHERE NOT EXISTS (
    SELECT 1 FROM UserModules um WHERE um.Name = m.Name AND um.ApplicationId = @AppId);

INSERT INTO RoleModuleMappings (CanAdd, CanEdit, CanAuthorize, CanDelete, CanView, CanReject, ModuleId, RoleId, ApplicationId)
SELECT 1, 1, 1, 1, 1, 1, um.Id, @RoleAdmin, @AppId
FROM UserModules um
WHERE um.ApplicationId = @AppId
  AND NOT EXISTS (
      SELECT 1 FROM RoleModuleMappings rm
      WHERE rm.ModuleId = um.Id AND rm.RoleId = @RoleAdmin AND rm.ApplicationId = @AppId);

COMMIT TRANSACTION;
PRINT 'ZoryaLMS identity bootstrap completed.';
GO
