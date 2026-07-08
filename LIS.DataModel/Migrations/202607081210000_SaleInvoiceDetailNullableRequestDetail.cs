namespace LIS.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class SaleInvoiceDetailNullableRequestDetail : DbMigration
    {
        public override void Up()
        {
            Sql(@"
DECLARE @fk NVARCHAR(256);
SELECT @fk = fk.name
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.columns c ON fkc.parent_column_id = c.column_id AND fkc.parent_object_id = c.object_id
WHERE fk.parent_object_id = OBJECT_ID(N'dbo.SaleInvoiceDetail') AND c.name = N'RequestDetailId';

IF @fk IS NOT NULL
    EXEC('ALTER TABLE dbo.SaleInvoiceDetail DROP CONSTRAINT [' + @fk + ']');

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.SaleInvoiceDetail') AND name = N'RequestDetailId' AND is_nullable = 0
)
    ALTER TABLE dbo.SaleInvoiceDetail ALTER COLUMN RequestDetailId BIGINT NULL;

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.SaleInvoiceDetail')
      AND referenced_object_id = OBJECT_ID(N'dbo.TestRequestDetails')
)
    ALTER TABLE dbo.SaleInvoiceDetail WITH CHECK
        ADD CONSTRAINT FK_SaleInvoiceDetail_TestRequestDetails
        FOREIGN KEY (RequestDetailId) REFERENCES dbo.TestRequestDetails(Id);
");
        }

        public override void Down()
        {
            Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.SaleInvoiceDetail')
      AND name = N'FK_SaleInvoiceDetail_TestRequestDetails'
)
    ALTER TABLE dbo.SaleInvoiceDetail DROP CONSTRAINT FK_SaleInvoiceDetail_TestRequestDetails;

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.SaleInvoiceDetail') AND name = N'RequestDetailId' AND is_nullable = 1
)
    ALTER TABLE dbo.SaleInvoiceDetail ALTER COLUMN RequestDetailId BIGINT NOT NULL;

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.SaleInvoiceDetail')
      AND referenced_object_id = OBJECT_ID(N'dbo.TestRequestDetails')
)
    ALTER TABLE dbo.SaleInvoiceDetail WITH CHECK
        ADD CONSTRAINT FK_dbo.SaleInvoiceDetail_dbo.TestRequestDetails_RequestDetailId
        FOREIGN KEY (RequestDetailId) REFERENCES dbo.TestRequestDetails(Id);
");
        }
    }
}
