-- Enterprise Notification Framework Phase 2 - schema enhancements
USE ZoryaLMS;
GO

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.NotificationConfiguration') AND name = N'SmsEnabled')
    ALTER TABLE dbo.NotificationConfiguration ADD SmsEnabled BIT NOT NULL DEFAULT 1;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.NotificationConfiguration') AND name = N'WhatsAppEnabled')
    ALTER TABLE dbo.NotificationConfiguration ADD WhatsAppEnabled BIT NOT NULL DEFAULT 1;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.NotificationTemplate') AND name = N'Version')
    ALTER TABLE dbo.NotificationTemplate ADD Version INT NOT NULL DEFAULT 1;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.NotificationTemplate') AND name = N'EffectiveFrom')
    ALTER TABLE dbo.NotificationTemplate ADD EffectiveFrom DATETIME NOT NULL DEFAULT GETDATE();

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.NotificationTemplate') AND name = N'EffectiveTo')
    ALTER TABLE dbo.NotificationTemplate ADD EffectiveTo DATETIME NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.NotificationAudit') AND name = N'InvoiceId')
    ALTER TABLE dbo.NotificationAudit ADD InvoiceId BIGINT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.NotificationAudit') AND name = N'ProviderName')
    ALTER TABLE dbo.NotificationAudit ADD ProviderName NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.NotificationAudit') AND name = N'TemplateVersion')
    ALTER TABLE dbo.NotificationAudit ADD TemplateVersion INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.NotificationAudit') AND name = N'ElapsedTimeMs')
    ALTER TABLE dbo.NotificationAudit ADD ElapsedTimeMs INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.NotificationAudit') AND name = N'CorrelationId')
    ALTER TABLE dbo.NotificationAudit ADD CorrelationId NVARCHAR(64) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.NotificationAudit') AND name = N'NextRetryOn')
    ALTER TABLE dbo.NotificationAudit ADD NextRetryOn DATETIME NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.NotificationAudit') AND name = N'Priority')
    ALTER TABLE dbo.NotificationAudit ADD Priority INT NOT NULL DEFAULT 0;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.NotificationTemplate') AND name = N'Version')
BEGIN
    EXEC(N'UPDATE dbo.NotificationTemplate SET Version = 1 WHERE Version IS NULL OR Version = 0');
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.NotificationTemplate') AND name = N'EffectiveFrom')
BEGIN
    EXEC(N'UPDATE dbo.NotificationTemplate SET EffectiveFrom = ISNULL(EffectiveFrom, GETDATE()) WHERE EffectiveFrom IS NULL');
END

PRINT 'Notification Framework Phase 2 schema ready.';
GO
