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

            //    var query = @"
            //SELECT FORMAT(SaleDate, 'MMM-yyyy') AS Month,
            //       SUM(TotalAmount) AS TotalSales
            //FROM Sales
            //WHERE SaleDate >= DATEADD(MONTH, -11, CAST(GETDATE() AS date))
            //GROUP BY FORMAT(SaleDate, 'MMM-yyyy'), YEAR(SaleDate), MONTH(SaleDate)
            //ORDER BY YEAR(SaleDate), MONTH(SaleDate);";
                var query = @"
            ;WITH Last12Months AS
(
    SELECT TOP (12)
        FORMAT(DATEADD(MONTH, - (n-1), CAST(GETDATE() AS date)), 'MMM-yyyy') AS Month,
        YEAR(DATEADD(MONTH, - (n-1), CAST(GETDATE() AS date))) AS Yr,
        MONTH(DATEADD(MONTH, - (n-1), CAST(GETDATE() AS date))) AS Mn
    FROM (SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
          FROM sys.objects) AS Numbers
)
SELECT m.Month,
       ISNULL(SUM(s.TotalAmount), 0) AS TotalSales
FROM Last12Months m
LEFT JOIN Sales s
    ON YEAR(s.SaleDate) = m.Yr
   AND MONTH(s.SaleDate) = m.Mn
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
                SUM(s.TotalQuantity) AS StockIn,
                SUM(s.AvailableQuantity) AS StockAvailable,
                SUM(s.UsedQuantity) AS StockUsed
            FROM StockMaster s
            JOIN Products p ON s.ProductId_FK = p.ProductId
            WHERE p.ProductId = @ProductId
            GROUP BY p.ProductId, p.ProductName";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ProductId", productId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var stock = new StockStaus
                            {
                                ProductId = reader.GetInt64(reader.GetOrdinal("ProductId")),
                                ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                                InStockCount = reader.GetDecimal(reader.GetOrdinal("StockIn")),
                                OutOfStockCount = reader.GetDecimal(reader.GetOrdinal("StockUsed")),
                                AvailableStockCount = reader.GetDecimal(reader.GetOrdinal("StockAvailable")),

                            };
                            stockData.Add(stock);


                        }
                    }
                }
            }

            return stockData;
        }
    }
}
