namespace LIS.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;

    /// <summary>
    /// Records the current domain model after the idempotent commission SQL migrations.
    /// Up is empty because those migrations already create the tables and columns.
    /// </summary>
    public partial class CommissionSchemaModelSync : DbMigration
    {
        public override void Up()
        {
        }

        public override void Down()
        {
        }
    }
}
