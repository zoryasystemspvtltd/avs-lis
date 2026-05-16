namespace LIS.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class sale : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SaleInvoiceDetail",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        SaleInvoiceId = c.Long(nullable: false),
                        TestId = c.Int(nullable: false),
                        Rate = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Quantity = c.Int(nullable: false),
                        Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DiscountAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TaxAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        NetAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        RequestDetailId = c.Long(nullable: false),
                        SampleNo = c.String(maxLength: 30),
                        CreatedBy = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.HISTestMaster", t => t.TestId)
                .ForeignKey("dbo.SaleInvoice", t => t.SaleInvoiceId)
                .ForeignKey("dbo.TestRequestDetails", t => t.RequestDetailId)
                .Index(t => t.SaleInvoiceId)
                .Index(t => t.TestId)
                .Index(t => t.RequestDetailId);
            
            CreateTable(
                "dbo.SaleInvoice",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        InvoiceNo = c.String(nullable: false, maxLength: 50),
                        InvoiceDate = c.DateTime(nullable: false),
                        RequestDetailId = c.Long(nullable: false),
                        PatientId = c.Long(nullable: false),
                        GrossAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DiscountAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TaxAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        NetAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        PaidAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DueAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        RefDoctorName = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                        CreatedBy = c.String(),
                        ModifiedOn = c.DateTime(nullable: false),
                        ModifiedBy = c.String(),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.PatientDetails", t => t.PatientId)
                .ForeignKey("dbo.TestRequestDetails", t => t.RequestDetailId)
                .Index(t => t.RequestDetailId)
                .Index(t => t.PatientId);
            
            AddColumn("dbo.HISTestMaster", "CreatedBy", c => c.String());
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.SaleInvoiceDetail", "RequestDetailId", "dbo.TestRequestDetails");
            DropForeignKey("dbo.SaleInvoiceDetail", "SaleInvoiceId", "dbo.SaleInvoice");
            DropForeignKey("dbo.SaleInvoice", "RequestDetailId", "dbo.TestRequestDetails");
            DropForeignKey("dbo.SaleInvoice", "PatientId", "dbo.PatientDetails");
            DropForeignKey("dbo.SaleInvoiceDetail", "TestId", "dbo.HISTestMaster");
            DropIndex("dbo.SaleInvoice", new[] { "PatientId" });
            DropIndex("dbo.SaleInvoice", new[] { "RequestDetailId" });
            DropIndex("dbo.SaleInvoiceDetail", new[] { "RequestDetailId" });
            DropIndex("dbo.SaleInvoiceDetail", new[] { "TestId" });
            DropIndex("dbo.SaleInvoiceDetail", new[] { "SaleInvoiceId" });
            DropColumn("dbo.HISTestMaster", "CreatedBy");
            DropTable("dbo.SaleInvoice");
            DropTable("dbo.SaleInvoiceDetail");
        }
    }
}
