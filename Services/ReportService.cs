using IMS.CommonUtilities;
using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;


namespace IMS.Services
{
    public class ReportService : IReportService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<ReportService> _logger;
        private readonly IUnitConversionService _unitConversionService;
        private readonly IProductService _productService;
        private readonly IAdminMeasuringUnitService _measuringUnitService;

        public ReportService(
            IDbContextFactory dbContextFactory,
            ILogger<ReportService> logger,
            IUnitConversionService unitConversionService,
            IProductService productService,
            IAdminMeasuringUnitService measuringUnitService)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _unitConversionService = unitConversionService;
            _productService = productService;
            _measuringUnitService = measuringUnitService;
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
                    using (var command = new SqlCommand("GetAllSalesReport", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        //command.Parameters.AddWithValue("@PageNo", pageNumber);
                        //command.Parameters.AddWithValue("@PageSize", pageSize);
                        command.Parameters.AddWithValue("@pIsDeleted", DBNull.Value);
                        command.Parameters.AddWithValue("@pCustomerId", (object)salesReportsFilters.CustomerId ?? DBNull.Value);
                        command.Parameters.AddWithValue("@pBillNumber",  DBNull.Value);
                        command.Parameters.AddWithValue("@pSaleFrom", salesReportsFilters.FromDate == default(DateTime) ? DateTimeHelper.Now : salesReportsFilters.FromDate);
                        command.Parameters.AddWithValue("@pSaleDateTo", salesReportsFilters.ToDate == default(DateTime) ? DateTimeHelper.Now : salesReportsFilters.ToDate);
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
                                    SupplierName = reader.IsDBNull(reader.GetOrdinal("SupplierName"))
            ? string.Empty
            : reader.GetString(reader.GetOrdinal("SupplierName")),

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
                        WHERE ISNULL(IsDeleted,0) = 0
                            AND (@CustomerId IS NULL OR CustomerId_FK = @CustomerId)
                            AND (@FromDate IS NULL OR SaleDate >= @FromDate)
                            AND (@ToDate IS NULL OR SaleDate <= @ToDate);
                    ";

                    using (var totalsCommand = new SqlCommand(totalsSql, connection))
                    {
                        totalsCommand.Parameters.AddWithValue("@CustomerId", (object)salesReportsFilters?.CustomerId ?? DBNull.Value);
                        totalsCommand.Parameters.AddWithValue("@FromDate", salesReportsFilters?.FromDate == default(DateTime) || salesReportsFilters?.FromDate ==null ? DBNull.Value : (object)salesReportsFilters.FromDate);
                        totalsCommand.Parameters.AddWithValue("@ToDate", salesReportsFilters?.ToDate == default(DateTime) || salesReportsFilters?.ToDate==null ? DBNull.Value : (object)salesReportsFilters.ToDate.Value.AddDays(1).AddSeconds(-1));

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
                        command.Parameters.AddWithValue("@pSaleFrom", salesReportsFilters.FromDate == default(DateTime) ? DateTimeHelper.Now : salesReportsFilters.FromDate);
                        command.Parameters.AddWithValue("@pSaleDateTo", salesReportsFilters.ToDate == default(DateTime) ? DateTimeHelper.Now : salesReportsFilters.ToDate);
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

                    if (sale.Count > 0)
                    {
                        var custIds = sale.Where(x => x.CustomerIdFk > 0).Select(x => x.CustomerIdFk).Distinct().ToList();
                        if (custIds.Count > 0)
                        {
                            var ph = string.Join(",", custIds.Select((_, i) => "@cu" + i));
                            using (var urduCmd = new SqlCommand($"SELECT CustomerId, UrduName FROM Customers WHERE CustomerId IN ({ph})", connection))
                            {
                                for (var i = 0; i < custIds.Count; i++)
                                    urduCmd.Parameters.AddWithValue("@cu" + i, custIds[i]);
                                using (var ur = await urduCmd.ExecuteReaderAsync())
                                {
                                    var urduMap = new Dictionary<long, string?>();
                                    while (await ur.ReadAsync())
                                    {
                                        var cid = ur.GetInt64(0);
                                        var un = ur.IsDBNull(1) ? null : ur.GetString(1);
                                        urduMap[cid] = string.IsNullOrWhiteSpace(un) ? null : un;
                                    }
                                    foreach (var row in sale)
                                    {
                                        if (urduMap.TryGetValue(row.CustomerIdFk, out var u))
                                            row.CustomerUrduName = u;
                                    }
                                }
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
            var productDetailSections = new List<ProductWiseProfitLossDetailSection>();
            int totalRecords = 0;
            decimal totalSalesAmount = 0;
            decimal totalPurchaseCost = 0;
            decimal totalProfitLoss = 0;

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    var toDateEnd = filters?.ToDate != null
                        ? filters.ToDate.Value.Date.AddDays(1).AddSeconds(-1)
                        : (DateTime?)null;

                    if (filters?.ViewMode == ProfitLossReportViewMode.Overall)
                    {
                        var allIds = await GetAllProfitLossProductIdsAsync(connection, filters, toDateEnd);
                        totalRecords = allIds.Count;
                        if (allIds.Count == 0)
                        {
                            return new ProfitLossReportViewModel
                            {
                                ProfitLossList = profitLossList,
                                ProductDetailSections = productDetailSections,
                                Filters = filters ?? new ProfitLossReportFilters(),
                                CurrentPage = 1,
                                TotalPages = 1,
                                PageSize = pageSize,
                                TotalCount = 0
                            };
                        }

                        var allSections = await BuildProductWiseProfitLossDetailSectionsAsync(connection, filters, allIds, toDateEnd);
                        foreach (var s in allSections)
                        {
                            profitLossList.Add(new ProfitLossReportItem
                            {
                                ProductId = s.ProductId,
                                ProductName = s.ProductName,
                                ProductUrduName = s.ProductUrduName,
                                ProductCode = s.ProductCode,
                                TotalQuantitySold = (long)Math.Round(s.SaleWeight, MidpointRounding.AwayFromZero),
                                TotalSalesAmount = s.SaleAmount,
                                TotalPurchaseCost = s.TotalStockAmount,
                                ProfitLoss = s.Profit,
                                ProfitLossPercentage = s.TotalStockAmount > 0 ? (s.Profit / s.TotalStockAmount) * 100m : 0m
                            });
                            totalSalesAmount += s.SaleAmount;
                            totalPurchaseCost += s.TotalStockAmount;
                            totalProfitLoss += s.Profit;
                        }

                        profitLossList = profitLossList.OrderBy(p => p.ProductName).ToList();

                        return new ProfitLossReportViewModel
                        {
                            ProfitLossList = profitLossList,
                            ProductDetailSections = new List<ProductWiseProfitLossDetailSection>(),
                            Filters = filters ?? new ProfitLossReportFilters(),
                            CurrentPage = 1,
                            TotalPages = 1,
                            PageSize = pageSize,
                            TotalCount = totalRecords,
                            TotalSalesAmount = totalSalesAmount,
                            TotalPurchaseCost = totalPurchaseCost,
                            TotalProfitLoss = totalProfitLoss
                        };
                    }

                    var (pageIds, count) = await GetProfitLossProductIdsPageAsync(connection, filters, pageNumber, pageSize ?? 10, toDateEnd);
                    totalRecords = count;
                    if (pageIds.Count == 0)
                    {
                        return new ProfitLossReportViewModel
                        {
                            ProfitLossList = profitLossList,
                            ProductDetailSections = productDetailSections,
                            Filters = filters ?? new ProfitLossReportFilters(),
                            CurrentPage = pageNumber,
                            TotalPages = pageSize.HasValue && pageSize.Value > 0
                                ? (int)Math.Ceiling(totalRecords / (double)pageSize.Value)
                                : 1,
                            PageSize = pageSize,
                            TotalCount = totalRecords
                        };
                    }

                    productDetailSections = await BuildProductWiseProfitLossDetailSectionsAsync(connection, filters, pageIds, toDateEnd);

                    foreach (var s in productDetailSections)
                    {
                        profitLossList.Add(new ProfitLossReportItem
                        {
                            ProductId = s.ProductId,
                            ProductName = s.ProductName,
                            ProductUrduName = s.ProductUrduName,
                            ProductCode = s.ProductCode,
                            TotalQuantitySold = (long)Math.Round(s.SaleWeight, MidpointRounding.AwayFromZero),
                            TotalSalesAmount = s.SaleAmount,
                            TotalPurchaseCost = s.TotalStockAmount,
                            ProfitLoss = s.Profit,
                            ProfitLossPercentage = s.TotalStockAmount > 0 ? (s.Profit / s.TotalStockAmount) * 100m : 0m
                        });
                        totalSalesAmount += s.SaleAmount;
                        totalPurchaseCost += s.TotalStockAmount;
                        totalProfitLoss += s.Profit;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetProductWiseProfitLoss failed.");
                throw;
            }

            return new ProfitLossReportViewModel
            {
                ProfitLossList = profitLossList,
                ProductDetailSections = productDetailSections,
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

        /// <summary>Distinct products with sales, purchases, product-expenses in range, or non-zero stock.</summary>
        private static async Task<(List<long> Ids, int TotalCount)> GetProfitLossProductIdsPageAsync(
            SqlConnection connection,
            ProfitLossReportFilters? filters,
            int pageNumber,
            int pageSize,
            DateTime? toDateEnd)
        {
            object fromParam = (object?)filters?.FromDate ?? DBNull.Value;
            object toParam = (object?)toDateEnd ?? DBNull.Value;
            object productIdParam = (object?)filters?.ProductId ?? DBNull.Value;

            const string unionBody = """
                SELECT sd.PrductId_FK AS ProductId
                FROM SaleDetails sd
                INNER JOIN Sales s ON sd.SaleId_FK = s.SaleId
                WHERE s.IsDeleted = 0
                  AND (@FromDate IS NULL OR s.SaleDate >= @FromDate)
                  AND (@ToDate IS NULL OR s.SaleDate <= @ToDate)
                  AND (@ProductId IS NULL OR sd.PrductId_FK = @ProductId)
                UNION
                SELECT poi.PrductId_FK AS ProductId
                FROM PurchaseOrderItems poi
                INNER JOIN PurchaseOrders po ON poi.PurchaseOrderId_FK = po.PurchaseOrderId
                WHERE ISNULL(po.IsDeleted, 0) = 0
                  AND (@FromDate IS NULL OR po.PurchaseOrderDate >= @FromDate)
                  AND (@ToDate IS NULL OR po.PurchaseOrderDate <= @ToDate)
                  AND (@ProductId IS NULL OR poi.PrductId_FK = @ProductId)
                UNION
                SELECT e.ProductId_FK AS ProductId
                FROM Expenses e
                WHERE e.ProductId_FK IS NOT NULL
                  AND (@FromDate IS NULL OR e.ExpenseDate >= @FromDate)
                  AND (@ToDate IS NULL OR e.ExpenseDate <= @ToDate)
                  AND (@ProductId IS NULL OR e.ProductId_FK = @ProductId)
                UNION
                SELECT sm.ProductId_FK AS ProductId
                FROM StockMaster sm
                WHERE (sm.AvailableQuantity <> 0 OR sm.TotalQuantity <> 0)
                  AND (@ProductId IS NULL OR sm.ProductId_FK = @ProductId)
                UNION
                SELECT pr.ProductId_FK AS ProductId
                FROM ProductRange pr
                WHERE (@ProductId IS NULL OR pr.ProductId_FK = @ProductId)
                """;

            var countSql = $"""
                SELECT COUNT(*) FROM (
                    SELECT DISTINCT ProductId FROM (
                        {unionBody}
                    ) u
                ) t
                """;

            await using (var countCmd = new SqlCommand(countSql, connection))
            {
                countCmd.Parameters.AddWithValue("@FromDate", fromParam);
                countCmd.Parameters.AddWithValue("@ToDate", toParam);
                countCmd.Parameters.AddWithValue("@ProductId", productIdParam);
                var countObj = await countCmd.ExecuteScalarAsync();
                var total = countObj != null && countObj != DBNull.Value ? Convert.ToInt32(countObj) : 0;
                if (total == 0)
                    return (new List<long>(), 0);

                var offset = Math.Max(0, (pageNumber - 1) * pageSize);
                var pageSql = $"""
                    SELECT ProductId FROM (
                        SELECT DISTINCT ProductId FROM (
                            {unionBody}
                        ) u
                    ) t
                    ORDER BY ProductId
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                    """;

                var ids = new List<long>();
                await using (var pageCmd = new SqlCommand(pageSql, connection))
                {
                    pageCmd.Parameters.AddWithValue("@FromDate", fromParam);
                    pageCmd.Parameters.AddWithValue("@ToDate", toParam);
                    pageCmd.Parameters.AddWithValue("@ProductId", productIdParam);
                    pageCmd.Parameters.AddWithValue("@Offset", offset);
                    pageCmd.Parameters.AddWithValue("@PageSize", pageSize);
                    await using var reader = await pageCmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                        ids.Add(reader.GetInt64(0));
                }

                return (ids, total);
            }
        }

        private async Task<List<long>> GetAllProfitLossProductIdsAsync(SqlConnection connection, ProfitLossReportFilters? filters, DateTime? toDateEnd)
        {
            object fromParam = (object?)filters?.FromDate ?? DBNull.Value;
            object toParam = (object?)toDateEnd ?? DBNull.Value;
            object productIdParam = (object?)filters?.ProductId ?? DBNull.Value;

            const string unionBody = """
                SELECT sd.PrductId_FK AS ProductId
                FROM SaleDetails sd
                INNER JOIN Sales s ON sd.SaleId_FK = s.SaleId
                WHERE s.IsDeleted = 0
                  AND (@FromDate IS NULL OR s.SaleDate >= @FromDate)
                  AND (@ToDate IS NULL OR s.SaleDate <= @ToDate)
                  AND (@ProductId IS NULL OR sd.PrductId_FK = @ProductId)
                UNION
                SELECT poi.PrductId_FK AS ProductId
                FROM PurchaseOrderItems poi
                INNER JOIN PurchaseOrders po ON poi.PurchaseOrderId_FK = po.PurchaseOrderId
                WHERE ISNULL(po.IsDeleted, 0) = 0
                  AND (@FromDate IS NULL OR po.PurchaseOrderDate >= @FromDate)
                  AND (@ToDate IS NULL OR po.PurchaseOrderDate <= @ToDate)
                  AND (@ProductId IS NULL OR poi.PrductId_FK = @ProductId)
                UNION
                SELECT e.ProductId_FK AS ProductId
                FROM Expenses e
                WHERE e.ProductId_FK IS NOT NULL
                  AND (@FromDate IS NULL OR e.ExpenseDate >= @FromDate)
                  AND (@ToDate IS NULL OR e.ExpenseDate <= @ToDate)
                  AND (@ProductId IS NULL OR e.ProductId_FK = @ProductId)
                UNION
                SELECT sm.ProductId_FK AS ProductId
                FROM StockMaster sm
                WHERE (sm.AvailableQuantity <> 0 OR sm.TotalQuantity <> 0)
                  AND (@ProductId IS NULL OR sm.ProductId_FK = @ProductId)
                UNION
                SELECT pr.ProductId_FK AS ProductId
                FROM ProductRange pr
                WHERE (@ProductId IS NULL OR pr.ProductId_FK = @ProductId)
                """;

            var sql = $"""
                SELECT ProductId FROM (
                    SELECT DISTINCT ProductId FROM (
                        {unionBody}
                    ) u
                ) t
                ORDER BY ProductId
                """;

            var ids = new List<long>();
            await using (var cmd = new SqlCommand(sql, connection))
            {
                cmd.Parameters.AddWithValue("@FromDate", fromParam);
                cmd.Parameters.AddWithValue("@ToDate", toParam);
                cmd.Parameters.AddWithValue("@ProductId", productIdParam);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    ids.Add(reader.GetInt64(0));
            }

            return ids;
        }

        /// <summary>
        /// Opening weight = current available + sales in period − purchases in period (same base units as StockMaster).
        /// Profit = Sale Amount + Balance Amount − Total Stock Amount; Balance Amount = Balance Weight × Total Stock Rate.
        /// </summary>
        private async Task<List<ProductWiseProfitLossDetailSection>> BuildProductWiseProfitLossDetailSectionsAsync(
            SqlConnection connection,
            ProfitLossReportFilters? filters,
            List<long> productIds,
            DateTime? toDateEnd)
        {
            if (productIds == null || productIds.Count == 0)
                return new List<ProductWiseProfitLossDetailSection>();

            object fromParam = (object?)filters?.FromDate ?? DBNull.Value;
            object toParam = (object?)toDateEnd ?? DBNull.Value;
            var idList = string.Join(",", productIds);

            var salesW = new Dictionary<long, decimal>();
            var salesA = new Dictionary<long, decimal>();
            var purchaseW = new Dictionary<long, decimal>();
            var purchaseA = new Dictionary<long, decimal>();
            var expenseA = new Dictionary<long, decimal>();
            var stockQty = new Dictionary<long, decimal>();
            var productMeta = new Dictionary<long, (string Name, string Code, string? Urdu)>();

            async Task ReadAggAsync(string sql, Action<long, decimal, decimal> addPair)
            {
                await using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@FromDate", fromParam);
                cmd.Parameters.AddWithValue("@ToDate", toParam);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var pid = reader.GetInt64(0);
                    var w = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1);
                    var a = reader.IsDBNull(2) ? 0m : reader.GetDecimal(2);
                    addPair(pid, w, a);
                }
            }

            var salesSql = $"""
                SELECT sd.PrductId_FK,
                       SUM(CAST(sd.Quantity AS DECIMAL(18, 4))),
                       SUM(sd.PayableAmount)
                FROM SaleDetails sd
                INNER JOIN Sales s ON sd.SaleId_FK = s.SaleId
                WHERE s.IsDeleted = 0
                  AND sd.PrductId_FK IN ({idList})
                  AND (@FromDate IS NULL OR s.SaleDate >= @FromDate)
                  AND (@ToDate IS NULL OR s.SaleDate <= @ToDate)
                GROUP BY sd.PrductId_FK
                """;
            await ReadAggAsync(salesSql, (pid, w, a) => { salesW[pid] = w; salesA[pid] = a; });

            var purchaseSql = $"""
                SELECT poi.PrductId_FK,
                       SUM(CAST(poi.Quantity AS DECIMAL(18, 4))),
                       SUM(poi.PayableAmount)
                FROM PurchaseOrderItems poi
                INNER JOIN PurchaseOrders po ON poi.PurchaseOrderId_FK = po.PurchaseOrderId
                WHERE ISNULL(po.IsDeleted, 0) = 0
                  AND poi.PrductId_FK IN ({idList})
                  AND (@FromDate IS NULL OR po.PurchaseOrderDate >= @FromDate)
                  AND (@ToDate IS NULL OR po.PurchaseOrderDate <= @ToDate)
                GROUP BY poi.PrductId_FK
                """;
            await ReadAggAsync(purchaseSql, (pid, w, a) => { purchaseW[pid] = w; purchaseA[pid] = a; });

            var expSql = $"""
                SELECT e.ProductId_FK, SUM(e.Amount), 0
                FROM Expenses e
                WHERE e.ProductId_FK IS NOT NULL
                  AND e.ProductId_FK IN ({idList})
                  AND (@FromDate IS NULL OR e.ExpenseDate >= @FromDate)
                  AND (@ToDate IS NULL OR e.ExpenseDate <= @ToDate)
                GROUP BY e.ProductId_FK
                """;
            await using (var cmd = new SqlCommand(expSql, connection))
            {
                cmd.Parameters.AddWithValue("@FromDate", fromParam);
                cmd.Parameters.AddWithValue("@ToDate", toParam);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var pid = reader.GetInt64(0);
                    var a = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1);
                    expenseA[pid] = a;
                }
            }

            var stockSql = $"""
                SELECT sm.ProductId_FK, sm.AvailableQuantity
                FROM StockMaster sm
                WHERE sm.ProductId_FK IN ({idList})
                """;
            await using (var sc = new SqlCommand(stockSql, connection))
            {
                await using var reader = await sc.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var pid = reader.GetInt64(0);
                    var q = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1);
                    stockQty[pid] = q;
                }
            }

            var metaSql = $"""
                SELECT p.ProductId, p.ProductName, ISNULL(p.ProductCode, ''), NULLIF(LTRIM(RTRIM(p.UrduName)), ''), p.UnitPrice
                FROM Products p
                WHERE p.ProductId IN ({idList})
                """;
            var productTableUnitPrice = new Dictionary<long, decimal>();
            await using (var mc = new SqlCommand(metaSql, connection))
            {
                await using var reader = await mc.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var pid = reader.GetInt64(0);
                    var name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                    var code = reader.IsDBNull(2) ? "" : reader.GetString(2);
                    string? urdu = reader.IsDBNull(3) ? null : reader.GetString(3);
                    var pu = reader.IsDBNull(4) ? 0m : reader.GetDecimal(4);
                    productMeta[pid] = (name, code, urdu);
                    productTableUnitPrice[pid] = pu;
                }
            }

            // Catalog rate for smallest MU (e.g. Khal kg @ 110) — used when period purchase/sale averages are unavailable.
            var smallestRangeUnitPrice = new Dictionary<long, decimal>();
            var smallestSql = $"""
                SELECT pr.ProductId_FK, MIN(pr.UnitPrice)
                FROM ProductRange pr
                INNER JOIN Products p ON p.ProductId = pr.ProductId_FK
                INNER JOIN AdminMeasuringUnits mu ON mu.MeasuringUnitId = pr.MeasuringUnitId_FK
                    AND p.MeasuringUnitTypeId_FK IS NOT NULL
                    AND mu.MeasuringUnitTypeId_FK = p.MeasuringUnitTypeId_FK
                    AND mu.IsSmallestUnit = 1
                WHERE pr.ProductId_FK IN ({idList})
                GROUP BY pr.ProductId_FK
                """;
            try
            {
                await using (var su = new SqlCommand(smallestSql, connection))
                {
                    await using var reader = await su.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var pid = reader.GetInt64(0);
                        var up = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1);
                        if (up > 0)
                            smallestRangeUnitPrice[pid] = up;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Smallest ProductRange unit price query failed; falling back to Products.UnitPrice only.");
            }

            var sections = new List<ProductWiseProfitLossDetailSection>();
            foreach (var pid in productIds)
            {
                salesW.TryGetValue(pid, out var sw);
                salesA.TryGetValue(pid, out var sa);
                purchaseW.TryGetValue(pid, out var pw);
                purchaseA.TryGetValue(pid, out var pa);
                expenseA.TryGetValue(pid, out var exp);
                stockQty.TryGetValue(pid, out var endQty);

                productMeta.TryGetValue(pid, out var meta);

                var previousWeight = endQty + sw - pw;
                if (previousWeight < 0)
                    previousWeight = 0;

                // Opening value: never use period sale average — that would revalue opening stock when you add a sale (wrong).
                // With purchases in range, use purchase average; otherwise catalog smallest-unit rate, then Products.UnitPrice.
                decimal previousAmount = 0m;
                if (pw > 0)
                    previousAmount = previousWeight * (pa / pw);
                else if (previousWeight > 0)
                {
                    if (smallestRangeUnitPrice.TryGetValue(pid, out var catalogRate) && catalogRate > 0)
                        previousAmount = previousWeight * catalogRate;
                    else if (productTableUnitPrice.TryGetValue(pid, out var listPrice) && listPrice > 0)
                        previousAmount = previousWeight * listPrice;
                }

                var totalStockWeight = previousWeight + pw;
                var totalStockAmount = previousAmount + pa + exp;
                var totalStockRate = totalStockWeight > 0 ? totalStockAmount / totalStockWeight : 0m;

                var balanceWeight = totalStockWeight - sw;
                if (balanceWeight < 0)
                    balanceWeight = 0;
                var balanceAmount = balanceWeight * totalStockRate;

                var profit = sa + balanceAmount - totalStockAmount;

                sections.Add(new ProductWiseProfitLossDetailSection
                {
                    ProductId = pid,
                    ProductName = meta.Name,
                    ProductCode = meta.Code,
                    ProductUrduName = meta.Urdu,
                    PreviousWeight = previousWeight,
                    PreviousAmount = previousAmount,
                    PurchaseWeight = pw,
                    PurchaseAmount = pa,
                    PurchaseExpenseAmount = exp,
                    TotalStockWeight = totalStockWeight,
                    TotalStockAmount = totalStockAmount,
                    SaleWeight = sw,
                    SaleAmount = sa,
                    BalanceStockWeight = balanceWeight,
                    BalanceStockAmount = balanceAmount,
                    BagsWeight = 0,
                    BagsRate = 0,
                    Profit = profit
                });
            }

            return sections;
        }

