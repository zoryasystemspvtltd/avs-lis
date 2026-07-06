IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.PatientDetails') AND name = 'Address'
)
BEGIN
    ALTER TABLE dbo.PatientDetails ADD Address NVARCHAR(500) NULL;
END
GO
