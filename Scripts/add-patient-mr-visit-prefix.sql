IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.PatientDetails') AND name = 'PatientPrefix'
)
BEGIN
    ALTER TABLE dbo.PatientDetails ADD PatientPrefix NVARCHAR(20) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.PatientDetails') AND name = 'MRNo'
)
BEGIN
    ALTER TABLE dbo.PatientDetails ADD MRNo NVARCHAR(30) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.PatientDetails') AND name = 'VisitId'
)
BEGIN
    ALTER TABLE dbo.PatientDetails ADD VisitId NVARCHAR(30) NULL;
END
GO

SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PatientDetails_MRNo' AND object_id = OBJECT_ID(N'dbo.PatientDetails'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_PatientDetails_MRNo
        ON dbo.PatientDetails (MRNo)
        WHERE IsActive = 1 AND MRNo IS NOT NULL AND MRNo <> '';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PatientDetails_VisitId' AND object_id = OBJECT_ID(N'dbo.PatientDetails'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_PatientDetails_VisitId
        ON dbo.PatientDetails (VisitId)
        WHERE IsActive = 1 AND VisitId IS NOT NULL AND VisitId <> '';
END
GO

IF EXISTS (
    SELECT 1 FROM sys.indexes i
    CROSS APPLY (SELECT filter_definition FROM sys.indexes WHERE object_id = i.object_id AND index_id = i.index_id) fd
    WHERE i.name = 'UX_PatientDetails_MRNo' AND i.object_id = OBJECT_ID(N'dbo.PatientDetails')
      AND (fd.filter_definition IS NULL OR fd.filter_definition NOT LIKE '%IsActive%')
)
BEGIN
    DROP INDEX UX_PatientDetails_MRNo ON dbo.PatientDetails;
    CREATE UNIQUE NONCLUSTERED INDEX UX_PatientDetails_MRNo
        ON dbo.PatientDetails (MRNo)
        WHERE IsActive = 1 AND MRNo IS NOT NULL AND MRNo <> '';
END
GO

IF EXISTS (
    SELECT 1 FROM sys.indexes i
    CROSS APPLY (SELECT filter_definition FROM sys.indexes WHERE object_id = i.object_id AND index_id = i.index_id) fd
    WHERE i.name = 'UX_PatientDetails_VisitId' AND i.object_id = OBJECT_ID(N'dbo.PatientDetails')
      AND (fd.filter_definition IS NULL OR fd.filter_definition NOT LIKE '%IsActive%')
)
BEGIN
    DROP INDEX UX_PatientDetails_VisitId ON dbo.PatientDetails;
    CREATE UNIQUE NONCLUSTERED INDEX UX_PatientDetails_VisitId
        ON dbo.PatientDetails (VisitId)
        WHERE IsActive = 1 AND VisitId IS NOT NULL AND VisitId <> '';
END
GO
