using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMS.Services
{
    public class SalesService : ISalesService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<SalesService> _logger;

        public SalesService(IDbContextFactory dbContextFactory, ILogger<SalesService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<SalesViewModel> GetAllSalesAsync(int pageNumber, int? pageSize, SalesFilters? filters)
        {
            var viewModel = new SalesViewModel();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    // First, let's try to get all sales without filters to see if data exists
                    using (var command = new SqlCommand("GetAllSales", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                        // Set default values that won't filter out all records
                        command.Parameters.AddWithValue("@pIsDeleted", 0); // Only show non-deleted records
                        command.Parameters.AddWithValue("@pBillNumber", DBNull.Value);
                        command.Parameters.AddWithValue("@pSaleFrom", DBNull.Value);
                        command.Parameters.AddWithValue("@pSaleDateTo", DBNull.Value);
                        command.Parameters.AddWithValue("@pDescription", DBNull.Value);
                        command.Parameters.AddWithValue("@PageNo", pageNumber);
                        command.Parameters.AddWithValue("@PageSize", pageSize);
                        
                        // Only set CustomerId if it's provided in filters
                        if (filters?.CustomerId.HasValue == true)
                        {
                            command.Parameters.AddWithValue("@pCustomerId", filters.CustomerId.Value);
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@pCustomerId", DBNull.Value);
                        }

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var salesList = new List<SaleWithCustomerViewModel>();
                            while (await reader.ReadAsync())
                            {
                                var sale = new SaleWithCustomerViewModel
                                {
                                    SaleId = reader.GetInt64("SaleId"),
                                    BillNumber = reader.GetInt64("BillNumber"),
                                    SaleDate = reader.GetDateTime("SaleDate"),
                                    TotalAmount = reader.GetDecimal("TotalAmount"),
                                    DiscountAmount = reader.GetDecimal("DiscountAmount"),
                                    TotalReceivedAmount = reader.GetDecimal("TotalReceivedAmount"),
                                    TotalDueAmount = reader.GetDecimal("TotalDueAmount"),
                                    CustomerIdFk = reader.GetInt64("CustomerId_FK"),
                                    CustomerName = reader.GetString("CustomerName"),
                                    SaleDescription = reader.IsDBNull("SaleDescription") ? null : reader.GetString("SaleDescription"),
                                    IsDeleted = reader.GetBoolean("IsDeleted"),
                                    CreatedDate = reader.IsDBNull("CreatedDate") ? null : reader.GetDateTime("CreatedDate"),
                                    CreatedBy = reader.IsDBNull("CreatedBy") ? null : reader.GetInt64("CreatedBy"),
                                    ModifiedDate = reader.IsDBNull("ModifiedDate") ? null : reader.GetDateTime("ModifiedDate"),
                                    ModifiedBy = reader.IsDBNull("ModifiedBy") ? null : reader.GetInt64("ModifiedBy")
                                };
                                salesList.Add(sale);
                            }

                            // Apply pagination manually
                            var totalCount = salesList.Count;
                            var offset = (pageNumber - 1) * (pageSize ?? 10);
                            var pagedSalesList = salesList.Skip(offset).Take(pageSize ?? 10).ToList();

                            viewModel.SalesList = pagedSalesList;
                            viewModel.CurrentPage = pageNumber;
                            viewModel.PageSize = pageSize ?? 10;
                            viewModel.TotalCount = totalCount;
                            viewModel.TotalPages = (int)Math.Ceiling((double)totalCount / (pageSize ?? 10));
                            
                            // Log the count for debugging
                            _logger.LogInformation($"Retrieved {totalCount} sales records");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all sales");
                throw;
            }
            return viewModel;
        }

        public async Task<Sale> GetSaleByIdAsync(long id)
        {
            Sale sale = null;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"SELECT SaleId, BillNumber, SaleDate, TotalAmount, DiscountAmount, TotalReceivedAmount, 
                                      TotalDueAmount, CustomerId_FK, SaleDescription, IsDeleted, CreatedDate, CreatedBy, 
                                      ModifiedDate, ModifiedBy 
                               FROM Sales WHERE SaleId = @SaleId";
                    
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@SaleId", id);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                sale = new Sale
                                {
                                    SaleId = reader.GetInt64("SaleId"),
                                    BillNumber = reader.GetInt64("BillNumber"),
                                    SaleDate = reader.GetDateTime("SaleDate"),
                                    TotalAmount = reader.GetDecimal("TotalAmount"),
                                    DiscountAmount = reader.GetDecimal("DiscountAmount"),
                                    TotalReceivedAmount = reader.GetDecimal("TotalReceivedAmount"),
                                    TotalDueAmount = reader.GetDecimal("TotalDueAmount"),
                                    CustomerIdFk = reader.GetInt64("CustomerId_FK"),
                                    SaleDescription = reader.IsDBNull("SaleDescription") ? null : reader.GetString("SaleDescription"),
                                    IsDeleted = reader.GetBoolean("IsDeleted"),
                                    CreatedDate = reader.IsDBNull("CreatedDate") ? DateTime.MinValue : reader.GetDateTime("CreatedDate"),
                                    CreatedBy = reader.IsDBNull("CreatedBy") ? 0 : reader.GetInt64("CreatedBy"),
                                    ModifiedDate = reader.IsDBNull("ModifiedDate") ? DateTime.MinValue : reader.GetDateTime("ModifiedDate"),
                                    ModifiedBy = reader.IsDBNull("ModifiedBy") ? 0 : reader.GetInt64("ModifiedBy")
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sale by ID");
                throw;
            }
            return sale;
        }

        public async Task<bool> CreateSaleAsync(Sale sale)
        {
            bool response = false;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("AddSale", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pTotalAmount", sale.TotalAmount);
                        command.Parameters.AddWithValue("@pTotalReceivedAmount", sale.TotalReceivedAmount);
                        command.Parameters.AddWithValue("@pTotalDueAmount", sale.TotalDueAmount);
                        command.Parameters.AddWithValue("@pCustomerId_FK", sale.CustomerIdFk);
                        command.Parameters.AddWithValue("@pCreatedDate", sale.CreatedDate == default(DateTime) ? DateTime.Now : sale.CreatedDate);
                        command.Parameters.AddWithValue("@pCreatedBy", sale.CreatedBy);
                        command.Parameters.AddWithValue("@pModifiedDate", sale.ModifiedDate == default(DateTime) ? DateTime.Now : sale.ModifiedDate);
                        command.Parameters.AddWithValue("@pModifiedBy", sale.ModifiedBy == 0 ? sale.CreatedBy : sale.ModifiedBy);
                        command.Parameters.AddWithValue("@pDiscountAmount", sale.DiscountAmount);
                        command.Parameters.AddWithValue("@pBillNumber", sale.BillNumber);
                        command.Parameters.AddWithValue("@pSaleDescription", sale.SaleDescription ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pSaleDate", sale.SaleDate);

                        var salesIdParam = new SqlParameter("@pSalesId", SqlDbType.BigInt)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(salesIdParam);

                        await command.ExecuteNonQueryAsync();
                        long newSalesId = (long)salesIdParam.Value;
                        response = newSalesId > 0;
                    }
                    #region sales details insertion
                    //Sales Details insertion can be handled here or in a separate method as needed 

                    // SP is AddSaleDetails
                    #endregion
                    #region get stock and update stock quantity
                    //get stock by id and update stock quantity
                    //GetStockByProductId
                    //update stock quantity in stocks table
                    //UpdateStock
                    #endregion
                    #region sales transaction insertion
                    //sales Transaction create here
                            //SaleTransactionCreate
                    #endregion
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sale");
                throw;
            }
            return response;
        }

        public long AddSaleDetails(long saleId, long productId, decimal unitPrice, long quantity, decimal salePrice,
        decimal lineDiscountAmount, decimal payableAmount, long productRangeId, out int returnValue)
        {
            long saleDetailsId = 0;
            returnValue = 0;

            using (SqlConnection connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("AddSaleDetails", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@pSaleId_FK", saleId);
                    command.Parameters.AddWithValue("@pPrductId_FK", productId);
                    command.Parameters.AddWithValue("@pUnitPrice", unitPrice);
                    command.Parameters.AddWithValue("@pQuantity", quantity);
                    command.Parameters.AddWithValue("@pSalePrice", salePrice);
                    command.Parameters.AddWithValue("@pLineDiscountAmount", lineDiscountAmount);
                    command.Parameters.AddWithValue("@pPayableAmount", payableAmount);
                    command.Parameters.AddWithValue("@ProductRangeId_FK", productRangeId);

                    SqlParameter saleDetailsIdParam = new SqlParameter("@pSaleDetailsId", SqlDbType.BigInt) { Direction = ParameterDirection.Output };
                    command.Parameters.Add(saleDetailsIdParam);

                    command.ExecuteNonQuery();

                    saleDetailsId = (saleDetailsIdParam.Value != DBNull.Value) ? Convert.ToInt64(saleDetailsIdParam.Value) : 0;
               
                }
            }

            return saleDetailsId;
        }

        //public Task<StockMaster> GetStockByProductId(long productId)
        //{
        //    long returnValue = 0;

        //    using (SqlConnection connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
        //    {
        //        connection.Open();
        //        using (SqlCommand command = new SqlCommand("GetStockByProductId", connection))
        //        {
        //            command.CommandType = CommandType.StoredProcedure;

        //            command.Parameters.AddWithValue("@pProductId", productId);

               
        //            var res= command.ExecuteNonQuery();
                    
                    
        //        }
        //    }

        //    return returnValue;
        //}
        public async Task<StockMaster> GetStockByProductIdAsync(long productId)
        {
            var stocks = new StockMaster();

            using (SqlConnection connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("GetStockByProductId", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@pProductId", productId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var stock = new StockMaster
                            {
                                StockMasterId = reader.GetInt64(reader.GetOrdinal("StockMasterId")),
                                ProductIdFk = reader.GetInt64(reader.GetOrdinal("ProductId_FK")),
                                AvailableQuantity = reader.GetDecimal(reader.GetOrdinal("AvailableQuantity")),
                                UsedQuantity = reader.GetDecimal(reader.GetOrdinal("UsedQuantity")),
                                TotalQuantity = reader.GetDecimal(reader.GetOrdinal("TotalQuantity"))
                            };
                            stocks = stock;


                        }
                    }
                }
            }

            return stocks;
        }



        public long UpdateStock(long stockMasterId, long productId, decimal availableQuantity, decimal totalQty, decimal usedQuantity,
            long modifiedBy, DateTime modifiedDate)
        {
            int returnValue = 0;

            using (SqlConnection connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("UpdateStock", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@pStockMasterId", stockMasterId);
                    command.Parameters.AddWithValue("@pProductId", productId);
                    command.Parameters.AddWithValue("@pAvailableQuantity", availableQuantity);
                    command.Parameters.AddWithValue("@pUsedQuantity", usedQuantity);
                    command.Parameters.AddWithValue("@TotalQuantity", totalQty);
                    command.Parameters.AddWithValue("@pModifiedBy", modifiedBy);
                    command.Parameters.AddWithValue("@pModifiedDate", modifiedDate);


                    command.ExecuteNonQuery();

                    returnValue = 1;
                }
            }

            return returnValue;
        }

        public long SaleTransactionCreate(long stockMasterId, decimal quantity, string comment, DateTime createdDate,
            long createdBy, long transactionStatusId, long saleId)
        {
            long returnValue = 0;

            using (SqlConnection connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("SaleTransactionCreate", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@pStockMasterId", stockMasterId);
                    command.Parameters.AddWithValue("@pQuantity", quantity);
                    command.Parameters.AddWithValue("@pComment", (object)comment ?? DBNull.Value);
                    command.Parameters.AddWithValue("@pCreatedDate", createdDate);
                    command.Parameters.AddWithValue("@pCreatedBy", createdBy);
                    command.Parameters.AddWithValue("@pTransactionStatusId", transactionStatusId);
                    command.Parameters.AddWithValue("@pSaleId", saleId);

                    SqlParameter saleDetailsIdParam = new SqlParameter("@transactionId", SqlDbType.BigInt) { Direction = ParameterDirection.Output };
                    command.Parameters.Add(saleDetailsIdParam);

                    command.ExecuteNonQuery();
                    returnValue = (saleDetailsIdParam.Value != DBNull.Value) ? Convert.ToInt64(saleDetailsIdParam.Value) : 0;
                    
                }
            }

            return returnValue;
        }

        public async Task<int> UpdateSaleAsync(Sale sale)
        {
            int response = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("UpdateSale", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pSalesId", sale.SaleId);
                        command.Parameters.AddWithValue("@pTotalAmount", sale.TotalAmount);
                        command.Parameters.AddWithValue("@pTotalReceivedAmount", sale.TotalReceivedAmount);
                        command.Parameters.AddWithValue("@pTotalDueAmount", sale.TotalDueAmount);
                        command.Parameters.AddWithValue("@pCustomerId_FK", sale.CustomerIdFk);
                        command.Parameters.AddWithValue("@pModifiedDate", sale.ModifiedDate == default(DateTime) ? DateTime.Now : sale.ModifiedDate);
                        command.Parameters.AddWithValue("@pModifiedBy", sale.ModifiedBy);
                        command.Parameters.AddWithValue("@pDiscountAmount", sale.DiscountAmount);
                        command.Parameters.AddWithValue("@pBillNumber", sale.BillNumber);
                        command.Parameters.AddWithValue("@pSaleDescription", sale.SaleDescription ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pSaleDate", sale.SaleDate);

                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        response = rowsAffected;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sale");
                throw;
            }
            return response;
        }

        public async Task<int> DeleteSaleAsync(long id, DateTime modifiedDate, long modifiedBy)
        {
            int response = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("DeleteSale", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pSaleId", id);
                        command.Parameters.AddWithValue("@pModifiedDate", modifiedDate);
                        command.Parameters.AddWithValue("@pModifiedBy", modifiedBy);

                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        response = rowsAffected;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting sale");
                throw;
            }
            return response;
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            var customers = new List<Customer>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"SELECT CustomerId, CustomerName FROM Customers WHERE IsEnabled = 1";
                    
                    using (var command = new SqlCommand(sql, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var customer = new Customer
                                {
                                    CustomerId = reader.GetInt64("CustomerId"),
                                    CustomerName = reader.GetString("CustomerName")
                                };
                                customers.Add(customer);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all customers");
                throw;
            }
            return customers;
        }

        public async Task<long> GetNextBillNumberAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"SELECT ISNULL(MAX(BillNumber), 0) + 1 FROM Sales";
                    
                    using (var command = new SqlCommand(sql, connection))
                    {
                        var result = await command.ExecuteScalarAsync();
                        return Convert.ToInt64(result);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next bill number");
                return 1; // Return 1 as fallback
            }
        }

        public async Task<bool> HasAnySalesAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT COUNT(*) FROM Sales WHERE IsDeleted = 0";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        var result = await command.ExecuteScalarAsync();
                        return Convert.ToInt32(result) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if sales exist");
                return false;
            }
        }

        public async Task<List<ProductSizeViewModel>> GetProductUnitPriceRangeByProductIdAsync(long productId)
        {
            var productSizes = new List<ProductSizeViewModel>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("GetProductUnitPriceRangeByProductId", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pProductId", productId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var productSize = new ProductSizeViewModel
                                {
                                    ProductRangeId = reader.GetInt64("ProductRangeId"),
                                    ProductId_FK = reader.GetInt64("ProductId_FK"),
                                    MeasuringUnitId_FK = reader.GetInt64("MeasuringUnitId_FK"),
                                    RangeFrom = reader.GetDecimal("RangeFrom"),
                                    RangeTo = reader.GetDecimal("RangeTo"),
                                    UnitPrice = reader.GetDecimal("UnitPrice"),
                                    MeasuringUnitName = reader.GetString("MeasuringUnitName"),
                                    MeasuringUnitAbbreviation = reader.GetString("MeasuringUnitAbbreviation")
                                };
                                productSizes.Add(productSize);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product unit price range by product ID");
                throw;
            }
            return productSizes;
        }

        public async Task<long> CreateSaleAsync(decimal totalAmount, decimal totalReceivedAmount, decimal totalDueAmount, 
            long customerId, DateTime createdDate, long createdBy, DateTime modifiedDate, long modifiedBy, 
            decimal discountAmount, long billNumber, string saleDescription, DateTime saleDate)
        {
            long saleId = 0;
            int returnValue = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("AddSale", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pTotalAmount", totalAmount);
                        command.Parameters.AddWithValue("@pTotalReceivedAmount", totalReceivedAmount);
                        command.Parameters.AddWithValue("@pTotalDueAmount", totalDueAmount);
                        command.Parameters.AddWithValue("@pCustomerId_FK", customerId);
                        command.Parameters.AddWithValue("@pCreatedDate", createdDate);
                        command.Parameters.AddWithValue("@pCreatedBy", createdBy);
                        command.Parameters.AddWithValue("@pModifiedDate", modifiedDate);
                        command.Parameters.AddWithValue("@pModifiedBy", modifiedBy);
                        command.Parameters.AddWithValue("@pDiscountAmount", discountAmount);
                        command.Parameters.AddWithValue("@pBillNumber", billNumber);
                        command.Parameters.AddWithValue("@pSaleDescription", saleDescription ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pSaleDate", saleDate);

                        var salesIdParam = new SqlParameter("@pSalesId", SqlDbType.BigInt)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(salesIdParam);

                        
                       

                        await command.ExecuteNonQueryAsync();
                        saleId = (long)salesIdParam.Value;
                       
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sale");
                throw;
            }
            return saleId;
        }

        public async Task<decimal> GetPreviousDueAmountByCustomerIdAsync(long customerId)
        {
            decimal response = decimal.Zero;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("GetPreviousDueAmountBySaleId", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pBillId", DBNull.Value);
                        command.Parameters.AddWithValue("@pCustomerId", customerId);

                       

                        var res = await command.ExecuteScalarAsync();
                        response= (decimal)res;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting previous due amount for customer {CustomerId}", customerId);
                return 0; // Return 0 if there's an error
            }
            return response;
        }
    }
}
