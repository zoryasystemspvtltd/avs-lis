-- Adds discount type/value persistence to Sale Invoice header and lines.
-- Tax remains a manually-entered amount (no schema change needed for tax).
-- Idempotent: safe to run multiple times.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.SaleInvoice') AND name = N'DiscountType'
)
BEGIN
    ALTER TABLE dbo.SaleInvoice ADD DiscountType NVARCHAR(20) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.SaleInvoice') AND name = N'DiscountValue'
)
BEGIN
    ALTER TABLE dbo.SaleInvoice ADD DiscountValue DECIMAL(18,2) NOT NULL CONSTRAINT DF_SaleInvoice_DiscountValue DEFAULT(0);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.SaleInvoiceDetail') AND name = N'DiscountType'
)
BEGIN
    ALTER TABLE dbo.SaleInvoiceDetail ADD DiscountType NVARCHAR(20) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.SaleInvoiceDetail') AND name = N'DiscountValue'
)
BEGIN
    ALTER TABLE dbo.SaleInvoiceDetail ADD DiscountValue DECIMAL(18,2) NOT NULL CONSTRAINT DF_SaleInvoiceDetail_DiscountValue DEFAULT(0);
END
GO
