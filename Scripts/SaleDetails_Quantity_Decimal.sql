-- Sale line quantity as decimal (e.g. 1.03 bags × 35kg unit).
-- Run on your SQL Server database used by IMS.
-- After this, alter dbo.AddSaleDetails (and any proc that INSERT/UPDATEs SaleDetails.Quantity)
-- so parameter @pQuantity is DECIMAL(18,4) instead of BIGINT.

IF COL_LENGTH('dbo.SaleDetails', 'Quantity') IS NOT NULL
BEGIN
    DECLARE @t sysname =
        (SELECT TYPE_NAME(system_type_id)
         FROM sys.columns
         WHERE object_id = OBJECT_ID('dbo.SaleDetails') AND name = 'Quantity');

    IF @t IN (N'bigint', N'int', N'smallint', N'tinyint')
    BEGIN
        ALTER TABLE dbo.SaleDetails
            ALTER COLUMN Quantity DECIMAL(18, 4) NOT NULL;
        PRINT 'SaleDetails.Quantity altered to DECIMAL(18,4).';
    END
    ELSE
        PRINT 'SaleDetails.Quantity is already non-integer type; no ALTER applied.';
END
GO
