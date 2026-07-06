-- Department Processing Category migration (standalone script for production)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Department') AND name = N'ProcessingCategory'
)
BEGIN
    ALTER TABLE dbo.Department
        ADD ProcessingCategory NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Department_ProcessingCategory DEFAULT (N'Laboratory');
END
GO

UPDATE dbo.Department SET ProcessingCategory = N'Diagnostic'
WHERE UPPER(ISNULL(Name, N'')) LIKE N'%RADIOLOGY%'
   OR UPPER(ISNULL(Name, N'')) LIKE N'%MRI%'
   OR UPPER(ISNULL(Name, N'')) LIKE N'%CT SCAN%'
   OR UPPER(ISNULL(Name, N'')) LIKE N'%CT-SCAN%'
   OR UPPER(ISNULL(Name, N'')) LIKE N'%MAMMO%'
   OR UPPER(ISNULL(Name, N'')) LIKE N'%SONO%'
   OR UPPER(ISNULL(Name, N'')) LIKE N'%SONOGRAPHY%'
   OR UPPER(ISNULL(Name, N'')) LIKE N'%ULTRASOUND%'
   OR UPPER(ISNULL(Name, N'')) LIKE N'%USG%'
   OR UPPER(ISNULL(Name, N'')) LIKE N'%X-RAY%'
   OR UPPER(ISNULL(Name, N'')) LIKE N'%XRAY%'
   OR UPPER(ISNULL(Code, N'')) LIKE N'%RAD%'
   OR UPPER(ISNULL(Code, N'')) LIKE N'%MRI%'
   OR UPPER(ISNULL(Code, N'')) LIKE N'%CT%'
   OR UPPER(ISNULL(Code, N'')) LIKE N'%USG%';
GO
