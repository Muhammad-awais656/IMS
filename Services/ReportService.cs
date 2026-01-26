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
            decimal totalAmount = 0;
            decimal totalDiscountAmount = 0;
            decimal totalReceivedAmount = 0;
            decimal totalDueAmount = 0;
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
                                var saleItem = new salesReportItems
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
                                };
                                
                                sale.Add(saleItem);
                                
                                // Calculate totals
                                totalAmount += saleItem.TotalAmount;
                                totalDiscountAmount += saleItem.DiscountAmount;
                                totalReceivedAmount += saleItem.TotalReceivedAmount;
                                totalDueAmount += saleItem.TotalDueAmount;
                            }

                            await reader.NextResultAsync();

                            if (await reader.ReadAsync())
                            {
                                totalRecords = reader.GetInt32(reader.GetOrdinal("TotalRecords"));
                            }

                        }
                    }

                    // Get overall totals from database
                    var totalsSql = @"
                        SELECT 
                            SUM(TotalAmount) AS TotalAmount,
                            SUM(DiscountAmount) AS TotalDiscountAmount,
                            SUM(TotalReceivedAmount) AS TotalReceivedAmount,
                            SUM(TotalDueAmount) AS TotalDueAmount
                        FROM Sales
                        WHERE IsDeleted = 0
                            AND (@CustomerId IS NULL OR CustomerId_FK = @CustomerId)
                            AND (@FromDate IS NULL OR SaleDate >= @FromDate)
                            AND (@ToDate IS NULL OR SaleDate <= @ToDate);
                    ";

                    using (var totalsCommand = new SqlCommand(totalsSql, connection))
                    {
                        totalsCommand.Parameters.AddWithValue("@CustomerId", (object)salesReportsFilters?.CustomerId ?? DBNull.Value);
                        totalsCommand.Parameters.AddWithValue("@FromDate", salesReportsFilters?.FromDate == default(DateTime) ? DBNull.Value : (object)salesReportsFilters.FromDate);
                        totalsCommand.Parameters.AddWithValue("@ToDate", salesReportsFilters?.ToDate == default(DateTime) ? DBNull.Value : (object)salesReportsFilters.ToDate.Value.AddDays(1).AddSeconds(-1));

                        using (var totalsReader = await totalsCommand.ExecuteReaderAsync())
                        {
                            if (await totalsReader.ReadAsync())
                            {
                                totalAmount = totalsReader.IsDBNull(totalsReader.GetOrdinal("TotalAmount")) ? 0m : totalsReader.GetDecimal(totalsReader.GetOrdinal("TotalAmount"));
                                totalDiscountAmount = totalsReader.IsDBNull(totalsReader.GetOrdinal("TotalDiscountAmount")) ? 0m : totalsReader.GetDecimal(totalsReader.GetOrdinal("TotalDiscountAmount"));
                                totalReceivedAmount = totalsReader.IsDBNull(totalsReader.GetOrdinal("TotalReceivedAmount")) ? 0m : totalsReader.GetDecimal(totalsReader.GetOrdinal("TotalReceivedAmount"));
                                totalDueAmount = totalsReader.IsDBNull(totalsReader.GetOrdinal("TotalDueAmount")) ? 0m : totalsReader.GetDecimal(totalsReader.GetOrdinal("TotalDueAmount"));
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
                TotalCount = totalRecords,
                TotalAmount = totalAmount,
                TotalDiscountAmount = totalDiscountAmount,
                TotalReceivedAmount = totalReceivedAmount,
                TotalDueAmount = totalDueAmount
            };
        }
        public async Task<ReportsViewModel> GetAllSalesReport(int pageNumber, int? pageSize, SalesReportsFilters? salesReportsFilters)
        {
            var sale = new List<salesReportItems>();
            var customers = new List<Customer>();
            int totalRecords = 0;
            decimal totalAmount = 0;
            decimal totalDiscountAmount = 0;
            decimal totalReceivedAmount = 0;
            decimal totalDueAmount = 0;
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

                    // Get overall totals from database
                    var totalsSql = @"
                        SELECT 
                            SUM(TotalAmount) AS TotalAmount,
                            SUM(DiscountAmount) AS TotalDiscountAmount,
                            SUM(TotalReceivedAmount) AS TotalReceivedAmount,
                            SUM(TotalDueAmount) AS TotalDueAmount
                        FROM Sales
                        WHERE IsDeleted = 0
                            AND (@CustomerId IS NULL OR CustomerId_FK = @CustomerId)
                            AND (@FromDate IS NULL OR SaleDate >= @FromDate)
                            AND (@ToDate IS NULL OR SaleDate <= @ToDate);
                    ";

                    using (var totalsCommand = new SqlCommand(totalsSql, connection))
                    {
                        totalsCommand.Parameters.AddWithValue("@CustomerId", (object)salesReportsFilters?.CustomerId ?? DBNull.Value);
                        totalsCommand.Parameters.AddWithValue("@FromDate", salesReportsFilters?.FromDate == default(DateTime) ? DBNull.Value : (object)salesReportsFilters.FromDate);
                        totalsCommand.Parameters.AddWithValue("@ToDate", salesReportsFilters?.ToDate == default(DateTime) ? DBNull.Value : (object)salesReportsFilters.ToDate.Value.AddDays(1).AddSeconds(-1));

                        using (var totalsReader = await totalsCommand.ExecuteReaderAsync())
                        {
                            if (await totalsReader.ReadAsync())
                            {
                                totalAmount = totalsReader.IsDBNull(totalsReader.GetOrdinal("TotalAmount")) ? 0m : totalsReader.GetDecimal(totalsReader.GetOrdinal("TotalAmount"));
                                totalDiscountAmount = totalsReader.IsDBNull(totalsReader.GetOrdinal("TotalDiscountAmount")) ? 0m : totalsReader.GetDecimal(totalsReader.GetOrdinal("TotalDiscountAmount"));
                                totalReceivedAmount = totalsReader.IsDBNull(totalsReader.GetOrdinal("TotalReceivedAmount")) ? 0m : totalsReader.GetDecimal(totalsReader.GetOrdinal("TotalReceivedAmount"));
                                totalDueAmount = totalsReader.IsDBNull(totalsReader.GetOrdinal("TotalDueAmount")) ? 0m : totalsReader.GetDecimal(totalsReader.GetOrdinal("TotalDueAmount"));
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
                TotalCount = totalRecords,
                TotalAmount = totalAmount,
                TotalDiscountAmount = totalDiscountAmount,
                TotalReceivedAmount = totalReceivedAmount,
                TotalDueAmount = totalDueAmount
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

        public async Task<DailyStockReportViewModel> GetDailyStockReport(int pageNumber, int? pageSize, DailyStockReportFilters? filters)
        {
            var stockList = new List<DailyStockReportItem>();
            int totalRecords = 0;
            decimal totalStockValue = 0;
            decimal totalAvailableQuantity = 0;
            decimal totalUsedQuantity = 0;
            decimal totalQuantity = 0;

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT 
                            p.ProductId,
                            p.ProductName,
                            ISNULL(p.ProductCode, '') AS ProductCode,
                            ISNULL(sm.TotalQuantity, 0) AS TotalQuantity,
                            ISNULL(sm.UsedQuantity, 0) AS UsedQuantity,
                            ISNULL(sm.AvailableQuantity, 0) AS AvailableQuantity,
                            ISNULL(pr.UnitPrice, 0) AS UnitPrice,
                            (ISNULL(sm.AvailableQuantity, 0) * ISNULL(pr.UnitPrice, 0)) AS StockValue,
                            ISNULL(p.Location, '') AS StockLocation
                        FROM Products p
                        LEFT JOIN StockMaster sm ON p.ProductId = sm.ProductId_FK
                        LEFT JOIN (
                            SELECT 
                                pr.ProductId_FK,
                                AVG(pr.UnitPrice) AS UnitPrice
                            FROM ProductRange pr
                            GROUP BY pr.ProductId_FK
                        ) pr ON p.ProductId = pr.ProductId_FK
                        WHERE p.IsEnabled = 1
                            AND (@ProductId IS NULL OR p.ProductId = @ProductId)
                        ORDER BY p.ProductName
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

                        SELECT COUNT(*) AS TotalRecords
                        FROM Products p
                        WHERE p.IsEnabled = 1
                            AND (@ProductId IS NULL OR p.ProductId = @ProductId);

                        SELECT 
                            SUM(ISNULL(sm.AvailableQuantity, 0)) AS TotalAvailableQuantity,
                            SUM(ISNULL(sm.UsedQuantity, 0)) AS TotalUsedQuantity,
                            SUM(ISNULL(sm.TotalQuantity, 0)) AS TotalQuantity,
                            SUM(ISNULL(sm.AvailableQuantity, 0) * ISNULL(pr.UnitPrice, 0)) AS TotalStockValue
                        FROM Products p
                        LEFT JOIN StockMaster sm ON p.ProductId = sm.ProductId_FK
                        LEFT JOIN (
                            SELECT 
                                pr.ProductId_FK,
                                AVG(pr.UnitPrice) AS UnitPrice
                            FROM ProductRange pr
                            GROUP BY pr.ProductId_FK
                        ) pr ON p.ProductId = pr.ProductId_FK
                        WHERE p.IsEnabled = 1
                            AND (@ProductId IS NULL OR p.ProductId = @ProductId);
                    ";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@ProductId", (object)filters?.ProductId ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Offset", (pageNumber - 1) * (pageSize ?? 10));
                        command.Parameters.AddWithValue("@PageSize", pageSize ?? 10);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            // Read stock data
                            while (await reader.ReadAsync())
                            {
                                stockList.Add(new DailyStockReportItem
                                {
                                    ProductId = reader.IsDBNull(reader.GetOrdinal("ProductId")) ? 0 : reader.GetInt64(reader.GetOrdinal("ProductId")),
                                    ProductName = reader.IsDBNull(reader.GetOrdinal("ProductName")) ? string.Empty : reader.GetString(reader.GetOrdinal("ProductName")),
                                    ProductCode = reader.IsDBNull(reader.GetOrdinal("ProductCode")) ? string.Empty : reader.GetString(reader.GetOrdinal("ProductCode")),
                                    TotalQuantity = reader.IsDBNull(reader.GetOrdinal("TotalQuantity")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalQuantity")),
                                    UsedQuantity = reader.IsDBNull(reader.GetOrdinal("UsedQuantity")) ? 0m : reader.GetDecimal(reader.GetOrdinal("UsedQuantity")),
                                    AvailableQuantity = reader.IsDBNull(reader.GetOrdinal("AvailableQuantity")) ? 0m : reader.GetDecimal(reader.GetOrdinal("AvailableQuantity")),
                                    UnitPrice = reader.IsDBNull(reader.GetOrdinal("UnitPrice")) ? 0m : reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                                    StockValue = reader.IsDBNull(reader.GetOrdinal("StockValue")) ? 0m : reader.GetDecimal(reader.GetOrdinal("StockValue")),
                                    StockLocation = reader.IsDBNull(reader.GetOrdinal("StockLocation")) ? string.Empty : reader.GetString(reader.GetOrdinal("StockLocation"))
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
                                totalAvailableQuantity = reader.IsDBNull(reader.GetOrdinal("TotalAvailableQuantity")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalAvailableQuantity"));
                                totalUsedQuantity = reader.IsDBNull(reader.GetOrdinal("TotalUsedQuantity")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalUsedQuantity"));
                                totalQuantity = reader.IsDBNull(reader.GetOrdinal("TotalQuantity")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalQuantity"));
                                totalStockValue = reader.IsDBNull(reader.GetOrdinal("TotalStockValue")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalStockValue"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return new DailyStockReportViewModel
            {
                StockList = stockList,
                Filters = filters ?? new DailyStockReportFilters(),
                CurrentPage = pageNumber,
                TotalPages = pageSize.HasValue && pageSize.Value > 0
                   ? (int)Math.Ceiling(totalRecords / (double)pageSize.Value)
                   : 1,
                PageSize = pageSize,
                TotalCount = totalRecords,
                TotalAvailableQuantity = totalAvailableQuantity,
                TotalUsedQuantity = totalUsedQuantity,
                TotalQuantity = totalQuantity,
                TotalStockValue = totalStockValue
            };
        }

        public async Task<DailyStockReportViewModel> GetDailyStockReportForExport(int pageNumber, int? pageSize, DailyStockReportFilters? filters)
        {
            // For export, get all records without pagination
            var stockList = new List<DailyStockReportItem>();
            decimal totalStockValue = 0;
            decimal totalAvailableQuantity = 0;
            decimal totalUsedQuantity = 0;
            decimal totalQuantity = 0;

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT 
                            p.ProductId,
                            p.ProductName,
                            ISNULL(p.ProductCode, '') AS ProductCode,
                            ISNULL(sm.TotalQuantity, 0) AS TotalQuantity,
                            ISNULL(sm.UsedQuantity, 0) AS UsedQuantity,
                            ISNULL(sm.AvailableQuantity, 0) AS AvailableQuantity,
                            ISNULL(pr.UnitPrice, 0) AS UnitPrice,
                            (ISNULL(sm.AvailableQuantity, 0) * ISNULL(pr.UnitPrice, 0)) AS StockValue,
                            ISNULL(p.Location, '') AS StockLocation
                        FROM Products p
                        LEFT JOIN StockMaster sm ON p.ProductId = sm.ProductId_FK
                        LEFT JOIN (
                            SELECT 
                                pr.ProductId_FK,
                                AVG(pr.UnitPrice) AS UnitPrice
                            FROM ProductRange pr
                            GROUP BY pr.ProductId_FK
                        ) pr ON p.ProductId = pr.ProductId_FK
                        WHERE p.IsEnabled = 1
                            AND (@ProductId IS NULL OR p.ProductId = @ProductId)
                        ORDER BY p.ProductName;

                        SELECT 
                            SUM(ISNULL(sm.AvailableQuantity, 0)) AS TotalAvailableQuantity,
                            SUM(ISNULL(sm.UsedQuantity, 0)) AS TotalUsedQuantity,
                            SUM(ISNULL(sm.TotalQuantity, 0)) AS TotalQuantity,
                            SUM(ISNULL(sm.AvailableQuantity, 0) * ISNULL(pr.UnitPrice, 0)) AS TotalStockValue
                        FROM Products p
                        LEFT JOIN StockMaster sm ON p.ProductId = sm.ProductId_FK
                        LEFT JOIN (
                            SELECT 
                                pr.ProductId_FK,
                                AVG(pr.UnitPrice) AS UnitPrice
                            FROM ProductRange pr
                            GROUP BY pr.ProductId_FK
                        ) pr ON p.ProductId = pr.ProductId_FK
                        WHERE p.IsEnabled = 1
                            AND (@ProductId IS NULL OR p.ProductId = @ProductId);
                    ";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@ProductId", (object)filters?.ProductId ?? DBNull.Value);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                stockList.Add(new DailyStockReportItem
                                {
                                    ProductId = reader.IsDBNull(reader.GetOrdinal("ProductId")) ? 0 : reader.GetInt64(reader.GetOrdinal("ProductId")),
                                    ProductName = reader.IsDBNull(reader.GetOrdinal("ProductName")) ? string.Empty : reader.GetString(reader.GetOrdinal("ProductName")),
                                    ProductCode = reader.IsDBNull(reader.GetOrdinal("ProductCode")) ? string.Empty : reader.GetString(reader.GetOrdinal("ProductCode")),
                                    TotalQuantity = reader.IsDBNull(reader.GetOrdinal("TotalQuantity")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalQuantity")),
                                    UsedQuantity = reader.IsDBNull(reader.GetOrdinal("UsedQuantity")) ? 0m : reader.GetDecimal(reader.GetOrdinal("UsedQuantity")),
                                    AvailableQuantity = reader.IsDBNull(reader.GetOrdinal("AvailableQuantity")) ? 0m : reader.GetDecimal(reader.GetOrdinal("AvailableQuantity")),
                                    UnitPrice = reader.IsDBNull(reader.GetOrdinal("UnitPrice")) ? 0m : reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                                    StockValue = reader.IsDBNull(reader.GetOrdinal("StockValue")) ? 0m : reader.GetDecimal(reader.GetOrdinal("StockValue")),
                                    StockLocation = reader.IsDBNull(reader.GetOrdinal("StockLocation")) ? string.Empty : reader.GetString(reader.GetOrdinal("StockLocation"))
                                });
                            }

                            await reader.NextResultAsync();
                            if (await reader.ReadAsync())
                            {
                                totalAvailableQuantity = reader.IsDBNull(reader.GetOrdinal("TotalAvailableQuantity")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalAvailableQuantity"));
                                totalUsedQuantity = reader.IsDBNull(reader.GetOrdinal("TotalUsedQuantity")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalUsedQuantity"));
                                totalQuantity = reader.IsDBNull(reader.GetOrdinal("TotalQuantity")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalQuantity"));
                                totalStockValue = reader.IsDBNull(reader.GetOrdinal("TotalStockValue")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalStockValue"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return new DailyStockReportViewModel
            {
                StockList = stockList,
                Filters = filters ?? new DailyStockReportFilters(),
                CurrentPage = 1,
                TotalPages = 1,
                PageSize = stockList.Count,
                TotalCount = stockList.Count,
                TotalAvailableQuantity = totalAvailableQuantity,
                TotalUsedQuantity = totalUsedQuantity,
                TotalQuantity = totalQuantity,
                TotalStockValue = totalStockValue
            };
        }

        public async Task<BankCreditDebitReportViewModel> GetBankCreditDebitReport(int pageNumber, int? pageSize, BankCreditDebitReportFilters? filters)
        {
            var transactionList = new List<BankCreditDebitReportItem>();
            int totalRecords = 0;
            decimal totalCreditAmount = 0;
            decimal totalDebitAmount = 0;
            decimal netBalance = 0;

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT 
                            ppsd.PersonalPaymentSaleDetailId AS TransactionId,
                            ppsd.PersonalPaymentId,
                            pp.BankName,
                            pp.AccountNumber,
                            pp.AccountHolderName,
                            ISNULL(pp.BankBranch, '') AS BankBranch,
                            ppsd.TransactionType,
                            ppsd.Amount,
                            ppsd.Balance,
                            ISNULL(ppsd.TransactionDescription, '') AS TransactionDescription,
                            ppsd.TransactionDate,
                            ppsd.SaleId,
                            ISNULL(s.BillNumber, 0) AS BillNumber,
                            ISNULL(s.SaleDescription, '') AS ReferenceDescription
                        FROM PersonalPaymentSaleDetail ppsd
                        INNER JOIN PersonalPayments pp ON ppsd.PersonalPaymentId = pp.PersonalPaymentId
                        LEFT JOIN Sales s ON ppsd.SaleId = s.SaleId
                        WHERE ppsd.IsActive = 1
                            AND (@FromDate IS NULL OR ppsd.TransactionDate >= @FromDate)
                            AND (@ToDate IS NULL OR ppsd.TransactionDate <= @ToDate)
                            AND (@PersonalPaymentId IS NULL OR ppsd.PersonalPaymentId = @PersonalPaymentId)
                            AND (@TransactionType IS NULL OR ppsd.TransactionType = @TransactionType)
                        ORDER BY ppsd.TransactionDate DESC, pp.BankName, pp.AccountNumber
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

                        SELECT COUNT(*) AS TotalRecords
                        FROM PersonalPaymentSaleDetail ppsd
                        WHERE ppsd.IsActive = 1
                            AND (@FromDate IS NULL OR ppsd.TransactionDate >= @FromDate)
                            AND (@ToDate IS NULL OR ppsd.TransactionDate <= @ToDate)
                            AND (@PersonalPaymentId IS NULL OR ppsd.PersonalPaymentId = @PersonalPaymentId)
                            AND (@TransactionType IS NULL OR ppsd.TransactionType = @TransactionType);

                        SELECT 
                            SUM(CASE WHEN TransactionType = 'Credit' THEN Amount ELSE 0 END) AS TotalCreditAmount,
                            SUM(CASE WHEN TransactionType = 'Debit' THEN Amount ELSE 0 END) AS TotalDebitAmount,
                            SUM(CASE WHEN TransactionType = 'Credit' THEN Amount ELSE -Amount END) AS NetBalance
                        FROM PersonalPaymentSaleDetail ppsd
                        WHERE ppsd.IsActive = 1
                            AND (@FromDate IS NULL OR ppsd.TransactionDate >= @FromDate)
                            AND (@ToDate IS NULL OR ppsd.TransactionDate <= @ToDate)
                            AND (@PersonalPaymentId IS NULL OR ppsd.PersonalPaymentId = @PersonalPaymentId)
                            AND (@TransactionType IS NULL OR ppsd.TransactionType = @TransactionType);
                    ";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@FromDate", (object)filters?.FromDate ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ToDate", filters?.ToDate != null ? filters.ToDate.Value.AddDays(1).AddSeconds(-1) : DBNull.Value);
                        command.Parameters.AddWithValue("@PersonalPaymentId", (object)filters?.PersonalPaymentId ?? DBNull.Value);
                        command.Parameters.AddWithValue("@TransactionType", string.IsNullOrEmpty(filters?.TransactionType) ? DBNull.Value : (object)filters.TransactionType);
                        command.Parameters.AddWithValue("@Offset", (pageNumber - 1) * (pageSize ?? 10));
                        command.Parameters.AddWithValue("@PageSize", pageSize ?? 10);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            // Read transaction data
                            while (await reader.ReadAsync())
                            {
                                transactionList.Add(new BankCreditDebitReportItem
                                {
                                    TransactionId = reader.IsDBNull(reader.GetOrdinal("TransactionId")) ? 0 : reader.GetInt64(reader.GetOrdinal("TransactionId")),
                                    PersonalPaymentId = reader.IsDBNull(reader.GetOrdinal("PersonalPaymentId")) ? 0 : reader.GetInt64(reader.GetOrdinal("PersonalPaymentId")),
                                    BankName = reader.IsDBNull(reader.GetOrdinal("BankName")) ? string.Empty : reader.GetString(reader.GetOrdinal("BankName")),
                                    AccountNumber = reader.IsDBNull(reader.GetOrdinal("AccountNumber")) ? string.Empty : reader.GetString(reader.GetOrdinal("AccountNumber")),
                                    AccountHolderName = reader.IsDBNull(reader.GetOrdinal("AccountHolderName")) ? string.Empty : reader.GetString(reader.GetOrdinal("AccountHolderName")),
                                    BankBranch = reader.IsDBNull(reader.GetOrdinal("BankBranch")) ? string.Empty : reader.GetString(reader.GetOrdinal("BankBranch")),
                                    TransactionType = reader.IsDBNull(reader.GetOrdinal("TransactionType")) ? string.Empty : reader.GetString(reader.GetOrdinal("TransactionType")),
                                    Amount = reader.IsDBNull(reader.GetOrdinal("Amount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("Amount")),
                                    Balance = reader.IsDBNull(reader.GetOrdinal("Balance")) ? 0m : reader.GetDecimal(reader.GetOrdinal("Balance")),
                                    TransactionDescription = reader.IsDBNull(reader.GetOrdinal("TransactionDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("TransactionDescription")),
                                    TransactionDate = reader.IsDBNull(reader.GetOrdinal("TransactionDate")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("TransactionDate")),
                                    SaleId = reader.IsDBNull(reader.GetOrdinal("SaleId")) ? null : reader.GetInt64(reader.GetOrdinal("SaleId")),
                                    BillNumber = reader.IsDBNull(reader.GetOrdinal("BillNumber")) ? null : reader.GetInt64(reader.GetOrdinal("BillNumber")),
                                    ReferenceDescription = reader.IsDBNull(reader.GetOrdinal("ReferenceDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("ReferenceDescription"))
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
                                totalCreditAmount = reader.IsDBNull(reader.GetOrdinal("TotalCreditAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalCreditAmount"));
                                totalDebitAmount = reader.IsDBNull(reader.GetOrdinal("TotalDebitAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalDebitAmount"));
                                netBalance = reader.IsDBNull(reader.GetOrdinal("NetBalance")) ? 0m : reader.GetDecimal(reader.GetOrdinal("NetBalance"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return new BankCreditDebitReportViewModel
            {
                TransactionList = transactionList,
                Filters = filters ?? new BankCreditDebitReportFilters(),
                CurrentPage = pageNumber,
                TotalPages = pageSize.HasValue && pageSize.Value > 0
                   ? (int)Math.Ceiling(totalRecords / (double)pageSize.Value)
                   : 1,
                PageSize = pageSize,
                TotalCount = totalRecords,
                TotalCreditAmount = totalCreditAmount,
                TotalDebitAmount = totalDebitAmount,
                NetBalance = netBalance
            };
        }

        public async Task<BankCreditDebitReportViewModel> GetBankCreditDebitReportForExport(int pageNumber, int? pageSize, BankCreditDebitReportFilters? filters)
        {
            // For export, get all records without pagination
            var transactionList = new List<BankCreditDebitReportItem>();
            decimal totalCreditAmount = 0;
            decimal totalDebitAmount = 0;
            decimal netBalance = 0;

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT 
                            ppsd.PersonalPaymentSaleDetailId AS TransactionId,
                            ppsd.PersonalPaymentId,
                            pp.BankName,
                            pp.AccountNumber,
                            pp.AccountHolderName,
                            ISNULL(pp.BankBranch, '') AS BankBranch,
                            ppsd.TransactionType,
                            ppsd.Amount,
                            ppsd.Balance,
                            ISNULL(ppsd.TransactionDescription, '') AS TransactionDescription,
                            ppsd.TransactionDate,
                            ppsd.SaleId,
                            ISNULL(s.BillNumber, 0) AS BillNumber,
                            ISNULL(s.SaleDescription, '') AS ReferenceDescription
                        FROM PersonalPaymentSaleDetail ppsd
                        INNER JOIN PersonalPayments pp ON ppsd.PersonalPaymentId = pp.PersonalPaymentId
                        LEFT JOIN Sales s ON ppsd.SaleId = s.SaleId
                        WHERE ppsd.IsActive = 1
                            AND (@FromDate IS NULL OR ppsd.TransactionDate >= @FromDate)
                            AND (@ToDate IS NULL OR ppsd.TransactionDate <= @ToDate)
                            AND (@PersonalPaymentId IS NULL OR ppsd.PersonalPaymentId = @PersonalPaymentId)
                            AND (@TransactionType IS NULL OR ppsd.TransactionType = @TransactionType)
                        ORDER BY ppsd.TransactionDate DESC, pp.BankName, pp.AccountNumber;

                        SELECT 
                            SUM(CASE WHEN TransactionType = 'Credit' THEN Amount ELSE 0 END) AS TotalCreditAmount,
                            SUM(CASE WHEN TransactionType = 'Debit' THEN Amount ELSE 0 END) AS TotalDebitAmount,
                            SUM(CASE WHEN TransactionType = 'Credit' THEN Amount ELSE -Amount END) AS NetBalance
                        FROM PersonalPaymentSaleDetail ppsd
                        WHERE ppsd.IsActive = 1
                            AND (@FromDate IS NULL OR ppsd.TransactionDate >= @FromDate)
                            AND (@ToDate IS NULL OR ppsd.TransactionDate <= @ToDate)
                            AND (@PersonalPaymentId IS NULL OR ppsd.PersonalPaymentId = @PersonalPaymentId)
                            AND (@TransactionType IS NULL OR ppsd.TransactionType = @TransactionType);
                    ";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@FromDate", (object)filters?.FromDate ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ToDate", filters?.ToDate != null ? filters.ToDate.Value.AddDays(1).AddSeconds(-1) : DBNull.Value);
                        command.Parameters.AddWithValue("@PersonalPaymentId", (object)filters?.PersonalPaymentId ?? DBNull.Value);
                        command.Parameters.AddWithValue("@TransactionType", string.IsNullOrEmpty(filters?.TransactionType) ? DBNull.Value : (object)filters.TransactionType);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                transactionList.Add(new BankCreditDebitReportItem
                                {
                                    TransactionId = reader.IsDBNull(reader.GetOrdinal("TransactionId")) ? 0 : reader.GetInt64(reader.GetOrdinal("TransactionId")),
                                    PersonalPaymentId = reader.IsDBNull(reader.GetOrdinal("PersonalPaymentId")) ? 0 : reader.GetInt64(reader.GetOrdinal("PersonalPaymentId")),
                                    BankName = reader.IsDBNull(reader.GetOrdinal("BankName")) ? string.Empty : reader.GetString(reader.GetOrdinal("BankName")),
                                    AccountNumber = reader.IsDBNull(reader.GetOrdinal("AccountNumber")) ? string.Empty : reader.GetString(reader.GetOrdinal("AccountNumber")),
                                    AccountHolderName = reader.IsDBNull(reader.GetOrdinal("AccountHolderName")) ? string.Empty : reader.GetString(reader.GetOrdinal("AccountHolderName")),
                                    BankBranch = reader.IsDBNull(reader.GetOrdinal("BankBranch")) ? string.Empty : reader.GetString(reader.GetOrdinal("BankBranch")),
                                    TransactionType = reader.IsDBNull(reader.GetOrdinal("TransactionType")) ? string.Empty : reader.GetString(reader.GetOrdinal("TransactionType")),
                                    Amount = reader.IsDBNull(reader.GetOrdinal("Amount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("Amount")),
                                    Balance = reader.IsDBNull(reader.GetOrdinal("Balance")) ? 0m : reader.GetDecimal(reader.GetOrdinal("Balance")),
                                    TransactionDescription = reader.IsDBNull(reader.GetOrdinal("TransactionDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("TransactionDescription")),
                                    TransactionDate = reader.IsDBNull(reader.GetOrdinal("TransactionDate")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("TransactionDate")),
                                    SaleId = reader.IsDBNull(reader.GetOrdinal("SaleId")) ? null : reader.GetInt64(reader.GetOrdinal("SaleId")),
                                    BillNumber = reader.IsDBNull(reader.GetOrdinal("BillNumber")) ? null : reader.GetInt64(reader.GetOrdinal("BillNumber")),
                                    ReferenceDescription = reader.IsDBNull(reader.GetOrdinal("ReferenceDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("ReferenceDescription"))
                                });
                            }

                            await reader.NextResultAsync();
                            if (await reader.ReadAsync())
                            {
                                totalCreditAmount = reader.IsDBNull(reader.GetOrdinal("TotalCreditAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalCreditAmount"));
                                totalDebitAmount = reader.IsDBNull(reader.GetOrdinal("TotalDebitAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalDebitAmount"));
                                netBalance = reader.IsDBNull(reader.GetOrdinal("NetBalance")) ? 0m : reader.GetDecimal(reader.GetOrdinal("NetBalance"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return new BankCreditDebitReportViewModel
            {
                TransactionList = transactionList,
                Filters = filters ?? new BankCreditDebitReportFilters(),
                CurrentPage = 1,
                TotalPages = 1,
                PageSize = transactionList.Count,
                TotalCount = transactionList.Count,
                TotalCreditAmount = totalCreditAmount,
                TotalDebitAmount = totalDebitAmount,
                NetBalance = netBalance
            };
        }

        public async Task<PurchaseReportViewModel> GetPurchaseReport(int pageNumber, int? pageSize, PurchaseReportFilters? filters)
        {
            var purchaseList = new List<PurchaseReportItem>();
            int totalRecords = 0;
            decimal totalAmount = 0;
            decimal totalDiscountAmount = 0;
            decimal totalPaidAmount = 0;
            decimal totalDueAmount = 0;

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT 
                            po.PurchaseOrderId,
                            ISNULL(s.SupplierName, '') AS VendorName,
                            po.BillNumber,
                            po.SupplierId_FK AS VendorIdFk,
                            po.PurchaseOrderDate AS PurchaseDate,
                            po.TotalAmount,
                            po.DiscountAmount,
                            po.TotalReceivedAmount AS PaidAmount,
                            po.TotalDueAmount AS DueAmount,
                            ISNULL(po.PurchaseOrderDescription, '') AS PurchaseDescription
                        FROM PurchaseOrders po
                        LEFT JOIN Suppliers s ON po.SupplierId_FK = s.SupplierId
                        WHERE po.IsDeleted = 0
                            AND (@FromDate IS NULL OR po.PurchaseOrderDate >= @FromDate)
                            AND (@ToDate IS NULL OR po.PurchaseOrderDate <= @ToDate)
                            AND (@VendorId IS NULL OR po.SupplierId_FK = @VendorId)
                        ORDER BY po.PurchaseOrderDate DESC, po.BillNumber DESC
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

                        SELECT COUNT(*) AS TotalRecords
                        FROM PurchaseOrders po
                        WHERE po.IsDeleted = 0
                            AND (@FromDate IS NULL OR po.PurchaseOrderDate >= @FromDate)
                            AND (@ToDate IS NULL OR po.PurchaseOrderDate <= @ToDate)
                            AND (@VendorId IS NULL OR po.SupplierId_FK = @VendorId);

                        SELECT 
                            SUM(TotalAmount) AS TotalAmount,
                            SUM(DiscountAmount) AS TotalDiscountAmount,
                            SUM(TotalReceivedAmount) AS TotalPaidAmount,
                            SUM(TotalDueAmount) AS TotalDueAmount
                        FROM PurchaseOrders po
                        WHERE po.IsDeleted = 0
                            AND (@FromDate IS NULL OR po.PurchaseOrderDate >= @FromDate)
                            AND (@ToDate IS NULL OR po.PurchaseOrderDate <= @ToDate)
                            AND (@VendorId IS NULL OR po.SupplierId_FK = @VendorId);
                    ";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@FromDate", (object)filters?.FromDate ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ToDate", filters?.ToDate != null ? filters.ToDate.Value.AddDays(1).AddSeconds(-1) : DBNull.Value);
                        command.Parameters.AddWithValue("@VendorId", (object)filters?.VendorId ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Offset", (pageNumber - 1) * (pageSize ?? 10));
                        command.Parameters.AddWithValue("@PageSize", pageSize ?? 10);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            // Read purchase data
                            while (await reader.ReadAsync())
                            {
                                purchaseList.Add(new PurchaseReportItem
                                {
                                    PurchaseOrderId = reader.IsDBNull(reader.GetOrdinal("PurchaseOrderId")) ? 0 : reader.GetInt64(reader.GetOrdinal("PurchaseOrderId")),
                                    VendorName = reader.IsDBNull(reader.GetOrdinal("VendorName")) ? string.Empty : reader.GetString(reader.GetOrdinal("VendorName")),
                                    BillNumber = reader.IsDBNull(reader.GetOrdinal("BillNumber")) ? 0 : reader.GetInt64(reader.GetOrdinal("BillNumber")),
                                    VendorIdFk = reader.IsDBNull(reader.GetOrdinal("VendorIdFk")) ? 0 : reader.GetInt64(reader.GetOrdinal("VendorIdFk")),
                                    PurchaseDate = reader.IsDBNull(reader.GetOrdinal("PurchaseDate")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("PurchaseDate")),
                                    TotalAmount = reader.IsDBNull(reader.GetOrdinal("TotalAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                                    DiscountAmount = reader.IsDBNull(reader.GetOrdinal("DiscountAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("DiscountAmount")),
                                    PaidAmount = reader.IsDBNull(reader.GetOrdinal("PaidAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("PaidAmount")),
                                    DueAmount = reader.IsDBNull(reader.GetOrdinal("DueAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("DueAmount")),
                                    PurchaseDescription = reader.IsDBNull(reader.GetOrdinal("PurchaseDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("PurchaseDescription"))
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
                                totalAmount = reader.IsDBNull(reader.GetOrdinal("TotalAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalAmount"));
                                totalDiscountAmount = reader.IsDBNull(reader.GetOrdinal("TotalDiscountAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalDiscountAmount"));
                                totalPaidAmount = reader.IsDBNull(reader.GetOrdinal("TotalPaidAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalPaidAmount"));
                                totalDueAmount = reader.IsDBNull(reader.GetOrdinal("TotalDueAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalDueAmount"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return new PurchaseReportViewModel
            {
                PurchaseList = purchaseList,
                Filters = filters ?? new PurchaseReportFilters(),
                CurrentPage = pageNumber,
                TotalPages = pageSize.HasValue && pageSize.Value > 0
                   ? (int)Math.Ceiling(totalRecords / (double)pageSize.Value)
                   : 1,
                PageSize = pageSize,
                TotalCount = totalRecords,
                TotalAmount = totalAmount,
                TotalDiscountAmount = totalDiscountAmount,
                TotalPaidAmount = totalPaidAmount,
                TotalDueAmount = totalDueAmount
            };
        }

        public async Task<PurchaseReportViewModel> GetPurchaseReportForExport(int pageNumber, int? pageSize, PurchaseReportFilters? filters)
        {
            // For export, get all records without pagination
            var purchaseList = new List<PurchaseReportItem>();
            decimal totalAmount = 0;
            decimal totalDiscountAmount = 0;
            decimal totalPaidAmount = 0;
            decimal totalDueAmount = 0;

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT 
                            po.PurchaseOrderId,
                            ISNULL(s.SupplierName, '') AS VendorName,
                            po.BillNumber,
                            po.SupplierId_FK AS VendorIdFk,
                            po.PurchaseOrderDate AS PurchaseDate,
                            po.TotalAmount,
                            po.DiscountAmount,
                            po.TotalReceivedAmount AS PaidAmount,
                            po.TotalDueAmount AS DueAmount,
                            ISNULL(po.PurchaseOrderDescription, '') AS PurchaseDescription
                        FROM PurchaseOrders po
                        LEFT JOIN Suppliers s ON po.SupplierId_FK = s.SupplierId
                        WHERE po.IsDeleted = 0
                            AND (@FromDate IS NULL OR po.PurchaseOrderDate >= @FromDate)
                            AND (@ToDate IS NULL OR po.PurchaseOrderDate <= @ToDate)
                            AND (@VendorId IS NULL OR po.SupplierId_FK = @VendorId)
                        ORDER BY po.PurchaseOrderDate DESC, po.BillNumber DESC;

                        SELECT 
                            SUM(TotalAmount) AS TotalAmount,
                            SUM(DiscountAmount) AS TotalDiscountAmount,
                            SUM(TotalReceivedAmount) AS TotalPaidAmount,
                            SUM(TotalDueAmount) AS TotalDueAmount
                        FROM PurchaseOrders po
                        WHERE po.IsDeleted = 0
                            AND (@FromDate IS NULL OR po.PurchaseOrderDate >= @FromDate)
                            AND (@ToDate IS NULL OR po.PurchaseOrderDate <= @ToDate)
                            AND (@VendorId IS NULL OR po.SupplierId_FK = @VendorId);
                    ";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@FromDate", (object)filters?.FromDate ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ToDate", filters?.ToDate != null ? filters.ToDate.Value.AddDays(1).AddSeconds(-1) : DBNull.Value);
                        command.Parameters.AddWithValue("@VendorId", (object)filters?.VendorId ?? DBNull.Value);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                purchaseList.Add(new PurchaseReportItem
                                {
                                    PurchaseOrderId = reader.IsDBNull(reader.GetOrdinal("PurchaseOrderId")) ? 0 : reader.GetInt64(reader.GetOrdinal("PurchaseOrderId")),
                                    VendorName = reader.IsDBNull(reader.GetOrdinal("VendorName")) ? string.Empty : reader.GetString(reader.GetOrdinal("VendorName")),
                                    BillNumber = reader.IsDBNull(reader.GetOrdinal("BillNumber")) ? 0 : reader.GetInt64(reader.GetOrdinal("BillNumber")),
                                    VendorIdFk = reader.IsDBNull(reader.GetOrdinal("VendorIdFk")) ? 0 : reader.GetInt64(reader.GetOrdinal("VendorIdFk")),
                                    PurchaseDate = reader.IsDBNull(reader.GetOrdinal("PurchaseDate")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("PurchaseDate")),
                                    TotalAmount = reader.IsDBNull(reader.GetOrdinal("TotalAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                                    DiscountAmount = reader.IsDBNull(reader.GetOrdinal("DiscountAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("DiscountAmount")),
                                    PaidAmount = reader.IsDBNull(reader.GetOrdinal("PaidAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("PaidAmount")),
                                    DueAmount = reader.IsDBNull(reader.GetOrdinal("DueAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("DueAmount")),
                                    PurchaseDescription = reader.IsDBNull(reader.GetOrdinal("PurchaseDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("PurchaseDescription"))
                                });
                            }

                            await reader.NextResultAsync();
                            if (await reader.ReadAsync())
                            {
                                totalAmount = reader.IsDBNull(reader.GetOrdinal("TotalAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalAmount"));
                                totalDiscountAmount = reader.IsDBNull(reader.GetOrdinal("TotalDiscountAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalDiscountAmount"));
                                totalPaidAmount = reader.IsDBNull(reader.GetOrdinal("TotalPaidAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalPaidAmount"));
                                totalDueAmount = reader.IsDBNull(reader.GetOrdinal("TotalDueAmount")) ? 0m : reader.GetDecimal(reader.GetOrdinal("TotalDueAmount"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return new PurchaseReportViewModel
            {
                PurchaseList = purchaseList,
                Filters = filters ?? new PurchaseReportFilters(),
                CurrentPage = 1,
                TotalPages = 1,
                PageSize = purchaseList.Count,
                TotalCount = purchaseList.Count,
                TotalAmount = totalAmount,
                TotalDiscountAmount = totalDiscountAmount,
                TotalPaidAmount = totalPaidAmount,
                TotalDueAmount = totalDueAmount
            };
        }

        public async Task<ProductWiseSalesReportViewModel> GetProductWiseSalesReport(int pageNumber, int? pageSize, ProductWiseSalesReportFilters? filters)
        {
            var salesList = new List<ProductWiseSalesReportItem>();
            int totalRecords = 0;
            decimal totalAmount = 0;
            decimal totalWeight = 0;
            long totalQty = 0;

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        WITH DailySales AS (
                            SELECT 
                                CAST(s.SaleDate AS DATE) AS SaleDate,
                                sd.PrductId_FK AS ProductId,
                                p.ProductName,
                                ISNULL(p.ProductCode, '') AS ProductCode,
                                SUM(CAST(sd.Quantity AS DECIMAL(18, 3))) AS Weight,
                                SUM(sd.Quantity) AS Qty,
                                SUM(sd.PayableAmount) AS Amount
                            FROM SaleDetails sd
                            INNER JOIN Sales s ON sd.SaleId_FK = s.SaleId
                            INNER JOIN Products p ON sd.PrductId_FK = p.ProductId
                            WHERE s.IsDeleted = 0
                                AND (@FromDate IS NULL OR CAST(s.SaleDate AS DATE) >= @FromDate)
                                AND (@ToDate IS NULL OR CAST(s.SaleDate AS DATE) <= @ToDate)
                                AND (@ProductId IS NULL OR sd.PrductId_FK = @ProductId)
                            GROUP BY CAST(s.SaleDate AS DATE), sd.PrductId_FK, p.ProductName, p.ProductCode
                        ),
                        ProductTotals AS (
                            SELECT 
                                ProductId,
                                ProductName,
                                ProductCode,
                                SUM(Weight) AS TotalWeight,
                                SUM(Qty) AS TotalQty,
                                SUM(Amount) AS TotalAmount,
                                CASE 
                                    WHEN SUM(Qty) > 0 THEN SUM(Amount) / SUM(Qty)
                                    ELSE 0
                                END AS AvgRate
                            FROM DailySales
                            GROUP BY ProductId, ProductName, ProductCode
                        )
                        SELECT 
                            ds.SaleDate,
                            ds.ProductId,
                            ds.ProductName,
                            ds.ProductCode,
                            ds.Weight,
                            ds.Qty,
                            CASE 
                                WHEN ds.Qty > 0 THEN ds.Amount / ds.Qty
                                ELSE 0
                            END AS Rate,
                            ds.Amount,
                            0 AS IsTotalRow
                        FROM DailySales ds
                        ORDER BY ds.ProductName, ds.SaleDate
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

                        SELECT COUNT(*) AS TotalRecords
                        FROM (
                            SELECT 
                                CAST(s.SaleDate AS DATE) AS SaleDate,
                                sd.PrductId_FK AS ProductId
                            FROM SaleDetails sd
                            INNER JOIN Sales s ON sd.SaleId_FK = s.SaleId
                            INNER JOIN Products p ON sd.PrductId_FK = p.ProductId
                            WHERE s.IsDeleted = 0
                                AND (@FromDate IS NULL OR CAST(s.SaleDate AS DATE) >= @FromDate)
                                AND (@ToDate IS NULL OR CAST(s.SaleDate AS DATE) <= @ToDate)
                                AND (@ProductId IS NULL OR sd.PrductId_FK = @ProductId)
                            GROUP BY CAST(s.SaleDate AS DATE), sd.PrductId_FK, p.ProductName, p.ProductCode
                        ) AS DailySales;

                        SELECT 
                            SUM(CAST(sd.Quantity AS DECIMAL(18, 3))) AS TotalWeight,
                            SUM(sd.Quantity) AS TotalQty,
                            SUM(sd.PayableAmount) AS TotalAmount
                        FROM SaleDetails sd
                        INNER JOIN Sales s ON sd.SaleId_FK = s.SaleId
                        WHERE s.IsDeleted = 0
                            AND (@FromDate IS NULL OR CAST(s.SaleDate AS DATE) >= @FromDate)
                            AND (@ToDate IS NULL OR CAST(s.SaleDate AS DATE) <= @ToDate)
                            AND (@ProductId IS NULL OR sd.PrductId_FK = @ProductId);
                    ";

                    var offset = (pageNumber - 1) * (pageSize ?? 10);
                    var pageSizeValue = pageSize ?? 10;

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@FromDate", (object)filters?.FromDate ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ToDate", (object)filters?.ToDate ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ProductId", (object)filters?.ProductId ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Offset", offset);
                        command.Parameters.AddWithValue("@PageSize", pageSizeValue);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            // Read daily sales data
                            while (await reader.ReadAsync())
                            {
                                salesList.Add(new ProductWiseSalesReportItem
                                {
                                    SaleDate = reader.IsDBNull(reader.GetOrdinal("SaleDate"))
                                        ? DateTime.MinValue
                                        : reader.GetDateTime(reader.GetOrdinal("SaleDate")),
                                    ProductId = reader.IsDBNull(reader.GetOrdinal("ProductId"))
                                        ? 0
                                        : reader.GetInt64(reader.GetOrdinal("ProductId")),
                                    ProductName = reader.IsDBNull(reader.GetOrdinal("ProductName"))
                                        ? string.Empty
                                        : reader.GetString(reader.GetOrdinal("ProductName")),
                                    ProductCode = reader.IsDBNull(reader.GetOrdinal("ProductCode"))
                                        ? string.Empty
                                        : reader.GetString(reader.GetOrdinal("ProductCode")),
                                    Weight = reader.IsDBNull(reader.GetOrdinal("Weight"))
                                        ? 0m
                                        : reader.GetDecimal(reader.GetOrdinal("Weight")),
                                    Qty = reader.IsDBNull(reader.GetOrdinal("Qty"))
                                        ? 0
                                        : reader.GetInt64(reader.GetOrdinal("Qty")),
                                    Rate = reader.IsDBNull(reader.GetOrdinal("Rate"))
                                        ? 0m
                                        : reader.GetDecimal(reader.GetOrdinal("Rate")),
                                    Amount = reader.IsDBNull(reader.GetOrdinal("Amount"))
                                        ? 0m
                                        : reader.GetDecimal(reader.GetOrdinal("Amount")),
                                    IsTotalRow = false
                                });
                            }

                            // Read total records
                            await reader.NextResultAsync();
                            if (await reader.ReadAsync())
                            {
                                totalRecords = reader.IsDBNull(reader.GetOrdinal("TotalRecords"))
                                    ? 0
                                    : reader.GetInt32(reader.GetOrdinal("TotalRecords"));
                            }

                            // Read summary totals
                            await reader.NextResultAsync();
                            if (await reader.ReadAsync())
                            {
                                totalWeight = reader.IsDBNull(reader.GetOrdinal("TotalWeight"))
                                    ? 0m
                                    : reader.GetDecimal(reader.GetOrdinal("TotalWeight"));
                                totalQty = reader.IsDBNull(reader.GetOrdinal("TotalQty"))
                                    ? 0
                                    : reader.GetInt64(reader.GetOrdinal("TotalQty"));
                                totalAmount = reader.IsDBNull(reader.GetOrdinal("TotalAmount"))
                                    ? 0m
                                    : reader.GetDecimal(reader.GetOrdinal("TotalAmount"));
                            }
                        }
                    }

                    // Add product total rows
                    // Group sales by product and add total rows
                    var productGroups = salesList.GroupBy(s => s.ProductId).ToList();
                    var finalList = new List<ProductWiseSalesReportItem>();
                    
                    foreach (var group in productGroups)
                    {
                        var productSales = group.OrderBy(s => s.SaleDate).ToList();
                        finalList.AddRange(productSales);
                        
                        // Add total row for this product
                        var productTotal = new ProductWiseSalesReportItem
                        {
                            SaleDate = DateTime.MinValue,
                            ProductId = group.Key,
                            ProductName = productSales.First().ProductName,
                            ProductCode = productSales.First().ProductCode,
                            Weight = productSales.Sum(s => s.Weight),
                            Qty = productSales.Sum(s => s.Qty),
                            Amount = productSales.Sum(s => s.Amount),
                            Rate = productSales.Sum(s => s.Qty) > 0 
                                ? productSales.Sum(s => s.Amount) / productSales.Sum(s => s.Qty) 
                                : 0m,
                            IsTotalRow = true
                        };
                        finalList.Add(productTotal);
                    }
                    
                    salesList = finalList;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return new ProductWiseSalesReportViewModel
            {
                SalesList = salesList,
                Filters = filters ?? new ProductWiseSalesReportFilters(),
                CurrentPage = pageNumber,
                TotalPages = pageSize.HasValue && pageSize.Value > 0
                    ? (int)Math.Ceiling(totalRecords / (double)pageSize.Value)
                    : 1,
                PageSize = pageSize,
                TotalCount = totalRecords,
                TotalAmount = totalAmount,
                TotalWeight = totalWeight,
                TotalQty = totalQty
            };
        }

        public async Task<GeneralExpensesReportViewModel> GetGeneralExpensesReport(int pageNumber, int? pageSize, GeneralExpensesReportFilters? filters)
        {
            var expensesList = new List<GeneralExpensesReportItem>();
            int totalRecords = 0;
            decimal totalAmount = 0;

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        WITH DailyExpenses AS (
                            SELECT 
                                CAST(e.ExpenseDate AS DATE) AS ExpenseDate,
                                e.ExpenseTypeId_FK AS ExpenseTypeId,
                                ISNULL(et.ExpenseTypeName, '') AS ExpenseTypeName,
                                MAX(e.ExpenseDetail) AS ExpenseDetail,
                                SUM(e.Amount) AS Amount
                            FROM Expenses e
                            LEFT JOIN AdminExpenseTypes et ON e.ExpenseTypeId_FK = et.ExpenseTypeId
                            WHERE (@FromDate IS NULL OR CAST(e.ExpenseDate AS DATE) >= @FromDate)
                                AND (@ToDate IS NULL OR CAST(e.ExpenseDate AS DATE) <= @ToDate)
                                AND (@ExpenseTypeId IS NULL OR e.ExpenseTypeId_FK = @ExpenseTypeId)
                            GROUP BY CAST(e.ExpenseDate AS DATE), e.ExpenseTypeId_FK, et.ExpenseTypeName
                        )
                        SELECT 
                            de.ExpenseDate,
                            de.ExpenseTypeId,
                            de.ExpenseTypeName,
                            de.ExpenseDetail,
                            de.Amount,
                            0 AS IsTotalRow
                        FROM DailyExpenses de
                        ORDER BY de.ExpenseTypeName, de.ExpenseDate
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

                        SELECT COUNT(*) AS TotalRecords
                        FROM (
                            SELECT 
                                CAST(e.ExpenseDate AS DATE) AS ExpenseDate,
                                e.ExpenseTypeId_FK AS ExpenseTypeId
                            FROM Expenses e
                            LEFT JOIN AdminExpenseTypes et ON e.ExpenseTypeId_FK = et.ExpenseTypeId
                            WHERE (@FromDate IS NULL OR CAST(e.ExpenseDate AS DATE) >= @FromDate)
                                AND (@ToDate IS NULL OR CAST(e.ExpenseDate AS DATE) <= @ToDate)
                                AND (@ExpenseTypeId IS NULL OR e.ExpenseTypeId_FK = @ExpenseTypeId)
                            GROUP BY CAST(e.ExpenseDate AS DATE), e.ExpenseTypeId_FK, et.ExpenseTypeName
                        ) AS DailyExpenses;

                        SELECT 
                            SUM(e.Amount) AS TotalAmount
                        FROM Expenses e
                        WHERE (@FromDate IS NULL OR CAST(e.ExpenseDate AS DATE) >= @FromDate)
                            AND (@ToDate IS NULL OR CAST(e.ExpenseDate AS DATE) <= @ToDate)
                            AND (@ExpenseTypeId IS NULL OR e.ExpenseTypeId_FK = @ExpenseTypeId);
                    ";

                    var offset = (pageNumber - 1) * (pageSize ?? 10);
                    var pageSizeValue = pageSize ?? 10;

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@FromDate", (object)filters?.FromDate ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ToDate", (object)filters?.ToDate ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ExpenseTypeId", (object)filters?.ExpenseTypeId ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Offset", offset);
                        command.Parameters.AddWithValue("@PageSize", pageSizeValue);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            // Read daily expenses data
                            while (await reader.ReadAsync())
                            {
                                expensesList.Add(new GeneralExpensesReportItem
                                {
                                    ExpenseDate = reader.IsDBNull(reader.GetOrdinal("ExpenseDate"))
                                        ? DateTime.MinValue
                                        : reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
                                    ExpenseTypeId = reader.IsDBNull(reader.GetOrdinal("ExpenseTypeId"))
                                        ? 0
                                        : reader.GetInt64(reader.GetOrdinal("ExpenseTypeId")),
                                    ExpenseTypeName = reader.IsDBNull(reader.GetOrdinal("ExpenseTypeName"))
                                        ? string.Empty
                                        : reader.GetString(reader.GetOrdinal("ExpenseTypeName")),
                                    ExpenseDetail = reader.IsDBNull(reader.GetOrdinal("ExpenseDetail"))
                                        ? string.Empty
                                        : reader.GetString(reader.GetOrdinal("ExpenseDetail")),
                                    Amount = reader.IsDBNull(reader.GetOrdinal("Amount"))
                                        ? 0m
                                        : reader.GetDecimal(reader.GetOrdinal("Amount")),
                                    IsTotalRow = false
                                });
                            }

                            // Read total records
                            await reader.NextResultAsync();
                            if (await reader.ReadAsync())
                            {
                                totalRecords = reader.IsDBNull(reader.GetOrdinal("TotalRecords"))
                                    ? 0
                                    : reader.GetInt32(reader.GetOrdinal("TotalRecords"));
                            }

                            // Read summary totals
                            await reader.NextResultAsync();
                            if (await reader.ReadAsync())
                            {
                                totalAmount = reader.IsDBNull(reader.GetOrdinal("TotalAmount"))
                                    ? 0m
                                    : reader.GetDecimal(reader.GetOrdinal("TotalAmount"));
                            }
                        }
                    }

                    // Add expense type total rows
                    // Group expenses by expense type and add total rows
                    var expenseTypeGroups = expensesList.GroupBy(e => e.ExpenseTypeId).ToList();
                    var finalList = new List<GeneralExpensesReportItem>();
                    
                    foreach (var group in expenseTypeGroups)
                    {
                        var typeExpenses = group.OrderBy(e => e.ExpenseDate).ToList();
                        finalList.AddRange(typeExpenses);
                        
                        // Add total row for this expense type
                        var typeTotal = new GeneralExpensesReportItem
                        {
                            ExpenseDate = DateTime.MinValue,
                            ExpenseTypeId = group.Key,
                            ExpenseTypeName = typeExpenses.First().ExpenseTypeName,
                            ExpenseDetail = string.Empty,
                            Amount = typeExpenses.Sum(e => e.Amount),
                            IsTotalRow = true
                        };
                        finalList.Add(typeTotal);
                    }
                    
                    expensesList = finalList;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return new GeneralExpensesReportViewModel
            {
                ExpensesList = expensesList,
                Filters = filters ?? new GeneralExpensesReportFilters(),
                CurrentPage = pageNumber,
                TotalPages = pageSize.HasValue && pageSize.Value > 0
                    ? (int)Math.Ceiling(totalRecords / (double)pageSize.Value)
                    : 1,
                PageSize = pageSize,
                TotalCount = totalRecords,
                TotalAmount = totalAmount
            };
        }

    }
}
