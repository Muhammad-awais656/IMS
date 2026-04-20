using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMS.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(IDbContextFactory dbContextFactory, ILogger<DashboardService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        private static void AddBranchParameter(SqlCommand command, int? branchId)
        {
            command.Parameters.AddWithValue("@BranchId", branchId.HasValue ? branchId.Value : DBNull.Value);
        }

        public async Task<int> GetTotalCategoryCount(int? branchId = null)
        {
            int totalCount = 0;
            try
            {
                using var connection = new SqlConnection(_dbContextFactory.DBConnectionString());
                await connection.OpenAsync();
                var sql = branchId.HasValue
                    ? @"SELECT COUNT(1) FROM AdminCategory c
INNER JOIN Users u ON u.UserId = c.CreatedBy
WHERE c.IsEnabled = 1 AND u.BranchId = @BranchId"
                    : "SELECT COUNT(1) FROM AdminCategory WHERE IsEnabled = 1";
                using var command = new SqlCommand(sql, connection);
                if (branchId.HasValue)
                    AddBranchParameter(command, branchId);
                totalCount = (int)await command.ExecuteScalarAsync()!;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetTotalCategoryCount failed");
            }

            return totalCount;
        }

        public async Task<int> GetTotalProductCount(int? branchId = null)
        {
            int totalCount = 0;
            try
            {
                using var connection = new SqlConnection(_dbContextFactory.DBConnectionString());
                await connection.OpenAsync();
                var sql = branchId.HasValue
                    ? @"SELECT COUNT(1) FROM Products p
INNER JOIN Users u ON u.UserId = p.CreatedBy
WHERE p.IsEnabled = 1 AND u.BranchId = @BranchId"
                    : "SELECT COUNT(1) FROM Products WHERE IsEnabled = 1";
                using var command = new SqlCommand(sql, connection);
                if (branchId.HasValue)
                    AddBranchParameter(command, branchId);
                totalCount = (int)await command.ExecuteScalarAsync()!;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetTotalProductCount failed");
            }

            return totalCount;
        }

        public async Task<int> GetTotalVendorsCount(int? branchId = null)
        {
            int totalCount = 0;
            try
            {
                using var connection = new SqlConnection(_dbContextFactory.DBConnectionString());
                await connection.OpenAsync();
                var sql = branchId.HasValue
                    ? @"SELECT COUNT(1) FROM AdminSuppliers v
INNER JOIN Users u ON u.UserId = v.CreatedBy
WHERE v.IsDeleted = 0 AND u.BranchId = @BranchId"
                    : "SELECT COUNT(1) FROM AdminSuppliers WHERE IsDeleted = 0";
                using var command = new SqlCommand(sql, connection);
                if (branchId.HasValue)
                    AddBranchParameter(command, branchId);
                totalCount = (int)await command.ExecuteScalarAsync()!;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetTotalVendorsCount failed");
            }

            return totalCount;
        }

        public async Task<SalesChartViewModel> GetLast12MonthsSalesAsync(int? branchId = null)
        {
            var viewModel = new SalesChartViewModel();

            using var connection = new SqlConnection(_dbContextFactory.DBConnectionString());
            await connection.OpenAsync();

            var branchFilter = branchId.HasValue
                ? @"AND EXISTS (SELECT 1 FROM Users u WHERE u.UserId = s.CreatedBy AND u.BranchId = @BranchId)"
                : string.Empty;

            var query = $@"
;WITH Last12Months AS
(
    SELECT TOP (12)
        FORMAT(DATEADD(MONTH, - (n-1), GETDATE()), 'MMM-yyyy') AS Month,
        YEAR(DATEADD(MONTH, - (n-1), GETDATE())) AS Yr,
        MONTH(DATEADD(MONTH, - (n-1), GETDATE())) AS Mn,
        DATEADD(MONTH, - (n-1), DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)) AS MonthStart,
        EOMONTH(DATEADD(MONTH, - (n-1), GETDATE())) AS MonthEnd
    FROM (SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
          FROM sys.objects) AS Numbers
)
SELECT m.Month,
       ISNULL(SUM(s.TotalAmount), 0) AS TotalSales
FROM Last12Months m
LEFT JOIN Sales s
    ON s.SaleDate >= m.MonthStart
   AND s.SaleDate <= m.MonthEnd
   AND s.IsDeleted = 0
   {branchFilter}
GROUP BY m.Month, m.Yr, m.Mn
ORDER BY m.Yr, m.Mn;
";

            using var command = new SqlCommand(query, connection);
            if (branchId.HasValue)
                AddBranchParameter(command, branchId);
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    viewModel.Months.Add(reader.GetString(0));
                    viewModel.Sales.Add(reader.GetDecimal(1));
                }
            }

            return viewModel;
        }

        public async Task<decimal> GetCurrentMonthRevenueAsync(int? branchId = null)
        {
            decimal currentMonthRevenue = 0;
            try
            {
                using var connection = new SqlConnection(_dbContextFactory.DBConnectionString());
                await connection.OpenAsync();

                var branchFilter = branchId.HasValue
                    ? "AND EXISTS (SELECT 1 FROM Users u WHERE u.UserId = Sales.CreatedBy AND u.BranchId = @BranchId)"
                    : string.Empty;

                var query = $@"
SELECT ISNULL(SUM(TotalAmount), 0) AS CurrentMonthRevenue
FROM Sales
WHERE YEAR(SaleDate) = YEAR(GETDATE())
  AND MONTH(SaleDate) = MONTH(GETDATE())
  AND IsDeleted = 0
  {branchFilter}";

                using var command = new SqlCommand(query, connection);
                if (branchId.HasValue)
                    AddBranchParameter(command, branchId);

                var result = await command.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    currentMonthRevenue = Convert.ToDecimal(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetCurrentMonthRevenueAsync failed");
            }

            return currentMonthRevenue;
        }

        public async Task<List<StockStaus>> GetStockStatusAsync(long? productId, int? branchId = null)
        {
            var stockData = new List<StockStaus>();

            using var connection = new SqlConnection(_dbContextFactory.DBConnectionString());
            await connection.OpenAsync();

            var branchFilter = branchId.HasValue
                ? "AND EXISTS (SELECT 1 FROM Users u WHERE u.UserId = p.CreatedBy AND u.BranchId = @BranchId)"
                : string.Empty;

            var query = $@"
SELECT
    p.ProductId,
    p.ProductName,
    ISNULL(SUM(s.TotalQuantity), 0) AS StockIn,
    ISNULL(SUM(s.AvailableQuantity), 0) AS StockAvailable,
    ISNULL(SUM(s.UsedQuantity), 0) AS StockUsed
FROM Products p
LEFT JOIN StockMaster s ON s.ProductId_FK = p.ProductId
WHERE p.ProductId = @ProductId
{branchFilter}
GROUP BY p.ProductId, p.ProductName";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ProductId", productId);
            if (branchId.HasValue)
                AddBranchParameter(command, branchId);

            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    var stock = new StockStaus
                    {
                        ProductId = reader.GetInt64(reader.GetOrdinal("ProductId")),
                        ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                        InStockCount = reader.IsDBNull(reader.GetOrdinal("StockIn")) ? 0 : reader.GetDecimal(reader.GetOrdinal("StockIn")),
                        OutOfStockCount = reader.IsDBNull(reader.GetOrdinal("StockUsed")) ? 0 : reader.GetDecimal(reader.GetOrdinal("StockUsed")),
                        AvailableStockCount = reader.IsDBNull(reader.GetOrdinal("StockAvailable")) ? 0 : reader.GetDecimal(reader.GetOrdinal("StockAvailable")),
                    };
                    stockData.Add(stock);
                }
                else
                {
                    stockData.Add(new StockStaus
                    {
                        ProductId = productId,
                        ProductName = "Unknown Product",
                        InStockCount = 0,
                        OutOfStockCount = 0,
                        AvailableStockCount = 0
                    });
                }
            }

            return stockData;
        }

        private static void AddDateRangeAndBranchParameters(SqlCommand cmd, DateTime fromDate, DateTime toDate, int? branchId)
        {
            cmd.Parameters.AddWithValue("@FromDate", fromDate.Date);
            cmd.Parameters.AddWithValue("@ToDate", toDate.Date);
            cmd.Parameters.AddWithValue("@BranchId", branchId.HasValue ? branchId.Value : DBNull.Value);
        }

        public async Task<AnalyticsSummaryDto> GetAnalyticsSummaryAsync(DateTime fromDate, DateTime toDate, int? branchId = null)
        {
            var dto = new AnalyticsSummaryDto();
            try
            {
                using var connection = new SqlConnection(_dbContextFactory.DBConnectionString());
                await connection.OpenAsync();

                const string branchSales = @" AND (@BranchId IS NULL OR EXISTS (SELECT 1 FROM Users u WHERE u.UserId = s.CreatedBy AND u.BranchId = @BranchId))";
                const string branchPo = @" AND (@BranchId IS NULL OR EXISTS (SELECT 1 FROM Users u WHERE u.UserId = p.CreatedBy AND u.BranchId = @BranchId))";
                const string branchEx = @" AND (@BranchId IS NULL OR EXISTS (SELECT 1 FROM Users u WHERE u.UserId = e.CreatedBy AND u.BranchId = @BranchId))";
                const string branchCust = @" AND (@BranchId IS NULL OR (c.CreatedBy IS NOT NULL AND EXISTS (SELECT 1 FROM Users u WHERE u.UserId = c.CreatedBy AND u.BranchId = @BranchId)))";
                const string branchProd = @" AND (@BranchId IS NULL OR EXISTS (SELECT 1 FROM Users u WHERE u.UserId = p.CreatedBy AND u.BranchId = @BranchId))";
                const string branchEmp = @" AND (@BranchId IS NULL OR EXISTS (SELECT 1 FROM Users u WHERE u.UserId = e.CreatedByUserIdFk AND u.BranchId = @BranchId))";

                async Task ReadSumCountAsync(string sql, Action<decimal, int> set)
                {
                    using var cmd = new SqlCommand(sql, connection);
                    AddDateRangeAndBranchParameters(cmd, fromDate, toDate, branchId);
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        var sum = reader.IsDBNull(0) ? 0m : reader.GetDecimal(0);
                        var cnt = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                        set(sum, cnt);
                    }
                }

                async Task ReadIntAsync(string sql, Action<int> set)
                {
                    using var cmd = new SqlCommand(sql, connection);
                    AddDateRangeAndBranchParameters(cmd, fromDate, toDate, branchId);
                    var o = await cmd.ExecuteScalarAsync();
                    set(o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o));
                }

                await ReadSumCountAsync($@"
SELECT ISNULL(SUM(s.TotalAmount), 0), COUNT(1)
FROM Sales s
WHERE s.IsDeleted = 0
  AND CAST(s.SaleDate AS DATE) BETWEEN @FromDate AND @ToDate
  {branchSales}", (a, c) => { dto.SalesTotal = a; dto.SalesCount = c; });

                await ReadSumCountAsync($@"
SELECT ISNULL(SUM(p.TotalAmount), 0), COUNT(1)
FROM PurchaseOrders p
WHERE p.IsDeleted = 0
  AND CAST(p.PurchaseOrderDate AS DATE) BETWEEN @FromDate AND @ToDate
  {branchPo}", (a, c) => { dto.PurchaseTotal = a; dto.PurchaseCount = c; });

                await ReadSumCountAsync($@"
SELECT ISNULL(SUM(e.Amount), 0), COUNT(1)
FROM Expenses e
WHERE CAST(e.ExpenseDate AS DATE) BETWEEN @FromDate AND @ToDate
  {branchEx}", (a, c) => { dto.ExpensesTotal = a; dto.ExpensesCount = c; });

                await ReadIntAsync($@"
SELECT COUNT(1)
FROM Customers c
WHERE c.IsEnabled = 1 AND c.CreatedDate IS NOT NULL
  AND CAST(c.CreatedDate AS DATE) BETWEEN @FromDate AND @ToDate
  {branchCust}", v => dto.NewCustomers = v);

                await ReadIntAsync($@"
SELECT COUNT(1)
FROM Products p
WHERE p.IsEnabled = 1
  AND CAST(p.CreatedDate AS DATE) BETWEEN @FromDate AND @ToDate
  {branchProd}", v => dto.NewProducts = v);

                await ReadIntAsync($@"
SELECT COUNT(1)
FROM Employees e
WHERE e.IsDeleted = 0 AND e.CreatedDate IS NOT NULL
  AND CAST(e.CreatedDate AS DATE) BETWEEN @FromDate AND @ToDate
  {branchEmp}", v => dto.NewEmployees = v);

                var stockSql = $@"
SELECT ISNULL(SUM(s.AvailableQuantity), 0)
FROM StockMaster s
INNER JOIN Products p ON p.ProductId = s.ProductId_FK
WHERE p.IsEnabled = 1
  AND (@BranchId IS NULL OR EXISTS (SELECT 1 FROM Users u WHERE u.UserId = p.CreatedBy AND u.BranchId = @BranchId))";
                using (var cmd = new SqlCommand(stockSql, connection))
                {
                    cmd.Parameters.AddWithValue("@BranchId", branchId.HasValue ? branchId.Value : DBNull.Value);
                    var o = await cmd.ExecuteScalarAsync();
                    dto.StockAvailableTotal = o == null || o == DBNull.Value ? 0 : Convert.ToDecimal(o);
                }

                var activeProdSql = branchId.HasValue
                    ? @"SELECT COUNT(1) FROM Products p
INNER JOIN Users u ON u.UserId = p.CreatedBy
WHERE p.IsEnabled = 1 AND u.BranchId = @BranchId"
                    : "SELECT COUNT(1) FROM Products WHERE IsEnabled = 1";
                using (var cmd = new SqlCommand(activeProdSql, connection))
                {
                    if (branchId.HasValue)
                        cmd.Parameters.AddWithValue("@BranchId", branchId.Value);
                    var o = await cmd.ExecuteScalarAsync();
                    dto.ActiveProductsTotal = o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
                }

                dto.NetFlowHint = dto.SalesTotal - dto.PurchaseTotal - dto.ExpensesTotal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetAnalyticsSummaryAsync failed");
            }

            return dto;
        }

        public async Task<IReadOnlyList<SalesTrendPointDto>> GetSalesTrendAsync(DateTime fromDate, DateTime toDate, int? branchId = null)
        {
            var list = new List<SalesTrendPointDto>();
            var days = (toDate.Date - fromDate.Date).TotalDays + 1;
            var branchFilter = @" AND (@BranchId IS NULL OR EXISTS (SELECT 1 FROM Users u WHERE u.UserId = s.CreatedBy AND u.BranchId = @BranchId))";

            try
            {
                using var connection = new SqlConnection(_dbContextFactory.DBConnectionString());
                await connection.OpenAsync();

                string sql;
                if (days <= 31)
                {
                    sql = $@"
SELECT CAST(s.SaleDate AS DATE), ISNULL(SUM(s.TotalAmount), 0)
FROM Sales s
WHERE s.IsDeleted = 0
  AND CAST(s.SaleDate AS DATE) BETWEEN @FromDate AND @ToDate
  {branchFilter}
GROUP BY CAST(s.SaleDate AS DATE)
ORDER BY CAST(s.SaleDate AS DATE)";
                }
                else
                {
                    sql = $@"
SELECT
    FORMAT(DATEFROMPARTS(YEAR(s.SaleDate), MONTH(s.SaleDate), 1), 'MMM yyyy'),
    YEAR(s.SaleDate),
    MONTH(s.SaleDate),
    ISNULL(SUM(s.TotalAmount), 0)
FROM Sales s
WHERE s.IsDeleted = 0
  AND CAST(s.SaleDate AS DATE) BETWEEN @FromDate AND @ToDate
  {branchFilter}
GROUP BY YEAR(s.SaleDate), MONTH(s.SaleDate), FORMAT(DATEFROMPARTS(YEAR(s.SaleDate), MONTH(s.SaleDate), 1), 'MMM yyyy')
ORDER BY YEAR(s.SaleDate), MONTH(s.SaleDate)";
                }

                using var cmd = new SqlCommand(sql, connection);
                AddDateRangeAndBranchParameters(cmd, fromDate, toDate, branchId);
                using var reader = await cmd.ExecuteReaderAsync();
                if (days <= 31)
                {
                    while (await reader.ReadAsync())
                    {
                        var d = reader.GetDateTime(0);
                        list.Add(new SalesTrendPointDto
                        {
                            PeriodKey = d.ToString("yyyy-MM-dd"),
                            Label = d.ToString("MMM d"),
                            Amount = reader.GetDecimal(1)
                        });
                    }
                }
                else
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new SalesTrendPointDto
                        {
                            Label = reader.GetString(0),
                            PeriodKey = $"{reader.GetInt32(1)}-{reader.GetInt32(2):D2}",
                            Amount = reader.GetDecimal(3)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetSalesTrendAsync failed");
            }

            return list;
        }
    }
}
