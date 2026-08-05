-- Enterprise Notification Framework (Phase 1) - schema + module seed
USE ZoryaLMS;
GO

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'NotificationConfiguration')
BEGIN
    CREATE TABLE dbo.NotificationConfiguration (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IsEnabled BIT NOT NULL DEFAULT 0,
        ChannelMode INT NOT NULL DEFAULT 3,
        RetryCount INT NOT NULL DEFAULT 3,
        RetryIntervalSeconds INT NOT NULL DEFAULT 30,
        DefaultChannel INT NOT NULL DEFAULT 1,
        CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedBy NVARCHAR(80) NULL,
        ModifiedOn DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedBy NVARCHAR(80) NULL
    );
END

IF NOT EXISTS (SELECT 1 FROM dbo.NotificationConfiguration)
BEGIN
    INSERT INTO dbo.NotificationConfiguration (IsEnabled, ChannelMode, RetryCount, RetryIntervalSeconds, DefaultChannel, CreatedBy, ModifiedBy)
    VALUES (0, 3, 3, 30, 1, N'system', N'system');
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'NotificationTemplate')
BEGIN
    CREATE TABLE dbo.NotificationTemplate (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EventCode NVARCHAR(50) NOT NULL,
        Channel INT NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Body NVARCHAR(MAX) NOT NULL,
        IsDefault BIT NOT NULL DEFAULT 1,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedBy NVARCHAR(80) NULL,
        ModifiedOn DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedBy NVARCHAR(80) NULL
    );
    CREATE INDEX IX_NotificationTemplate_Event ON dbo.NotificationTemplate(EventCode, Channel);
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'NotificationAudit')
BEGIN
    CREATE TABLE dbo.NotificationAudit (
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PatientId BIGINT NULL,
        PatientName NVARCHAR(50) NULL,
        InvoiceNo NVARCHAR(50) NULL,
        EventCode NVARCHAR(50) NOT NULL,
        Channel INT NOT NULL,
        TemplateId INT NULL,
        RecipientPhone NVARCHAR(20) NULL,
        MessageBody NVARCHAR(MAX) NULL,
        Status INT NOT NULL DEFAULT 0,
        RetryCount INT NOT NULL DEFAULT 0,
        ProviderResponse NVARCHAR(500) NULL,
        ErrorMessage NVARCHAR(500) NULL,
        SecureLinkToken NVARCHAR(100) NULL,
        CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
        SentOn DATETIME NULL,
        CreatedBy NVARCHAR(80) NULL
    );
    CREATE INDEX IX_NotificationAudit_Invoice ON dbo.NotificationAudit(InvoiceNo, EventCode, Status);
    CREATE INDEX IX_NotificationAudit_CreatedOn ON dbo.NotificationAudit(CreatedOn DESC);
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'SecureLinkToken')
BEGIN
    CREATE TABLE dbo.SecureLinkToken (
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Token NVARCHAR(64) NOT NULL,
        InvoiceNo NVARCHAR(50) NULL,
        PatientId BIGINT NULL,
        ExpiresOn DATETIME NOT NULL,
        IsUsed BIT NOT NULL DEFAULT 0,
        CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedBy NVARCHAR(80) NULL
    );
    CREATE UNIQUE INDEX UX_SecureLinkToken_Token ON dbo.SecureLinkToken(Token);
END

