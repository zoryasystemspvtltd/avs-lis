namespace LIS.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class rate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.TestRateMaster",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Rate = c.Decimal(nullable: false, precision: 18, scale: 2),
                        EffectiveStart = c.DateTime(nullable: false),
                        EffectiveEnd = c.DateTime(nullable: false),
                        CreatedOn = c.DateTime(nullable: false),
                        ModifiedOn = c.DateTime(),
                        IsActive = c.Boolean(nullable: false),
                        CreatedBy = c.String(),
                        TestId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.HISTestMaster", t => t.TestId)
                .Index(t => t.TestId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TestRateMaster", "TestId", "dbo.HISTestMaster");
            DropIndex("dbo.TestRateMaster", new[] { "TestId" });
            DropTable("dbo.TestRateMaster");
        }
    }
}
