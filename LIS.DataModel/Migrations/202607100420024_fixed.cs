namespace LIS.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _fixed : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PatientVisit",
                c => new
                    {
                        PatientVisitId = c.Long(nullable: false, identity: true),
                        PatientId = c.Long(nullable: false),
                        VisitId = c.String(nullable: false, maxLength: 30),
                        VisitDateTime = c.DateTime(nullable: false),
                        SaleInvoiceId = c.Long(),
                        VisitStatus = c.Int(nullable: false),
                        CreatedBy = c.String(maxLength: 80),
                        CreatedOn = c.DateTime(nullable: false),
                        ModifiedBy = c.String(maxLength: 80),
                        ModifiedOn = c.DateTime(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.PatientVisitId)
                .ForeignKey("dbo.PatientDetails", t => t.PatientId)
                .Index(t => t.PatientId);
            
            AddColumn("dbo.RadiologyRequestDetail", "PatientVisitId", c => c.Long());
            AddColumn("dbo.SaleInvoice", "PatientVisitId", c => c.Long());
            AddColumn("dbo.TestRequestDetails", "PatientVisitId", c => c.Long());
            CreateIndex("dbo.RadiologyRequestDetail", "PatientVisitId");
            CreateIndex("dbo.SaleInvoice", "PatientVisitId");
            CreateIndex("dbo.TestRequestDetails", "PatientVisitId");
            AddForeignKey("dbo.RadiologyRequestDetail", "PatientVisitId", "dbo.PatientVisit", "PatientVisitId");
            AddForeignKey("dbo.SaleInvoice", "PatientVisitId", "dbo.PatientVisit", "PatientVisitId");
            AddForeignKey("dbo.TestRequestDetails", "PatientVisitId", "dbo.PatientVisit", "PatientVisitId");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TestRequestDetails", "PatientVisitId", "dbo.PatientVisit");
            DropForeignKey("dbo.SaleInvoice", "PatientVisitId", "dbo.PatientVisit");
            DropForeignKey("dbo.RadiologyRequestDetail", "PatientVisitId", "dbo.PatientVisit");
            DropForeignKey("dbo.PatientVisit", "PatientId", "dbo.PatientDetails");
            DropIndex("dbo.TestRequestDetails", new[] { "PatientVisitId" });
            DropIndex("dbo.SaleInvoice", new[] { "PatientVisitId" });
            DropIndex("dbo.RadiologyRequestDetail", new[] { "PatientVisitId" });
            DropIndex("dbo.PatientVisit", new[] { "PatientId" });
            DropColumn("dbo.TestRequestDetails", "PatientVisitId");
            DropColumn("dbo.SaleInvoice", "PatientVisitId");
            DropColumn("dbo.RadiologyRequestDetail", "PatientVisitId");
            DropTable("dbo.PatientVisit");
        }
    }
}
