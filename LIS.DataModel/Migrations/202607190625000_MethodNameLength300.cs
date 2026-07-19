namespace LIS.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class MethodNameLength300 : DbMigration
    {
        public override void Up()
        {
            Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.MethodMaster')
      AND name = N'Name'
      AND (max_length <> 600 OR is_nullable <> 0)
)
    ALTER TABLE dbo.MethodMaster ALTER COLUMN Name NVARCHAR(300) NOT NULL;
");
        }

        public override void Down()
        {
            Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.MethodMaster')
      AND name = N'Name'
      AND (max_length <> 200 OR is_nullable <> 0)
)
    ALTER TABLE dbo.MethodMaster ALTER COLUMN Name NVARCHAR(100) NOT NULL;
");
        }
    }
}
