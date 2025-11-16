using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Data;


namespace IMS.Services
{
    public class ReportService : IReportService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<ReportService> _logger;

        public ReportService(IDbContextFactory dbContextFactory, ILogger<ReportService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<ReportsViewModel> GetAllSales(int pageNumber, int? pageSize, SalesReportsFilters? salesReportsFilters)
        {
            var sale = new List<salesReportItems>();
            var customers = new List<Customer>();
            int totalRecords = 0;
            try
            {

                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {

                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetAllSales", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@PageNo", pageNumber);
                        command.Parameters.AddWithValue("@PageSize", pageSize);
                        command.Parameters.AddWithValue("@pIsDeleted", DBNull.Value);
                        command.Parameters.AddWithValue("@pCustomerId", (object)salesReportsFilters.CustomerId ?? DBNull.Value);
                        command.Parameters.AddWithValue("@pBillNumber",  DBNull.Value);
                        command.Parameters.AddWithValue("@pSaleFrom", salesReportsFilters.FromDate == default(DateTime) ? DateTime.Now : salesReportsFilters.FromDate);
                        command.Parameters.AddWithValue("@pSaleDateTo", salesReportsFilters.ToDate == default(DateTime) ? DateTime.Now : salesReportsFilters.ToDate);
                        command.Parameters.AddWithValue("@pDescription",  DBNull.Value);
                        using (var reader = await command.ExecuteReaderAsync())
                        {

                            while (await reader.ReadAsync())
                            {
                                sale.Add(new salesReportItems
                                {
                                    SaleId = reader.IsDBNull(reader.GetOrdinal("SaleId"))
            ? 0
            : reader.GetInt64(reader.GetOrdinal("SaleId")),

                                    CustomerName = reader.IsDBNull(reader.GetOrdinal("CustomerName"))
            ? string.Empty
            : reader.GetString(reader.GetOrdinal("CustomerName")),

                                    BillNumber = reader.IsDBNull(reader.GetOrdinal("BillNumber"))
            ? 0
            : reader.GetInt64(reader.GetOrdinal("BillNumber")),

                                    SaleDate = reader.IsDBNull(reader.GetOrdinal("SaleDate"))
            ? DateTime.MinValue
            : reader.GetDateTime(reader.GetOrdinal("SaleDate")),

                                    TotalAmount = reader.IsDBNull(reader.GetOrdinal("TotalAmount"))
            ? 0m
            : reader.GetDecimal(reader.GetOrdinal("TotalAmount")),

                                    DiscountAmount = reader.IsDBNull(reader.GetOrdinal("DiscountAmount"))
            ? 0m
            : reader.GetDecimal(reader.GetOrdinal("DiscountAmount")),

                                    TotalReceivedAmount = reader.IsDBNull(reader.GetOrdinal("TotalReceivedAmount"))
            ? 0m
            : reader.GetDecimal(reader.GetOrdinal("TotalReceivedAmount")),

                                    CustomerIdFk = reader.IsDBNull(reader.GetOrdinal("CustomerId_FK"))
            ? 0
            : reader.GetInt64(reader.GetOrdinal("CustomerId_FK")),

                                    TotalDueAmount = reader.IsDBNull(reader.GetOrdinal("TotalDueAmount"))
            ? 0m
            : reader.GetDecimal(reader.GetOrdinal("TotalDueAmount")),

                                    SaleDescription = reader.IsDBNull(reader.GetOrdinal("SaleDescription"))
            ? string.Empty
            : reader.GetString(reader.GetOrdinal("SaleDescription")),


                                });
                              

                            }

                            await reader.NextResultAsync();

                            if (await reader.ReadAsync())
                            {
                                totalRecords = reader.GetInt32(reader.GetOrdinal("TotalRecords"));
                            }

                        }
                    }



                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

            }



            return new ReportsViewModel
            {
                SalesList = sale,
               
                CurrentPage = pageNumber,
                TotalPages = pageSize.HasValue && pageSize.Value > 0
                   ? (int)Math.Ceiling(totalRecords / (double)pageSize.Value)
                   : 1,
                PageSize = pageSize,
                TotalCount = totalRecords
            };
        }
        public async Task<ReportsViewModel> GetAllSalesReport(int pageNumber, int? pageSize, SalesReportsFilters? salesReportsFilters)
        {
            var sale = new List<salesReportItems>();
            var customers = new List<Customer>();
            int totalRecords = 0;
            try
            {

                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {

                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetAllSalesReport", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                    
                        command.Parameters.AddWithValue("@pIsDeleted", DBNull.Value);
                        command.Parameters.AddWithValue("@pCustomerId", (object)salesReportsFilters.CustomerId ?? DBNull.Value);
                        command.Parameters.AddWithValue("@pBillNumber", DBNull.Value);
                        command.Parameters.AddWithValue("@pSaleFrom", salesReportsFilters.FromDate == default(DateTime) ? DateTime.Now : salesReportsFilters.FromDate);
                        command.Parameters.AddWithValue("@pSaleDateTo", salesReportsFilters.ToDate == default(DateTime) ? DateTime.Now : salesReportsFilters.ToDate);
                        command.Parameters.AddWithValue("@pDescription", DBNull.Value);
                        using (var reader = await command.ExecuteReaderAsync())
                        {

                            while (await reader.ReadAsync())
                            {
                                sale.Add(new salesReportItems
                                {
                                    SaleId = reader.IsDBNull(reader.GetOrdinal("SaleId"))
            ? 0
            : reader.GetInt64(reader.GetOrdinal("SaleId")),

                                    CustomerName = reader.IsDBNull(reader.GetOrdinal("CustomerName"))
            ? string.Empty
            : reader.GetString(reader.GetOrdinal("CustomerName")),

                                    BillNumber = reader.IsDBNull(reader.GetOrdinal("BillNumber"))
            ? 0
            : reader.GetInt64(reader.GetOrdinal("BillNumber")),

                                    SaleDate = reader.IsDBNull(reader.GetOrdinal("SaleDate"))
            ? DateTime.MinValue
            : reader.GetDateTime(reader.GetOrdinal("SaleDate")),

                                    TotalAmount = reader.IsDBNull(reader.GetOrdinal("TotalAmount"))
            ? 0m
            : reader.GetDecimal(reader.GetOrdinal("TotalAmount")),

                                    DiscountAmount = reader.IsDBNull(reader.GetOrdinal("DiscountAmount"))
            ? 0m
            : reader.GetDecimal(reader.GetOrdinal("DiscountAmount")),

                                    TotalReceivedAmount = reader.IsDBNull(reader.GetOrdinal("TotalReceivedAmount"))
            ? 0m
            : reader.GetDecimal(reader.GetOrdinal("TotalReceivedAmount")),

                                    CustomerIdFk = reader.IsDBNull(reader.GetOrdinal("CustomerId_FK"))
            ? 0
            : reader.GetInt64(reader.GetOrdinal("CustomerId_FK")),

                                    TotalDueAmount = reader.IsDBNull(reader.GetOrdinal("TotalDueAmount"))
            ? 0m
            : reader.GetDecimal(reader.GetOrdinal("TotalDueAmount")),

                                    SaleDescription = reader.IsDBNull(reader.GetOrdinal("SaleDescription"))
            ? string.Empty
            : reader.GetString(reader.GetOrdinal("SaleDescription")),


                                });


                            }

                            await reader.NextResultAsync();

                            if (await reader.ReadAsync())
                            {
                                totalRecords = reader.GetInt32(reader.GetOrdinal("TotalRecords"));
                            }

                        }
                    }



                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

            }



            return new ReportsViewModel
            {
                SalesList = sale,

                CurrentPage = pageNumber,
                TotalPages = pageSize.HasValue && pageSize.Value > 0
                   ? (int)Math.Ceiling(totalRecords / (double)pageSize.Value)
                   : 1,
                PageSize = pageSize,
                TotalCount = totalRecords
            };
        }