        public async Task<BankBalancesReportViewModel> GetBankBalancesReport(BankBalancesReportFilters? filters)
        {
            var rows = new List<BankBalancesReportRow>();
            decimal totalBalance = 0;

            try
            {
                var reportDate = filters?.ReportDate ?? DateTimeHelper.Now;
                var asOfEnd = reportDate.Date.AddDays(1).AddSeconds(-1);

                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    const string sql = @"
                        SELECT 
                            pp.PersonalPaymentId,
                            pp.BankName,
                            ISNULL(pp.AccountNumber, '') AS AccountNumber,
                            ISNULL(pp.AccountHolderName, '') AS AccountHolderName,
                            ISNULL(pp.BankBranch, '') AS BankBranch,
                            ISNULL(SUM(
                                CASE 
                                    WHEN ppsd.TransactionType = N'Credit' THEN ppsd.Amount
                                    WHEN ppsd.TransactionType = N'Debit' THEN -ppsd.Amount
                                    ELSE 0
                                END
                            ), 0) AS BalanceAsOf
                        FROM PersonalPayments pp
                        LEFT JOIN PersonalPaymentSaleDetail ppsd ON pp.PersonalPaymentId = ppsd.PersonalPaymentId
                            AND ppsd.IsActive = 1
                            AND ppsd.TransactionDate <= @AsOfEnd
                        WHERE pp.IsActive = 1
                            AND (@PersonalPaymentId IS NULL OR pp.PersonalPaymentId = @PersonalPaymentId)
                        GROUP BY pp.PersonalPaymentId, pp.BankName, pp.AccountNumber, pp.AccountHolderName, pp.BankBranch
                        ORDER BY pp.BankName, pp.AccountNumber";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@AsOfEnd", asOfEnd);
                        command.Parameters.AddWithValue("@PersonalPaymentId", (object?)filters?.PersonalPaymentId ?? DBNull.Value);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var bal = reader.IsDBNull(reader.GetOrdinal("BalanceAsOf"))
                                    ? 0m
                                    : reader.GetDecimal(reader.GetOrdinal("BalanceAsOf"));
                                rows.Add(new BankBalancesReportRow
                                {
                                    PersonalPaymentId = reader.GetInt64(reader.GetOrdinal("PersonalPaymentId")),
                                    BankName = reader.IsDBNull(reader.GetOrdinal("BankName")) ? string.Empty : reader.GetString(reader.GetOrdinal("BankName")),
                                    AccountNumber = reader.IsDBNull(reader.GetOrdinal("AccountNumber")) ? string.Empty : reader.GetString(reader.GetOrdinal("AccountNumber")),
                                    AccountHolderName = reader.IsDBNull(reader.GetOrdinal("AccountHolderName")) ? string.Empty : reader.GetString(reader.GetOrdinal("AccountHolderName")),
                                    BankBranch = reader.IsDBNull(reader.GetOrdinal("BankBranch")) ? null : reader.GetString(reader.GetOrdinal("BankBranch")),
                                    BalanceAsOf = bal
                                });
                                totalBalance += bal;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetBankBalancesReport failed.");
            }

