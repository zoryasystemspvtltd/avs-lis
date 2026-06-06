-- Seed LIS master/billing modules and grant Administrator full access (existing databases).
-- Run against AVSLIS database after migrations.

DECLARE @AppId INT = (SELECT TOP 1 Id FROM ClientApplication WHERE AccessKey = 'DUMMY');
DECLARE @AdminRoleId NVARCHAR(128) = (SELECT TOP 1 Id FROM AspNetRoles WHERE Name = 'Administrator');

IF @AppId IS NULL OR @AdminRoleId IS NULL
BEGIN
    RAISERROR('ClientApplication (DUMMY) or Administrator role not found. Adjust script for your environment.', 16, 1);
    RETURN;
END

DECLARE @Modules TABLE (Name NVARCHAR(128), Url NVARCHAR(128), [Order] INT);
INSERT INTO @Modules (Name, Url, [Order]) VALUES
    (N'Masters', N'/masters', 8),
    (N'TestRates', N'/test-rates', 9),
    (N'SaleInvoices', N'/sale-invoices', 10),
    (N'HisTest', N'/test-master', 11);

INSERT INTO UserModules (Name, Url, [Order], ApplicationId, IsSyatem)
SELECT m.Name, m.Url, m.[Order], @AppId, 0
FROM @Modules m
WHERE NOT EXISTS (SELECT 1 FROM UserModules um WHERE um.Name = m.Name AND um.ApplicationId = @AppId);

INSERT INTO RoleModuleMappings (CanAdd, CanEdit, CanAuthorize, CanDelete, CanView, CanReject, ModuleId, RoleId, ApplicationId)
SELECT 1, 1, 1, 1, 1, 1, um.Id, @AdminRoleId, @AppId
FROM UserModules um
INNER JOIN @Modules m ON m.Name = um.Name
WHERE um.ApplicationId = @AppId
  AND NOT EXISTS (
      SELECT 1 FROM RoleModuleMappings rm
      WHERE rm.ModuleId = um.Id AND rm.RoleId = @AdminRoleId AND rm.ApplicationId = @AppId
  );

PRINT 'Master module seed completed.';
