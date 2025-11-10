using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMS.Services
{
    public class StockService : IStockService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<StockService> _logger;

        public StockService(IDbContextFactory dbContextFactory, ILogger<StockService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<bool> CreateStockAsync(StockMaster stock)
        {
            bool response = false;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    //var sql = @"INSERT INTO StockMaster (ProductId_FK, AvailableQuantity, UsedQuantity, TotalQuantity, UploadedDate, Comment, CreatedDate, CreatedBy) 
                    //           VALUES (@ProductIdFk, @AvailableQuantity, @UsedQuantity, @TotalQuantity, @UploadedDate, @Comment, @CreatedDate, @CreatedBy);
                    //           SELECT SCOPE_IDENTITY();";
                    
                    using (var command = new SqlCommand("UploadStock", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pProductId", stock.ProductIdFk);
                        command.Parameters.AddWithValue("@pQuantity", stock.AvailableQuantity);
                    
                        command.Parameters.AddWithValue("@pComment", stock.Comment ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pCreatedDate", stock.CreatedDate == default(DateTime) ? DateTime.Now : stock.CreatedDate);
                        command.Parameters.AddWithValue("@pCreatedBy", stock.CreatedBy);
                        command.Parameters.AddWithValue("@pModifiedDate", stock.ModifiedDate == default(DateTime) || stock.ModifiedDate==null ? DateTime.Now : stock.ModifiedDate);
                        command.Parameters.AddWithValue("@pModifiedBy", stock.ModifiedBy == null ? stock.CreatedBy : stock.ModifiedBy);

                        var unitTypeyIdParam = new SqlParameter("@pStockMasterId", SqlDbType.BigInt)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(unitTypeyIdParam);
                        await command.ExecuteNonQueryAsync();
                        long newunitTypeyIdParamParam = (long)unitTypeyIdParam.Value;
                        if (newunitTypeyIdParamParam != 0)
                        {
                            response = true;
                        }
                       
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating stock");
                throw;
            }
            return response;
        }

        public async Task<int> UpdateStockAsync(StockMaster stock)
        {
            int response = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                  
                    
                    using (var command = new SqlCommand("UpdateStock", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pStockMasterId", stock.StockMasterId);
                        command.Parameters.AddWithValue("@pProductId", stock.ProductIdFk);
                        command.Parameters.AddWithValue("@pAvailableQuantity", stock.AvailableQuantity);
                        command.Parameters.AddWithValue("@pUsedQuantity", stock.UsedQuantity);
                        command.Parameters.AddWithValue("@TotalQuantity", stock.TotalQuantity);
                        command.Parameters.AddWithValue("@pModifiedBy", stock.ModifiedBy);
                        command.Parameters.AddWithValue("@pModifiedDate", stock.ModifiedDate == default(DateTime) ? DateTime.Now : stock.ModifiedDate);

                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        response = rowsAffected;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock");
                throw;
            }
            return response;
        }

        public async Task<int> DeleteStockAsync(long id, DateTime modifiedDate, long modifiedBy)
        {
            int response = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                   // var sql = @"DELETE FROM StockMaster WHERE StockMasterId = @StockMasterId";
                    
                    using (var command = new SqlCommand("DeleteStockByMasterId", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@StockMasterId", id);

                       
                        var RowAffected = new SqlParameter("@RowsAffected", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(RowAffected);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected != 0)
                        {
                            response = rowsAffected;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting stock");
                throw;
            }
            return response;
        }

        public async Task<StockMaster> GetStockByIdAsync(long id)
        {
            var stock = new StockMaster();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    //var sql = @"SELECT StockMasterId, ProductId_FK as ProductIdFk, AvailableQuantity, UsedQuantity, TotalQuantity, 
                    //                  UploadedDate, Comment, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy 
                    //           FROM StockMaster WHERE StockMasterId = @StockMasterId";
                    
                    using (var command = new SqlCommand("GetStockByMasterId", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@StockMasterId", id);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                stock = new StockMaster
                                {
                                    StockMasterId = reader.GetInt64("StockMasterId"),
                                    ProductIdFk = reader.GetInt64("ProductId_FK"),
                                    AvailableQuantity = reader.GetDecimal("AvailableQuantity"),
                                    UsedQuantity = reader.GetDecimal("UsedQuantity"),
                                    TotalQuantity = reader.GetDecimal("TotalQuantity"),
                                    UploadedDate = reader.IsDBNull("UploadedDate") ? null : reader.GetDateTime("UploadedDate"),
                                    Comment = reader.IsDBNull("Comment") ? null : reader.GetString("Comment"),
                                    CreatedDate = reader.IsDBNull("CreatedDate") ? DateTime.MinValue : reader.GetDateTime("CreatedDate"),
                                    CreatedBy = reader.IsDBNull("CreatedBy") ? 0 : reader.GetInt64("CreatedBy"),
                                    ModifiedDate = reader.IsDBNull("ModifiedDate") ? null : reader.GetDateTime("ModifiedDate"),
                                    ModifiedBy = reader.IsDBNull("ModifiedBy") ? null : reader.GetInt64("ModifiedBy")
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock by ID");
                throw;
            }
            return stock;
        }

        public async Task<StockViewModel> GetAllStocksAsync(int pageNumber, int? pageSize, StockFilters? filters)
        {
            var viewModel = new StockViewModel();
            var stockList = new List<StockWithProductViewModel>();
            var totalRecords = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                  
                    using (var command = new SqlCommand("GetAllStocks", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pProductName", filters?.ProductName ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@PageNumber", pageNumber);
                        command.Parameters.AddWithValue("@PageSize", pageSize);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                           
                            while (await reader.ReadAsync())
                            {
                                var stock = new StockWithProductViewModel
                                {
                                    StockMasterId = reader.GetInt64("StockMasterId"),
                                    AvailableQuantity = reader.GetDecimal("AvailableQuantity"),
                                    UsedQuantity = reader.GetDecimal("UsedQuantity"),
                                    TotalQuantity = reader.GetDecimal("TotalQuantity"),
                                    CreatedDate = reader.IsDBNull("CreatedDate") ? null : reader.GetDateTime("CreatedDate"),
                                    CreatedBy = reader.IsDBNull("CreatedBy") ? 0 : reader.GetInt64("CreatedBy"),
                                    ModifiedDate = reader.IsDBNull("ModifiedDate") ? null : reader.GetDateTime("ModifiedDate"),
                                    ModifiedBy = reader.IsDBNull("ModifiedBy") ? null : reader.GetInt64("ModifiedBy"),
                                    ProductName = reader.GetString("ProductName"),
                                    ProductCode = reader.IsDBNull("ProductCode") ? null : reader.GetString("ProductCode"),
                                    StockLocaion = reader.IsDBNull("Location") ? null : reader.GetString("Location"),
                                    UnitPrice = reader.GetDecimal("UnitPrice")
                                };
                                stockList.Add(stock);
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
                _logger.LogError(ex, "Error getting all stocks");
                throw;
            }
            return new StockViewModel
            {
                StockList = stockList,
                CurrentPage = pageNumber,
                TotalPages = pageSize.HasValue && pageSize.Value > 0
                    ? (int)Math.Ceiling(totalRecords / (double)pageSize.Value)
                    : 1,
                PageSize = pageSize,
                TotalCount = totalRecords

            }; 
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            var products = new List<Product>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"SELECT ProductId, ProductName, ProductCode FROM Products WHERE IsEnabled = 1 OR IsEnabled IS NULL";
                    
                    using (var command = new SqlCommand(sql, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var product = new Product
                                {
                                    ProductId = reader.GetInt64("ProductId"),
                                    ProductName = reader.GetString("ProductName"),
                                    ProductCode = reader.IsDBNull("ProductCode") ? null : reader.GetString("ProductCode")
                                };
                                products.Add(product);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all products");
                throw;
            }
            return products;
        }

        public async Task<List<TransactionStatus>> GetAllTransactionStatusesAsync()
        {
            var statuses = new List<TransactionStatus>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"SELECT StockTransactionStatusId, TransactionStatusName FROM TransactionStatus";
                    
                    using (var command = new SqlCommand(sql, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var status = new TransactionStatus
                                {
                                    StockTransactionStatusId = reader.GetInt64("StockTransactionStatusId"),
                                    TransactionStatusName = reader.IsDBNull("TransactionStatusName") ? null : reader.GetString("TransactionStatusName")
                                };
                                statuses.Add(status);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all transaction statuses");
                throw;
            }
            return statuses;
        }

        public async Task<List<AdminCategory>> GetAllEnabledCategoriesAsync()
        {
            var categories = new List<AdminCategory>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"SELECT CategoryId, CategoryName FROM AdminCategory WHERE IsEnabled = 1 OR IsEnabled IS NULL";
                    
                    using (var command = new SqlCommand(sql, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var category = new AdminCategory
                                {
                                    CategoryId = reader.GetInt64("CategoryId"),
                                    CategoryName = reader.IsDBNull("CategoryName") ? null : reader.GetString("CategoryName")
                                };
                                categories.Add(category);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all enabled categories");
                throw;
            }
            return categories;
        }

        public async Task<List<Product>> GetProductsByCategoryIdAsync(long categoryId)
        {
            var products = new List<Product>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"SELECT ProductId, ProductName, ProductCode FROM Products 
                               WHERE CategoryId_FK = @CategoryId AND (IsEnabled = 1 OR IsEnabled IS NULL)";
                    
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@CategoryId", categoryId);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var product = new Product
                                {
                                    ProductId = reader.GetInt64("ProductId"),
                                    ProductName = reader.GetString("ProductName"),
                                    ProductCode = reader.IsDBNull("ProductCode") ? null : reader.GetString("ProductCode")
                                };
                                products.Add(product);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products by category ID");
                throw;
            }
            return products;
        }

        public async Task<StockMaster> GetStockByProductIdAsync(long productId)
        {
            var stock = new StockMaster();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"SELECT StockMasterId, ProductId_FK as ProductIdFk, AvailableQuantity, UsedQuantity, TotalQuantity, 
                                      UploadedDate, Comment, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy 
                               FROM StockMaster WHERE ProductId_FK = @ProductId";
                    
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@ProductId", productId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                stock = new StockMaster
                                {
                                    StockMasterId = reader.GetInt64("StockMasterId"),
                                    ProductIdFk = reader.GetInt64("ProductIdFk"),
                                    AvailableQuantity = reader.GetDecimal("AvailableQuantity"),
                                    UsedQuantity = reader.GetDecimal("UsedQuantity"),
                                    TotalQuantity = reader.GetDecimal("TotalQuantity"),
                                    UploadedDate = reader.IsDBNull("UploadedDate") ? null : reader.GetDateTime("UploadedDate"),
                                    Comment = reader.IsDBNull("Comment") ? null : reader.GetString("Comment"),
                                    CreatedDate = reader.IsDBNull("CreatedDate") ? DateTime.MinValue : reader.GetDateTime("CreatedDate"),
                                    CreatedBy = reader.IsDBNull("CreatedBy") ? 0 : reader.GetInt64("CreatedBy"),
                                    ModifiedDate = reader.IsDBNull("ModifiedDate") ? null : reader.GetDateTime("ModifiedDate"),
                                    ModifiedBy = reader.IsDBNull("ModifiedBy") ? null : reader.GetInt64("ModifiedBy")
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock by product ID");
                throw;
            }
            return stock;
        }

        public async Task<StockHistoryViewModel> GetStockHistoryAsync(int pageNumber, int? pageSize, StockHistoryFilters? filters)
        {
            var viewModel = new StockHistoryViewModel();
            var transactionList = new List<StockTransactionHistoryViewModel>();
            var totalRecords = 0;
            
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    // If filtering by specific product (StockMasterId), use the stored procedure
                    if (filters?.StockMasterId.HasValue == true)
                    {
                        using (var command = new SqlCommand("GetStockTransactionsByStockMasterId", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            command.Parameters.AddWithValue("@pStockMasterId", filters.StockMasterId.Value);
                            command.Parameters.AddWithValue("@TransactionStatusId", filters.TransactionTypeId==0 || filters.TransactionTypeId ==null ? DBNull.Value : filters.TransactionTypeId);
                            command.Parameters.AddWithValue("@FromDate", filters.FromDate == null ? DBNull.Value : filters.FromDate);
                            command.Parameters.AddWithValue("@ToDate", filters.ToDate == null ? DBNull.Value : filters.ToDate);
                            
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var transaction = new StockTransactionHistoryViewModel
                                    {
                                        StockTransactionId = reader.GetInt64("StockTransactionId"),
                                        //StockMasterIdFk = reader.IsDBNull("StockMasterId_FK") ? 0 : reader.GetInt64("StockMasterId_FK"),
                                        StockQuantity = reader.GetDecimal("StockQuantity"),
                                        TransactionDate = reader.GetDateTime("TransactionDate"),
                                        CreatedBy = reader.GetInt64("CreatedBy"),
                                        CreatedDate = reader.GetDateTime("CreatedDate"),
                                        //TransactionStatusId = reader.IsDBNull("TransactionStatusId") ? 0 : reader.GetInt64("TransactionStatusId"),
                                        //Comment = reader.IsDBNull("Comment") ? null : reader.GetString("Comment"),
                                        //SaleId = reader.IsDBNull("SaleId") ? null : reader.GetInt64("SaleId"),
                                        //ProductName = reader.IsDBNull("ProductName") ? "" : reader.GetString("ProductName"),
                                        //ProductCode = reader.IsDBNull("ProductCode") ? null : reader.GetString("ProductCode"),
                                        //UserName = reader.IsDBNull("UserName") ? "System" : reader.GetString("UserName"),
                                        //TransactionStatusName = reader.IsDBNull("TransactionStatusName") ? "" : reader.GetString("TransactionStatusName"),
                                        TransactionType = reader.IsDBNull("TransactionStatusName") ? "" : reader.GetString("TransactionStatusName"),
                                        Description = reader.IsDBNull("Comment") ? "" : reader.GetString("Comment")
                                    };
                                    transactionList.Add(transaction);
                                }

                                // Move to second result set for total count
                                await reader.NextResultAsync();

                                if (await reader.ReadAsync())
                                {
                                    totalRecords = reader.GetInt32("TotalRecords");
                                }
                            }
                        }
                        //totalRecords = transactionList.Count;
                    }
                   
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock history");
                throw;
            }
            
            return new StockHistoryViewModel
            {
                TransactionList = transactionList,
                CurrentPage = pageNumber,
                TotalPages = pageSize.HasValue && pageSize.Value > 0
                    ? (int)Math.Ceiling(totalRecords / (double)pageSize.Value)
                    : 1,
                PageSize = pageSize,
                TotalCount = totalRecords,
                FromDate = filters?.FromDate,
                ToDate = filters?.ToDate,
                TransactionType = filters?.TransactionType
            };
        }
        
        
    }
}

