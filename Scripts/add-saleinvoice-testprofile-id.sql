IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.SaleInvoiceDetail') AND name = 'TestProfileId'
)
BEGIN
    ALTER TABLE dbo.SaleInvoiceDetail ADD TestProfileId INT NULL;
END
GO
