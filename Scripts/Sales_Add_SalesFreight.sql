-- Adds persisted freight (Karaya) for sales. Run on your SQL Server database before deploying the app changes.
-- Adjust procedure definitions to match your existing AddSale / UpdateSale / GetSaleBySaleId bodies.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Sales') AND name = N'SalesFreight'
)
BEGIN
    ALTER TABLE dbo.Sales ADD
        SalesFreight DECIMAL(18, 3) NOT NULL
        CONSTRAINT DF_Sales_SalesFreight DEFAULT (0);
END
GO

/*
Update stored procedures (examples — replace with your actual parameter lists and INSERT/UPDATE columns):

1) AddSale
   - Add parameter: @pSalesFreight DECIMAL(18,3) = 0
   - In INSERT INTO Sales (...), include SalesFreight and VALUES (..., @pSalesFreight, ...)

2) UpdateSale
   - Add parameter: @pSalesFreight DECIMAL(18,3) = 0
   - In UPDATE Sales SET ..., include SalesFreight = @pSalesFreight

3) GetSaleBySaleId (and any other sale SELECT used by the app)
   - Include SalesFreight in the SELECT list so GetSaleByIdAsync can read it.
*/
