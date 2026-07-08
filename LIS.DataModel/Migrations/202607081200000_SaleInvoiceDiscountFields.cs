namespace LIS.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class SaleInvoiceDiscountFields : DbMigration
    {
        public override void Up()
        {
            Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.SaleInvoice') AND name = N'DiscountType'
)
    ALTER TABLE dbo.SaleInvoice ADD DiscountType NVARCHAR(20) NULL;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.SaleInvoice') AND name = N'DiscountValue'
)
    ALTER TABLE dbo.SaleInvoice ADD DiscountValue DECIMAL(18,2) NOT NULL CONSTRAINT DF_SaleInvoice_DiscountValue DEFAULT(0);

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.SaleInvoiceDetail') AND name = N'DiscountType'
)
    ALTER TABLE dbo.SaleInvoiceDetail ADD DiscountType NVARCHAR(20) NULL;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.SaleInvoiceDetail') AND name = N'DiscountValue'
)
    ALTER TABLE dbo.SaleInvoiceDetail ADD DiscountValue DECIMAL(18,2) NOT NULL CONSTRAINT DF_SaleInvoiceDetail_DiscountValue DEFAULT(0);
");
        }

        public override void Down()
        {
            Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.SaleInvoiceDetail') AND name = N'DiscountValue')
    ALTER TABLE dbo.SaleInvoiceDetail DROP CONSTRAINT DF_SaleInvoiceDetail_DiscountValue;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.SaleInvoiceDetail') AND name = N'DiscountValue')
    ALTER TABLE dbo.SaleInvoiceDetail DROP COLUMN DiscountValue;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.SaleInvoiceDetail') AND name = N'DiscountType')
    ALTER TABLE dbo.SaleInvoiceDetail DROP COLUMN DiscountType;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.SaleInvoice') AND name = N'DiscountValue')
    ALTER TABLE dbo.SaleInvoice DROP CONSTRAINT DF_SaleInvoice_DiscountValue;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.SaleInvoice') AND name = N'DiscountValue')
    ALTER TABLE dbo.SaleInvoice DROP COLUMN DiscountValue;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.SaleInvoice') AND name = N'DiscountType')
    ALTER TABLE dbo.SaleInvoice DROP COLUMN DiscountType;
");
        }
    }
}
