-- Purchase order line quantity as decimal (e.g. fractional base units), aligned with SaleDetails.Quantity.
-- Run on the IMS SQL Server database.
-- After this, ensure dbo.AddBillDetails (and any proc that INSERT/UPDATEs PurchaseOrderItems.Quantity)
-- uses a DECIMAL(18,4) parameter for quantity instead of INT/BIGINT.

IF COL_LENGTH('dbo.PurchaseOrderItems', 'Quantity') IS NOT NULL
BEGIN
    DECLARE @t sysname =
        (SELECT TYPE_NAME(system_type_id)
         FROM sys.columns
         WHERE object_id = OBJECT_ID('dbo.PurchaseOrderItems') AND name = 'Quantity');

    IF @t IN (N'bigint', N'int', N'smallint', N'tinyint')
    BEGIN
        ALTER TABLE dbo.PurchaseOrderItems
            ALTER COLUMN Quantity DECIMAL(18, 4) NOT NULL;
        PRINT 'PurchaseOrderItems.Quantity altered to DECIMAL(18,4).';
    END
    ELSE
        PRINT 'PurchaseOrderItems.Quantity is already non-integer type; no ALTER applied.';
END
GO
