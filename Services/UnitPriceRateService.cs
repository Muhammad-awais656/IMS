using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;

namespace IMS.Services
{
    /// <inheritdoc cref="IUnitPriceRateService"/>
    public sealed class UnitPriceRateService : IUnitPriceRateService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<UnitPriceRateService> _logger;
        private readonly IUnitConversionService _unitConversionService;

        public UnitPriceRateService(
            IDbContextFactory dbContextFactory,
            ILogger<UnitPriceRateService> logger,
            IUnitConversionService unitConversionService)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _unitConversionService = unitConversionService;
        }

        /// <inheritdoc />
        public async Task<decimal> GetUnitPriceRateAsync(long productId,long productRangeId, CancellationToken cancellationToken = default)
        {
            //var d = await GetUnitPriceRateDetailAsync(productId, cancellationToken);
            var d = await GetUnitPriceRateDetail1Async(productId, productRangeId, cancellationToken);
            return d.Rate;
        }

        /// <inheritdoc />
        public async Task<UnitPriceRateDetail> GetUnitPriceRateDetailAsync(long productId, CancellationToken cancellationToken = default)
        {
            if (productId <= 0)
                return ZeroDetail();

            try
            {
                await using var connection = new SqlConnection(_dbContextFactory.DBConnectionString());
                await connection.OpenAsync(cancellationToken);

                var purchasePrice = await GetWeightedAveragePurchasePriceAsync(connection, productId, cancellationToken);
                if (purchasePrice <= 0)
                    purchasePrice = await GetProductUnitPriceAsync(connection, productId, cancellationToken);

                var smallestRangeUnitPrice = await ResolveSmallestUnitRangeUnitPriceAsync(connection, productId, cancellationToken);
                var stockValuePerUnit = purchasePrice * smallestRangeUnitPrice;

                var totalExpenses = await GetTotalProductExpensesAsync(connection, productId, cancellationToken);
                var availableQty = await GetAvailableQuantityAsync(connection, productId, cancellationToken);

                var expensePerUnit = availableQty > 0 ? totalExpenses / availableQty : 0m;
                var rate = stockValuePerUnit + expensePerUnit;

                return new UnitPriceRateDetail
                {
                    Rate = rate,
                    PurchasePrice = purchasePrice,
                    SmallestUnitRangeUnitPrice = smallestRangeUnitPrice,
                    StockValuePerUnit = stockValuePerUnit,
                    TotalProductExpenses = totalExpenses,
                    AvailableQuantity = availableQty,
                    ExpensePerUnit = expensePerUnit
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUnitPriceRateDetailAsync failed for product {ProductId}", productId);
                throw;
            }
        }
        public async Task<UnitPriceRateDetail> GetUnitPriceRateDetail1Async(long productId,long productRangeId, CancellationToken cancellationToken = default)
        {
            if (productId <= 0)
                return ZeroDetail();

            try
            {
                await using var connection = new SqlConnection(_dbContextFactory.DBConnectionString());
                await connection.OpenAsync(cancellationToken);

                //var purchasePrice = await GetWeightedAveragePurchasePriceAsync(connection, productId, cancellationToken);
                //if (purchasePrice <= 0)
                //    purchasePrice = await GetProductUnitPriceAsync(connection, productId, cancellationToken);

                var smallestRangeUnitPrice = await ResolveSmallestUnitRangeUnitPriceAsync(connection, productId, cancellationToken);
                //var stockValuePerUnit = purchasePrice * smallestRangeUnitPrice;

                var totalExpenses = await GetTotalProductExpensesAsync(connection, productId, cancellationToken);
                var availableQty = await GetAvailableQuantityAsync(connection, productId, cancellationToken);

                var (selectedMeasuringUnitId, productRangeUnitPrice) =
                    await GetSelectedProductRangeMetaAsync(connection, productId, productRangeId, cancellationToken);
                var smallestMeasuringUnitId = await GetSmallestMeasuringUnitIdForProductAsync(connection, productId, cancellationToken);

                // How many "smallest MU" fit in 1 unit of the selected MU (same as ConvertUnitAsync(selected, smallest, 1)).
                var unitConversionFactor = await ResolveSmallestUnitsPerOneSelectedUnitAsync(
                    selectedMeasuringUnitId,
                    smallestMeasuringUnitId);

                // Landed cost per smallest-unit of stock (matches old: (smallest*qty + expenses)/qty).
                decimal totalStockValue = 0m;
                decimal landedPerSmallestUnit;
                if (availableQty > 0)
                {
                    totalStockValue = smallestRangeUnitPrice * availableQty + totalExpenses;
                    landedPerSmallestUnit = totalStockValue / availableQty;
                }
                else
                {
                    landedPerSmallestUnit = smallestRangeUnitPrice > 0 ? smallestRangeUnitPrice : 0m;
                }

                decimal rate = landedPerSmallestUnit;

                // Compare apples to apples: never multiply "rate" first (that included expense per unit) and then
                // set previousRate = smallest * factor — that compared scaled-landed to catalog-only scaled.
                if (unitConversionFactor > 0 && productRangeUnitPrice > 0)
                {
                    // Catalog anchor: smallest-range list price scaled to the selected MU (110 * correct factor, not an arbitrary UC row).
                    var previousRate = smallestRangeUnitPrice * unitConversionFactor;
                    var scaledLandedRate = landedPerSmallestUnit * unitConversionFactor;
                    var priceDifference = scaledLandedRate - previousRate;
                    rate = productRangeUnitPrice + priceDifference;
                }

                return new UnitPriceRateDetail
                {
                    Rate = rate,
                    SmallestUnitRangeUnitPrice = smallestRangeUnitPrice,
                    TotalProductExpenses = totalExpenses,
                    AvailableQuantity = availableQty,
                    TotalStockValue = totalStockValue

                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUnitPriceRateDetailAsync failed for product {ProductId}", productId);
                throw;
            }
        }

        private static UnitPriceRateDetail ZeroDetail() => new()
        {
            Rate = 0m,
            PurchasePrice = 0,
            SmallestUnitRangeUnitPrice = 0,
            StockValuePerUnit = 0,
            TotalProductExpenses = 0,
            AvailableQuantity = 0,
            ExpensePerUnit = 0,
            TotalStockValue = 0
        };

        private static async Task<decimal> GetWeightedAveragePurchasePriceAsync(SqlConnection connection, long productId, CancellationToken ct)
        {
            const string sql = """
                SELECT
                    CASE WHEN SUM(CAST(poi.Quantity AS DECIMAL(18, 4))) > 0
                        THEN SUM(CAST(poi.PurchasePrice AS DECIMAL(18, 4)) * CAST(poi.Quantity AS DECIMAL(18, 4)))
                            / SUM(CAST(poi.Quantity AS DECIMAL(18, 4)))
                        ELSE NULL
                    END
                FROM PurchaseOrderItems poi
                INNER JOIN PurchaseOrders po ON po.PurchaseOrderId = poi.PurchaseOrderId_FK
                    AND ISNULL(po.IsDeleted,0)=0
                WHERE poi.PrductId_FK = @pProductId
                """;

            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@pProductId", productId);
            var o = await cmd.ExecuteScalarAsync(ct);
            if (o == null || o == DBNull.Value)
                return 0m;
            return Convert.ToDecimal(o);
        }

        private static async Task<decimal> GetProductUnitPriceAsync(SqlConnection connection, long productId, CancellationToken ct)
        {
            const string sql = "SELECT UnitPrice FROM Products WHERE ProductId = @pProductId";
            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@pProductId", productId);
            var o = await cmd.ExecuteScalarAsync(ct);
            if (o == null || o == DBNull.Value)
                return 0m;
            return Convert.ToDecimal(o);
        }

        /// <summary>
        /// ProductRange.UnitPrice for the smallest measuring unit; if no row matches, lowest range unit price; if still none, 1 (neutral multiplier).
        /// </summary>
        private static async Task<decimal> ResolveSmallestUnitRangeUnitPriceAsync(SqlConnection connection, long productId, CancellationToken ct)
        {
            const string sqlSmallest = """
                SELECT TOP 1 pr.UnitPrice
                FROM ProductRange pr
                INNER JOIN Products p ON p.ProductId = pr.ProductId_FK AND p.ProductId = @pProductId
                INNER JOIN AdminMeasuringUnits mu ON mu.MeasuringUnitId = pr.MeasuringUnitId_FK
                    AND p.MeasuringUnitTypeId_FK IS NOT NULL
                    AND mu.MeasuringUnitTypeId_FK = p.MeasuringUnitTypeId_FK
                    AND mu.IsSmallestUnit = 1
                WHERE ISNULL(pr.IsDeleted, 0) = 0
                
                """;

            await using (var cmd = new SqlCommand(sqlSmallest, connection))
            {
                cmd.Parameters.AddWithValue("@pProductId", productId);
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                    return reader.GetDecimal(0);
            }

            const string sqlFallback = """
                SELECT TOP 1 pr.UnitPrice
                FROM ProductRange pr
                WHERE pr.ProductId_FK = @pProductId
                  AND ISNULL(pr.IsDeleted, 0) = 0
                  
                """;

            await using (var fb = new SqlCommand(sqlFallback, connection))
            {
                fb.Parameters.AddWithValue("@pProductId", productId);
                var o = await fb.ExecuteScalarAsync(ct);
                if (o != null && o != DBNull.Value)
                    return Convert.ToDecimal(o);
            }

            return 0m;
        }

        private static async Task<decimal> GetTotalProductExpensesAsync(SqlConnection connection, long productId, CancellationToken ct)
        {
            const string sql = """
                SELECT ISNULL(SUM(Amount), 0)
                FROM Expenses
                WHERE ProductId_FK = @pProductId
                """;

            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@pProductId", productId);
            var o = await cmd.ExecuteScalarAsync(ct);
            if (o == null || o == DBNull.Value)
                return 0m;
            return Convert.ToDecimal(o);
        }
      

        private static async Task<decimal> GetAvailableQuantityAsync(SqlConnection connection, long productId, CancellationToken ct)
        {
            const string sql = """
                SELECT ISNULL(AvailableQuantity, 0)
                FROM StockMaster
                WHERE ProductId_FK = @pProductId
                """;

            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@pProductId", productId);
            var o = await cmd.ExecuteScalarAsync(ct);
            if (o == null || o == DBNull.Value)
                return 0m;
            return Convert.ToDecimal(o);
        }

        private static async Task<(long? MeasuringUnitId, decimal UnitPrice)> GetSelectedProductRangeMetaAsync(
            SqlConnection connection,
            long productId,
            long productRangeId,
            CancellationToken ct)
        {
            const string sql = """
                SELECT pr.MeasuringUnitId_FK, pr.UnitPrice
                FROM ProductRange pr
                INNER JOIN Products p ON p.ProductId = pr.ProductId_FK AND p.ProductId = @pProductId
                WHERE pr.ProductRangeId = @pProductRangeId
                """;

            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@pProductId", productId);
            cmd.Parameters.AddWithValue("@pProductRangeId", productRangeId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return (null, 0m);

            var muOrd = reader.GetOrdinal("MeasuringUnitId_FK");
            var upOrd = reader.GetOrdinal("UnitPrice");
            var muId = reader.IsDBNull(muOrd) ? (long?)null : reader.GetInt64(muOrd);
            var unitPrice = reader.IsDBNull(upOrd) ? 0m : reader.GetDecimal(upOrd);
            return (muId, unitPrice);
        }

        private static async Task<long?> GetSmallestMeasuringUnitIdForProductAsync(
            SqlConnection connection,
            long productId,
            CancellationToken ct)
        {
            const string sql = """
                SELECT TOP 1 mu.MeasuringUnitId
                FROM ProductRange pr
                INNER JOIN Products p ON p.ProductId = pr.ProductId_FK AND p.ProductId = @pProductId
                INNER JOIN AdminMeasuringUnits mu ON mu.MeasuringUnitId = pr.MeasuringUnitId_FK
                    AND p.MeasuringUnitTypeId_FK IS NOT NULL
                    AND mu.MeasuringUnitTypeId_FK = p.MeasuringUnitTypeId_FK
                    AND mu.IsSmallestUnit = 1
                WHERE ISNULL(pr.IsDeleted, 0) = 0
                ORDER BY pr.ProductRangeId
                """;

            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@pProductId", productId);
            var o = await cmd.ExecuteScalarAsync(ct);
            if (o == null || o == DBNull.Value)
                return null;
            return Convert.ToInt64(o);
        }

        /// <summary>
        /// Number of smallest-MU units equivalent to 1 unit of the selected MU (matches <see cref="IUnitConversionService.ConvertUnitAsync"/> selected → smallest).
        /// </summary>
        private async Task<decimal> ResolveSmallestUnitsPerOneSelectedUnitAsync(
            long? selectedMeasuringUnitId,
            long? smallestMeasuringUnitId)
        {
            if (!selectedMeasuringUnitId.HasValue || !smallestMeasuringUnitId.HasValue)
                return 0m;
            if (selectedMeasuringUnitId.Value == smallestMeasuringUnitId.Value)
                return 1m;

            var forward = await _unitConversionService.ConvertUnitAsync(
                selectedMeasuringUnitId.Value,
                smallestMeasuringUnitId.Value,
                1m);
            if (forward.HasValue)
                return forward.Value;

            var reverse = await _unitConversionService.ConvertUnitAsync(
                smallestMeasuringUnitId.Value,
                selectedMeasuringUnitId.Value,
                1m);
            if (reverse.HasValue && reverse.Value != 0m)
                return 1m / reverse.Value;

            _logger.LogWarning(
                "No unit conversion between selected MU {Sel} and smallest MU {Sm} — cannot scale catalog rate.",
                selectedMeasuringUnitId,
                smallestMeasuringUnitId);
            return 0m;
        }
    }
}
