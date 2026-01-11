using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Net.Mail;

namespace IMS.Services
{
    public class VendorService : IVendor
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<VendorService> _logger;

        public VendorService(IDbContextFactory dbContextFactory, ILogger<VendorService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<bool> CreateVendorAsync(AdminSupplier vendor)
        {
            bool response = false;
            try
            {
                
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("AddSupplier", connection))
                    {
                         
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pSupplierName", vendor.SupplierName);
                        command.Parameters.AddWithValue("@pSupplierDescription", vendor.SupplierDescription);
                        command.Parameters.AddWithValue("@pSupplierPhoneNumber", vendor.SupplierPhoneNumber);
                        command.Parameters.AddWithValue("@pSupplierNTN", vendor.SupplierNtn);
                        command.Parameters.AddWithValue("@pSupplierEmail", vendor?.SupplierEmail != null ? vendor?.SupplierEmail : DBNull.Value);
                        command.Parameters.AddWithValue("@pSupplierAddress", vendor?.SupplierAddress != null ? vendor?.SupplierAddress : DBNull.Value);
                        command.Parameters.AddWithValue("@pCreatedDate", vendor?.CreatedDate == default(DateTime) ? DBNull.Value : vendor?.CreatedDate);
                        command.Parameters.AddWithValue("@pModifiedDate", vendor?.ModifiedDate == default(DateTime) ? DBNull.Value : vendor?.ModifiedDate);
                        command.Parameters.AddWithValue("@pIsDeleted", vendor?.IsDeleted != null ? vendor?.IsDeleted: DBNull.Value);
                        command.Parameters.AddWithValue("@pCreatedBy", vendor?.CreatedBy != null ? vendor.CreatedBy : DBNull.Value);
                        command.Parameters.AddWithValue("@pModifiedBy", vendor?.ModifiedBy != null ? vendor.ModifiedBy : DBNull.Value);
                        
                        var VEndorIdParam = new SqlParameter("@pSupplierId", SqlDbType.BigInt)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(VEndorIdParam);
                        await command.ExecuteNonQueryAsync();
                        long newAdmincategoryIdParam = (long)VEndorIdParam.Value;
                        if (newAdmincategoryIdParam != 0)
                        {
                            response = true;
                        }

                    }
                }
            }
            catch
            {

            }
            return response;
        }

        public async Task<int> DeleteVendorAsync(long id)
        {
            int response = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("DeleteSupplier", connection))
                    {
                        command.Parameters.AddWithValue("@pSupplierId", id);
                        command.CommandType = CommandType.StoredProcedure;
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
            catch
            {

            }
            return response;
        }

        public async Task<AdminSupplier> GetVendorByIdAsync(long id)
        {
            AdminSupplier supplier = new AdminSupplier();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetSupplier", connection))
                    {
                        command.Parameters.AddWithValue("@pSupplierId", id);
                        command.CommandType = CommandType.StoredProcedure;
                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    return new AdminSupplier
                                    {
                                        SupplierId = reader.GetInt64(reader.GetOrdinal("SupplierId")),
                                        SupplierName=reader.GetString(reader.GetOrdinal("SupplierName")),
                                        SupplierDescription =reader.GetString(reader.GetOrdinal("SupplierDescription")),
                                        SupplierPhoneNumber=reader.GetString(reader.GetOrdinal("SupplierPhoneNumber")),
                                        SupplierNtn = reader.GetString(reader.GetOrdinal("SupplierNTN")),
                                        SupplierEmail=reader.GetString(reader.GetOrdinal("SupplierEmail")),
                                        SupplierAddress=reader.GetString(reader.GetOrdinal("SupplierAddress")),
                                        IsDeleted=reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                                        CreatedBy=reader.GetInt64(reader.GetOrdinal("CreatedBy")),
                                        CreatedDate= reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                        ModifiedBy= reader.GetInt64(reader.GetOrdinal("ModifiedBy")),
                                        ModifiedDate= reader.GetDateTime(reader.GetOrdinal("ModifiedDate"))
                                    };
                                }
                            }
                        }
                        catch
                        {

                        }

                    }
                }
            }
            catch
            {

            }
            return supplier;
        }

        public async Task<VendorViewModel> GetAllVendors(int pageNumber, int? pageSize, string? SupplierName, string? contactNo, string? NTN)
        {
            var vendor = new List<AdminSupplier>();
            int totalRecords = 0;
            try
            {
               
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetAllSupplier", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@PageNo", pageNumber);
                        command.Parameters.AddWithValue("@PageSize", pageSize);
                        command.Parameters.AddWithValue("@pSupplierName", (object)SupplierName ?? DBNull.Value);
                        command.Parameters.AddWithValue("@pContactNumber", (object)contactNo ?? DBNull.Value);
                        command.Parameters.AddWithValue("@pNTN", (object)NTN ?? DBNull.Value);
                        command.Parameters.AddWithValue("@pIsDeleted", DBNull.Value);
                        using (var reader = await command.ExecuteReaderAsync())
                        {

                            while (await reader.ReadAsync())
                            {
                                vendor.Add(new AdminSupplier
                                {
                                    SupplierId = reader.GetInt64(reader.GetOrdinal("SupplierId")),
                                    SupplierName = reader.GetString(reader.GetOrdinal("SupplierName")),
                                    SupplierPhoneNumber = reader.IsDBNull(reader.GetOrdinal("SupplierPhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("SupplierPhoneNumber")),
                                    SupplierNtn = reader.IsDBNull(reader.GetOrdinal("SupplierNTN")) ? null : reader.GetString(reader.GetOrdinal("SupplierNtn")),
                                    SupplierEmail = reader.IsDBNull(reader.GetOrdinal("SupplierEmail")) ? null : reader.GetString(reader.GetOrdinal("SupplierEmail")),
                                    SupplierAddress = reader.GetString(reader.GetOrdinal("SupplierAddress")),
                                    IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted"))
                                });
                            }
                            // Move to second result set for total count
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
                _logger.LogError(ex, "Error fetching customers");

            }



            return new VendorViewModel
            {
                VendorList = vendor,
                CurrentPage = pageNumber,
                TotalPages = pageSize.HasValue && pageSize.Value > 0
                   ? (int)Math.Ceiling(totalRecords / (double)pageSize.Value)
                   : 1,
                PageSize = pageSize,
                TotalCount = totalRecords
            };
        }

        public async Task<List<AdminSupplier>> GetAllEnabledVendors()
        {
            var vendor = new List<AdminSupplier>();
           
            try
            {

                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {

                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetAllEnabledSupplier", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                    
                        using (var reader = await command.ExecuteReaderAsync())
                        {

                            while (await reader.ReadAsync())
                            {
                                vendor.Add(new AdminSupplier
                                {
                                    SupplierId = reader.GetInt64(reader.GetOrdinal("SupplierId")),
                                    SupplierName = reader.GetString(reader.GetOrdinal("SupplierName")),
                                    
                                });
                            }
                           
                            

                        }
                    }



                }


            }
            catch 
            {
                

            }



            return vendor;
        }

        public async Task<int> UpdateVendorAsync(AdminSupplier customer)
        {
            var RowsAffectedResponse = 0;


            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                     


                    await connection.OpenAsync();
                    using (var command = new SqlCommand("ModifySupplier", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pSupplierName", customer.SupplierName);
                        command.Parameters.AddWithValue("@pSupplierDescription", customer.SupplierDescription);
                        command.Parameters.AddWithValue("@pSupplierPhoneNumber", customer.SupplierPhoneNumber);
                        command.Parameters.AddWithValue("@pSupplierNTN", customer.SupplierNtn);
                        command.Parameters.AddWithValue("@pSupplierEmail", customer.SupplierEmail);
                        command.Parameters.AddWithValue("@pSupplierAddress", customer.SupplierAddress);
                        command.Parameters.AddWithValue("@pIsDeleted", customer.IsDeleted);
                       
                        command.Parameters.AddWithValue("@pModifiedDate", customer?.ModifiedDate == default(DateTime) ? DBNull.Value : customer?.ModifiedDate);
                        command.Parameters.AddWithValue("@pModifiedBy", customer?.ModifiedBy);
                        command.Parameters.AddWithValue("@pSupplierId", customer?.SupplierId);

                        var expenseTypeidParam = new SqlParameter("@RowsAffected", SqlDbType.BigInt)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(expenseTypeidParam);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected != 0)
                        {
                            RowsAffectedResponse = rowsAffected;
                        }
                    }
                }
            }
            catch
            {
            }
            return RowsAffectedResponse;
        }

        // Bill Generation Methods
        public async Task<long> GetNextBillNumberAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetNextVendorBillNumber", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        var result = await command.ExecuteScalarAsync();
                        return result != null ? Convert.ToInt64(result) : 1;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next bill number");
                return 1;
            }
        }

        public async Task<List<Product>> GetAllEnabledProductsAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetAllEnabledProducts", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        var products = new List<Product>();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                products.Add(new Product
                                {
                                    ProductId = reader.GetInt64("ProductId"),
                                    ProductName = reader.GetString("ProductName"),
                                    ProductCode = reader.IsDBNull("ProductCode") ? null : reader.GetString("ProductCode"),
                                    UnitPrice = reader.GetDecimal("UnitPrice"),
                                    IsEnabled = reader.GetByte("IsEnabled")
                                });
                            }
                        }
                        return products;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enabled products");
                return new List<Product>();
            }
        }

        public async Task<List<ProductSizeViewModel>> GetProductUnitPriceRangeByProductIdAsync(long productId)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetProductUnitPriceRangeByProductId", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ProductId", productId);
                        
                        var productSizes = new List<ProductSizeViewModel>();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                productSizes.Add(new ProductSizeViewModel
                                {
                                    ProductRangeId = reader.GetInt64("ProductRangeId"),
                                    ProductId_FK = reader.GetInt64("ProductId_FK"),
                                    MeasuringUnitId_FK = reader.GetInt64("MeasuringUnitId_FK"),
                                    RangeFrom = reader.GetDecimal("RangeFrom"),
                                    RangeTo = reader.GetDecimal("RangeTo"),
                                    UnitPrice = reader.GetDecimal("UnitPrice"),
                                    MeasuringUnitName = reader.IsDBNull("MeasuringUnitName") ? null : reader.GetString("MeasuringUnitName"),
                                    MeasuringUnitAbbreviation = reader.IsDBNull("MeasuringUnitAbbreviation") ? null : reader.GetString("MeasuringUnitAbbreviation")
                                });
                            }
                        }
                        return productSizes;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product unit price ranges for product {ProductId}", productId);
                return new List<ProductSizeViewModel>();
            }
        }

        public async Task<StockMaster?> GetStockByProductIdAsync(long productId)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetStockByProductId", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pProductId", productId);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new StockMaster
                                {
                                    StockMasterId = reader.GetInt64("StockMasterId"),
                                    ProductIdFk = reader.GetInt64("ProductId_FK"),
                                    AvailableQuantity = reader.GetDecimal("AvailableQuantity"),
                                    TotalQuantity = reader.GetDecimal("TotalQuantity"),
                                    UsedQuantity = reader.GetDecimal("UsedQuantity")
                                };
                            }
                        }
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock for product {ProductId}", productId);
                return null;
            }
        }

        public async Task<decimal> GetPreviousDueAmountByVendorIdAsync(long vendorId)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetPreviousDueAmountByVendorId", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@VendorId", vendorId);
                        
                        var result = await command.ExecuteScalarAsync();
                        return result != null ? Convert.ToDecimal(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting previous due amount for vendor {VendorId}", vendorId);
                return 0;
            }
        }

        public async Task<long> CreateVendorBillAsync(decimal totalAmount, decimal paidAmount, decimal dueAmount, long vendorId, DateTime createdDate, long createdBy, DateTime modifiedDate, long modifiedBy, decimal discountAmount, long billNumber, string description, DateTime billDate, string? paymentMethod, long? onlineAccountId)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    //using (var command = new SqlCommand("CreateVendorBill", connection))
                    //{
                    //    command.CommandType = CommandType.StoredProcedure;
                    //    command.Parameters.AddWithValue("@TotalAmount", totalAmount);
                    //    command.Parameters.AddWithValue("@PaidAmount", paidAmount);
                    //    command.Parameters.AddWithValue("@DueAmount", dueAmount);
                    //    command.Parameters.AddWithValue("@VendorId", vendorId);
                    //    command.Parameters.AddWithValue("@CreatedDate", createdDate);
                    //    command.Parameters.AddWithValue("@CreatedBy", createdBy);
                    //    command.Parameters.AddWithValue("@ModifiedDate", modifiedDate);
                    //    command.Parameters.AddWithValue("@ModifiedBy", modifiedBy);
                    //    command.Parameters.AddWithValue("@DiscountAmount", discountAmount);
                    //    command.Parameters.AddWithValue("@BillNumber", billNumber);
                    //    command.Parameters.AddWithValue("@Description", description);
                    //    command.Parameters.AddWithValue("@BillDate", billDate);
                    //    command.Parameters.AddWithValue("@PaymentMethod", paymentMethod ?? (object)DBNull.Value);
                    //    command.Parameters.AddWithValue("@OnlineAccountId", onlineAccountId ?? (object)DBNull.Value);

                    //    var billIdParam = new SqlParameter("@BillId", SqlDbType.BigInt)
                    //    {
                    //        Direction = ParameterDirection.Output
                    //    };
                    //    command.Parameters.Add(billIdParam);

                    //    await command.ExecuteNonQueryAsync();
                    //    return (long)billIdParam.Value;
                    //}
                    // Create the bill using AddBill stored procedure
                    using (var command = new SqlCommand("AddBill", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pTotalAmount", totalAmount);
                        command.Parameters.AddWithValue("@pTotalReceivedAmount", paidAmount);
                        command.Parameters.AddWithValue("@pTotalDueAmount", dueAmount);
                        command.Parameters.AddWithValue("@pSupplierId_FK", vendorId);
                        command.Parameters.AddWithValue("@pCreatedDate", createdDate);
                        command.Parameters.AddWithValue("@pCreatedBy", createdBy == null ? DBNull.Value : createdBy);
                        command.Parameters.AddWithValue("@pModifiedDate", createdDate);
                        command.Parameters.AddWithValue("@pModifiedBy", createdBy == null ? createdBy : createdBy);
                        command.Parameters.AddWithValue("@pDiscountAmount", discountAmount);
                        command.Parameters.AddWithValue("@pBillNumber", billNumber);
                        command.Parameters.AddWithValue("@pBillDescription", description ?? "");
                        command.Parameters.AddWithValue("@pBillDate", billDate);
                        command.Parameters.AddWithValue("@PaymentMethod", paymentMethod ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@OnlineAccountId", onlineAccountId ?? (object)DBNull.Value);

                        // Output parameter for BillId
                        var billIdParam = new SqlParameter("@pBillId", SqlDbType.BigInt)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(billIdParam);

                        await command.ExecuteNonQueryAsync();
                        return (long)billIdParam.Value;
                    }


                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vendor bill");
                return 0;
            }
        }

        public async Task<long> AddVendorBillDetails(long billId, long productId, decimal unitPrice, decimal purchasePrice, decimal quantity, decimal salePrice, decimal lineDiscountAmount, decimal payableAmount, long productRangeId)
        {
       
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    connection.Open();
                    using (var itemCommand = new SqlCommand("AddBillDetails", connection))
                    {
                        itemCommand.CommandType = CommandType.StoredProcedure;
                        itemCommand.Parameters.AddWithValue("@pBillId_FK", billId);
                        itemCommand.Parameters.AddWithValue("@pPrductId_FK", productId);
                        itemCommand.Parameters.AddWithValue("@pUnitPrice", unitPrice);
                        // Convert decimal quantity to long for database (round to nearest whole number)
                        // Note: Database stores as long, but we accept decimal for precision during conversion
                        itemCommand.Parameters.AddWithValue("@pQuantity", (long)Math.Round(quantity, MidpointRounding.AwayFromZero));
                        itemCommand.Parameters.AddWithValue("@pPurchasePrice", purchasePrice);
                        itemCommand.Parameters.AddWithValue("@pLineDiscountAmount", lineDiscountAmount);
                        itemCommand.Parameters.AddWithValue("@pPayableAmount", payableAmount);
                        itemCommand.Parameters.AddWithValue("@ProductRangeId_FK", productRangeId);

                        // Output parameter for BillDetailsId
                        var billDetailsIdParam = new SqlParameter("@pBillDetailsId", SqlDbType.BigInt)
                        {
                            Direction = ParameterDirection.Output
                        };
                        itemCommand.Parameters.Add(billDetailsIdParam);

                        await itemCommand.ExecuteNonQueryAsync();
                        
                        return (long)billDetailsIdParam.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding vendor bill details");
                return 0;
            }
        }

        public long UpdateStock(long stockMasterId, long productId, decimal availableQuantity, decimal totalQuantity, decimal usedQuantity, long modifiedBy, DateTime modifiedDate)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    connection.Open();
                    using (var command = new SqlCommand("UpdateStock", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pStockMasterId", stockMasterId);
                        command.Parameters.AddWithValue("@pProductId", productId);
                        command.Parameters.AddWithValue("@pAvailableQuantity", availableQuantity);
                        command.Parameters.AddWithValue("@TotalQuantity", totalQuantity);
                        command.Parameters.AddWithValue("@pUsedQuantity", usedQuantity);
                        command.Parameters.AddWithValue("@pModifiedBy", modifiedBy);
                        command.Parameters.AddWithValue("@pModifiedDate", modifiedDate);

                        //var returnValueParam = new SqlParameter("@ReturnValue", SqlDbType.Int)
                        //{
                        //    Direction = ParameterDirection.Output
                        //};
                        //command.Parameters.Add(returnValueParam);

                        command.ExecuteNonQuery();
                        return 1;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock");
                return 0;
            }
        }

        public long VendorBillTransactionCreate(long stockMasterId, decimal quantity, string description, DateTime transactionDate, long createdBy, int transactionType, long billId)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    connection.Open();
                    using (var command = new SqlCommand("CreateStockTransaction", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pStockMasterId", stockMasterId);
                        command.Parameters.AddWithValue("@pQuantity", quantity);
                        command.Parameters.AddWithValue("@pComment", description);
                        command.Parameters.AddWithValue("@pCreatedDate", transactionDate);
                        command.Parameters.AddWithValue("@pCreatedBy", createdBy);
                        command.Parameters.AddWithValue("@pTransactionStatusId", 3);
                        
                        command.Parameters.AddWithValue("@purchaseOrderId", billId);

                        var transactionIdParam = new SqlParameter("@transactionId", SqlDbType.BigInt)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(transactionIdParam);

                        command.ExecuteNonQuery();
                        return (long)transactionIdParam.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vendor bill transaction");
                return 0;
            }
        }

        public async Task<long> ProcessOnlinePaymentTransactionAsync(long personalPaymentId, long billId, decimal amount, string description, long createdBy, DateTime createdDate)
        {
            long transactionDetailId = 0;
            int returnValue = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("ProcessOnlinePurchaseTransaction", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pPersonalPaymentId", personalPaymentId);
                        command.Parameters.AddWithValue("@pPurchaseId", billId);
                        command.Parameters.AddWithValue("@pDebitAmount", amount);
                        command.Parameters.AddWithValue("@pTransactionDescription", description);
                        command.Parameters.AddWithValue("@pCreatedBy", createdBy);
                        command.Parameters.AddWithValue("@pCreatedDate", createdDate);

                        var transactionIdParam = new SqlParameter("@pPersonalPaymentPurchaseDetailId", SqlDbType.BigInt)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(transactionIdParam);
                        var returnValueParam = new SqlParameter("@pReturnValue", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(returnValueParam);
                        await command.ExecuteNonQueryAsync();
                        transactionDetailId = (long)transactionIdParam.Value;
                        returnValue = (int)returnValueParam.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing online payment transaction");
                return 0;
            }
            return transactionDetailId;
        }

        public async Task<VendorBillPrintViewModel?> GetVendorBillForPrintAsync(long billId)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetVendorBillForPrint", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@BillId", billId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var bill = new VendorBillPrintViewModel
                                {
                                    BillId = reader.GetInt64("BillId"),
                                    BillNumber = reader.GetInt64("BillNumber"),
                                    BillDate = reader.GetDateTime("BillDate"),
                                    TotalAmount = reader.GetDecimal("TotalAmount"),
                                    DiscountAmount = reader.GetDecimal("DiscountAmount"),
                                    PaidAmount = reader.GetDecimal("PaidAmount"),
                                    DueAmount = reader.GetDecimal("DueAmount"),
                                    VendorIdFk = reader.GetInt64("VendorId"),
                                    VendorName = reader.GetString("VendorName"),
                                    BillDescription = reader.IsDBNull("Description") ? null : reader.GetString("Description")
                                };

                                // Get bill details
                                reader.NextResult();
                                var billDetails = new List<VendorBillDetailPrintViewModel>();
                                while (await reader.ReadAsync())
                                {
                                    billDetails.Add(new VendorBillDetailPrintViewModel
                                    {
                                        ProductId = reader.GetInt64("ProductId"),
                                        ProductName = reader.GetString("ProductName"),
                                        UnitPrice = reader.GetDecimal("UnitPrice"),
                                        PurchasePrice = reader.GetDecimal("PurchasePrice"),
                                        Quantity = reader.GetInt64("Quantity"),
                                        SalePrice = reader.GetDecimal("SalePrice"),
                                        LineDiscountAmount = reader.GetDecimal("LineDiscountAmount"),
                                        PayableAmount = reader.GetDecimal("PayableAmount"),
                                        ProductRangeId = reader.GetInt64("ProductRangeId")
                                    });
                                }
                                bill.BillDetails = billDetails;
                                return bill;
                            }
                        }
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vendor bill for print");
                return null;
            }
        }

        public async Task<PersonalPaymentViewModel> GetAllPersonalPaymentsAsync(int pageNumber, int pageSize, PersonalPaymentFilters filters)
        {
            PersonalPaymentViewModel personalPaymentViewModel = new PersonalPaymentViewModel();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetAllPersonalPayments", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@PageNumber", pageNumber);
                        command.Parameters.AddWithValue("@PageSize", pageSize);
                        command.Parameters.AddWithValue("@IsActive", filters.IsActive ?? (object)DBNull.Value);

                        var payments = new List<PersonalPaymentModel>();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                payments.Add(new PersonalPaymentModel
                                {
                                    PersonalPaymentId = reader.GetInt64("PersonalPaymentId"),
                                    BankName = reader.GetString("BankName"),
                                    AccountNumber = reader.GetString("AccountNumber"),
                                    AccountHolderName = reader.GetString("AccountHolderName"),
                                    IsActive = reader.GetBoolean("IsActive")
                                });
                            }
                        }
                        personalPaymentViewModel.PersonalPaymentList = payments;
                       
                    }
                }
                return 
                    personalPaymentViewModel;
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting personal payments");
                return personalPaymentViewModel;
            }
        }
    }
}
