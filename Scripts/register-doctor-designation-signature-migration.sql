-- Register identity migration after doctor designation/signature columns (idempotent)
IF NOT EXISTS (
    SELECT 1 FROM dbo.MigrationHistory
    WHERE Migration_PK = N'202606281200000_DoctorDesignationSignature'
      AND ContextKey = N'Lis.Api.Migrations.Configuration'
)
BEGIN
    INSERT INTO dbo.MigrationHistory (Migration_PK, ContextKey, Model, ProductVersion)
    SELECT
        N'202606281200000_DoctorDesignationSignature',
        N'Lis.Api.Migrations.Configuration',
        Model,
        ProductVersion
    FROM dbo.MigrationHistory
    WHERE Migration_PK = N'202604270642026_fresh'
      AND ContextKey = N'Lis.Api.Migrations.Configuration';
END
