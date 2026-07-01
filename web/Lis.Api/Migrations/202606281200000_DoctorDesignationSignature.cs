namespace Lis.Api.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class DoctorDesignationSignature : DbMigration
    {
        public override void Up()
        {
            Sql(@"
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
");
        }

        public override void Down()
        {
            Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.AspNetUsers') AND name = N'DoctorSignaturePath'
)
    ALTER TABLE dbo.AspNetUsers DROP COLUMN DoctorSignaturePath;

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.AspNetUsers') AND name = N'DoctorDesignation'
)
    ALTER TABLE dbo.AspNetUsers DROP COLUMN DoctorDesignation;
");
        }
    }
}
