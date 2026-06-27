-- Grant IIS app pool access to ZoryaLMS (same as legacy AVSLIS deployment).
USE ZoryaLMS;
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'IIS APPPOOL\AVILIS_API_POOL')
BEGIN
    CREATE LOGIN [IIS APPPOOL\AVILIS_API_POOL] FROM WINDOWS;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'IIS APPPOOL\AVILIS_API_POOL')
BEGIN
    CREATE USER [IIS APPPOOL\AVILIS_API_POOL] FOR LOGIN [IIS APPPOOL\AVILIS_API_POOL];
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_role_members drm
    INNER JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id
    INNER JOIN sys.database_principals m ON drm.member_principal_id = m.principal_id
    WHERE m.name = N'IIS APPPOOL\AVILIS_API_POOL' AND r.name = N'db_owner')
BEGIN
    ALTER ROLE db_owner ADD MEMBER [IIS APPPOOL\AVILIS_API_POOL];
END
GO

PRINT 'IIS APPPOOL\AVILIS_API_POOL granted db_owner on ZoryaLMS.';
GO
