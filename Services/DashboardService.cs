using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
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
       
        public  async Task<int>  GetTotalCategoryCount()
        {
            int totalCount = 0;
            try
            {


                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    var sql = "select COUNT(1) from AdminCategory where IsEnabled=1";
                    using (var command = new SqlCommand(sql, connection))
                    {

                        totalCount = (int)await command.ExecuteScalarAsync();
                        
                    }
                }
            }
            catch
            {

            }
            return totalCount;
        }

  

        async Task<int> IDashboardService.GetTotalProductCount()
        {
            int totalCount = 0;
            try
            {


                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    var sql = "select COUNT(1) from Products where IsEnabled=1";
                    using (var command = new SqlCommand(sql, connection))
                    {

                        totalCount = (int)await command.ExecuteScalarAsync();

                    }
                }
            }
            catch
            {

            }
            return totalCount;
        }

        async Task<int> IDashboardService.GetTotalVendorsCount()
        {
            int totalCount = 0;
            try
            {


                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    var sql = "select COUNT(1) from AdminSuppliers where IsDeleted=0";
                    using (var command = new SqlCommand(sql, connection))
                    {

                        totalCount = (int)await command.ExecuteScalarAsync();

                    }
                }
            }
            catch
            {

            }
            return totalCount;
        }
        public async Task<SalesChartViewModel> GetLast12MonthsSalesAsync()
        {
            var viewModel = new SalesChartViewModel();

            using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
            {
                await connection.OpenAsync();

                // Improved query that handles timezone and date filtering better
                var query = @"
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
GROUP BY m.Month, m.Yr, m.Mn
ORDER BY m.Yr, m.Mn;
";

                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            viewModel.Months.Add(reader.GetString(0));
                            viewModel.Sales.Add(reader.GetDecimal(1));
                        }
                    }
                }
            }

            return viewModel;
        }

        public async Task<decimal> GetCurrentMonthRevenueAsync()
        {
            decimal currentMonthRevenue = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    var query = @"
                        SELECT ISNULL(SUM(TotalAmount), 0) AS CurrentMonthRevenue
                        FROM Sales 
                        WHERE YEAR(SaleDate) = YEAR(GETDATE()) 
                          AND MONTH(SaleDate) = MONTH(GETDATE())
                          AND IsDeleted = 0";
                    
                    using (var command = new SqlCommand(query, connection))
                    {
                        var result = await command.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            currentMonthRevenue = Convert.ToDecimal(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error if needed
                currentMonthRevenue = 0;
            }
            
            return currentMonthRevenue;
        }

        public async Task<List<StockStaus>> GetStockStatusAsync(long? productId)
        {
            var stockData = new List<StockStaus>();

            using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
            {
                await connection.OpenAsync();

                var query = @"
                SELECT 
                p.ProductId,
                p.ProductName,
                ISNULL(SUM(s.TotalQuantity), 0) AS StockIn,
                ISNULL(SUM(s.AvailableQuantity), 0) AS StockAvailable,
                ISNULL(SUM(s.UsedQuantity), 0) AS StockUsed
            FROM Products p
            LEFT JOIN StockMaster s ON s.ProductId_FK = p.ProductId
            WHERE p.ProductId = @ProductId
            GROUP BY p.ProductId, p.ProductName";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ProductId", productId);
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
                            // If no data found, return default values
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
                }
            }

            return stockData;
        }
    }
}
