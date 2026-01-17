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
            var totalRecords = 0;
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
                        command.Parameters.AddWithValue("@pBillNumber", filters.BillNumber == null ? DBNull.Value :filters.BillNumber);
                        command.Parameters.AddWithValue("@pSaleFrom", filters.SaleFrom==null ? DBNull.Value: filters.SaleFrom);
                        command.Parameters.AddWithValue("@pSaleDateTo", filters.SaleDateTo==null ? DBNull.Value:filters.SaleDateTo);
                        command.Parameters.AddWithValue("@pDescription", string.IsNullOrEmpty(filters.Description) ?DBNull.Value : filters.Description);
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
                                    PaymentMethod = reader.IsDBNull("PaymentMethod") ? null : reader.GetString("PaymentMethod"),
                                    IsDeleted = reader.GetBoolean("IsDeleted"),
                                    CreatedDate = reader.IsDBNull("CreatedDate") ? null : reader.GetDateTime("CreatedDate"),
                                    CreatedBy = reader.IsDBNull("CreatedBy") ? null : reader.GetInt64("CreatedBy"),
                                    ModifiedDate = reader.IsDBNull("ModifiedDate") ? null : reader.GetDateTime("ModifiedDate"),
                                    ModifiedBy = reader.IsDBNull("ModifiedBy") ? null : reader.GetInt64("ModifiedBy")
                                };
                                salesList.Add(sale);
                            }
                            await reader.NextResultAsync();

                            if (await reader.ReadAsync())
                            {
                                totalRecords = reader.GetInt32(reader.GetOrdinal("TotalRecords"));
                            }

                            // Apply pagination manually
                            var totalCount = totalRecords;
                            var offset = (pageNumber - 1) * (pageSize ?? 10);
                            var pagedSalesList = offset;

                            viewModel.SalesList = salesList;
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
                    
                    //var sql = @"SELECT SaleId, BillNumber, SaleDate, TotalAmount, DiscountAmount, TotalReceivedAmount, 
                    //                  TotalDueAmount, CustomerId_FK, SaleDescription, IsDeleted, CreatedDate, CreatedBy, 
                    //                  ModifiedDate, ModifiedBy 
                    //           FROM Sales WHERE SaleId = @SaleId";
                    
                    using (var command = new SqlCommand("GetSaleBySaleId", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pSaleId", id);

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
                                    ModifiedBy = reader.IsDBNull("ModifiedBy") ? 0 : reader.GetInt64("ModifiedBy"),
                                    PaymentMethod = reader.IsDBNull("PaymentMethod") ? null : reader.GetString("PaymentMethod"),
                                    OnlineAccountId = reader.IsDBNull("OnlineAccountId") ? 0 : reader.GetInt64("OnlineAccountId"),
                                    PersonalPaymentId = reader.IsDBNull("PersonalPaymentId") ? 0 : reader.GetInt64("PersonalPaymentId"),

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

        public long AddSaleDetails(long saleId, long productId, decimal unitPrice, decimal quantity, decimal salePrice,
        decimal lineDiscountAmount, decimal payableAmount, long productRangeId,DateTime currentdate,long userId,string paymentMethod,long? onlineAccountId, out int returnValue)
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
                    // Convert decimal quantity to long for database (round to nearest whole number)
                    // Note: Database stores as long, but we accept decimal for precision during conversion
                    command.Parameters.AddWithValue("@pQuantity", (long)Math.Round(quantity, MidpointRounding.AwayFromZero));
                    command.Parameters.AddWithValue("@pSalePrice", salePrice);
                    command.Parameters.AddWithValue("@pLineDiscountAmount", lineDiscountAmount);
                    command.Parameters.AddWithValue("@pPayableAmount", payableAmount);
                    command.Parameters.AddWithValue("@ProductRangeId_FK", productRangeId);
                    command.Parameters.AddWithValue("@CreatedDate", currentdate);
                    command.Parameters.AddWithValue("@CreatedBy", userId);
                    command.Parameters.AddWithValue("@PaymentMethod", paymentMethod ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@onlineAccountId", onlineAccountId ?? (object)DBNull.Value);
                    
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
                    command.Parameters.AddWithValue("@pSaleId", saleId==0 ? DBNull.Value : saleId);

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
                        command.Parameters.AddWithValue("@PaymentMethod", sale.PaymentMethod);
                        command.Parameters.AddWithValue("@OnlineAccountId", sale.OnlineAccountId ?? (object)DBNull.Value);
                       

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
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Step 1: Get sale details to reverse stock (using the same connection/transaction)
                            var saleDetails = new List<SaleDetailViewModel>();
                            using (var command = new SqlCommand("GetSaleDetailsBySaleId", connection, transaction))
                            {
                                command.CommandType = CommandType.StoredProcedure;
                                command.Parameters.AddWithValue("@pSaleId", id);
                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        saleDetails.Add(new SaleDetailViewModel
                                        {
                                            ProductId = reader.IsDBNull("PrductId_FK") ? 0 : reader.GetInt64("PrductId_FK"),
                                            ProductRangeId = reader.IsDBNull("ProductRangeId_FK") ? 0 : reader.GetInt64("ProductRangeId_FK"),
                                            ProductName = reader.IsDBNull("ProductName") ? string.Empty : reader.GetString("ProductName"),
                                            MeasuringUnitAbbreviation = reader.IsDBNull("MeasuringUnitAbbreviation") ? string.Empty : reader.GetString("MeasuringUnitAbbreviation"),
                                            UnitPrice = reader.IsDBNull("UnitPrice") ? 0m : reader.GetDecimal("UnitPrice"),
                                            Quantity = reader.IsDBNull("Quantity") ? 0 : reader.GetInt64("Quantity"),
                                            SalePrice = reader.IsDBNull("SalePrice") ? 0m : reader.GetDecimal("SalePrice"),
                                            LineDiscountAmount = reader.IsDBNull("LineDiscountAmount") ? 0m : reader.GetDecimal("LineDiscountAmount"),
                                            PayableAmount = reader.IsDBNull("PayableAmount") ? 0m : reader.GetDecimal("PayableAmount"),
                                            PaymentMethod = reader.IsDBNull("PaymentMethod") ? string.Empty : reader.GetString("PaymentMethod"),
                                        });
                                    }
                                }
                            }
                            _logger.LogInformation("Found {Count} sale details to process for sale {SaleId}", saleDetails.Count, id);

                            // Step 2: Restore stock for each product (increase available quantity, decrease used quantity)
                            foreach (var detail in saleDetails)
                            {
                                // Get stock within the transaction
                                StockMaster? prodMaster = null;
                                using (var stockCommand = new SqlCommand("GetStockByProductId", connection, transaction))
                                {
                                    stockCommand.CommandType = CommandType.StoredProcedure;
                                    stockCommand.Parameters.AddWithValue("@pProductId", detail.ProductId);
                                    using (var reader = await stockCommand.ExecuteReaderAsync())
                                    {
                                        if (await reader.ReadAsync())
                                        {
                                            prodMaster = new StockMaster
                                            {
                                                StockMasterId = reader.GetInt64(reader.GetOrdinal("StockMasterId")),
                                                ProductIdFk = reader.GetInt64(reader.GetOrdinal("ProductId_FK")),
                                                AvailableQuantity = reader.GetDecimal(reader.GetOrdinal("AvailableQuantity")),
                                                UsedQuantity = reader.GetDecimal(reader.GetOrdinal("UsedQuantity")),
                                                TotalQuantity = reader.GetDecimal(reader.GetOrdinal("TotalQuantity"))
                                            };
                                        }
                                    }
                                }

                                if (prodMaster != null)
                                {
                                    // Calculate new quantities (restore stock that was decreased)
                                    var newAvailableQuantity = prodMaster.AvailableQuantity + (decimal)detail.Quantity;
                                    var newUsedQuantity = prodMaster.UsedQuantity - (decimal)detail.Quantity;
                                    
                                    // Ensure quantities don't go negative
                                    if (newUsedQuantity < 0) newUsedQuantity = 0;

                                    // Update stock quantity (INCREASE available quantity, DECREASE used quantity) within transaction
                                    using (var updateStockCommand = new SqlCommand("UpdateStock", connection, transaction))
                                    {
                                        updateStockCommand.CommandType = CommandType.StoredProcedure;
                                        updateStockCommand.Parameters.AddWithValue("@pStockMasterId", prodMaster.StockMasterId);
                                        updateStockCommand.Parameters.AddWithValue("@pProductId", detail.ProductId);
                                        updateStockCommand.Parameters.AddWithValue("@pAvailableQuantity", newAvailableQuantity);
                                        updateStockCommand.Parameters.AddWithValue("@pUsedQuantity", newUsedQuantity);
                                        updateStockCommand.Parameters.AddWithValue("@TotalQuantity", prodMaster.TotalQuantity); // Total quantity remains same
                                        updateStockCommand.Parameters.AddWithValue("@pModifiedBy", modifiedBy);
                                        updateStockCommand.Parameters.AddWithValue("@pModifiedDate", modifiedDate);

                                        await updateStockCommand.ExecuteNonQueryAsync();
                                    }

                                    _logger.LogInformation("Stock restored for product {ProductId}: Increased available by {Quantity}, Decreased used by {Quantity}. New available: {NewAvailable}, New used: {NewUsed}",
                                        detail.ProductId, detail.Quantity, detail.Quantity, newAvailableQuantity, newUsedQuantity);
                                }
                                else
                                {
                                    _logger.LogWarning("Stock master not found for product {ProductId} when deleting sale {SaleId}", detail.ProductId, id);
                                }
                            }

                            // Step 3: Delete sale details using DeleteSaleDetailBySaleId stored procedure
                            using (var command = new SqlCommand("DeleteSaleDetailBySaleId", connection, transaction))
                            {
                                command.CommandType = CommandType.StoredProcedure;
                                command.Parameters.AddWithValue("@pSaleId", id);
                                var detailsAffected = await command.ExecuteNonQueryAsync();
                                _logger.LogInformation("Deleted {Count} sale details for sale {SaleId}", detailsAffected, id);
                            }

                            // Step 4: Mark transactions as deleted (soft delete) where SaleId = id
                            using (var command = new SqlCommand(@"
                                UPDATE StockTransactions 
                                SET IsDeleted = 1, ModifiedDate = GETDATE() 
                                WHERE SaleId = @SaleId AND (IsDeleted = 0 OR IsDeleted IS NULL)", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@SaleId", id);
                                var transactionsAffected = await command.ExecuteNonQueryAsync();
                                _logger.LogInformation("Marked {Count} transactions as deleted for sale {SaleId}", transactionsAffected, id);
                            }

                            // Step 4.5: Handle online payment records if payment method is Online
                            // Get sale information to check payment method
                            string? paymentMethod = null;
                            long? personalPaymentId = null;
                            using (var saleCommand = new SqlCommand("GetSaleBySaleId", connection, transaction))
                            {
                                saleCommand.CommandType = CommandType.StoredProcedure;
                                saleCommand.Parameters.AddWithValue("@pSaleId", id);
                                using (var reader = await saleCommand.ExecuteReaderAsync())
                                {
                                    if (await reader.ReadAsync())
                                    {
                                        paymentMethod = reader.IsDBNull("PaymentMethod") ? null : reader.GetString("PaymentMethod");
                                        personalPaymentId = reader.IsDBNull("PersonalPaymentId") ? null : reader.GetInt64("PersonalPaymentId");
                                    }
                                }
                            }

                            if (paymentMethod != null && paymentMethod.Equals("Online", StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.LogInformation("Sale {SaleId} has Online payment method, updating PersonalPaymentSaleDetail and PersonalPayments", id);

                                // Step 4.5.1: Get PersonalPaymentIds from PersonalPaymentSaleDetail for this sale
                                var personalPaymentIds = new List<long>();
                                using (var command = new SqlCommand(@"
                                    SELECT DISTINCT PersonalPaymentId 
                                    FROM PersonalPaymentSaleDetail 
                                    WHERE SaleId = @SaleId AND IsActive = 1", connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@SaleId", id);
                                    using (var reader = await command.ExecuteReaderAsync())
                                    {
                                        while (await reader.ReadAsync())
                                        {
                                            if (!reader.IsDBNull("PersonalPaymentId"))
                                            {
                                                personalPaymentIds.Add(reader.GetInt64("PersonalPaymentId"));
                                            }
                                        }
                                    }
                                }

                                // Also add the PersonalPaymentId from the sale if it exists
                                if (personalPaymentId.HasValue && personalPaymentId.Value > 0 && !personalPaymentIds.Contains(personalPaymentId.Value))
                                {
                                    personalPaymentIds.Add(personalPaymentId.Value);
                                }

                                // Step 4.5.2: Update PersonalPaymentSaleDetail - Set IsActive = 0 where SaleId = id
                                using (var command = new SqlCommand(@"
                                    UPDATE PersonalPaymentSaleDetail 
                                    SET IsActive = 0, ModifiedDate = GETDATE() 
                                    WHERE SaleId = @SaleId AND IsActive = 1", connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@SaleId", id);
                                    var detailsAffected = await command.ExecuteNonQueryAsync();
                                    _logger.LogInformation("Updated {Count} PersonalPaymentSaleDetail records (IsActive = 0) for sale {SaleId}", detailsAffected, id);
                                }

                                // Step 4.5.3: Calculate total amount from PersonalPaymentSaleDetail and update DebitAmount in PersonalPayments
                                if (personalPaymentIds.Count > 0)
                                {
                                    // Calculate total amount per PersonalPaymentId from PersonalPaymentSaleDetail
                                    var paymentAmounts = new Dictionary<long, decimal>();
                                    using (var command = new SqlCommand(@"
                                        SELECT PersonalPaymentId, SUM(Amount) as TotalAmount
                                        FROM PersonalPaymentSaleDetail 
                                        WHERE SaleId = @SaleId AND IsActive = 0
                                        GROUP BY PersonalPaymentId", connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@SaleId", id);
                                        using (var reader = await command.ExecuteReaderAsync())
                                        {
                                            while (await reader.ReadAsync())
                                            {
                                                if (!reader.IsDBNull("PersonalPaymentId") && !reader.IsDBNull("TotalAmount"))
                                                {
                                                    var ppId = reader.GetInt64("PersonalPaymentId");
                                                    var totalAmount = reader.GetDecimal("TotalAmount");
                                                    paymentAmounts[ppId] = totalAmount;
                                                }
                                            }
                                        }
                                    }

                                    // Update PersonalPayments - Decrease DebitAmount by the calculated total amount
                                    foreach (var ppId in personalPaymentIds)
                                    {
                                        // Get current DebitAmount for this PersonalPayment
                                        decimal currentDebitAmount = 0;
                                        using (var getCommand = new SqlCommand(@"
                                            SELECT CreditAmount 
                                            FROM PersonalPayments 
                                            WHERE PersonalPaymentId = @PersonalPaymentId", connection, transaction))
                                        {
                                            getCommand.Parameters.AddWithValue("@PersonalPaymentId", ppId);
                                            var result = await getCommand.ExecuteScalarAsync();
                                            if (result != null && result != DBNull.Value)
                                            {
                                                currentDebitAmount = Convert.ToDecimal(result);
                                            }
                                        }

                                        // Calculate new DebitAmount (subtract the total amount from PersonalPaymentSaleDetail)
                                        decimal amountToSubtract = paymentAmounts.ContainsKey(ppId) ? paymentAmounts[ppId] : 0;
                                        decimal newDebitAmount = currentDebitAmount - amountToSubtract;
                                        
                                        // Ensure DebitAmount doesn't go negative
                                        if (newDebitAmount < 0) newDebitAmount = 0;

                                        // Update PersonalPayments with new DebitAmount
                                        using (var updateCommand = new SqlCommand(@"
                                            UPDATE PersonalPayments 
                                            SET CreditAmount = @NewDebitAmount, ModifiedDate = GETDATE() 
                                            WHERE PersonalPaymentId = @PersonalPaymentId", connection, transaction))
                                        {
                                            updateCommand.Parameters.AddWithValue("@PersonalPaymentId", ppId);
                                            updateCommand.Parameters.AddWithValue("@NewDebitAmount", newDebitAmount);
                                            var paymentsAffected = await updateCommand.ExecuteNonQueryAsync();
                                            _logger.LogInformation("Updated PersonalPayment {PersonalPaymentId} for sale {SaleId}: DebitAmount decreased by {AmountToSubtract} (from {OldDebitAmount} to {NewDebitAmount}), Rows affected: {RowsAffected}", 
                                                ppId, id, amountToSubtract, currentDebitAmount, newDebitAmount, paymentsAffected);
                                        }
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("No PersonalPaymentIds found for sale {SaleId} with Online payment method", id);
                                }
                            }

                            // Step 5: Mark the sale as deleted (soft delete) using DeleteSale stored procedure
                            using (var command = new SqlCommand("DeleteSale", connection, transaction))
                            {
                                command.CommandType = CommandType.StoredProcedure;
                                command.Parameters.AddWithValue("@pSaleId", id);
                                command.Parameters.AddWithValue("@pModifiedDate", modifiedDate);
                                command.Parameters.AddWithValue("@pModifiedBy", modifiedBy);

                                var rowsAffected = await command.ExecuteNonQueryAsync();
                                
                                if (rowsAffected != 0)
                                {
                                    transaction.Commit();
                                    _logger.LogInformation("Sale soft deleted successfully: {SaleId}, Rows affected: {RowsAffected}", id, rowsAffected);
                                    response = rowsAffected;
                                }
                                else
                                {
                                    transaction.Rollback();
                                    _logger.LogWarning("No rows affected when deleting sale {SaleId}", id);
                                    response = 0;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.LogError(ex, "Error deleting sale {SaleId} in transaction", id);
                            throw;
                        }
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

        public async Task<List<SaleDetailViewModel>> GetSaleDetailsBySaleIdAsync(long saleId)
        {
            var saleDetails = new List<SaleDetailViewModel>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("GetSaleDetailsBySaleId", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pSaleId", saleId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                saleDetails.Add(new SaleDetailViewModel
                                {
                                    ProductId = reader.IsDBNull("PrductId_FK") ? 0 : reader.GetInt64("PrductId_FK"),
                                    ProductRangeId = reader.IsDBNull("ProductRangeId_FK") ? 0 : reader.GetInt64("ProductRangeId_FK"),
                                    ProductName = reader.IsDBNull("ProductName") ? string.Empty : reader.GetString("ProductName"),
                                    MeasuringUnitAbbreviation = reader.IsDBNull("MeasuringUnitAbbreviation") ? string.Empty : reader.GetString("MeasuringUnitAbbreviation"),
                                    UnitPrice = reader.IsDBNull("UnitPrice") ? 0m : reader.GetDecimal("UnitPrice"),
                                    Quantity = reader.IsDBNull("Quantity") ? 0 : reader.GetInt64("Quantity"),
                                    SalePrice = reader.IsDBNull("SalePrice") ? 0m : reader.GetDecimal("SalePrice"),
                                    LineDiscountAmount = reader.IsDBNull("LineDiscountAmount") ? 0m : reader.GetDecimal("LineDiscountAmount"),
                                    PayableAmount = reader.IsDBNull("PayableAmount") ? 0m : reader.GetDecimal("PayableAmount")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sale details by sale ID");
                throw;
            }
            return saleDetails;
        }

        public async Task<SalePrintViewModel> GetSaleForPrintAsync(long saleId)
        {
            var salePrint = new SalePrintViewModel();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    // Get sale information
                    var saleSql = @"SELECT s.SaleId, s.BillNumber, s.SaleDate, s.TotalAmount, s.DiscountAmount, 
                                          s.TotalReceivedAmount, s.TotalDueAmount, s.CustomerId_FK, s.SaleDescription,
                                          c.CustomerName
                                   FROM Sales s
                                   LEFT JOIN Customers c ON s.CustomerId_FK = c.CustomerId
                                   WHERE s.SaleId = @SaleId";
                    
                    using (var command = new SqlCommand(saleSql, connection))
                    {
                        command.Parameters.AddWithValue("@SaleId", saleId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                salePrint.SaleId = reader.GetInt64("SaleId");
                                salePrint.BillNumber = reader.GetInt64("BillNumber");
                                salePrint.SaleDate = reader.GetDateTime("SaleDate");
                                salePrint.TotalAmount = reader.GetDecimal("TotalAmount");
                                salePrint.DiscountAmount = reader.GetDecimal("DiscountAmount");
                                salePrint.TotalReceivedAmount = reader.GetDecimal("TotalReceivedAmount");
                                salePrint.TotalDueAmount = reader.GetDecimal("TotalDueAmount");
                                salePrint.CustomerIdFk = reader.GetInt64("CustomerId_FK");
                                salePrint.CustomerName = reader.IsDBNull("CustomerName") ? "Unknown Customer" : reader.GetString("CustomerName");
                                salePrint.SaleDescription = reader.IsDBNull("SaleDescription") ? null : reader.GetString("SaleDescription");
                            }
                        }
                    }

                    // Get sale details with product names
                    var detailsSql = @"SELECT sd.SaleDetailId, sd.PrductId_FK, sd.UnitPrice, sd.Quantity, 
                                             sd.SalePrice, sd.LineDiscountAmount, sd.PayableAmount, sd.ProductRangeId_FK,
                                             p.ProductName
                                      FROM SaleDetails sd
                                      LEFT JOIN Products p ON sd.PrductId_FK = p.ProductId
                                      WHERE sd.SaleId_FK = @SaleId";
                    
                    using (var command = new SqlCommand(detailsSql, connection))
                    {
                        command.Parameters.AddWithValue("@SaleId", saleId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                salePrint.SaleDetails.Add(new SaleDetailPrintViewModel
                                {
                                    ProductId = reader.GetInt64("PrductId_FK"),
                                    ProductName = reader.IsDBNull("ProductName") ? "Unknown Product" : reader.GetString("ProductName"),
                                    UnitPrice = reader.GetDecimal("UnitPrice"),
                                    Quantity = reader.GetInt64("Quantity"),
                                    SalePrice = reader.GetDecimal("SalePrice"),
                                    LineDiscountAmount = reader.GetDecimal("LineDiscountAmount"),
                                    PayableAmount = reader.GetDecimal("PayableAmount"),
                                    ProductRangeId = reader.GetInt64("ProductRangeId_FK")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sale for print");
                throw;
            }
            return salePrint;
        }

        public async Task<int> DeleteSaleDetailsBySaleIdAsync(long saleId)
        {
            int response = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("DeleteSaleDetailBySaleId", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pSaleId", saleId);
                        response = await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting sale details by sale ID");
                throw;
            }
            return response;
        }
        public async Task<int> TransactionDeleteAndStockUpdate(long saleId)
        {
            int response = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("TransactionsDelete", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pSaleId", saleId);
                        response = await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting sale details by sale ID");
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
                _logger.LogInformation("=== DEBUGGING: GetProductUnitPriceRangeByProductIdAsync called with productId: {ProductId}", productId);
                
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    _logger.LogInformation("Database connection opened successfully");
                    
                    using (var command = new SqlCommand("GetProductUnitPriceRangeByProductId", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pProductId", productId);
                        _logger.LogInformation("Executing stored procedure GetProductUnitPriceRangeByProductId with productId: {ProductId}", productId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            int recordCount = 0;
                            while (await reader.ReadAsync())
                            {
                                recordCount++;
                                _logger.LogInformation("Reading record {Count} - ProductRangeId: {ProductRangeId}, RangeFrom: {RangeFrom}, RangeTo: {RangeTo}, UnitPrice: {UnitPrice}, MeasuringUnit: {MeasuringUnitName}", 
                                    recordCount, reader.GetInt64("ProductRangeId"), reader.GetDecimal("RangeFrom"), reader.GetDecimal("RangeTo"), reader.GetDecimal("UnitPrice"), reader.GetString("MeasuringUnitName"));
                                
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
                            _logger.LogInformation("Total records read from database: {Count}", recordCount);
                        }
                    }
                }
                _logger.LogInformation("Returning {Count} product sizes from GetProductUnitPriceRangeByProductIdAsync", productSizes.Count);
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
            decimal discountAmount, long billNumber, string saleDescription, DateTime saleDate, 
            string paymentMethod = null, long? onlineAccountId = null)
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
                        command.Parameters.AddWithValue("@pPaymentMethod", paymentMethod ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pOnlineAccountId", onlineAccountId ?? (object)DBNull.Value);

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

        public async Task<long> ProcessOnlinePaymentTransactionAsync(long personalPaymentId, long saleId, decimal creditAmount, 
            string transactionDescription, long createdBy, DateTime? createdDate = null)
        {
            long transactionDetailId = 0;
            int returnValue = 0;
            
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("ProcessOnlinePaymentTransaction", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pPersonalPaymentId", personalPaymentId);
                        command.Parameters.AddWithValue("@pSaleId", saleId);
                        command.Parameters.AddWithValue("@pCreditAmount", creditAmount);
                        command.Parameters.AddWithValue("@pTransactionDescription", 
                            string.IsNullOrEmpty(transactionDescription) ? (object)DBNull.Value : transactionDescription);
                        command.Parameters.AddWithValue("@pCreatedBy", createdBy);
                        command.Parameters.AddWithValue("@pCreatedDate", createdDate ?? (object)DBNull.Value);
                        
                        var transactionDetailIdParam = new SqlParameter("@pPersonalPaymentSaleDetailId", SqlDbType.BigInt)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(transactionDetailIdParam);
                        
                        var returnValueParam = new SqlParameter("@pReturnValue", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(returnValueParam);
                        
                        await command.ExecuteNonQueryAsync();
                        
                        transactionDetailId = (long)transactionDetailIdParam.Value;
                        returnValue = (int)returnValueParam.Value;
                        
                        if (returnValue < 0)
                        {
                            _logger.LogError("Error processing online payment transaction. Return value: {ReturnValue}", returnValue);
                            throw new Exception($"Failed to process online payment transaction. Error code: {returnValue}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing online payment transaction for PersonalPaymentId: {PersonalPaymentId}, SaleId: {SaleId}", 
                    personalPaymentId, saleId);
                throw;
            }
            
            return transactionDetailId;
        }
    }
}