        public async Task<ProfitLossReportViewModel> GetProductWiseProfitLoss(int pageNumber, int? pageSize, ProfitLossReportFilters? filters)
        {
            var profitLossList = new List<ProfitLossReportItem>();
            int totalRecords = 0;
            decimal totalSalesAmount = 0;
            decimal totalPurchaseCost = 0;
            decimal totalProfitLoss = 0;

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    // Build the SQL query for product-wise profit/loss
                    var sql = @"
                       
 WITH SalesData AS (
                            SELECT 
                                sd.PrductId_FK AS ProductId,
                                p.ProductName,
                                p.ProductCode,
                                SUM(sd.Quantity) AS TotalQuantitySold,
                                SUM(sd.PayableAmount) AS TotalSalesAmount
                            FROM SaleDetails sd
                            INNER JOIN Sales s ON sd.SaleId_FK = s.SaleId
                            INNER JOIN Products p ON sd.PrductId_FK = p.ProductId
                            WHERE s.IsDeleted = 0
                                AND (@FromDate IS NULL OR s.SaleDate >= @FromDate)
                                AND (@ToDate IS NULL OR s.SaleDate <= @ToDate)
                                AND (@ProductId IS NULL OR sd.PrductId_FK = @ProductId)
                            GROUP BY sd.PrductId_FK, p.ProductName, p.ProductCode
                        ),
                        PurchaseData AS (
                            SELECT 
                                poi.PrductId_FK AS ProductId,
                                AVG(poi.UnitPrice) AS AvgPurchasePrice,
                                SUM(poi.Quantity) AS TotalQuantityPurchased
                            FROM PurchaseOrderItems poi
                            INNER JOIN PurchaseOrders po ON poi.PurchaseOrderId_FK = po.PurchaseOrderId
                            WHERE (@FromDate IS NULL OR po.PurchaseOrderDate >= @FromDate)
                                AND (@ToDate IS NULL OR po.PurchaseOrderDate <= @ToDate)
                                AND (@ProductId IS NULL OR poi.PrductId_FK = @ProductId)
                            GROUP BY poi.PrductId_FK
                        )
                        SELECT 
                            sd.ProductId,
                            sd.ProductName,
                            ISNULL(sd.ProductCode, '') AS ProductCode,
                            sd.TotalQuantitySold,
                            sd.TotalSalesAmount,
                            ISNULL(pd.AvgPurchasePrice * sd.TotalQuantitySold, 0) AS TotalPurchaseCost,
                            (sd.TotalSalesAmount - ISNULL(pd.AvgPurchasePrice * sd.TotalQuantitySold, 0)) AS ProfitLoss,
                            CASE 
                                WHEN ISNULL(pd.AvgPurchasePrice * sd.TotalQuantitySold, 0) > 0 
                                THEN ((sd.TotalSalesAmount - ISNULL(pd.AvgPurchasePrice * sd.TotalQuantitySold, 0)) / (pd.AvgPurchasePrice * sd.TotalQuantitySold)) * 100
                                ELSE 0
                            END AS ProfitLossPercentage
                        FROM SalesData sd
                        LEFT JOIN PurchaseData pd ON sd.ProductId = pd.ProductId
                        ORDER BY sd.ProductName
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

                        SELECT COUNT(*) AS TotalRecords
                        FROM (
                            SELECT DISTINCT sd.PrductId_FK
                            FROM SaleDetails sd
                            INNER JOIN Sales s ON sd.SaleId_FK = s.SaleId
                            WHERE s.IsDeleted = 0
                                AND (@FromDate IS NULL OR s.SaleDate >= @FromDate)
                                AND (@ToDate IS NULL OR s.SaleDate <= @ToDate)
                                AND (@ProductId IS NULL OR sd.PrductId_FK = @ProductId)
                        ) AS ProductCount;

                        SELECT 
                            SUM(TotalSalesAmount) AS TotalSalesAmount,
                            SUM(TotalPurchaseCost) AS TotalPurchaseCost,
                            SUM(ProfitLoss) AS TotalProfitLoss
                        FROM (
                            SELECT 
                                sd.PrductId_FK AS ProductId,
                                SUM(sd.PayableAmount) AS TotalSalesAmount,
                                ISNULL(pd.AvgPurchasePrice * SUM(sd.Quantity), 0) AS TotalPurchaseCost,
                                (SUM(sd.PayableAmount) - ISNULL(pd.AvgPurchasePrice * SUM(sd.Quantity), 0)) AS ProfitLoss
                            FROM SaleDetails sd
                            INNER JOIN Sales s ON sd.SaleId_FK = s.SaleId
                            LEFT JOIN (
                                SELECT 
                                    poi.PrductId_FK,
                                    AVG(poi.UnitPrice) AS AvgPurchasePrice
                                FROM PurchaseOrderItems poi
                                INNER JOIN PurchaseOrders po ON poi.PurchaseOrderId_FK = po.PurchaseOrderId
                                WHERE (@FromDate IS NULL OR po.PurchaseOrderDate >= @FromDate)
                                    AND (@ToDate IS NULL OR po.PurchaseOrderDate <= @ToDate)
                                GROUP BY poi.PrductId_FK
                            ) pd ON sd.PrductId_FK = pd.PrductId_FK
                            WHERE s.IsDeleted = 0
                                AND (@FromDate IS NULL OR s.SaleDate >= @FromDate)
                                AND (@ToDate IS NULL OR s.SaleDate <= @ToDate)
                                AND (@ProductId IS NULL OR sd.PrductId_FK = @ProductId)
                            GROUP BY sd.PrductId_FK, pd.AvgPurchasePrice
                        ) AS Summary;
                    ";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@FromDate", (object)filters?.FromDate ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ToDate", filters?.ToDate != null ? filters.ToDate.Value.AddDays(1).AddSeconds(-1) : DBNull.Value);
                        command.Parameters.AddWithValue("@ProductId", (object)filters?.ProductId ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Offset", (pageNumber - 1) * (pageSize ?? 10));
                        command.Parameters.AddWithValue("@PageSize", pageSize ?? 10);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            // Read product-wise data
                            while (await reader.ReadAsync())
                            {
                                profitLossList.Add(new ProfitLossReportItem
                                {
                                    ProductId = reader.IsDBNull(reader.GetOrdinal("ProductId")) ? 0 : reader.GetInt64(reader.GetOrdinal("ProductId")),
                                    ProductName = reader.IsDBNull(reader.GetOrdinal("ProductName")) ? string.Empty : reader.GetString(reader.GetOrdinal("ProductName")),
                                    ProductCode = reader.IsDBNull(reader.GetOrdinal("ProductCode")) ? string.Empty : reader.GetString(reader.GetOrdinal("ProductCode")),
                                    TotalQuantitySold = reader.IsDBNull(reader.GetOrdinal("TotalQuantitySold")) ? 0 : reader.GetInt64(reader.GetOrdinal("TotalQuantitySold")),
                                    TotalSalesAmount = reader.IsDBNull(reader.GetOrdinal("TotalSalesAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalSalesAmount")),
                                    TotalPurchaseCost = reader.IsDBNull(reader.GetOrdinal("TotalPurchaseCost")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalPurchaseCost")),
                                    ProfitLoss = reader.IsDBNull(reader.GetOrdinal("ProfitLoss")) ? 0m : reader.GetDecimal(reader.GetOrdinal("ProfitLoss")),
                                    ProfitLossPercentage = reader.IsDBNull(reader.GetOrdinal("ProfitLossPercentage")) ? 0m : reader.GetDecimal(reader.GetOrdinal("ProfitLossPercentage"))
                                });
                            }

                            // Read total records
                            await reader.NextResultAsync();
                            if (await reader.ReadAsync())
                            {
                                totalRecords = reader.IsDBNull(reader.GetOrdinal("TotalRecords")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalRecords"));
                            }

                            // Read summary totals
                            await reader.NextResultAsync();
                            if (await reader.ReadAsync())
                            {
                                totalSalesAmount = reader.IsDBNull(reader.GetOrdinal("TotalSalesAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalSalesAmount"));
                                totalPurchaseCost = reader.IsDBNull(reader.GetOrdinal("TotalPurchaseCost")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalPurchaseCost"));
                                totalProfitLoss = reader.IsDBNull(reader.GetOrdinal("TotalProfitLoss")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalProfitLoss"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return new ProfitLossReportViewModel
            {
                ProfitLossList = profitLossList,
                Filters = filters ?? new ProfitLossReportFilters(),
                CurrentPage = pageNumber,
                TotalPages = pageSize.HasValue && pageSize.Value > 0
                   ? (int)Math.Ceiling(totalRecords / (double)pageSize.Value)
                   : 1,
                PageSize = pageSize,
                TotalCount = totalRecords,
                TotalSalesAmount = totalSalesAmount,
                TotalPurchaseCost = totalPurchaseCost,
                TotalProfitLoss = totalProfitLoss
            };
        }

        public async Task<ProfitLossReportViewModel> GetProductWiseProfitLossReport(int pageNumber, int? pageSize, ProfitLossReportFilters? filters)
        {
            // For export, get all records without pagination
            var profitLossList = new List<ProfitLossReportItem>();
            decimal totalSalesAmount = 0;
            decimal totalPurchaseCost = 0;
            decimal totalProfitLoss = 0;

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    //var sql = @"
                    //    WITH SalesData AS (
                    //        SELECT 
                    //            sd.PrductId_FK AS ProductId,
                    //            p.ProductName,
                    //            p.ProductCode,
                    //            SUM(sd.Quantity) AS TotalQuantitySold,
                    //            SUM(sd.PayableAmount) AS TotalSalesAmount
                    //        FROM SaleDetail sd
                    //        INNER JOIN Sale s ON sd.SaleId_FK = s.SaleId
                    //        INNER JOIN Product p ON sd.PrductId_FK = p.ProductId
                    //        WHERE s.IsDeleted = 0
                    //            AND (@FromDate IS NULL OR s.SaleDate >= @FromDate)
                    //            AND (@ToDate IS NULL OR s.SaleDate <= @ToDate)
                    //            AND (@ProductId IS NULL OR sd.PrductId_FK = @ProductId)
                    //        GROUP BY sd.PrductId_FK, p.ProductName, p.ProductCode
                    //    ),
                    //    PurchaseData AS (
                    //        SELECT 
                    //            poi.PrductId_FK AS ProductId,
                    //            AVG(poi.UnitPrice) AS AvgPurchasePrice,
                    //            SUM(poi.Quantity) AS TotalQuantityPurchased
                    //        FROM PurchaseOrderItem poi
                    //        INNER JOIN PurchaseOrder po ON poi.PurchaseOrderId_FK = po.PurchaseOrderId
                    //        WHERE (@FromDate IS NULL OR po.PurchaseOrderDate >= @FromDate)
                    //            AND (@ToDate IS NULL OR po.PurchaseOrderDate <= @ToDate)
                    //            AND (@ProductId IS NULL OR poi.PrductId_FK = @ProductId)
                    //        GROUP BY poi.PrductId_FK
                    //    )
                    //    SELECT 
                    //        sd.ProductId,
                    //        sd.ProductName,
                    //        ISNULL(sd.ProductCode, '') AS ProductCode,
                    //        sd.TotalQuantitySold,
                    //        sd.TotalSalesAmount,
                    //        ISNULL(pd.AvgPurchasePrice * sd.TotalQuantitySold, 0) AS TotalPurchaseCost,
                    //        (sd.TotalSalesAmount - ISNULL(pd.AvgPurchasePrice * sd.TotalQuantitySold, 0)) AS ProfitLoss,
                    //        CASE 
                    //            WHEN ISNULL(pd.AvgPurchasePrice * sd.TotalQuantitySold, 0) > 0 
                    //            THEN ((sd.TotalSalesAmount - ISNULL(pd.AvgPurchasePrice * sd.TotalQuantitySold, 0)) / (pd.AvgPurchasePrice * sd.TotalQuantitySold)) * 100
                    //            ELSE 0
                    //        END AS ProfitLossPercentage
                    //    FROM SalesData sd
                    //    LEFT JOIN PurchaseData pd ON sd.ProductId = pd.ProductId
                    //    ORDER BY sd.ProductName;

                    //    SELECT 
                    //        SUM(TotalSalesAmount) AS TotalSalesAmount,
                    //        SUM(TotalPurchaseCost) AS TotalPurchaseCost,
                    //        SUM(ProfitLoss) AS TotalProfitLoss
                    //    FROM (
                    //        SELECT 
                    //            sd.PrductId_FK AS ProductId,
                    //            SUM(sd.PayableAmount) AS TotalSalesAmount,
                    //            ISNULL(pd.AvgPurchasePrice * SUM(sd.Quantity), 0) AS TotalPurchaseCost,
                    //            (SUM(sd.PayableAmount) - ISNULL(pd.AvgPurchasePrice * SUM(sd.Quantity), 0)) AS ProfitLoss
                    //        FROM SaleDetail sd
                    //        INNER JOIN Sale s ON sd.SaleId_FK = s.SaleId
                    //        LEFT JOIN (
                    //            SELECT 
                    //                poi.PrductId_FK,
                    //                AVG(poi.UnitPrice) AS AvgPurchasePrice
                    //            FROM PurchaseOrderItem poi
                    //            INNER JOIN PurchaseOrder po ON poi.PurchaseOrderId_FK = po.PurchaseOrderId
                    //            WHERE (@FromDate IS NULL OR po.PurchaseOrderDate >= @FromDate)
                    //                AND (@ToDate IS NULL OR po.PurchaseOrderDate <= @ToDate)
                    //            GROUP BY poi.PrductId_FK
                    //        ) pd ON sd.PrductId_FK = pd.PrductId_FK
                    //        WHERE s.IsDeleted = 0
                    //            AND (@FromDate IS NULL OR s.SaleDate >= @FromDate)
                    //            AND (@ToDate IS NULL OR s.SaleDate <= @ToDate)
                    //            AND (@ProductId IS NULL OR sd.PrductId_FK = @ProductId)
                    //        GROUP BY sd.PrductId_FK, pd.AvgPurchasePrice
                    //    ) AS Summary;
                    //";

                    using (var command = new SqlCommand("GetSalesProfitSummary", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@FromDate", (object)filters?.FromDate ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ToDate", filters?.ToDate != null ? filters.ToDate.Value.AddDays(1).AddSeconds(-1) : DBNull.Value);
                        command.Parameters.AddWithValue("@ProductId", (object)filters?.ProductId ?? DBNull.Value);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                profitLossList.Add(new ProfitLossReportItem
                                {
                                    ProductId = reader.IsDBNull(reader.GetOrdinal("ProductId")) ? 0 : reader.GetInt64(reader.GetOrdinal("ProductId")),
                                    ProductName = reader.IsDBNull(reader.GetOrdinal("ProductName")) ? string.Empty : reader.GetString(reader.GetOrdinal("ProductName")),
                                    ProductCode = reader.IsDBNull(reader.GetOrdinal("ProductCode")) ? string.Empty : reader.GetString(reader.GetOrdinal("ProductCode")),
                                    TotalQuantitySold = reader.IsDBNull(reader.GetOrdinal("TotalQuantitySold")) ? 0 : reader.GetInt64(reader.GetOrdinal("TotalQuantitySold")),
                                    TotalSalesAmount = reader.IsDBNull(reader.GetOrdinal("TotalSalesAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalSalesAmount")),
                                    TotalPurchaseCost = reader.IsDBNull(reader.GetOrdinal("TotalPurchaseCost")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalPurchaseCost")),
                                    ProfitLoss = reader.IsDBNull(reader.GetOrdinal("ProfitLoss")) ? 0m : reader.GetDecimal(reader.GetOrdinal("ProfitLoss")),
                                    ProfitLossPercentage = reader.IsDBNull(reader.GetOrdinal("ProfitLossPercentage")) ? 0m : reader.GetDecimal(reader.GetOrdinal("ProfitLossPercentage"))
                                });
                            }

                            await reader.NextResultAsync();
                            if (await reader.ReadAsync())
                            {
                                totalSalesAmount = reader.IsDBNull(reader.GetOrdinal("TotalSalesAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalSalesAmount"));
                                totalPurchaseCost = reader.IsDBNull(reader.GetOrdinal("TotalPurchaseCost")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalPurchaseCost"));
                                totalProfitLoss = reader.IsDBNull(reader.GetOrdinal("TotalProfitLoss")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalProfitLoss"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return new ProfitLossReportViewModel
            {
                ProfitLossList = profitLossList,
                Filters = filters ?? new ProfitLossReportFilters(),
                CurrentPage = 1,
                TotalPages = 1,
                PageSize = profitLossList.Count,
                TotalCount = profitLossList.Count,
                TotalSalesAmount = totalSalesAmount,
                TotalPurchaseCost = totalPurchaseCost,
                TotalProfitLoss = totalProfitLoss
            };
        }

    }
}