IF NOT EXISTS (SELECT 1 FROM dbo.NotificationTemplate WHERE EventCode = N'ReportReadyPaid' AND Channel = 1)
BEGIN
    INSERT INTO dbo.NotificationTemplate (EventCode, Channel, Name, Body, IsDefault, IsActive, CreatedBy, ModifiedBy)
    VALUES
    (N'ReportReadyPaid', 1, N'Report Ready (Fully Paid) - SMS',
     N'Dear {PatientName},' + CHAR(13)+CHAR(10)+CHAR(13)+CHAR(10)+
     N'Your {ReportType} report is now available.' + CHAR(13)+CHAR(10)+CHAR(13)+CHAR(10)+
     N'Download:' + CHAR(13)+CHAR(10)+'{ReportDownloadLink}' + CHAR(13)+CHAR(10)+CHAR(13)+CHAR(10)+
     N'Thank you.' + CHAR(13)+CHAR(10)+'{OrganizationName}', 1, 1, N'system', N'system'),
    (N'ReportReadyPaid', 2, N'Report Ready (Fully Paid) - WhatsApp',
     N'Dear {PatientName},' + CHAR(13)+CHAR(10)+CHAR(13)+CHAR(10)+
     N'Your {ReportType} report is now available.' + CHAR(13)+CHAR(10)+CHAR(13)+CHAR(10)+
     N'Download:' + CHAR(13)+CHAR(10)+'{ReportDownloadLink}' + CHAR(13)+CHAR(10)+CHAR(13)+CHAR(10)+
     N'Thank you.' + CHAR(13)+CHAR(10)+'{OrganizationName}', 1, 1, N'system', N'system'),
    (N'ReportReadyPartialPayment', 1, N'Outstanding Payment - SMS',
     N'Dear {PatientName},' + CHAR(13)+CHAR(10)+CHAR(13)+CHAR(10)+
     N'Your {ReportType} report is ready.' + CHAR(13)+CHAR(10)+CHAR(13)+CHAR(10)+
     N'Outstanding Amount:' + CHAR(13)+CHAR(10)+N'₹ {OutstandingAmount}' + CHAR(13)+CHAR(10)+CHAR(13)+CHAR(10)+
     N'Please complete payment to receive your report.' + CHAR(13)+CHAR(10)+CHAR(13)+CHAR(10)+
     N'Thank you.' + CHAR(13)+CHAR(10)+'{OrganizationName}', 1, 1, N'system', N'system'),
    (N'ReportReadyPartialPayment', 2, N'Outstanding Payment - WhatsApp',
     N'Dear {PatientName},' + CHAR(13)+CHAR(10)+CHAR(13)+CHAR(10)+
     N'Your {ReportType} report is ready.' + CHAR(13)+CHAR(10)+CHAR(13)+CHAR(10)+
     N'Outstanding Amount:' + CHAR(13)+CHAR(10)+N'₹ {OutstandingAmount}' + CHAR(13)+CHAR(10)+CHAR(13)+CHAR(10)+
     N'Please complete payment to receive your report.' + CHAR(13)+CHAR(10)+CHAR(13)+CHAR(10)+
     N'Thank you.' + CHAR(13)+CHAR(10)+'{OrganizationName}', 1, 1, N'system', N'system');
END

DECLARE @AppId INT = (SELECT TOP 1 Id FROM ClientApplication WHERE AccessKey = N'DXI800');
IF @AppId IS NULL
BEGIN
    RAISERROR('ClientApplication DXI800 not found.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM UserModules WHERE Name = N'NotificationConfiguration' AND ApplicationId = @AppId)
BEGIN
    INSERT INTO UserModules (Name, Url, [Order], ApplicationId, IsSyatem)
    VALUES (N'NotificationConfiguration', N'/notification-configuration', 55, @AppId, 0);
END

DECLARE @ModuleId INT = (SELECT TOP 1 Id FROM UserModules WHERE Name = N'NotificationConfiguration' AND ApplicationId = @AppId);
DECLARE @RoleAdmin NVARCHAR(128) = (SELECT TOP 1 Id FROM AspNetRoles WHERE Name = N'Administrator');

IF @ModuleId IS NOT NULL AND @RoleAdmin IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM RoleModuleMappings WHERE ModuleId = @ModuleId AND RoleId = @RoleAdmin AND ApplicationId = @AppId)
BEGIN
    INSERT INTO RoleModuleMappings (CanAdd, CanEdit, CanAuthorize, CanDelete, CanView, CanReject, ModuleId, RoleId, ApplicationId)
    VALUES (1, 1, 1, 1, 1, 1, @ModuleId, @RoleAdmin, @AppId);
END

PRINT 'Notification Framework schema and module ready.';
GO
