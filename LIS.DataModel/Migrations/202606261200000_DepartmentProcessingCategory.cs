namespace LIS.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class DepartmentProcessingCategory : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Department", "ProcessingCategory", c => c.String(nullable: false, maxLength: 20, defaultValue: "Laboratory"));

            Sql(@"
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
");
        }

        public override void Down()
        {
            DropColumn("dbo.Department", "ProcessingCategory");
        }
    }
}
