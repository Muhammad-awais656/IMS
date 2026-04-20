using IMS.Models;

namespace IMS.Common_Interfaces
{
    /// <summary>
    /// Computes an effective per-unit stock rate: (weighted purchase price × smallest product-range unit price) + (product expenses ÷ available stock).
    /// </summary>
    public interface IUnitPriceRateService
    {
        /// <summary>
        /// Returns the effective unit rate for the product’s available stock (smallest-unit basis).
        /// Formula: <c>(purchasePrice × smallestRangeUnitPrice) + (sum(expenses on product) / availableQuantity)</c>.
        /// Purchase price is the quantity-weighted average <c>PurchasePrice</c> from <c>PurchaseOrderItems</c>, or <c>Products.UnitPrice</c> if there are no purchase lines.
        /// </summary>
        Task<decimal> GetUnitPriceRateAsync(long productId, long productRangeId, CancellationToken cancellationToken = default);

        /// <summary>Same calculation as <see cref="GetUnitPriceRateAsync"/>, with a full breakdown.</summary>
        Task<UnitPriceRateDetail> GetUnitPriceRateDetailAsync(long productId, CancellationToken cancellationToken = default);
    }
}
