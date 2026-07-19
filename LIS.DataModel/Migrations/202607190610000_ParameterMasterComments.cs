namespace LIS.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ParameterMasterComments : DbMigration
    {
        public override void Up()
        {
            Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.HISParameterMaster') AND name = N'Comments'
)
    ALTER TABLE dbo.HISParameterMaster ADD Comments NVARCHAR(MAX) NULL;
");
        }

        public override void Down()
        {
            Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.HISParameterMaster') AND name = N'Comments'
)
    ALTER TABLE dbo.HISParameterMaster DROP COLUMN Comments;
");
        }
    }
}
