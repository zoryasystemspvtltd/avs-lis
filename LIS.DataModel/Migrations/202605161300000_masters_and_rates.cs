namespace LIS.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class masters_and_rates : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.TestRateMaster",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    TestId = c.Int(nullable: false),
                    Rate = c.Decimal(nullable: false, precision: 18, scale: 2),
                    EmergencyRate = c.Decimal(nullable: false, precision: 18, scale: 2),
                    DiscountPercent = c.Decimal(nullable: false, precision: 18, scale: 2),
                    TaxPercent = c.Decimal(nullable: false, precision: 18, scale: 2),
                    RateType = c.Int(nullable: false),
                    CorporateId = c.Int(),
                    ReferralDoctorId = c.Int(),
                    TestProfileId = c.Int(),
                    EffectiveStart = c.DateTime(nullable: false),
                    EffectiveEnd = c.DateTime(nullable: false),
                    IsActive = c.Boolean(nullable: false),
                    CreatedBy = c.String(),
                    CreatedOn = c.DateTime(nullable: false),
                    ModifiedBy = c.String(),
                    ModifiedOn = c.DateTime(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.HISTestMaster", t => t.TestId)
                .Index(t => t.TestId);

            CreateTable(
                "dbo.ReferralDoctorMaster",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Code = c.String(nullable: false, maxLength: 20),
                    Name = c.String(nullable: false, maxLength: 100),
                    Phone = c.String(maxLength: 15),
                    Email = c.String(maxLength: 100),
                    Address = c.String(maxLength: 200),
                    IsActive = c.Boolean(nullable: false),
                    CreatedBy = c.String(),
                    CreatedOn = c.DateTime(nullable: false),
                    ModifiedBy = c.String(),
                    ModifiedOn = c.DateTime(nullable: false),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.CorporateMaster",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Code = c.String(nullable: false, maxLength: 20),
                    Name = c.String(nullable: false, maxLength: 100),
                    Address = c.String(maxLength: 200),
                    Phone = c.String(maxLength: 15),
                    ContactPerson = c.String(maxLength: 100),
                    DefaultDiscountPercent = c.Decimal(nullable: false, precision: 18, scale: 2),
                    IsActive = c.Boolean(nullable: false),
                    CreatedBy = c.String(),
                    CreatedOn = c.DateTime(nullable: false),
                    ModifiedBy = c.String(),
                    ModifiedOn = c.DateTime(nullable: false),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.TestGroupMaster",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Code = c.String(nullable: false, maxLength: 20),
                    Name = c.String(nullable: false, maxLength: 100),
                    SpecimenTag = c.String(maxLength: 10),
                    IsActive = c.Boolean(nullable: false),
                    CreatedBy = c.String(),
                    CreatedOn = c.DateTime(nullable: false),
                    ModifiedBy = c.String(),
                    ModifiedOn = c.DateTime(nullable: false),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.TestCategoryMaster",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Code = c.String(nullable: false, maxLength: 20),
                    Name = c.String(nullable: false, maxLength: 100),
                    IsActive = c.Boolean(nullable: false),
                    CreatedBy = c.String(),
                    CreatedOn = c.DateTime(nullable: false),
                    ModifiedBy = c.String(),
                    ModifiedOn = c.DateTime(nullable: false),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.TestProfileMaster",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Code = c.String(nullable: false, maxLength: 20),
                    Name = c.String(nullable: false, maxLength: 100),
                    PackageRate = c.Decimal(nullable: false, precision: 18, scale: 2),
                    IsActive = c.Boolean(nullable: false),
                    CreatedBy = c.String(),
                    CreatedOn = c.DateTime(nullable: false),
                    ModifiedBy = c.String(),
                    ModifiedOn = c.DateTime(nullable: false),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.TestProfileDetail",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    TestProfileId = c.Int(nullable: false),
                    TestId = c.Int(nullable: false),
                    Quantity = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.TestProfileMaster", t => t.TestProfileId)
                .ForeignKey("dbo.HISTestMaster", t => t.TestId)
                .Index(t => t.TestProfileId)
                .Index(t => t.TestId);

            CreateTable(
                "dbo.UnitMaster",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Code = c.String(nullable: false, maxLength: 20),
                    Name = c.String(nullable: false, maxLength: 50),
                    IsActive = c.Boolean(nullable: false),
                    CreatedBy = c.String(),
                    CreatedOn = c.DateTime(nullable: false),
                    ModifiedBy = c.String(),
                    ModifiedOn = c.DateTime(nullable: false),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.MethodMaster",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Code = c.String(nullable: false, maxLength: 20),
                    Name = c.String(nullable: false, maxLength: 100),
                    IsActive = c.Boolean(nullable: false),
                    CreatedBy = c.String(),
                    CreatedOn = c.DateTime(nullable: false),
                    ModifiedBy = c.String(),
                    ModifiedOn = c.DateTime(nullable: false),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.SampleTypeMaster",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Code = c.String(nullable: false, maxLength: 20),
                    Name = c.String(nullable: false, maxLength: 100),
                    IsActive = c.Boolean(nullable: false),
                    CreatedBy = c.String(),
                    CreatedOn = c.DateTime(nullable: false),
                    ModifiedBy = c.String(),
                    ModifiedOn = c.DateTime(nullable: false),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.ContainerMaster",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Code = c.String(nullable: false, maxLength: 20),
                    Name = c.String(nullable: false, maxLength: 100),
                    Color = c.String(maxLength: 50),
                    IsActive = c.Boolean(nullable: false),
                    CreatedBy = c.String(),
                    CreatedOn = c.DateTime(nullable: false),
                    ModifiedBy = c.String(),
                    ModifiedOn = c.DateTime(nullable: false),
                })
                .PrimaryKey(t => t.Id);

            AddColumn("dbo.SaleInvoice", "InvoiceStatus", c => c.Int(nullable: false, defaultValue: 0));
            AddColumn("dbo.SaleInvoice", "PaymentStatus", c => c.Int(nullable: false, defaultValue: 0));
            AddColumn("dbo.SaleInvoice", "ReferralDoctorId", c => c.Int());
            AddColumn("dbo.SaleInvoice", "CorporateId", c => c.Int());
            AddColumn("dbo.SaleInvoice", "Notes", c => c.String());
            AlterColumn("dbo.SaleInvoice", "RequestDetailId", c => c.Long());
        }

        public override void Down()
        {
            AlterColumn("dbo.SaleInvoice", "RequestDetailId", c => c.Long(nullable: false));
            DropColumn("dbo.SaleInvoice", "Notes");
            DropColumn("dbo.SaleInvoice", "CorporateId");
            DropColumn("dbo.SaleInvoice", "ReferralDoctorId");
            DropColumn("dbo.SaleInvoice", "PaymentStatus");
            DropColumn("dbo.SaleInvoice", "InvoiceStatus");

            DropForeignKey("dbo.TestProfileDetail", "TestId", "dbo.HISTestMaster");
            DropForeignKey("dbo.TestProfileDetail", "TestProfileId", "dbo.TestProfileMaster");
            DropForeignKey("dbo.TestRateMaster", "TestId", "dbo.HISTestMaster");
            DropIndex("dbo.TestProfileDetail", new[] { "TestId" });
            DropIndex("dbo.TestProfileDetail", new[] { "TestProfileId" });
            DropIndex("dbo.TestRateMaster", new[] { "TestId" });

            DropTable("dbo.ContainerMaster");
            DropTable("dbo.SampleTypeMaster");
            DropTable("dbo.MethodMaster");
            DropTable("dbo.UnitMaster");
            DropTable("dbo.TestProfileDetail");
            DropTable("dbo.TestProfileMaster");
            DropTable("dbo.TestCategoryMaster");
            DropTable("dbo.TestGroupMaster");
            DropTable("dbo.CorporateMaster");
            DropTable("dbo.ReferralDoctorMaster");
            DropTable("dbo.TestRateMaster");
        }
    }
}
