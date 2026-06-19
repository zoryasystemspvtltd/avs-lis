-- FDD: Sample Collection, Sample Receiving, Radiology Report Entry
-- Run against AVSLIS database before deploying API.

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'TestRequestDetails') AND name = N'CollectedBy')
    ALTER TABLE TestRequestDetails ADD CollectedBy NVARCHAR(80) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'TestRequestDetails') AND name = N'CollectedRemarks')
    ALTER TABLE TestRequestDetails ADD CollectedRemarks NVARCHAR(500) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'TestRequestDetails') AND name = N'ReceivedBy')
    ALTER TABLE TestRequestDetails ADD ReceivedBy NVARCHAR(80) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'TestRequestDetails') AND name = N'ReceivedRemarks')
    ALTER TABLE TestRequestDetails ADD ReceivedRemarks NVARCHAR(500) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'SampleRejectionReasonMaster')
BEGIN
    CREATE TABLE SampleRejectionReasonMaster (
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
    CREATE UNIQUE INDEX UX_SampleRejectionReason_Code ON SampleRejectionReasonMaster(Code);
END

IF NOT EXISTS (SELECT 1 FROM SampleRejectionReasonMaster WHERE Code = N'RJ01')
BEGIN
    INSERT INTO SampleRejectionReasonMaster (Code, Name, Category, IsActive, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn) VALUES
        (N'RJ01', N'Hemolyzed', N'Receiving', 1, N'system', GETDATE(), N'system', GETDATE()),
        (N'RJ02', N'Clotted', N'Receiving', 1, N'system', GETDATE(), N'system', GETDATE()),
        (N'RJ03', N'Insufficient Quantity', N'Receiving', 1, N'system', GETDATE(), N'system', GETDATE()),
        (N'RJ04', N'Wrong Container', N'Receiving', 1, N'system', GETDATE(), N'system', GETDATE()),
        (N'RJ05', N'Leakage', N'Receiving', 1, N'system', GETDATE(), N'system', GETDATE()),
        (N'RJ06', N'Barcode Missing', N'Receiving', 1, N'system', GETDATE(), N'system', GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'RadiologyRequestDetail')
BEGIN
    CREATE TABLE RadiologyRequestDetail (
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
        ModifiedOn DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_RadiologyRequest_Patient ON RadiologyRequestDetail(PatientId);
    CREATE INDEX IX_RadiologyRequest_Status ON RadiologyRequestDetail(ReportStatus);
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'RadiologyResultDetail')
BEGIN
    CREATE TABLE RadiologyResultDetail (
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
        CONSTRAINT FK_RadiologyResult_Request FOREIGN KEY (RadiologyRequestId) REFERENCES RadiologyRequestDetail(Id)
    );
    CREATE UNIQUE INDEX UX_RadiologyResult_Request ON RadiologyResultDetail(RadiologyRequestId);
END

DECLARE @AppId INT = (SELECT TOP 1 Id FROM ClientApplication WHERE AccessKey = 'DXI800');
IF @AppId IS NULL
    SET @AppId = (SELECT TOP 1 Id FROM ClientApplication WHERE AccessKey = 'DUMMY');
DECLARE @AdminRoleId NVARCHAR(128) = (SELECT TOP 1 Id FROM AspNetRoles WHERE Name = 'Administrator');

IF @AppId IS NOT NULL AND @AdminRoleId IS NOT NULL
BEGIN
    DECLARE @Modules TABLE (Name NVARCHAR(128), Url NVARCHAR(128), [Order] INT);
    INSERT INTO @Modules (Name, Url, [Order]) VALUES
        (N'SampleCollection', N'/sample-collection', 20),
        (N'SampleReceiving', N'/sample-receiving', 21),
        (N'RadiologyReportEntry', N'/radiology-report-entry', 22),
        (N'RadiologyReports', N'/reports/radiology', 23);

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
END

PRINT 'FDD schema and module seed completed.';
