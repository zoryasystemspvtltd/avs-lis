namespace Lis.Api.Migrations
{
    using System.Data.Entity.Migrations;

    /// <summary>
    /// Adds RoleMenuPermission overlay table for menu-level role permissions.
    /// Idempotent: safe when table already exists (SQL script / prior deploy).
    /// </summary>
    public partial class AddRoleMenuPermission : DbMigration
    {
        public override void Up()
        {
            Sql(@"
IF OBJECT_ID(N'dbo.RoleMenuPermission', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RoleMenuPermission
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RoleMenuPermission PRIMARY KEY,
        RoleId          NVARCHAR(128) NOT NULL,
        ModuleId        BIGINT NOT NULL,
        MenuKey         NVARCHAR(100) NOT NULL,
        CanView         BIT NOT NULL CONSTRAINT DF_RoleMenuPermission_CanView DEFAULT(0),
        CanAdd          BIT NOT NULL CONSTRAINT DF_RoleMenuPermission_CanAdd DEFAULT(0),
        CanEdit         BIT NOT NULL CONSTRAINT DF_RoleMenuPermission_CanEdit DEFAULT(0),
        CanDelete       BIT NOT NULL CONSTRAINT DF_RoleMenuPermission_CanDelete DEFAULT(0),
        CanAuthorize    BIT NOT NULL CONSTRAINT DF_RoleMenuPermission_CanAuthorize DEFAULT(0),
        CanReject       BIT NOT NULL CONSTRAINT DF_RoleMenuPermission_CanReject DEFAULT(0),
        IsActive        BIT NOT NULL CONSTRAINT DF_RoleMenuPermission_IsActive DEFAULT(1),
        ApplicationId   INT NULL,
        CreatedBy       NVARCHAR(128) NULL,
        CreatedOn       DATETIME NULL,
        ModifiedBy      NVARCHAR(128) NULL,
        ModifiedOn      DATETIME NULL,
        CONSTRAINT FK_RoleMenuPermission_Role FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE,
        CONSTRAINT FK_RoleMenuPermission_Module FOREIGN KEY (ModuleId) REFERENCES dbo.UserModules(Id) ON DELETE CASCADE,
        CONSTRAINT FK_RoleMenuPermission_App FOREIGN KEY (ApplicationId) REFERENCES dbo.ClientApplication(Id)
    );

    CREATE INDEX IX_RoleMenuPermission_RoleId ON dbo.RoleMenuPermission(RoleId);
    CREATE INDEX IX_RoleMenuPermission_ModuleId ON dbo.RoleMenuPermission(ModuleId);
    CREATE INDEX IX_RoleMenuPermission_ApplicationId ON dbo.RoleMenuPermission(ApplicationId);

    CREATE UNIQUE INDEX IX_RoleMenuPermission_Unique
        ON dbo.RoleMenuPermission(RoleId, ModuleId, MenuKey, ApplicationId);
END
");
        }

        public override void Down()
        {
            Sql(@"
IF OBJECT_ID(N'dbo.RoleMenuPermission', N'U') IS NOT NULL
    DROP TABLE dbo.RoleMenuPermission;
");
        }
    }
}
