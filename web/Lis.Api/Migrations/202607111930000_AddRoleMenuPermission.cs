namespace Lis.Api.Migrations
{
    using System.Data.Entity.Migrations;

    public class AddRoleMenuPermission : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.RoleMenuPermission",
                c => new
                {
                    Id = c.Long(nullable: false, identity: true),
                    RoleId = c.String(nullable: false, maxLength: 128),
                    ModuleId = c.Long(nullable: false),
                    MenuKey = c.String(nullable: false, maxLength: 100),
                    CanView = c.Boolean(nullable: false),
                    CanAdd = c.Boolean(nullable: false),
                    CanEdit = c.Boolean(nullable: false),
                    CanDelete = c.Boolean(nullable: false),
                    CanAuthorize = c.Boolean(nullable: false),
                    CanReject = c.Boolean(nullable: false),
                    IsActive = c.Boolean(nullable: false),
                    ApplicationId = c.Int(),
                    CreatedBy = c.String(maxLength: 128),
                    CreatedOn = c.DateTime(),
                    ModifiedBy = c.String(maxLength: 128),
                    ModifiedOn = c.DateTime(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetRoles", t => t.RoleId, cascadeDelete: true)
                .ForeignKey("dbo.UserModules", t => t.ModuleId, cascadeDelete: true)
                .ForeignKey("dbo.ClientApplication", t => t.ApplicationId)
                .Index(t => t.RoleId)
                .Index(t => t.ModuleId)
                .Index(t => t.ApplicationId)
                .Index(t => new { t.RoleId, t.ModuleId, t.MenuKey, t.ApplicationId }, unique: true, name: "IX_RoleMenuPermission_Unique");
        }

        public override void Down()
        {
            DropForeignKey("dbo.RoleMenuPermission", "ApplicationId", "dbo.ClientApplication");
            DropForeignKey("dbo.RoleMenuPermission", "ModuleId", "dbo.UserModules");
            DropForeignKey("dbo.RoleMenuPermission", "RoleId", "dbo.AspNetRoles");
            DropIndex("dbo.RoleMenuPermission", "IX_RoleMenuPermission_Unique");
            DropIndex("dbo.RoleMenuPermission", new[] { "ApplicationId" });
            DropIndex("dbo.RoleMenuPermission", new[] { "ModuleId" });
            DropIndex("dbo.RoleMenuPermission", new[] { "RoleId" });
            DropTable("dbo.RoleMenuPermission");
        }
    }
}
