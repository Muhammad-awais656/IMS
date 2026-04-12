namespace IMS.Models;

/// <summary>
/// Result of the unit price rate calculation, showing how the effective per-unit rate is built.
/// </summary>
public sealed class UnitPriceRateDetail
{
    /// <summary>Final rate per smallest-unit of stock (before any caller-specific rounding).</summary>
    public decimal Rate { get; init; }

    /// <summary>Weighted average purchase price from purchase lines, or product unit price when no purchases exist.</summary>
    public decimal PurchasePrice { get; init; }

    /// <summary>ProductRange unit price for the range tied to the smallest measuring unit.</summary>
    public decimal SmallestUnitRangeUnitPrice { get; init; }

    /// <summary><c>PurchasePrice × SmallestUnitRangeUnitPrice</c> — base stock value per unit before expense allocation.</summary>
    public decimal StockValuePerUnit { get; init; }

    /// <summary>Sum of expense amounts linked to this product.</summary>
    public decimal TotalProductExpenses { get; init; }

    /// <summary>Available stock quantity for the product.</summary>
    public decimal AvailableQuantity { get; init; }

    /// <summary><c>TotalProductExpenses / AvailableQuantity</c> when quantity is positive; otherwise 0.</summary>
    public decimal ExpensePerUnit { get; init; }
    public decimal TotalStockValue { get; init; }
}