            return new BankBalancesReportViewModel
            {
                Filters = filters ?? new BankBalancesReportFilters(),
                Rows = rows,
                TotalBalance = totalBalance
            };
        }

        public async Task<ProfitLossReportViewModel> GetProductWiseProfitLossReport(int pageNumber, int? pageSize, ProfitLossReportFilters? filters)
        {
            var profitLossList = new List<ProfitLossReportItem>();
            var productDetailSections = new List<ProductWiseProfitLossDetailSection>();
            decimal totalSalesAmount = 0;
            decimal totalPurchaseCost = 0;
            decimal totalProfitLoss = 0;

            try
            {
                await using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    var toDateEnd = filters?.ToDate != null
                        ? filters.ToDate.Value.Date.AddDays(1).AddSeconds(-1)
                        : (DateTime?)null;

                    var allIds = await GetAllProfitLossProductIdsAsync(connection, filters, toDateEnd);
                    productDetailSections = await BuildProductWiseProfitLossDetailSectionsAsync(connection, filters, allIds, toDateEnd);

                    foreach (var s in productDetailSections)
                    {
                        profitLossList.Add(new ProfitLossReportItem
                        {
                            ProductId = s.ProductId,
                            ProductName = s.ProductName,
                            ProductUrduName = s.ProductUrduName,
                            ProductCode = s.ProductCode,
                            TotalQuantitySold = (long)Math.Round(s.SaleWeight, MidpointRounding.AwayFromZero),
                            TotalSalesAmount = s.SaleAmount,
                            TotalPurchaseCost = s.TotalStockAmount,
                            ProfitLoss = s.Profit,
                            ProfitLossPercentage = s.TotalStockAmount > 0 ? (s.Profit / s.TotalStockAmount) * 100m : 0m
                        });
                        totalSalesAmount += s.SaleAmount;
                        totalPurchaseCost += s.TotalStockAmount;
                        totalProfitLoss += s.Profit;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetProductWiseProfitLossReport failed.");
                throw;
            }

            var n = profitLossList.Count;
            return new ProfitLossReportViewModel
            {
                ProfitLossList = profitLossList,
                ProductDetailSections = productDetailSections,
                Filters = filters ?? new ProfitLossReportFilters(),
                CurrentPage = 1,
                TotalPages = 1,
                PageSize = n,
                TotalCount = n,
                TotalSalesAmount = totalSalesAmount,
                TotalPurchaseCost = totalPurchaseCost,
                TotalProfitLoss = totalProfitLoss
            };
        }

        public async Task<DailyStockReportViewModel> GetDailyStockReport(int pageNumber, int? pageSize, DailyStockReportFilters? filters)
        {
            if (filters != null
                && filters.DisplayMeasuringUnitId.HasValue
                && filters.DisplayMeasuringUnitId.Value > 0)
            {
                var full = await GetDailyStockReportForExport(1, null, filters);
                var all = full.StockList ?? new List<DailyStockReportItem>();
                var displayUnitTotalRows = all.Count;
                var psz = pageSize ?? 10;
                var paged = psz > 0
                    ? all.Skip((pageNumber - 1) * psz).Take(psz).ToList()
                    : new List<DailyStockReportItem>();
                full.StockList = paged;
                full.Filters = filters;
                full.CurrentPage = pageNumber;
                full.TotalPages = psz > 0 && displayUnitTotalRows > 0
                    ? Math.Max(1, (int)Math.Ceiling(displayUnitTotalRows / (double)psz))
                    : 1;
                full.PageSize = psz;
                full.TotalCount = displayUnitTotalRows;
                return full;
            }

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
                            p.UrduName AS ProductUrduName,
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
                                MIN(pr.UnitPrice) AS UnitPrice
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
                                MIN(pr.UnitPrice) AS UnitPrice
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
                                    ProductUrduName = reader.IsDBNull(reader.GetOrdinal("ProductUrduName")) ? null : reader.GetString(reader.GetOrdinal("ProductUrduName")),
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

            var vm = new DailyStockReportViewModel
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
            await ApplyMeasuringUnitDisplayToDailyStockAsync(vm, filters);
            return vm;
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
                            p.UrduName AS ProductUrduName,
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
                                MIN(pr.UnitPrice) AS UnitPrice
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
                                MIN(pr.UnitPrice) AS UnitPrice
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
                                    ProductUrduName = reader.IsDBNull(reader.GetOrdinal("ProductUrduName")) ? null : reader.GetString(reader.GetOrdinal("ProductUrduName")),
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

            var exportVm = new DailyStockReportViewModel
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
            await ApplyMeasuringUnitDisplayToDailyStockAsync(exportVm, filters);
            return exportVm;
        }

        private async Task ApplyMeasuringUnitDisplayToDailyStockAsync(DailyStockReportViewModel vm, DailyStockReportFilters? filters)
        {
            if (filters?.DisplayMeasuringUnitId == null || filters.DisplayMeasuringUnitId.Value <= 0)
                return;

            var displayUnitId = filters.DisplayMeasuringUnitId.Value;
            try
            {
                var mu = await _measuringUnitService.GetAdminMeasuringUnitByIdAsync(displayUnitId);
                if (mu != null)
                {
                    vm.DisplayMeasuringUnitName = mu.MeasuringUnitName?.Trim();
                    var abbr = mu.MeasuringUnitAbbreviation?.Trim();
                    vm.DisplayMeasuringUnitAbbreviation = !string.IsNullOrEmpty(abbr)
                        ? abbr
                        : vm.DisplayMeasuringUnitName;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load measuring unit {UnitId} for daily stock display", displayUnitId);
            }

            if (vm.StockList == null || vm.StockList.Count == 0)
                return;

            foreach (var item in vm.StockList)
            {
                var baseUnitId = await GetBaseUnitIdForProductAsync(item.ProductId);
                if (!baseUnitId.HasValue)
                    continue;

                // UnitPrice from query is MIN(ProductRange.UnitPrice), treated as per base (smallest) unit — same basis as StockMaster quantities.
                var unitPricePerBase = item.UnitPrice;

                item.TotalQuantity = await ConvertStockDisplayQuantityAsync(baseUnitId.Value, displayUnitId, item.TotalQuantity);
                item.UsedQuantity = await ConvertStockDisplayQuantityAsync(baseUnitId.Value, displayUnitId, item.UsedQuantity);
                item.AvailableQuantity = await ConvertStockDisplayQuantityAsync(baseUnitId.Value, displayUnitId, item.AvailableQuantity);

                item.UnitPrice = await ConvertUnitPriceFromBaseToDisplayAsync(baseUnitId.Value, displayUnitId, unitPricePerBase);
                item.StockValue = item.AvailableQuantity * item.UnitPrice;
            }

            vm.TotalQuantity = vm.StockList.Sum(i => i.TotalQuantity);
            vm.TotalUsedQuantity = vm.StockList.Sum(i => i.UsedQuantity);
            vm.TotalAvailableQuantity = vm.StockList.Sum(i => i.AvailableQuantity);
            vm.TotalStockValue = vm.StockList.Sum(i => i.StockValue);
        }

        private async Task<long?> GetBaseUnitIdForProductAsync(long productId)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(productId);
                if (product?.ProductList == null || !product.ProductList.MeasuringUnitTypeIdFk.HasValue)
                    return null;

                var measuringUnits = await _measuringUnitService.GetAllEnabledMeasuringUnitsByMUTIdAsync(product.ProductList.MeasuringUnitTypeIdFk);
                var smallestUnit = measuringUnits.FirstOrDefault(mu => mu.IsSmallestUnit);
                return smallestUnit?.MeasuringUnitId;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetBaseUnitIdForProductAsync failed for product {ProductId}", productId);
                return null;
            }
        }

        /// <summary>Matches StockController.ConvertStockForDisplay: quantities in base (smallest) unit → selected display unit.</summary>
        private async Task<decimal> ConvertStockDisplayQuantityAsync(long baseUnitId, long displayUnitId, decimal stockInBaseUnit)
        {
            if (baseUnitId == displayUnitId)
                return stockInBaseUnit;

            var conversionsFromSelectedUnit = await _unitConversionService.GetConversionsByFromUnitAsync(displayUnitId);
            var conversion = conversionsFromSelectedUnit.FirstOrDefault(c => c.ToUnitId == baseUnitId && c.IsEnabled);
            if (conversion != null)
                return stockInBaseUnit / conversion.ConversionFactor;

            var conversionsFromBaseUnit = await _unitConversionService.GetConversionsByFromUnitAsync(baseUnitId);
            var reverseConversion = conversionsFromBaseUnit.FirstOrDefault(c => c.ToUnitId == displayUnitId && c.IsEnabled);
            if (reverseConversion != null)
                return stockInBaseUnit * reverseConversion.ConversionFactor;

            return stockInBaseUnit;
        }

        /// <summary>
        /// Converts unit price from per base (smallest) unit to per selected display unit.
        /// Keeps monetary value consistent: available (base) × price (base) = available (display) × price (display).
        /// Uses the same conversion paths as <see cref="ConvertStockDisplayQuantityAsync"/>.
        /// </summary>
        private async Task<decimal> ConvertUnitPriceFromBaseToDisplayAsync(long baseUnitId, long displayUnitId, decimal unitPricePerBase)
        {
            if (baseUnitId == displayUnitId)
                return unitPricePerBase;

            var conversionsFromSelectedUnit = await _unitConversionService.GetConversionsByFromUnitAsync(displayUnitId);
            var conversion = conversionsFromSelectedUnit.FirstOrDefault(c => c.ToUnitId == baseUnitId && c.IsEnabled);
            if (conversion != null)
                return unitPricePerBase * conversion.ConversionFactor;

            var conversionsFromBaseUnit = await _unitConversionService.GetConversionsByFromUnitAsync(baseUnitId);
            var reverseConversion = conversionsFromBaseUnit.FirstOrDefault(c => c.ToUnitId == displayUnitId && c.IsEnabled);
            if (reverseConversion != null)
                return unitPricePerBase / reverseConversion.ConversionFactor;

            return unitPricePerBase;
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
                            s.UrduName AS VendorUrduName,
                            ISNULL(c.CustomerName, '') AS CustomerName,
                            c.UrduName AS CustomerUrduName,

                            po.BillNumber,
                            po.SupplierId_FK AS VendorIdFk,
                            po.PurchaseOrderDate AS PurchaseDate,
                            po.TotalAmount,
                            po.DiscountAmount,
                            po.TotalReceivedAmount AS PaidAmount,
                            po.TotalDueAmount AS DueAmount,
                            ISNULL(po.PurchaseOrderDescription, '') AS PurchaseDescription
                        FROM PurchaseOrders po
                        LEFT JOIN AdminSuppliers s ON po.SupplierId_FK = s.SupplierId
                        left join Customers c ON c.CustomerId = po.CustomerId_FK
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
                                    VendorUrduName = reader.IsDBNull(reader.GetOrdinal("VendorUrduName")) ? null : reader.GetString(reader.GetOrdinal("VendorUrduName")),
                                    CustomerName = reader.IsDBNull(reader.GetOrdinal("CustomerName")) ? string.Empty : reader.GetString(reader.GetOrdinal("CustomerName")),
                                    CustomerUrduName = reader.IsDBNull(reader.GetOrdinal("CustomerUrduName")) ? null : reader.GetString(reader.GetOrdinal("CustomerUrduName")),
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
                            s.UrduName AS VendorUrduName,
                            ISNULL(c.CustomerName, '') AS CustomerName,
                            c.UrduName AS CustomerUrduName,
                            po.BillNumber,
                            po.SupplierId_FK AS VendorIdFk,
                            po.PurchaseOrderDate AS PurchaseDate,
                            po.TotalAmount,
                            po.DiscountAmount,
                            po.TotalReceivedAmount AS PaidAmount,
                            po.TotalDueAmount AS DueAmount,
                            ISNULL(po.PurchaseOrderDescription, '') AS PurchaseDescription
                        FROM PurchaseOrders po
                        LEFT JOIN AdminSuppliers s ON po.SupplierId_FK = s.SupplierId
                        LEFT JOIN Customers c ON c.CustomerId = po.CustomerId_FK
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
                                    VendorUrduName = reader.IsDBNull(reader.GetOrdinal("VendorUrduName")) ? null : reader.GetString(reader.GetOrdinal("VendorUrduName")),
                                    CustomerName = reader.IsDBNull(reader.GetOrdinal("CustomerName")) ? string.Empty : reader.GetString(reader.GetOrdinal("CustomerName")),
                                    CustomerUrduName = reader.IsDBNull(reader.GetOrdinal("CustomerUrduName")) ? null : reader.GetString(reader.GetOrdinal("CustomerUrduName")),
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
                                MAX(ISNULL(p.UrduName, '')) AS ProductUrduName,
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
                            NULLIF(ds.ProductUrduName, '') AS ProductUrduName,
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
                                    ProductUrduName = reader.IsDBNull(reader.GetOrdinal("ProductUrduName"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("ProductUrduName")),
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
                            ProductUrduName = productSales.First().ProductUrduName,
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

        public async Task<ProductWisePurchaseReportViewModel> GetProductWisePurchaseReport(int pageNumber, int? pageSize, ProductWisePurchaseReportFilters? filters)
        {
            var purchaseList = new List<ProductWisePurchaseReportItem>();
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
                        WITH DailyPurchases AS (
                            SELECT 
                                CAST(po.PurchaseOrderDate AS DATE) AS PurchaseDate,
                                poi.PrductId_FK AS ProductId,
                                p.ProductName,
                                MAX(ISNULL(p.UrduName, '')) AS ProductUrduName,
                                ISNULL(p.ProductCode, '') AS ProductCode,
                                SUM(CAST(poi.Quantity AS DECIMAL(18, 3))) AS Weight,
                                SUM(poi.Quantity) AS Qty,
                                SUM(poi.PayableAmount) AS Amount
                            FROM PurchaseOrderItems poi
                            INNER JOIN PurchaseOrders po ON poi.PurchaseOrderId_FK = po.PurchaseOrderId
                            INNER JOIN Products p ON poi.PrductId_FK = p.ProductId
                            WHERE po.IsDeleted = 0
                                AND (@FromDate IS NULL OR CAST(po.PurchaseOrderDate AS DATE) >= @FromDate)
                                AND (@ToDate IS NULL OR CAST(po.PurchaseOrderDate AS DATE) <= @ToDate)
                                AND (@ProductId IS NULL OR poi.PrductId_FK = @ProductId)
                            GROUP BY CAST(po.PurchaseOrderDate AS DATE), poi.PrductId_FK, p.ProductName, p.ProductCode
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
                            FROM DailyPurchases
                            GROUP BY ProductId, ProductName, ProductCode
                        )
                        SELECT 
                            dp.PurchaseDate,
                            dp.ProductId,
                            dp.ProductName,
                            NULLIF(dp.ProductUrduName, '') AS ProductUrduName,
                            dp.ProductCode,
                            dp.Weight,
                            dp.Qty,
                            CASE 
                                WHEN dp.Qty > 0 THEN dp.Amount / dp.Qty
                                ELSE 0
                            END AS Rate,
                            dp.Amount,
                            0 AS IsTotalRow
                        FROM DailyPurchases dp
                        ORDER BY dp.ProductName, dp.PurchaseDate
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

                        SELECT COUNT(*) AS TotalRecords
                        FROM (
                            SELECT 
                                CAST(po.PurchaseOrderDate AS DATE) AS PurchaseDate,
                                poi.PrductId_FK AS ProductId
                            FROM PurchaseOrderItems poi
                            INNER JOIN PurchaseOrders po ON poi.PurchaseOrderId_FK = po.PurchaseOrderId
                            INNER JOIN Products p ON poi.PrductId_FK = p.ProductId
                            WHERE po.IsDeleted = 0
                                AND (@FromDate IS NULL OR CAST(po.PurchaseOrderDate AS DATE) >= @FromDate)
                                AND (@ToDate IS NULL OR CAST(po.PurchaseOrderDate AS DATE) <= @ToDate)
                                AND (@ProductId IS NULL OR poi.PrductId_FK = @ProductId)
                            GROUP BY CAST(po.PurchaseOrderDate AS DATE), poi.PrductId_FK, p.ProductName, p.ProductCode
                        ) AS DailyPurchases;

                        SELECT 
                            SUM(CAST(poi.Quantity AS DECIMAL(18, 3))) AS TotalWeight,
                            SUM(poi.Quantity) AS TotalQty,
                            SUM(poi.PayableAmount) AS TotalAmount
                        FROM PurchaseOrderItems poi
                        INNER JOIN PurchaseOrders po ON poi.PurchaseOrderId_FK = po.PurchaseOrderId
                        WHERE po.IsDeleted = 0
                            AND (@FromDate IS NULL OR CAST(po.PurchaseOrderDate AS DATE) >= @FromDate)
                            AND (@ToDate IS NULL OR CAST(po.PurchaseOrderDate AS DATE) <= @ToDate)
                            AND (@ProductId IS NULL OR poi.PrductId_FK = @ProductId);
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
                            // Read daily purchase data
                            while (await reader.ReadAsync())
                            {
                                purchaseList.Add(new ProductWisePurchaseReportItem
                                {
                                    PurchaseDate = reader.IsDBNull(reader.GetOrdinal("PurchaseDate"))
                                        ? DateTime.MinValue
                                        : reader.GetDateTime(reader.GetOrdinal("PurchaseDate")),
                                    ProductId = reader.IsDBNull(reader.GetOrdinal("ProductId"))
                                        ? 0
                                        : reader.GetInt64(reader.GetOrdinal("ProductId")),
                                    ProductName = reader.IsDBNull(reader.GetOrdinal("ProductName"))
                                        ? string.Empty
                                        : reader.GetString(reader.GetOrdinal("ProductName")),
                                    ProductUrduName = reader.IsDBNull(reader.GetOrdinal("ProductUrduName"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("ProductUrduName")),
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
                    // Group purchases by product and add total rows
                    var productGroups = purchaseList.GroupBy(p => p.ProductId).ToList();
                    var finalList = new List<ProductWisePurchaseReportItem>();
                    
                    foreach (var group in productGroups)
                    {
                        var productPurchases = group.OrderBy(p => p.PurchaseDate).ToList();
                        finalList.AddRange(productPurchases);
                        
                        // Add total row for this product
                        var productTotal = new ProductWisePurchaseReportItem
                        {
                            PurchaseDate = DateTime.MinValue,
                            ProductId = group.Key,
                            ProductName = productPurchases.First().ProductName,
                            ProductUrduName = productPurchases.First().ProductUrduName,
                            ProductCode = productPurchases.First().ProductCode,
                            Weight = productPurchases.Sum(p => p.Weight),
                            Qty = productPurchases.Sum(p => p.Qty),
                            Amount = productPurchases.Sum(p => p.Amount),
                            Rate = productPurchases.Sum(p => p.Qty) > 0 
                                ? productPurchases.Sum(p => p.Amount) / productPurchases.Sum(p => p.Qty) 
                                : 0m,
                            IsTotalRow = true
                        };
                        finalList.Add(productTotal);
                    }
                    
                    purchaseList = finalList;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return new ProductWisePurchaseReportViewModel
            {
                PurchaseList = purchaseList,
                Filters = filters ?? new ProductWisePurchaseReportFilters(),
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

        public async Task<DailyStockPositionReportViewModel> GetDailyStockPositionReport(DailyStockPositionReportFilters? filters)
        {
            var stockPositionList = new List<DailyStockPositionReportItem>();
            decimal totalPurchase = 0;
            decimal totalSales = 0;
            decimal totalClosing = 0;
            decimal totalBags = decimal.Zero;

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    var reportDate = filters?.ReportDate ?? DateTimeHelper.Now;
                    var reportDateOnly = reportDate.Date;

                    var sql = @"
                        WITH ProductPurchase AS (
                            SELECT 
                                poi.PrductId_FK AS ProductId,
                                SUM(poi.Quantity) AS PurchaseQuantity
                            FROM PurchaseOrderItems poi
                            INNER JOIN PurchaseOrders po ON poi.PurchaseOrderId_FK = po.PurchaseOrderId
                            WHERE po.IsDeleted = 0
                                AND CAST(po.PurchaseOrderDate AS DATE) = @ReportDate
                            GROUP BY poi.PrductId_FK
                        ),
                        ProductSales AS (
                            SELECT 
                                sd.PrductId_FK AS ProductId,
                                SUM(sd.Quantity) AS SalesQuantity
                            FROM SaleDetails sd
                            INNER JOIN Sales s ON sd.SaleId_FK = s.SaleId
                            WHERE s.IsDeleted = 0
                                AND CAST(s.SaleDate AS DATE) = @ReportDate
                            GROUP BY sd.PrductId_FK
                        ),
                        ProductStock AS (
                            SELECT 
                                sm.ProductId_FK AS ProductId,
                                sm.AvailableQuantity AS ClosingStock
                            FROM StockMaster sm
                            WHERE sm.AvailableQuantity >= 0
                        )
                        SELECT 
                            p.ProductId,
                            p.ProductName,
                            p.UrduName AS ProductUrduName,
                            ISNULL(p.ProductCode, '') AS ProductCode,
                            ISNULL(pp.PurchaseQuantity, 0) AS PurchaseQuantity,
                            ISNULL(ps.SalesQuantity, 0) AS SalesQuantity,
                            ISNULL(pst.ClosingStock, 0) AS ClosingStock,
                            CASE 
                                WHEN ISNULL(pst.ClosingStock, 0) > 0 THEN CAST(ISNULL(pst.ClosingStock, 0) / 34.0 AS BIGINT)
                                ELSE 0
                            END AS Bags
                        FROM Products p
                        LEFT JOIN ProductPurchase pp ON p.ProductId = pp.ProductId
                        LEFT JOIN ProductSales ps ON p.ProductId = ps.ProductId
                        LEFT JOIN ProductStock pst ON p.ProductId = pst.ProductId
                        WHERE p.IsEnabled = 1
                        ORDER BY p.ProductName;
                    ";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@ReportDate", reportDateOnly);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var purchaseQty = reader.IsDBNull(reader.GetOrdinal("PurchaseQuantity"))
                                    ? 0m
                                    : reader.GetInt64(reader.GetOrdinal("PurchaseQuantity"));
                                var salesQty = reader.IsDBNull(reader.GetOrdinal("SalesQuantity"))
                                    ? 0m
                                    : reader.GetInt64(reader.GetOrdinal("SalesQuantity"));
                                var closingStock = reader.IsDBNull(reader.GetOrdinal("ClosingStock"))
                                    ? 0
                                    : reader.GetDecimal(reader.GetOrdinal("ClosingStock"));
                                var bags = reader.IsDBNull(reader.GetOrdinal("Bags"))
                                    ? 0
                                    : reader.GetInt64(reader.GetOrdinal("Bags"));

                                stockPositionList.Add(new DailyStockPositionReportItem
                                {
                                    ProductId = reader.GetInt64(reader.GetOrdinal("ProductId")),
                                    ProductName = reader.IsDBNull(reader.GetOrdinal("ProductName"))
                                        ? string.Empty
                                        : reader.GetString(reader.GetOrdinal("ProductName")),
                                    ProductUrduName = reader.IsDBNull(reader.GetOrdinal("ProductUrduName"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("ProductUrduName")),
                                    ProductCode = reader.IsDBNull(reader.GetOrdinal("ProductCode"))
                                        ? string.Empty
                                        : reader.GetString(reader.GetOrdinal("ProductCode")),
                                    PurchaseQuantity = purchaseQty,
                                    SalesQuantity = salesQty,
                                    ClosingStock = closingStock,
                                    Bags = bags
                                });

                                totalPurchase += purchaseQty;
                                totalSales += salesQty;
                                totalClosing += closingStock;
                                totalBags += bags;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return new DailyStockPositionReportViewModel
            {
                StockPositionList = stockPositionList,
                Filters = filters ?? new DailyStockPositionReportFilters { ReportDate = DateTimeHelper.Now },
                TotalPurchase = totalPurchase,
                TotalSales = totalSales,
                TotalClosing = totalClosing,
                TotalBags = totalBags
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
                                AND ISNULL(e.ProductId_FK,0) = 0
                               
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
                                AND (@ProductId IS NULL OR e.ProductId_FK = @ProductId)
                               
                            GROUP BY CAST(e.ExpenseDate AS DATE), e.ExpenseTypeId_FK, et.ExpenseTypeName
                        ) AS DailyExpenses;

                        SELECT 
                            SUM(e.Amount) AS TotalAmount
                        FROM Expenses e
                        WHERE (@FromDate IS NULL OR CAST(e.ExpenseDate AS DATE) >= @FromDate)
                            AND (@ToDate IS NULL OR CAST(e.ExpenseDate AS DATE) <= @ToDate)
                            AND (@ExpenseTypeId IS NULL OR e.ExpenseTypeId_FK = @ExpenseTypeId)
                            AND ISNULL(e.ProductId_FK,0) = 0;
                    ";

                    var offset = (pageNumber - 1) * (pageSize ?? 10);
                    var pageSizeValue = pageSize ?? 10;

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@FromDate", (object)filters?.FromDate ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ToDate", (object)filters?.ToDate ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ExpenseTypeId", (object)filters?.ExpenseTypeId ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ProductId", (object)filters?.ProductId ?? DBNull.Value);
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
        public async Task<GeneralExpensesReportViewModel> GetPOExpensesReport(int pageNumber, int? pageSize, GeneralExpensesReportFilters? filters)
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
                                ISNULL(p.ProductName, '') AS ProductName,
                                MAX(ISNULL(p.UrduName, '')) AS ProductUrduName,
                                MAX(e.ExpenseDetail) AS ExpenseDetail,
                                SUM(e.Amount) AS Amount
                            FROM Expenses e
                            LEFT JOIN AdminExpenseTypes et ON e.ExpenseTypeId_FK = et.ExpenseTypeId
                            LEFT JOIN Products p ON e.ProductId_FK = p.ProductId
                            WHERE (@FromDate IS NULL OR CAST(e.ExpenseDate AS DATE) >= @FromDate)
                                AND (@ToDate IS NULL OR CAST(e.ExpenseDate AS DATE) <= @ToDate)
                                AND (e.ProductId_FK IS NOT NULL AND (@ProductId IS NULL OR e.ProductId_FK = @ProductId))
                               
                            GROUP BY CAST(e.ExpenseDate AS DATE), e.ExpenseTypeId_FK, et.ExpenseTypeName, p.ProductName
                        )
                        SELECT 
                            de.ExpenseDate,
                            de.ExpenseTypeId,
                            de.ExpenseTypeName,
                            de.ProductName,
                            NULLIF(de.ProductUrduName, '') AS ProductUrduName,
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
                                AND (e.ProductId_FK IS NOT NULL AND (@ProductId IS NULL OR e.ProductId_FK = @ProductId))
                            GROUP BY CAST(e.ExpenseDate AS DATE), e.ExpenseTypeId_FK, et.ExpenseTypeName
                        ) AS DailyExpenses;

                        SELECT 
                            SUM(e.Amount) AS TotalAmount
                        FROM Expenses e
                        WHERE (@FromDate IS NULL OR CAST(e.ExpenseDate AS DATE) >= @FromDate)
                            AND (@ToDate IS NULL OR CAST(e.ExpenseDate AS DATE) <= @ToDate)
                            AND (e.ProductId_FK IS NOT NULL AND (@ProductId IS NULL OR e.ProductId_FK = @ProductId));
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
                                    ProductName = reader.IsDBNull(reader.GetOrdinal("ProductName"))
                                        ? string.Empty
                                        : reader.GetString(reader.GetOrdinal("ProductName")),
                                    ProductUrduName = reader.IsDBNull(reader.GetOrdinal("ProductUrduName"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("ProductUrduName")),
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

        public async Task<CustomerLedgerReportViewModel> GetCustomerLedgerReport(CustomerLedgerReportFilters? filters)
        {
            var ledgerRows = new List<(DateTime Date, string? CustomerName, string? CustomerUrduName, string? GLAccount, decimal Debit, decimal Credit)>();
            string? customerName = null;
            string? customerHeaderUrdu = null;
            decimal totalDebit = 0;
            decimal totalCredit = 0;
            if (filters == null)
                filters = new CustomerLedgerReportFilters();
            var hasCustomerFilter = filters.CustomerId.HasValue && filters.CustomerId.Value > 0;

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    var fromDate = filters.FromDate ?? (DateTime?)null;
                    var toDate = filters.ToDate.HasValue ? filters.ToDate.Value.AddDays(1).AddSeconds(-1) : (DateTime?)null;

                    // Sales: Debit = TotalAmount (stored net = sum of line payables; DiscountAmount is line-discount total for display only)
                    var salesSql = @"
                        SELECT s.SaleDate AS [Date], ISNULL(c.CustomerName, '') AS CustomerName,
                               c.UrduName AS CustomerUrduName,
                               s.TotalAmount AS DebitAmount,
                               ISNULL(s.SaleDescription, '') AS GLAccount, s.BillNumber
                        FROM Sales s
                        LEFT JOIN Customers c ON s.CustomerId_FK = c.CustomerId
                        WHERE (s.IsDeleted = 0 OR s.IsDeleted IS NULL)
                          AND (@CustomerId IS NULL OR s.CustomerId_FK = @CustomerId)
                          AND (@FromDate IS NULL OR s.SaleDate >= @FromDate)
                          AND (@ToDate IS NULL OR s.SaleDate <= @ToDate)
                        ORDER BY s.SaleDate, s.SaleId";
                    using (var cmd = new SqlCommand(salesSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@CustomerId", hasCustomerFilter ? (object)filters.CustomerId!.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var date = reader.GetDateTime(reader.GetOrdinal("Date"));
                                var cName = reader.IsDBNull(reader.GetOrdinal("CustomerName")) ? null : reader.GetString(reader.GetOrdinal("CustomerName"));
                                if (string.IsNullOrWhiteSpace(cName)) cName = null;
                                string? cUrdu = null;
                                try
                                {
                                    var uOrd = reader.GetOrdinal("CustomerUrduName");
                                    if (!reader.IsDBNull(uOrd))
                                        cUrdu = reader.GetString(uOrd);
                                }
                                catch { /* column optional */ }
                                if (string.IsNullOrWhiteSpace(cUrdu)) cUrdu = null;
                                var debit = reader.GetDecimal(reader.GetOrdinal("DebitAmount"));
                                var gl = reader.IsDBNull(reader.GetOrdinal("GLAccount")) ? "" : reader.GetString(reader.GetOrdinal("GLAccount"));
                                if (string.IsNullOrWhiteSpace(gl) && !reader.IsDBNull(reader.GetOrdinal("BillNumber")))
                                    gl = "Bill #" + reader.GetInt64(reader.GetOrdinal("BillNumber"));
                                ledgerRows.Add((date, cName, cUrdu, string.IsNullOrWhiteSpace(gl) ? null : gl, debit, 0));
                                totalDebit += debit;
                            }
                        }
                    }

                    // Payments: Credit = PaymentAmount, include CustomerName
                    var paymentsSql = @"
                        SELECT p.PaymentDate AS [Date], ISNULL(c.CustomerName, '') AS CustomerName,
                               c.UrduName AS CustomerUrduName,
                               p.PaymentAmount AS CreditAmount, ISNULL(p.Description, '') AS GLAccount
                        FROM Payments p
                        LEFT JOIN Customers c ON p.CustomerId = c.CustomerId
                        WHERE (@CustomerId IS NULL OR p.CustomerId = @CustomerId)
                          AND (@FromDate IS NULL OR p.PaymentDate >= @FromDate)
                          AND (@ToDate IS NULL OR p.PaymentDate <= @ToDate)
                        ORDER BY p.PaymentDate, p.PaymentId";
                    using (var cmd = new SqlCommand(paymentsSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@CustomerId", hasCustomerFilter ? (object)filters.CustomerId!.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var date = reader.GetDateTime(reader.GetOrdinal("Date"));
                                var cName = reader.IsDBNull(reader.GetOrdinal("CustomerName")) ? null : reader.GetString(reader.GetOrdinal("CustomerName"));
                                if (string.IsNullOrWhiteSpace(cName)) cName = null;
                                string? cUrduP = null;
                                try
                                {
                                    var uOrd = reader.GetOrdinal("CustomerUrduName");
                                    if (!reader.IsDBNull(uOrd))
                                        cUrduP = reader.GetString(uOrd);
                                }
                                catch { }
                                if (string.IsNullOrWhiteSpace(cUrduP)) cUrduP = null;
                                var credit = reader.GetDecimal(reader.GetOrdinal("CreditAmount"));
                                var gl = reader.IsDBNull(reader.GetOrdinal("GLAccount")) ? null : reader.GetString(reader.GetOrdinal("GLAccount"));
                                if (string.IsNullOrWhiteSpace(gl)) gl = null;
                                ledgerRows.Add((date, cName, cUrduP, gl, 0, credit));
                                totalCredit += credit;
                            }
                        }
                    }

                    if (hasCustomerFilter)
                    {
                        using (var cmd = new SqlCommand("SELECT CustomerName, UrduName FROM Customers WHERE CustomerId = @CustomerId", connection))
                        {
                            cmd.Parameters.AddWithValue("@CustomerId", filters.CustomerId!.Value);
                            using (var r = await cmd.ExecuteReaderAsync())
                            {
                                if (await r.ReadAsync())
                                {
                                    customerName = r.IsDBNull(0) ? null : r.GetString(0);
                                    var u = r.FieldCount > 1 && !r.IsDBNull(1) ? r.GetString(1) : null;
                                    customerHeaderUrdu = string.IsNullOrWhiteSpace(u) ? null : u;
                                }
                            }
                        }
                    }
                    else
                        customerName = "All Customers";
                }

                // Sort by Customer first so each customer's entries appear together, then by Date; compute running balance per customer
                var sorted = ledgerRows
                    .OrderBy(x => x.CustomerName ?? "")
                    .ThenBy(x => x.Date)
                    .ThenBy(x => x.Debit > 0 ? 0 : 1)
                    .ToList();
                decimal runningBalance = 0;
                string? previousCustomerKey = null;
                var ledgerList = new List<CustomerLedgerReportItem>();
                foreach (var row in sorted)
                {
                    var customerKey = row.CustomerName ?? "";
                    if (customerKey != previousCustomerKey)
                    {
                        runningBalance = 0;
                        previousCustomerKey = customerKey;
                    }
                    runningBalance += row.Debit - row.Credit;
                    ledgerList.Add(new CustomerLedgerReportItem
                    {
                        Date = row.Date,
                        CustomerName = row.CustomerName,
                        CustomerUrduName = row.CustomerUrduName,
                        GLAccount = row.GLAccount,
                        Debit = row.Debit,
                        Credit = row.Credit,
                        Balance = runningBalance
                    });
                }

                return new CustomerLedgerReportViewModel
                {
                    LedgerList = ledgerList,
                    Filters = filters,
                    CustomerName = customerName,
                    CustomerUrduName = customerHeaderUrdu,
                    TotalDebit = totalDebit,
                    TotalCredit = totalCredit,
                    ClosingBalance = totalDebit - totalCredit
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCustomerLedgerReport error");
                return new CustomerLedgerReportViewModel
                {
                    LedgerList = new List<CustomerLedgerReportItem>(),
                    Filters = filters,
                    CustomerName = null,
                    CustomerUrduName = null,
                    TotalDebit = 0,
                    TotalCredit = 0,
                    ClosingBalance = 0
                };
            }
        }

        public async Task<VendorLedgerReportViewModel> GetVendorLedgerReport(VendorLedgerReportFilters? filters)
        {
            var ledgerRows = new List<(DateTime Date, string? VendorName, string? VendorUrduName, string? GLAccount, decimal Debit, decimal Credit)>();
            string? vendorName = null;
            string? vendorHeaderUrdu = null;
            decimal totalDebit = 0;
            decimal totalCredit = 0;
            if (filters == null)
                filters = new VendorLedgerReportFilters();
            var hasVendorFilter = filters.VendorId.HasValue && filters.VendorId.Value > 0;

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    var fromDate = filters.FromDate ?? (DateTime?)null;
                    var toDate = filters.ToDate.HasValue ? filters.ToDate.Value.AddDays(1).AddSeconds(-1) : (DateTime?)null;

                    // PurchaseOrders: Debit = TotalAmount (stored net; same pattern as sales)
                    var purchasesSql = @"
                        SELECT po.PurchaseOrderDate AS [Date], ISNULL(s.SupplierName, '') AS VendorName,
                               s.UrduName AS VendorUrduName,
                               po.TotalAmount AS DebitAmount,
                               ISNULL(po.PurchaseOrderDescription, '') AS GLAccount, po.BillNumber
                        FROM PurchaseOrders po
                        LEFT JOIN AdminSuppliers s ON po.SupplierId_FK = s.SupplierId
                        WHERE (po.IsDeleted = 0 OR po.IsDeleted IS NULL)
                          AND (@VendorId IS NULL OR po.SupplierId_FK = @VendorId)
                          AND (@FromDate IS NULL OR po.PurchaseOrderDate >= @FromDate)
                          AND (@ToDate IS NULL OR po.PurchaseOrderDate <= @ToDate)
                        ORDER BY po.PurchaseOrderDate, po.PurchaseOrderId";
                    using (var cmd = new SqlCommand(purchasesSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@VendorId", hasVendorFilter ? (object)filters.VendorId!.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var date = reader.GetDateTime(reader.GetOrdinal("Date"));
                                var vName = reader.IsDBNull(reader.GetOrdinal("VendorName")) ? null : reader.GetString(reader.GetOrdinal("VendorName"));
                                if (string.IsNullOrWhiteSpace(vName)) vName = null;
                                string? vUrdu = null;
                                try
                                {
                                    var uOrd = reader.GetOrdinal("VendorUrduName");
                                    if (!reader.IsDBNull(uOrd))
                                        vUrdu = reader.GetString(uOrd);
                                }
                                catch { }
                                if (string.IsNullOrWhiteSpace(vUrdu)) vUrdu = null;
                                var debit = reader.GetDecimal(reader.GetOrdinal("DebitAmount"));
                                var gl = reader.IsDBNull(reader.GetOrdinal("GLAccount")) ? "" : reader.GetString(reader.GetOrdinal("GLAccount"));
                                if (string.IsNullOrWhiteSpace(gl))
                                {
                                    if (!reader.IsDBNull(reader.GetOrdinal("BillNumber")))
                                        gl = "Bill #" + reader.GetInt64(reader.GetOrdinal("BillNumber"));
                                }
                                ledgerRows.Add((date, vName, vUrdu, string.IsNullOrWhiteSpace(gl) ? null : gl, debit, 0));
                                totalDebit += debit;
                            }
                        }
                    }

                    // BillPayments: Credit = PaymentAmount, include VendorName
                    var paymentsSql = @"
                        SELECT p.PaymentDate AS [Date], ISNULL(s.SupplierName, '') AS VendorName,
                               s.UrduName AS VendorUrduName,
                               p.PaymentAmount AS CreditAmount, ISNULL(p.Description, '') AS GLAccount
                        FROM BillPayments p
                        LEFT JOIN AdminSuppliers s ON p.SupplierId_FK = s.SupplierId
                        WHERE (@VendorId IS NULL OR p.SupplierId_FK = @VendorId)
                          AND (@FromDate IS NULL OR p.PaymentDate >= @FromDate)
                          AND (@ToDate IS NULL OR CAST(p.PaymentDate AS DATE) <= @ToDate)
                        ORDER BY p.PaymentDate, p.PaymentId";
                    using (var cmd = new SqlCommand(paymentsSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@VendorId", hasVendorFilter ? (object)filters.VendorId!.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ToDate", (object?)toDate ?? DBNull.Value);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var date = reader.GetDateTime(reader.GetOrdinal("Date"));
                                var vName = reader.IsDBNull(reader.GetOrdinal("VendorName")) ? null : reader.GetString(reader.GetOrdinal("VendorName"));
                                if (string.IsNullOrWhiteSpace(vName)) vName = null;
                                string? vUrduP = null;
                                try
                                {
                                    var uOrd = reader.GetOrdinal("VendorUrduName");
                                    if (!reader.IsDBNull(uOrd))
                                        vUrduP = reader.GetString(uOrd);
                                }
                                catch { }
                                if (string.IsNullOrWhiteSpace(vUrduP)) vUrduP = null;
                                var credit = reader.GetDecimal(reader.GetOrdinal("CreditAmount"));
                                var gl = reader.IsDBNull(reader.GetOrdinal("GLAccount")) ? null : reader.GetString(reader.GetOrdinal("GLAccount"));
                                if (string.IsNullOrWhiteSpace(gl)) gl = null;
                                ledgerRows.Add((date, vName, vUrduP, gl, 0, credit));
                                totalCredit += credit;
                            }
                        }
                    }

                    if (hasVendorFilter)
                    {
                        using (var cmd = new SqlCommand("SELECT SupplierName, UrduName FROM AdminSuppliers WHERE SupplierId = @VendorId", connection))
                        {
                            cmd.Parameters.AddWithValue("@VendorId", filters.VendorId!.Value);
                            using (var r = await cmd.ExecuteReaderAsync())
                            {
                                if (await r.ReadAsync())
                                {
                                    vendorName = r.IsDBNull(0) ? null : r.GetString(0);
                                    var u = r.FieldCount > 1 && !r.IsDBNull(1) ? r.GetString(1) : null;
                                    vendorHeaderUrdu = string.IsNullOrWhiteSpace(u) ? null : u;
                                }
                            }
                        }
                    }
                    else
                        vendorName = "All Vendors";
                }

                // Sort by Vendor first so each vendor's entries appear together, then by Date; compute running balance per vendor
                var sorted = ledgerRows
                    .OrderBy(x => x.VendorName ?? "")
                    .ThenBy(x => x.Date)
                    .ThenBy(x => x.Debit > 0 ? 0 : 1)
                    .ToList();
                decimal runningBalance = 0;
                string? previousVendorKey = null;
                var ledgerList = new List<VendorLedgerReportItem>();
                foreach (var row in sorted)
                {
                    var vendorKey = row.VendorName ?? "";
                    if (vendorKey != previousVendorKey)
                    {
                        runningBalance = 0;
                        previousVendorKey = vendorKey;
                    }
                    runningBalance += row.Debit - row.Credit;
                    ledgerList.Add(new VendorLedgerReportItem
                    {
                        Date = row.Date,
                        VendorName = row.VendorName,
                        VendorUrduName = row.VendorUrduName,
                        GLAccount = row.GLAccount,
                        Debit = row.Debit,
                        Credit = row.Credit,
                        Balance = runningBalance
                    });
                }

                return new VendorLedgerReportViewModel
                {
                    LedgerList = ledgerList,
                    Filters = filters,
                    VendorName = vendorName,
                    VendorUrduName = vendorHeaderUrdu,
                    TotalDebit = totalDebit,
                    TotalCredit = totalCredit,
                    ClosingBalance = totalDebit - totalCredit
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetVendorLedgerReport error");
                return new VendorLedgerReportViewModel
                {
                    LedgerList = new List<VendorLedgerReportItem>(),
                    Filters = filters,
                    VendorName = null,
                    VendorUrduName = null,
                    TotalDebit = 0,
                    TotalCredit = 0,
                    ClosingBalance = 0
                };
            }
        }

        public async Task<PayableReceivableReportViewModel> GetPayableReceivableReport(PayableReceivableReportFilters? filters)
        {
            filters ??= new PayableReceivableReportFilters();
            var asOf = filters.AsOfDate?.Date ?? DateTimeHelper.Now.Date;
            var asOfEnd = asOf.AddDays(1).AddSeconds(-1);
            var hasCustomerFilter = filters.CustomerId.HasValue && filters.CustomerId.Value > 0;
            var hasVendorFilter = filters.VendorId.HasValue && filters.VendorId.Value > 0;

            var rows = new List<PayableReceivableReportItem>();
            decimal totalRecv = 0;
            decimal totalPay = 0;

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    var customerSql = @"
                        SELECT c.CustomerName,
                               ISNULL(d1.DebitTotal, 0) - ISNULL(p1.CreditTotal, 0) AS AsOfBalance
                        FROM Customers c
                        LEFT JOIN (
                            SELECT s.CustomerId_FK AS CustomerId,
                                   SUM(s.TotalAmount) AS DebitTotal
                            FROM Sales s
                            WHERE (s.IsDeleted = 0 OR s.IsDeleted IS NULL)
                              AND s.SaleDate <= @AsOfEnd
                            GROUP BY s.CustomerId_FK
                        ) d1 ON c.CustomerId = d1.CustomerId
                        LEFT JOIN (
                            SELECT p.CustomerId, SUM(p.PaymentAmount) AS CreditTotal
                            FROM Payments p
                            WHERE p.PaymentDate <= @AsOfEnd
                            GROUP BY p.CustomerId
                        ) p1 ON c.CustomerId = p1.CustomerId
                        WHERE (
                            (@HasCustomer = 1 AND c.CustomerId = @CustomerId)
                            OR (@HasCustomer = 0 AND c.IsEnabled = 1)
                          )
                        ORDER BY c.CustomerName";

                    using (var cmd = new SqlCommand(customerSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@AsOfEnd", asOfEnd);
                        cmd.Parameters.AddWithValue("@HasCustomer", hasCustomerFilter ? 1 : 0);
                        cmd.Parameters.AddWithValue("@CustomerId", hasCustomerFilter ? (object)filters.CustomerId!.Value : DBNull.Value);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var closing = reader.GetDecimal(reader.GetOrdinal("AsOfBalance"));
                                // Positive = they owe us → receivable column; negative = we owe them (credit) → payable column as positive
                                decimal pay = 0, recv = 0;
                                if (closing >= 0)
                                {
                                    recv = closing;
                                    totalRecv += closing;
                                }
                                else
                                {
                                    pay = -closing;
                                    totalPay += -closing;
                                }
                                rows.Add(new PayableReceivableReportItem
                                {
                                    AsOfDate = asOf,
                                    CustomerName = reader.IsDBNull(reader.GetOrdinal("CustomerName")) ? null : reader.GetString(reader.GetOrdinal("CustomerName")),
                                    VendorName = null,
                                    Payable = pay,
                                    Receivable = recv
                                });
                            }
                        }
                    }

                    var vendorSql = @"
                        SELECT v.SupplierName AS VendorName,
                               ISNULL(po1.DebitTotal, 0) - ISNULL(bp1.CreditTotal, 0) AS AsOfBalance
                        FROM AdminSuppliers v
                        LEFT JOIN (
                            SELECT po.SupplierId_FK AS SupplierId,
                                   SUM(po.TotalAmount) AS DebitTotal
                            FROM PurchaseOrders po
                            WHERE (po.IsDeleted = 0 OR po.IsDeleted IS NULL)
                              AND po.PurchaseOrderDate <= @AsOfEnd
                            GROUP BY po.SupplierId_FK
                        ) po1 ON v.SupplierId = po1.SupplierId
                        LEFT JOIN (
                            SELECT p.SupplierId_FK AS SupplierId,
                                   SUM(p.PaymentAmount) AS CreditTotal
                            FROM BillPayments p
                            WHERE p.PaymentDate <= @AsOfEnd
                            GROUP BY p.SupplierId_FK
                        ) bp1 ON v.SupplierId = bp1.SupplierId
                        WHERE (
                            (@HasVendor = 1 AND v.SupplierId = @VendorId)
                            OR (@HasVendor = 0 AND v.IsDeleted = 0)
                          )
                        ORDER BY v.SupplierName";

                    using (var cmd = new SqlCommand(vendorSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@AsOfEnd", asOfEnd);
                        cmd.Parameters.AddWithValue("@HasVendor", hasVendorFilter ? 1 : 0);
                        cmd.Parameters.AddWithValue("@VendorId", hasVendorFilter ? (object)filters.VendorId!.Value : DBNull.Value);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var closing = reader.GetDecimal(reader.GetOrdinal("AsOfBalance"));
                                // Positive = we owe vendor → payable column; negative = vendor owes us / advance → receivable column as positive
                                decimal pay = 0, recv = 0;
                                if (closing >= 0)
                                {
                                    pay = closing;
                                    totalPay += closing;
                                }
                                else
                                {
                                    recv = -closing;
                                    totalRecv += -closing;
                                }
                                rows.Add(new PayableReceivableReportItem
                                {
                                    AsOfDate = asOf,
                                    CustomerName = null,
                                    VendorName = reader.IsDBNull(reader.GetOrdinal("VendorName")) ? null : reader.GetString(reader.GetOrdinal("VendorName")),
                                    Payable = pay,
                                    Receivable = recv
                                });
                            }
                        }
                    }
                }

                return new PayableReceivableReportViewModel
                {
                    Rows = rows,
                    Filters = filters,
                    TotalReceivable = totalRecv,
                    TotalPayable = totalPay
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPayableReceivableReport error");
                return new PayableReceivableReportViewModel
                {
                    Rows = new List<PayableReceivableReportItem>(),
                    Filters = filters,
                    TotalReceivable = 0,
                    TotalPayable = 0
                };
            }
        }

        public async Task<CustomerBalanceReportViewModel> GetCustomerBalanceReport(CustomerBalanceReportFilters? filters)
        {
            if (filters == null)
                filters = new CustomerBalanceReportFilters();
            var asOf = filters.AsOfDate?.Date ?? DateTimeHelper.Now.Date;
            var asOfEnd = asOf.AddDays(1).AddSeconds(-1);
            var hasCustomerFilter = filters.CustomerId.HasValue && filters.CustomerId.Value > 0;
            string? scopeLabel = hasCustomerFilter ? null : "All Customers";

            var list = new List<CustomerBalanceReportItem>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT c.CustomerId, c.CustomerName,
                               c.UrduName AS CustomerUrduName,
                               ISNULL(d.DebitTotal, 0) - ISNULL(pay.CreditTotal, 0) AS Balance
                        FROM Customers c
                        LEFT JOIN (
                            SELECT s.CustomerId_FK AS CustomerId,
                                   SUM(s.TotalAmount) AS DebitTotal
                            FROM Sales s
                            WHERE (s.IsDeleted = 0 OR s.IsDeleted IS NULL)
                              AND s.SaleDate <= @AsOfEnd
                            GROUP BY s.CustomerId_FK
                        ) d ON c.CustomerId = d.CustomerId
                        LEFT JOIN (
                            SELECT p.CustomerId,
                                   SUM(p.PaymentAmount) AS CreditTotal
                            FROM Payments p
                            WHERE p.PaymentDate <= @AsOfEnd
                            GROUP BY p.CustomerId
                        ) pay ON c.CustomerId = pay.CustomerId
                        WHERE (
                            (@HasCustomer = 1 AND c.CustomerId = @CustomerId)
                            OR (@HasCustomer = 0 AND c.IsEnabled = 1)
                          )
                        ORDER BY c.CustomerName";
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@AsOfEnd", asOfEnd);
                        cmd.Parameters.AddWithValue("@HasCustomer", hasCustomerFilter ? 1 : 0);
                        cmd.Parameters.AddWithValue("@CustomerId", hasCustomerFilter ? (object)filters.CustomerId!.Value : DBNull.Value);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                list.Add(new CustomerBalanceReportItem
                                {
                                    CustomerId = reader.GetInt64(reader.GetOrdinal("CustomerId")),
                                    CustomerName = reader.IsDBNull(reader.GetOrdinal("CustomerName")) ? null : reader.GetString(reader.GetOrdinal("CustomerName")),
                                    CustomerUrduName = reader.IsDBNull(reader.GetOrdinal("CustomerUrduName")) ? null : reader.GetString(reader.GetOrdinal("CustomerUrduName")),
                                    AsOfDate = asOf,
                                    Balance = reader.GetDecimal(reader.GetOrdinal("Balance"))
                                });
                            }
                        }
                    }

                    if (hasCustomerFilter && list.Count == 0)
                    {
                        using (var cmd = new SqlCommand("SELECT CustomerName FROM Customers WHERE CustomerId = @CustomerId", connection))
                        {
                            cmd.Parameters.AddWithValue("@CustomerId", filters.CustomerId!.Value);
                            var nameObj = await cmd.ExecuteScalarAsync();
                            if (nameObj != null && nameObj != DBNull.Value)
                                scopeLabel = nameObj.ToString();
                        }
                    }
                    else if (hasCustomerFilter && list.Count > 0)
                        scopeLabel = list[0].CustomerName;
                    else if (!hasCustomerFilter)
                        scopeLabel = "All Customers";
                }

                return new CustomerBalanceReportViewModel
                {
                    BalanceList = list,
                    Filters = filters,
                    ScopeLabel = scopeLabel,
                    TotalBalance = list.Sum(x => x.Balance)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCustomerBalanceReport error");
                return new CustomerBalanceReportViewModel
                {
                    BalanceList = new List<CustomerBalanceReportItem>(),
                    Filters = filters,
                    ScopeLabel = scopeLabel,
                    TotalBalance = 0
                };
            }
        }

        public async Task<VendorBalanceReportViewModel> GetVendorBalanceReport(VendorBalanceReportFilters? filters)
        {
            if (filters == null)
                filters = new VendorBalanceReportFilters();
            var asOf = filters.AsOfDate?.Date ?? DateTimeHelper.Now.Date;
            var asOfEnd = asOf.AddDays(1).AddSeconds(-1);
            var hasVendorFilter = filters.VendorId.HasValue && filters.VendorId.Value > 0;
            string? scopeLabel = null;

            var list = new List<VendorBalanceReportItem>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT v.SupplierId AS VendorId, v.SupplierName AS VendorName,
                               v.UrduName AS VendorUrduName,
                               ISNULL(po.DebitTotal, 0) - ISNULL(bp.CreditTotal, 0) AS Balance
                        FROM AdminSuppliers v
                        LEFT JOIN (
                            SELECT po.SupplierId_FK AS SupplierId,
                                   SUM(po.TotalAmount) AS DebitTotal
                            FROM PurchaseOrders po
                            WHERE (po.IsDeleted = 0 OR po.IsDeleted IS NULL)
                              AND po.PurchaseOrderDate <= @AsOfEnd
                            GROUP BY po.SupplierId_FK
                        ) po ON v.SupplierId = po.SupplierId
                        LEFT JOIN (
                            SELECT p.SupplierId_FK AS SupplierId,
                                   SUM(p.PaymentAmount) AS CreditTotal
                            FROM BillPayments p
                            WHERE p.PaymentDate <= @AsOfEnd
                            GROUP BY p.SupplierId_FK
                        ) bp ON v.SupplierId = bp.SupplierId
                        WHERE (
                            (@HasVendor = 1 AND v.SupplierId = @VendorId)
                            OR (@HasVendor = 0 AND v.IsDeleted = 0)
                          )
                        ORDER BY v.SupplierName";
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@AsOfEnd", asOfEnd);
                        cmd.Parameters.AddWithValue("@HasVendor", hasVendorFilter ? 1 : 0);
                        cmd.Parameters.AddWithValue("@VendorId", hasVendorFilter ? (object)filters.VendorId!.Value : DBNull.Value);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                list.Add(new VendorBalanceReportItem
                                {
                                    VendorId = reader.GetInt64(reader.GetOrdinal("VendorId")),
                                    VendorName = reader.IsDBNull(reader.GetOrdinal("VendorName")) ? null : reader.GetString(reader.GetOrdinal("VendorName")),
                                    VendorUrduName = reader.IsDBNull(reader.GetOrdinal("VendorUrduName")) ? null : reader.GetString(reader.GetOrdinal("VendorUrduName")),
                                    AsOfDate = asOf,
                                    Balance = reader.GetDecimal(reader.GetOrdinal("Balance"))
                                });
                            }
                        }
                    }

                    if (hasVendorFilter && list.Count == 0)
                    {
                        using (var cmd = new SqlCommand("SELECT SupplierName FROM AdminSuppliers WHERE SupplierId = @VendorId", connection))
                        {
                            cmd.Parameters.AddWithValue("@VendorId", filters.VendorId!.Value);
                            var nameObj = await cmd.ExecuteScalarAsync();
                            if (nameObj != null && nameObj != DBNull.Value)
                                scopeLabel = nameObj.ToString();
                        }
                    }
                    else if (hasVendorFilter && list.Count > 0)
                        scopeLabel = list[0].VendorName;
                    else if (!hasVendorFilter)
                        scopeLabel = "All Vendors";
                }

                return new VendorBalanceReportViewModel
                {
                    BalanceList = list,
                    Filters = filters,
                    ScopeLabel = scopeLabel,
                    TotalBalance = list.Sum(x => x.Balance)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetVendorBalanceReport error");
                return new VendorBalanceReportViewModel
                {
                    BalanceList = new List<VendorBalanceReportItem>(),
                    Filters = filters,
                    ScopeLabel = scopeLabel,
                    TotalBalance = 0
                };
            }
        }

        public async Task<CashInHandReportViewModel> GetCashInHandReport(int pageNumber, int? pageSize, CashInHandReportFilters? filters)
        {
            var list = new List<CashInHandReportItem>();
            int totalRecords = 0;
            decimal totalCashIn = 0;
            var f = NormalizeCashInHandFilters(filters);

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    var sql = @"
;WITH Combined AS (
    SELECT 
        s.SaleDate AS TransactionDate,
        s.BillNumber,
        ISNULL(c.CustomerName, N'') AS PartyName,
        N'Cash sale' AS SourceKind,
        s.TotalReceivedAmount AS CashAmount,
        s.SaleId AS SaleId,
        CAST(NULL AS BIGINT) AS PaymentId
    FROM Sales s
    LEFT JOIN Customers c ON s.CustomerId_FK = c.CustomerId
    WHERE (s.IsDeleted = 0 OR s.IsDeleted IS NULL)
      AND UPPER(LTRIM(RTRIM(ISNULL(s.PaymentMethod, N'')))) = N'CASH'
      AND s.SaleDate >= @FromDate AND s.SaleDate < @ToDateExclusive
      AND (@CustomerId IS NULL OR s.CustomerId_FK = @CustomerId)

    UNION ALL

    SELECT 
        p.PaymentDate AS TransactionDate,
        s.BillNumber,
        ISNULL(c.CustomerName, N'') AS PartyName,
        N'Cash payment' AS SourceKind,
        p.PaymentAmount AS CashAmount,
        CAST(NULL AS BIGINT) AS SaleId,
        p.PaymentId AS PaymentId
    FROM Payments p
    INNER JOIN Sales s ON p.SaleId = s.SaleId
    LEFT JOIN Customers c ON p.CustomerId = c.CustomerId
    WHERE (s.IsDeleted = 0 OR s.IsDeleted IS NULL)
      AND UPPER(LTRIM(RTRIM(ISNULL(p.paymentMethod, N'')))) = N'CASH'
      AND UPPER(LTRIM(RTRIM(ISNULL(s.PaymentMethod, N'')))) <> N'CASH'
      AND p.PaymentDate >= @FromDate AND p.PaymentDate < @ToDateExclusive
      AND (@CustomerId IS NULL OR p.CustomerId = @CustomerId OR s.CustomerId_FK = @CustomerId)
)
SELECT TransactionDate, BillNumber, PartyName, SourceKind, CashAmount, SaleId, PaymentId
FROM Combined
ORDER BY TransactionDate DESC, BillNumber DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

SELECT COUNT(1) AS TotalRecords FROM (
    SELECT 1 AS N FROM Sales s
    WHERE (s.IsDeleted = 0 OR s.IsDeleted IS NULL)
      AND UPPER(LTRIM(RTRIM(ISNULL(s.PaymentMethod, N'')))) = N'CASH'
      AND s.SaleDate >= @FromDate AND s.SaleDate < @ToDateExclusive
      AND (@CustomerId IS NULL OR s.CustomerId_FK = @CustomerId)
    UNION ALL
    SELECT 1 AS N FROM Payments p
    INNER JOIN Sales s ON p.SaleId = s.SaleId
    WHERE (s.IsDeleted = 0 OR s.IsDeleted IS NULL)
      AND UPPER(LTRIM(RTRIM(ISNULL(p.paymentMethod, N'')))) = N'CASH'
      AND UPPER(LTRIM(RTRIM(ISNULL(s.PaymentMethod, N'')))) <> N'CASH'
      AND p.PaymentDate >= @FromDate AND p.PaymentDate < @ToDateExclusive
      AND (@CustomerId IS NULL OR p.CustomerId = @CustomerId OR s.CustomerId_FK = @CustomerId)
) AS CashInHandRowCount;

SELECT ISNULL(SUM(agg.CashAmount), 0) AS TotalCashIn FROM (
    SELECT s.TotalReceivedAmount AS CashAmount
    FROM Sales s
    WHERE (s.IsDeleted = 0 OR s.IsDeleted IS NULL)
      AND UPPER(LTRIM(RTRIM(ISNULL(s.PaymentMethod, N'')))) = N'CASH'
      AND s.SaleDate >= @FromDate AND s.SaleDate < @ToDateExclusive
      AND (@CustomerId IS NULL OR s.CustomerId_FK = @CustomerId)
    UNION ALL
    SELECT p.PaymentAmount AS CashAmount
    FROM Payments p
    INNER JOIN Sales s ON p.SaleId = s.SaleId
    WHERE (s.IsDeleted = 0 OR s.IsDeleted IS NULL)
      AND UPPER(LTRIM(RTRIM(ISNULL(p.paymentMethod, N'')))) = N'CASH'
      AND UPPER(LTRIM(RTRIM(ISNULL(s.PaymentMethod, N'')))) <> N'CASH'
      AND p.PaymentDate >= @FromDate AND p.PaymentDate < @ToDateExclusive
      AND (@CustomerId IS NULL OR p.CustomerId = @CustomerId OR s.CustomerId_FK = @CustomerId)
) AS agg;
";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@FromDate", f.FromDate);
                        command.Parameters.AddWithValue("@ToDateExclusive", f.ToDateExclusive);
                        command.Parameters.AddWithValue("@CustomerId", (object)f.CustomerId ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Offset", (pageNumber - 1) * (pageSize ?? 10));
                        command.Parameters.AddWithValue("@PageSize", pageSize ?? 10);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                list.Add(new CashInHandReportItem
                                {
                                    TransactionDate = reader.GetDateTime(0),
                                    BillNumber = reader.IsDBNull(1) ? 0 : Convert.ToInt64(reader.GetValue(1)),
                                    PartyName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                    SourceKind = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                    CashAmount = reader.IsDBNull(4) ? 0m : reader.GetDecimal(4),
                                    SaleId = reader.IsDBNull(5) ? null : Convert.ToInt64(reader.GetValue(5)),
                                    PaymentId = reader.IsDBNull(6) ? null : Convert.ToInt64(reader.GetValue(6))
                                });
                            }

                            await reader.NextResultAsync();
                            if (await reader.ReadAsync())
                                totalRecords = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0));

                            await reader.NextResultAsync();
                            if (await reader.ReadAsync())
                                totalCashIn = reader.IsDBNull(0) ? 0m : Convert.ToDecimal(reader.GetValue(0));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCashInHandReport error");
            }

            return new CashInHandReportViewModel
            {
                Items = list,
                Filters = f.Filters,
                CurrentPage = pageNumber,
                TotalPages = pageSize.HasValue && pageSize.Value > 0
                    ? Math.Max(1, (int)Math.Ceiling(totalRecords / (double)pageSize.Value))
                    : 1,
                PageSize = pageSize,
                TotalCount = totalRecords,
                TotalCashIn = totalCashIn
            };
        }

        public async Task<CashInHandReportViewModel> GetCashInHandReportForExport(CashInHandReportFilters? filters)
        {
            var list = new List<CashInHandReportItem>();
            decimal totalCashIn = 0;
            var f = NormalizeCashInHandFilters(filters);

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    var sql = @"
;WITH Combined AS (
    SELECT 
        s.SaleDate AS TransactionDate,
        s.BillNumber,
        ISNULL(c.CustomerName, N'') AS PartyName,
        N'Cash sale' AS SourceKind,
        s.TotalReceivedAmount AS CashAmount,
        s.SaleId AS SaleId,
        CAST(NULL AS BIGINT) AS PaymentId
    FROM Sales s
    LEFT JOIN Customers c ON s.CustomerId_FK = c.CustomerId
    WHERE (s.IsDeleted = 0 OR s.IsDeleted IS NULL)
      AND UPPER(LTRIM(RTRIM(ISNULL(s.PaymentMethod, N'')))) = N'CASH'
      AND s.SaleDate >= @FromDate AND s.SaleDate < @ToDateExclusive
      AND (@CustomerId IS NULL OR s.CustomerId_FK = @CustomerId)

    UNION ALL

    SELECT 
        p.PaymentDate AS TransactionDate,
        s.BillNumber,
        ISNULL(c.CustomerName, N'') AS PartyName,
        N'Cash payment' AS SourceKind,
        p.PaymentAmount AS CashAmount,
        CAST(NULL AS BIGINT) AS SaleId,
        p.PaymentId AS PaymentId
    FROM Payments p
    INNER JOIN Sales s ON p.SaleId = s.SaleId
    LEFT JOIN Customers c ON p.CustomerId = c.CustomerId
    WHERE (s.IsDeleted = 0 OR s.IsDeleted IS NULL)
      AND UPPER(LTRIM(RTRIM(ISNULL(p.paymentMethod, N'')))) = N'CASH'
      AND UPPER(LTRIM(RTRIM(ISNULL(s.PaymentMethod, N'')))) <> N'CASH'
      AND p.PaymentDate >= @FromDate AND p.PaymentDate < @ToDateExclusive
      AND (@CustomerId IS NULL OR p.CustomerId = @CustomerId OR s.CustomerId_FK = @CustomerId)
)
SELECT TransactionDate, BillNumber, PartyName, SourceKind, CashAmount, SaleId, PaymentId
FROM Combined
ORDER BY TransactionDate DESC, BillNumber DESC;

SELECT ISNULL(SUM(agg.CashAmount), 0) AS TotalCashIn FROM (
    SELECT s.TotalReceivedAmount AS CashAmount
    FROM Sales s
    WHERE (s.IsDeleted = 0 OR s.IsDeleted IS NULL)
      AND UPPER(LTRIM(RTRIM(ISNULL(s.PaymentMethod, N'')))) = N'CASH'
      AND s.SaleDate >= @FromDate AND s.SaleDate < @ToDateExclusive
      AND (@CustomerId IS NULL OR s.CustomerId_FK = @CustomerId)
    UNION ALL
    SELECT p.PaymentAmount AS CashAmount
    FROM Payments p
    INNER JOIN Sales s ON p.SaleId = s.SaleId
    WHERE (s.IsDeleted = 0 OR s.IsDeleted IS NULL)
      AND UPPER(LTRIM(RTRIM(ISNULL(p.paymentMethod, N'')))) = N'CASH'
      AND UPPER(LTRIM(RTRIM(ISNULL(s.PaymentMethod, N'')))) <> N'CASH'
      AND p.PaymentDate >= @FromDate AND p.PaymentDate < @ToDateExclusive
      AND (@CustomerId IS NULL OR p.CustomerId = @CustomerId OR s.CustomerId_FK = @CustomerId)
) AS agg;
";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@FromDate", f.FromDate);
                        command.Parameters.AddWithValue("@ToDateExclusive", f.ToDateExclusive);
                        command.Parameters.AddWithValue("@CustomerId", (object)f.CustomerId ?? DBNull.Value);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                list.Add(new CashInHandReportItem
                                {
                                    TransactionDate = reader.GetDateTime(0),
                                    BillNumber = reader.IsDBNull(1) ? 0 : Convert.ToInt64(reader.GetValue(1)),
                                    PartyName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                    SourceKind = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                    CashAmount = reader.IsDBNull(4) ? 0m : reader.GetDecimal(4),
                                    SaleId = reader.IsDBNull(5) ? null : Convert.ToInt64(reader.GetValue(5)),
                                    PaymentId = reader.IsDBNull(6) ? null : Convert.ToInt64(reader.GetValue(6))
                                });
                            }

                            await reader.NextResultAsync();
                            if (await reader.ReadAsync())
                                totalCashIn = reader.IsDBNull(0) ? 0m : Convert.ToDecimal(reader.GetValue(0));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCashInHandReportForExport error");
            }

            return new CashInHandReportViewModel
            {
                Items = list,
                Filters = f.Filters,
                CurrentPage = 1,
                TotalPages = 1,
                PageSize = list.Count,
                TotalCount = list.Count,
                TotalCashIn = totalCashIn
            };
        }

        private sealed class CashInHandNormalizedFilters
        {
            public DateTime FromDate { get; set; }
            public DateTime ToDateExclusive { get; set; }
            public long? CustomerId { get; set; }
            public CashInHandReportFilters Filters { get; set; } = new CashInHandReportFilters();
        }

        private static CashInHandNormalizedFilters NormalizeCashInHandFilters(CashInHandReportFilters? filters)
        {
            var now = DateTimeHelper.Now.Date;
            var from = filters?.FromDate?.Date ?? new DateTime(now.Year, now.Month, 1);
            var to = filters?.ToDate?.Date ?? now;
            if (to < from)
                to = from;

            return new CashInHandNormalizedFilters
            {
                FromDate = from,
                ToDateExclusive = to.AddDays(1),
                CustomerId = filters?.CustomerId is > 0 ? filters.CustomerId : null,
                Filters = new CashInHandReportFilters
                {
                    FromDate = from,
                    ToDate = to,
                    CustomerId = filters?.CustomerId is > 0 ? filters.CustomerId : null
                }
            };
        }

    }
}
