-- Doctor designation and signature path on AspNetUsers (idempotent)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.AspNetUsers') AND name = N'DoctorDesignation'
)
    ALTER TABLE dbo.AspNetUsers ADD DoctorDesignation NVARCHAR(100) NULL;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.AspNetUsers') AND name = N'DoctorSignaturePath'
)
    ALTER TABLE dbo.AspNetUsers ADD DoctorSignaturePath NVARCHAR(300) NULL;
