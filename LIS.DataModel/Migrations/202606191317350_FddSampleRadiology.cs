namespace LIS.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class FddSampleRadiology : DbMigration
    {
        public override void Up()
        {
            Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TestRequestDetails') AND name = N'CollectedBy')
    ALTER TABLE dbo.TestRequestDetails ADD CollectedBy NVARCHAR(80) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TestRequestDetails') AND name = N'CollectedRemarks')
    ALTER TABLE dbo.TestRequestDetails ADD CollectedRemarks NVARCHAR(500) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TestRequestDetails') AND name = N'ReceivedBy')
    ALTER TABLE dbo.TestRequestDetails ADD ReceivedBy NVARCHAR(80) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TestRequestDetails') AND name = N'ReceivedRemarks')
    ALTER TABLE dbo.TestRequestDetails ADD ReceivedRemarks NVARCHAR(500) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.SaleInvoiceDetail') AND name = N'TestProfileId')
    ALTER TABLE dbo.SaleInvoiceDetail ADD TestProfileId INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'SampleRejectionReasonMaster')
BEGIN
    CREATE TABLE dbo.SampleRejectionReasonMaster (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Code NVARCHAR(20) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Category NVARCHAR(30) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedBy NVARCHAR(80) NULL,
        CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedBy NVARCHAR(80) NULL,
        ModifiedOn DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE UNIQUE INDEX UX_SampleRejectionReason_Code ON dbo.SampleRejectionReasonMaster(Code);
END

IF NOT EXISTS (SELECT 1 FROM dbo.SampleRejectionReasonMaster WHERE Code = N'RJ01')
BEGIN
    INSERT INTO dbo.SampleRejectionReasonMaster (Code, Name, Category, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn) VALUES
        (N'RJ01', N'Hemolyzed', N'Receiving', 1, N'system', GETDATE(), N'system', GETDATE()),
        (N'RJ02', N'Clotted', N'Receiving', 1, N'system', GETDATE(), N'system', GETDATE()),
        (N'RJ03', N'Insufficient Quantity', N'Receiving', 1, N'system', GETDATE(), N'system', GETDATE()),
        (N'RJ04', N'Wrong Container', N'Receiving', 1, N'system', GETDATE(), N'system', GETDATE()),
        (N'RJ05', N'Leakage', N'Receiving', 1, N'system', GETDATE(), N'system', GETDATE()),
        (N'RJ06', N'Barcode Missing', N'Receiving', 1, N'system', GETDATE(), N'system', GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'RadiologyRequestDetail')
BEGIN
    CREATE TABLE dbo.RadiologyRequestDetail (
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PatientId BIGINT NOT NULL,
        HISRequestNo NVARCHAR(20) NULL,
        AccessionNo NVARCHAR(30) NULL,
        Modality NVARCHAR(30) NULL,
        HISTestCode NVARCHAR(20) NULL,
        HISTestName NVARCHAR(100) NULL,
        Department NVARCHAR(80) NULL,
        ReportStatus INT NOT NULL DEFAULT 0,
        CreatedBy NVARCHAR(80) NULL,
        CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedBy NVARCHAR(80) NULL,
        ModifiedOn DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_RadiologyRequest_Patient FOREIGN KEY (PatientId) REFERENCES dbo.PatientDetails(Id)
    );
    CREATE INDEX IX_RadiologyRequest_Patient ON dbo.RadiologyRequestDetail(PatientId);
    CREATE INDEX IX_RadiologyRequest_Status ON dbo.RadiologyRequestDetail(ReportStatus);
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'RadiologyResultDetail')
BEGIN
    CREATE TABLE dbo.RadiologyResultDetail (
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RadiologyRequestId BIGINT NOT NULL,
        ClinicalHistory NVARCHAR(MAX) NULL,
        Findings NVARCHAR(MAX) NULL,
        Impression NVARCHAR(MAX) NULL,
        Recommendation NVARCHAR(MAX) NULL,
        AuthorizedBy NVARCHAR(80) NULL,
        AuthorizedOn DATETIME NULL,
        DigitalSignature NVARCHAR(200) NULL,
        CreatedBy NVARCHAR(80) NULL,
        CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedBy NVARCHAR(80) NULL,
        ModifiedOn DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_RadiologyResult_Request FOREIGN KEY (RadiologyRequestId) REFERENCES dbo.RadiologyRequestDetail(Id)
    );
    CREATE UNIQUE INDEX UX_RadiologyResult_Request ON dbo.RadiologyResultDetail(RadiologyRequestId);
END
");
        }

        public override void Down()
        {
            Sql(@"
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'RadiologyResultDetail')
    DROP TABLE dbo.RadiologyResultDetail;

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'RadiologyRequestDetail')
    DROP TABLE dbo.RadiologyRequestDetail;

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'SampleRejectionReasonMaster')
    DROP TABLE dbo.SampleRejectionReasonMaster;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TestRequestDetails') AND name = N'ReceivedRemarks')
    ALTER TABLE dbo.TestRequestDetails DROP COLUMN ReceivedRemarks;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TestRequestDetails') AND name = N'ReceivedBy')
    ALTER TABLE dbo.TestRequestDetails DROP COLUMN ReceivedBy;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TestRequestDetails') AND name = N'CollectedRemarks')
    ALTER TABLE dbo.TestRequestDetails DROP COLUMN CollectedRemarks;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TestRequestDetails') AND name = N'CollectedBy')
    ALTER TABLE dbo.TestRequestDetails DROP COLUMN CollectedBy;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.SaleInvoiceDetail') AND name = N'TestProfileId')
    ALTER TABLE dbo.SaleInvoiceDetail DROP COLUMN TestProfileId;
");
        }
    }
}
