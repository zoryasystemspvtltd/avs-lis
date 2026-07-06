-- Allow invoice lines without a lab TestRequest (e.g. radiology-only lines)
DECLARE @fk NVARCHAR(256);
SELECT @fk = fk.name
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.columns c ON fkc.parent_column_id = c.column_id AND fkc.parent_object_id = c.object_id
WHERE fk.parent_object_id = OBJECT_ID(N'dbo.SaleInvoiceDetail') AND c.name = 'RequestDetailId';

IF @fk IS NOT NULL
    EXEC('ALTER TABLE dbo.SaleInvoiceDetail DROP CONSTRAINT [' + @fk + ']');

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.SaleInvoiceDetail') AND name = 'RequestDetailId' AND is_nullable = 0
)
BEGIN
    ALTER TABLE dbo.SaleInvoiceDetail ALTER COLUMN RequestDetailId BIGINT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.SaleInvoiceDetail')
      AND referenced_object_id = OBJECT_ID(N'dbo.TestRequestDetails')
)
BEGIN
    ALTER TABLE dbo.SaleInvoiceDetail WITH CHECK
        ADD CONSTRAINT FK_SaleInvoiceDetail_TestRequestDetails
        FOREIGN KEY (RequestDetailId) REFERENCES dbo.TestRequestDetails(Id);
END
GO
