namespace LIS.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class NotificationFramework : DbMigration
    {
        public override void Up()
        {
            Sql(@"
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
END
");
        }

        public override void Down()
        {
            Sql(@"
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'SecureLinkToken') DROP TABLE dbo.SecureLinkToken;
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'NotificationAudit') DROP TABLE dbo.NotificationAudit;
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'NotificationTemplate') DROP TABLE dbo.NotificationTemplate;
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'NotificationConfiguration') DROP TABLE dbo.NotificationConfiguration;
");
        }
    }
}
