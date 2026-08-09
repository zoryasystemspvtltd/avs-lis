namespace LIS.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class notification : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.NotificationAudit",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        PatientId = c.Long(),
                        PatientName = c.String(maxLength: 50),
                        InvoiceId = c.Long(),
                        InvoiceNo = c.String(maxLength: 50),
                        EventCode = c.String(nullable: false, maxLength: 50),
                        Channel = c.Int(nullable: false),
                        TemplateId = c.Int(),
                        TemplateVersion = c.Int(),
                        ProviderName = c.String(maxLength: 50),
                        RecipientPhone = c.String(maxLength: 20),
                        MessageBody = c.String(),
                        Status = c.Int(nullable: false),
                        RetryCount = c.Int(nullable: false),
                        Priority = c.Int(nullable: false),
                        ProviderResponse = c.String(maxLength: 500),
                        ErrorMessage = c.String(maxLength: 500),
                        SecureLinkToken = c.String(maxLength: 100),
                        CorrelationId = c.String(maxLength: 64),
                        ElapsedTimeMs = c.Int(),
                        CreatedOn = c.DateTime(nullable: false),
                        SentOn = c.DateTime(),
                        NextRetryOn = c.DateTime(),
                        CreatedBy = c.String(maxLength: 80),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.NotificationConfiguration",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        IsEnabled = c.Boolean(nullable: false),
                        ChannelMode = c.Int(nullable: false),
                        RetryCount = c.Int(nullable: false),
                        RetryIntervalSeconds = c.Int(nullable: false),
                        DefaultChannel = c.Int(nullable: false),
                        SmsEnabled = c.Boolean(nullable: false),
                        WhatsAppEnabled = c.Boolean(nullable: false),
                        CreatedOn = c.DateTime(nullable: false),
                        CreatedBy = c.String(maxLength: 80),
                        ModifiedOn = c.DateTime(nullable: false),
                        ModifiedBy = c.String(maxLength: 80),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.NotificationTemplate",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        EventCode = c.String(nullable: false, maxLength: 50),
                        Channel = c.Int(nullable: false),
                        Name = c.String(nullable: false, maxLength: 100),
                        Body = c.String(nullable: false),
                        IsDefault = c.Boolean(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        Version = c.Int(nullable: false),
                        EffectiveFrom = c.DateTime(nullable: false),
                        EffectiveTo = c.DateTime(),
                        CreatedOn = c.DateTime(nullable: false),
                        CreatedBy = c.String(maxLength: 80),
                        ModifiedOn = c.DateTime(nullable: false),
                        ModifiedBy = c.String(maxLength: 80),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.SecureLinkToken",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Token = c.String(nullable: false, maxLength: 64),
                        InvoiceNo = c.String(maxLength: 50),
                        PatientId = c.Long(),
                        ExpiresOn = c.DateTime(nullable: false),
                        IsUsed = c.Boolean(nullable: false),
                        CreatedOn = c.DateTime(nullable: false),
                        CreatedBy = c.String(maxLength: 80),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.SecureLinkToken");
            DropTable("dbo.NotificationTemplate");
            DropTable("dbo.NotificationConfiguration");
            DropTable("dbo.NotificationAudit");
        }
    }
}
