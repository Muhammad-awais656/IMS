using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMS.Services
{
    public class VendorBillsService : IVendorBillsService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<VendorBillsService> _logger;
        private readonly IVendor _vendorService;

        public VendorBillsService(IDbContextFactory dbContextFactory, ILogger<VendorBillsService> logger, IVendor vendorService)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _vendorService = vendorService;
        }

        public async Task<VendorBillsViewModel> GetAllBillsAsync(int pageNumber, int? pageSize, VendorBillsFilters? filters)
        {
            var viewModel = new VendorBillsViewModel();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("GetAllBills", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                        // Set parameters for the stored procedure
                        command.Parameters.AddWithValue("@PageNo", pageNumber);
                        command.Parameters.AddWithValue("@PageSize", pageSize ?? 10);
                        command.Parameters.AddWithValue("@pIsDeleted", false);
                        command.Parameters.AddWithValue("@pSupplierId", filters?.VendorId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pBillNumber", filters?.BillNumber ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pBillFrom", filters?.BillDateFrom ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pBillDateTo", filters?.BillDateTo ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pDescription", filters?.Description ?? (object)DBNull.Value);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var billsList = new List<VendorBillViewModel>();
                            decimal totalAmount = 0;
                            decimal totalDiscountAmount = 0;
                            decimal totalPaidAmount = 0;
                            decimal totalPayableAmount = 0;
                            int totalRecords = 0;

                            // Read first result set - paginated bills data
                            while (await reader.ReadAsync())
                            {
                                var bill = new VendorBillViewModel
                                {
                                    BillId = reader.IsDBNull(reader.GetOrdinal("PurchaseOrderId")) ? 0 : reader.GetInt64(reader.GetOrdinal("PurchaseOrderId")),
                                    BillNumber = reader.IsDBNull(reader.GetOrdinal("BillNumber")) ? 0 : reader.GetInt64(reader.GetOrdinal("BillNumber")),
                                    VendorName = reader.IsDBNull(reader.GetOrdinal("SupplierName")) ? string.Empty : reader.GetString(reader.GetOrdinal("SupplierName")),
                                    BillDate = reader.IsDBNull(reader.GetOrdinal("CreatedDate")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                    TotalAmount = reader.IsDBNull(reader.GetOrdinal("TotalAmount")) ? 0 : reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                                    DiscountAmount = reader.IsDBNull(reader.GetOrdinal("DiscountAmount")) ? 0 : reader.GetDecimal(reader.GetOrdinal("DiscountAmount")),
                                    PaidAmount = reader.IsDBNull(reader.GetOrdinal("TotalReceivedAmount")) ? 0 : reader.GetDecimal(reader.GetOrdinal("TotalReceivedAmount")),
                                    TotalPayableAmount = reader.IsDBNull(reader.GetOrdinal("TotalDueAmount")) ? 0 : reader.GetDecimal(reader.GetOrdinal("TotalDueAmount")),
                                    Description = reader.IsDBNull(reader.GetOrdinal("PurchaseOrderDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("PurchaseOrderDescription")),
                                    PaymentMethod = reader.IsDBNull(reader.GetOrdinal("PaymentMethod")) ? string.Empty : reader.GetString(reader.GetOrdinal("PaymentMethod"))
                                };

                                billsList.Add(bill);

                                // Calculate totals
                                totalAmount += bill.TotalAmount;
                                totalDiscountAmount += bill.DiscountAmount;
                                totalPaidAmount += bill.PaidAmount;
                                totalPayableAmount += bill.TotalPayableAmount;
                            }

                            // Move to second result set - total count
                            await reader.NextResultAsync();
                            if (await reader.ReadAsync())
                            {
                                totalRecords = reader.GetInt32(reader.GetOrdinal("TotalRecords"));
                            }

                            viewModel.BillsList = billsList;
                            viewModel.TotalAmount = totalAmount;
                            viewModel.TotalDiscountAmount = totalDiscountAmount;
                            viewModel.TotalPaidAmount = totalPaidAmount;
                            viewModel.TotalPayableAmount = totalPayableAmount;
                            viewModel.CurrentPage = pageNumber;
                            viewModel.PageSize = pageSize ?? 10;
                            viewModel.TotalCount = totalRecords;
                            viewModel.TotalPages = (int)Math.Ceiling((double)totalRecords / (pageSize ?? 10));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all bills");
                throw;
            }
            return viewModel;
        }

        public async Task<List<AdminSupplier>> GetAllVendorsAsync()
        {
            var vendors = new List<AdminSupplier>();
            int totalRecords = 0;
            try
            {
                _logger.LogInformation("Getting connection string...");
                var connectionString = _dbContextFactory.DBConnectionString();
                _logger.LogInformation("Connection string retrieved successfully");
                
                using (var connection = new SqlConnection(connectionString))
                {
                    _logger.LogInformation("Opening database connection...");
                    await connection.OpenAsync();
                    _logger.LogInformation("Database connection opened successfully");
                    
                    using (var command = new SqlCommand("GetAllSupplier", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@PageNo", 1);
                        command.Parameters.AddWithValue("@PageSize", 1000); // Get all vendors for dropdown
                        command.Parameters.AddWithValue("@pSupplierName", DBNull.Value);
                        command.Parameters.AddWithValue("@pContactNumber", DBNull.Value);
                        command.Parameters.AddWithValue("@pNTN", DBNull.Value);
                        command.Parameters.AddWithValue("@pIsDeleted", false);
                        
                        _logger.LogInformation("Executing GetAllSupplier stored procedure...");
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var vendor = new AdminSupplier
                                {
                                    SupplierId = reader.GetInt64("SupplierId"),
                                    SupplierName = reader.GetString("SupplierName")
                                };
                                vendors.Add(vendor);
                            }
                            
                            // Move to second result set (total count) - we don't need it for this method
                            await reader.NextResultAsync();

                            if (await reader.ReadAsync())
                            {
                                totalRecords = reader.GetInt32(reader.GetOrdinal("TotalRecords"));
                            }
                        }
                        _logger.LogInformation("Retrieved {VendorCount} vendors from database", vendors.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all vendors: {ErrorMessage}", ex.Message);
                throw;
            }
            return vendors;
        }

        public async Task<List<SupplierBillNumber>> GetSupplierBillNumbersAsync(long supplierId)
        {
            var billNumbers = new List<SupplierBillNumber>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("GetAllSupplierBillNumbers", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pSupplierId", supplierId);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var billNumber = new SupplierBillNumber
                                {
                                    BillNumber = reader.GetInt64("BillNumber"),
                                    PurchaseOrderId = reader.GetInt64("PurchaseOrderId"),
                                    TotalDueAmount = reader.GetDecimal("TotalDueAmount")
                                };
                                billNumbers.Add(billNumber);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting supplier bill numbers");
                throw;
            }
            return billNumbers;
        }

        public async Task<BillGenerationViewModel> GetBillGenerationDataAsync(long? vendorId = null)
        {
            var viewModel = new BillGenerationViewModel();
            try
            {
                // Get all vendors
                viewModel.VendorList = await GetAllVendorsAsync();

                // If vendor is selected, get vendor-specific data
                if (vendorId.HasValue)
                {
                    viewModel.SelectedVendorId = vendorId.Value;
                    var selectedVendor = viewModel.VendorList.FirstOrDefault(v => v.SupplierId == vendorId.Value);
                    if (selectedVendor != null)
                    {
                        viewModel.SelectedVendorName = selectedVendor.SupplierName;
                    }

                    // Get next bill number
                    viewModel.BillNumber = await GetNextBillNumberAsync(vendorId.Value);

                    // Get vendor products
                    viewModel.ProductList = await GetVendorProductsAsync(vendorId.Value);

                    // Get previous due amount
                    viewModel.PreviousDue = await GetPreviousDueAmountAsync(null, vendorId.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bill generation data");
                throw;
            }
            return viewModel;
        }

        public async Task<long> GetNextBillNumberAsync(long supplierId)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("GetPOBillNumber", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pSupplierId", supplierId);

                        var result = await command.ExecuteScalarAsync();
                        return result != null ? Convert.ToInt64(result)+1 : 1;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next bill number for supplier {SupplierId}", supplierId);
                return 1; // Default to 1 if error occurs
            }
        }

        public async Task<List<Product>> GetVendorProductsAsync(long supplierId)
        {
            var products = new List<Product>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("GetAllProducts", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pIsEnabled", true);
                        command.Parameters.AddWithValue("@pProductName", DBNull.Value);
                        command.Parameters.AddWithValue("@pProductCode", DBNull.Value);
                        command.Parameters.AddWithValue("@pPriceFrom", DBNull.Value);
                        command.Parameters.AddWithValue("@pPriceTo", DBNull.Value);
                        command.Parameters.AddWithValue("@pSizeId", DBNull.Value);
                        command.Parameters.AddWithValue("@pCategoryId", DBNull.Value);
                        command.Parameters.AddWithValue("@plabelId", DBNull.Value);
                        command.Parameters.AddWithValue("@pMeasuringUnitTypeId", DBNull.Value);
                        command.Parameters.AddWithValue("@SupplierId", supplierId);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var product = new Product
                                {
                                    ProductId = reader.GetInt64("ProductId"),
                                    ProductName = reader.GetString("ProductName"),
                                    ProductCode = reader.GetString("ProductCode")
                                };
                                products.Add(product);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vendor products for supplier {SupplierId}", supplierId);
                throw;
            }
            return products;
        }

        public async Task<List<ProductRange>> GetProductUnitPriceRangesAsync(long productId)
        {
            var productRanges = new List<ProductRange>();
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
                                var productRange = new ProductRange
                                {
                                    ProductRangeId = reader.GetInt64("ProductRangeId"),
                                    ProductIdFk = reader.GetInt64("ProductIdFk"),
                                    MeasuringUnitIdFk = reader.GetInt64("MeasuringUnitIdFk"),
                                    RangeFrom = reader.GetDecimal("RangeFrom"),
                                    RangeTo = reader.GetDecimal("RangeTo"),
                                    UnitPrice = reader.GetDecimal("UnitPrice")
                                };
                                productRanges.Add(productRange);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product unit price ranges for product {ProductId}", productId);
                throw;
            }
            return productRanges;
        }

        public async Task<decimal> GetPreviousDueAmountAsync(long? billId, long vendorId)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand(@"
                        SELECT ISNULL(SUM(TotalDueAmount), 0) AS PreviousDueAmount
                        FROM PurchaseOrders
                        WHERE SupplierId_FK = @vendorId
                        AND IsDeleted = 0
                        AND (@billId IS NULL OR BillId != @billId)", connection))
                    {
                        command.Parameters.AddWithValue("@vendorId", vendorId);
                        command.Parameters.AddWithValue("@billId", billId ?? (object)DBNull.Value);

                        var result = await command.ExecuteScalarAsync();
                        return result != null ? Convert.ToDecimal(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting previous due amount for bill {BillId} and vendor {VendorId}", billId, vendorId);
                return 0;
            }
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            var products = new List<Product>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetAllEnabledProducts", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                products.Add(new Product
                                {
                                    ProductId = reader.GetInt64("ProductId"),
                                    ProductCode = reader.GetString("ProductCode"),
                                    ProductName = reader.GetString("ProductName")
                                });
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

        public async Task<List<ProductRange>> GetProductSizesAsync(long productId)
        {
            var productSizes = new List<ProductRange>();
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
                                productSizes.Add(new ProductRange
                                {
                                    ProductRangeId = reader.GetInt64("ProductRangeId"),
                                    UnitPrice = reader.GetDecimal("UnitPrice"),
                                    RangeFrom = reader.GetDecimal("RangeFrom"),
                                    RangeTo = reader.GetDecimal("RangeTo"),
                                    MeasuringUnitIdFk = reader.GetInt64("MeasuringUnitId_FK"),
                                    MeasuringUnitName = reader.IsDBNull("MeasuringUnitName") ? "Unit" : reader.GetString("MeasuringUnitName"),
                                    MeasuringUnitAbbreviation = reader.IsDBNull("MeasuringUnitAbbreviation") ? "U" : reader.GetString("MeasuringUnitAbbreviation")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product sizes for product {ProductId}", productId);
                throw;
            }
            return productSizes;
        }

        public async Task<decimal> GetPreviousDueAmountAsync(long vendorId)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("GetPreviousDueAmountByBillId", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pVendorId", vendorId);
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

        public async Task<VendorBillViewModel?> GetVendorBillByIdAsync(long billId)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetBillByBillId", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pBillId", billId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var paymentMethod = "";
                                try
                                {
                                    if (!reader.IsDBNull(reader.GetOrdinal("PaymentMethod")))
                                    {
                                        paymentMethod = reader.GetString("PaymentMethod");
                                    }
                                }
                                catch
                                {
                                    paymentMethod = "";
                                }
                                
                                long? onlineAccountId = null;
                                try
                                {
                                    if (!reader.IsDBNull(reader.GetOrdinal("OnlineAccountId")))
                                    {
                                        onlineAccountId = reader.GetInt64("OnlineAccountId");
                                    }
                                }
                                catch
                                {
                                    onlineAccountId = null;
                                }
                                
                                return new VendorBillViewModel
                                {
                                    BillId = reader.GetInt64("PurchaseOrderId"),
                                    VendorId = reader.GetInt64("SupplierId_FK"),
                                    VendorName = reader.GetString("VendorName"),
                                    BillNumber = reader.GetInt64("BillNumber"),
                                    BillDate = reader.GetDateTime("PurchaseOrderDate"),
                                    TotalAmount = reader.GetDecimal("TotalAmount"),
                                    DiscountAmount = reader.GetDecimal("DiscountAmount"),
                                    PaidAmount = reader.GetDecimal("TotalReceivedAmount"),
                                    DueAmount = reader.GetDecimal("TotalDueAmount"),
                                    Description = reader.IsDBNull("PurchaseOrderDescription") ? "" : reader.GetString("PurchaseOrderDescription"),
                                    PaymentMethod = paymentMethod,
                                    OnlineAccountId = onlineAccountId
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vendor bill by ID {BillId}", billId);
                throw;
            }
            return null;
        }


        public async Task<BillPayment?> GetVendorBillByPaymentIdAsync(long paymentId)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetBillPaymentByPaymentId", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@paymentId", paymentId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var paymentMethod = "";
                                try
                                {
                                    if (!reader.IsDBNull(reader.GetOrdinal("PaymentMethod")))
                                    {
                                        paymentMethod = reader.GetString("PaymentMethod");
                                    }
                                }
                                catch
                                {
                                    paymentMethod = "";
                                }

                                long? onlineAccountId = null;
                                try
                                {
                                    if (!reader.IsDBNull(reader.GetOrdinal("onlineAccountId")))
                                    {
                                        onlineAccountId = reader.GetInt64("onlineAccountId");
                                    }
                                }
                                catch
                                {
                                    onlineAccountId = null;
                                }

                                return new BillPayment
                                {
                                    PaymentId = reader.GetInt64("PaymentId"),
                                    PaymentAmount = reader.GetDecimal("PaymentAmount"),
                                    BillId = reader.GetInt64("BillId"),
                                    SupplierIdFk = reader.GetInt64("SupplierId_FK"),
                                    Description = reader.IsDBNull("Description") ? "" : reader.GetString("Description"),
                                    PaymentDate =  reader.GetDateTime("PaymentDate"),
                                    PaymentMethod = paymentMethod,
                                    onlineAccountId = onlineAccountId
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vendor bill by ID {BillId}", paymentId);
                throw;
            }
            return null;
        }

        public async Task<List<BillItemViewModel>> GetVendorBillItemsAsync(long billId)
        {
            var billItems = new List<BillItemViewModel>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetBillDetailsByBillId", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pBillId", billId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var productCode = reader.IsDBNull(reader.GetOrdinal("ProductCode")) ? "" : reader.GetString("ProductCode");
                                var rangeFrom = reader.IsDBNull(reader.GetOrdinal("RangeFrom")) ? 0 : reader.GetDecimal("RangeFrom");
                                var rangeTo = reader.IsDBNull(reader.GetOrdinal("RangeTo")) ? 0 : reader.GetDecimal("RangeTo");
                                var productSize = "";
                                if (rangeFrom > 0 || rangeTo > 0)
                                {
                                    productSize = rangeFrom.ToString("0.##") + " - " + rangeTo.ToString("0.##");
                                }
                                
                                var measuringUnitAbbreviation = "";
                                try
                                {
                                    if (!reader.IsDBNull(reader.GetOrdinal("MeasuringUnitAbbreviation")))
                                    {
                                        measuringUnitAbbreviation = reader.GetString("MeasuringUnitAbbreviation");
                                    }
                                }
                                catch
                                {
                                    measuringUnitAbbreviation = "";
                                }
                                
                                billItems.Add(new BillItemViewModel
                                {
                                    BillItemId = reader.GetInt64("PurchaseOrderId_FK"),
                                    ProductId = reader.GetInt64("PrductId_FK"),
                                    ProductRangeId = reader.GetInt64("ProductRangeId_FK"),
                                    UnitPrice = reader.GetDecimal("UnitPrice"),
                                    Quantity = reader.GetInt64("Quantity"),
                                    DiscountAmount = reader.GetDecimal("LineDiscountAmount"),
                                    PayableAmount = reader.GetDecimal("PayableAmount"),
                                    ProductName = reader.IsDBNull("ProductName") ? "" : reader.GetString("ProductName"),
                                    ProductCode = productCode,
                                    ProductSize = productSize,
                                    MeasuringUnitAbbreviation = measuringUnitAbbreviation
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vendor bill items for bill {BillId}", billId);
                throw;
            }
            return billItems;
        }

        public async Task<bool> UpdateVendorBillAsync(long billId, VendorBillGenerationViewModel model)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Step 1: Update the main bill using UpdateBill stored procedure
                            using (var command = new SqlCommand("UpdateBill", connection, transaction))
                            {
                                command.CommandType = CommandType.StoredProcedure;
                                command.Parameters.AddWithValue("@pBillId", billId);
                                command.Parameters.AddWithValue("@pTotalAmount", model.TotalAmount);
                                command.Parameters.AddWithValue("@pTotalReceivedAmount", model.PaidAmount);
                                command.Parameters.AddWithValue("@pTotalDueAmount", model.DueAmount);
                                command.Parameters.AddWithValue("@pSupplierId_FK", model.VendorId);
                                command.Parameters.AddWithValue("@pModifiedDate", model.ModifiedDate == default(DateTime) ? DateTime.Now : model.ModifiedDate);
                                command.Parameters.AddWithValue("@pModifiedBy", model.ModifiedBy);
                                command.Parameters.AddWithValue("@pDiscountAmount", model.DiscountAmount);
                                command.Parameters.AddWithValue("@pBillNumber", model.BillNumber);
                                command.Parameters.AddWithValue("@pBillDescription", model.Description ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@pBillDate", model.BillDate);

                                await command.ExecuteNonQueryAsync();
                            }

                            // Step 2: Delete transactions using TransactionsDelete stored procedure
                            using (var command = new SqlCommand("TransactionsDelete", connection, transaction))
                            {
                                command.CommandType = CommandType.StoredProcedure;
                                command.Parameters.AddWithValue("@pSaleId", billId);
                                await command.ExecuteNonQueryAsync();
                            }

                            // Step 3: Delete existing bill details using DeleteBillDetailByBillId stored procedure
                            using (var command = new SqlCommand("DeleteBillDetailByBillId", connection, transaction))
                            {
                                command.CommandType = CommandType.StoredProcedure;
                                command.Parameters.AddWithValue("@pBillId", billId);
                                await command.ExecuteNonQueryAsync();
                            }

                            // Step 4: Add new bill details using AddBillDetails stored procedure
                            foreach (var item in model.BillDetails)
                            {
                                using (var command = new SqlCommand("AddBillDetails", connection, transaction))
                                {
                                    command.CommandType = CommandType.StoredProcedure;
                                    command.Parameters.AddWithValue("@pBillId_FK", billId);
                                    command.Parameters.AddWithValue("@pPrductId_FK", item.ProductId);
                                    command.Parameters.AddWithValue("@pUnitPrice", item.UnitPrice);
                                    command.Parameters.AddWithValue("@pQuantity", item.Quantity);
                                    command.Parameters.AddWithValue("@pPurchasePrice", item.PurchasePrice);
                                    command.Parameters.AddWithValue("@pLineDiscountAmount", item.LineDiscountAmount);
                                    command.Parameters.AddWithValue("@pPayableAmount", item.PayableAmount);
                                    command.Parameters.AddWithValue("@ProductRangeId_FK", item.ProductRangeId);
                                    
                                    // Output parameter for BillDetailsId
                                    var billDetailsIdParam = new SqlParameter("@pBillDetailsId", SqlDbType.BigInt)
                                    {
                                        Direction = ParameterDirection.Output
                                    };
                                    command.Parameters.Add(billDetailsIdParam);

                                    await command.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.LogError(ex, "Error updating vendor bill {BillId} in transaction", billId);
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vendor bill {BillId}", billId);
                return false;
            }
        }

        public async Task<long> CreateBillAsync(GenerateBillViewModel model)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            long billId = 0;
                            long paymentId = 0;
                            var createdDate = DateTime.Now;
                            var createdBy = 1; // TODO: Get from session

                            // Create the bill using AddBill stored procedure
                            using (var command = new SqlCommand("AddBill", connection, transaction))
                            {
                                command.CommandType = CommandType.StoredProcedure;
                                command.Parameters.AddWithValue("@pTotalAmount", model.TotalAmount);
                                command.Parameters.AddWithValue("@pTotalReceivedAmount", model.PaidAmount);
                                command.Parameters.AddWithValue("@pTotalDueAmount", model.DueAmount);
                                command.Parameters.AddWithValue("@pSupplierId_FK", model.VendorId);
                                command.Parameters.AddWithValue("@pCreatedDate", createdDate);
                                command.Parameters.AddWithValue("@pCreatedBy", model.CreatedBy==null ? createdBy : model.CreatedBy);
                                command.Parameters.AddWithValue("@pModifiedDate", createdDate);
                                command.Parameters.AddWithValue("@pModifiedBy", model.CreatedBy == null ? createdBy : model.CreatedBy);
                                command.Parameters.AddWithValue("@pDiscountAmount", model.DiscountAmount);
                                command.Parameters.AddWithValue("@pBillNumber", model.BillNumber);
                                command.Parameters.AddWithValue("@pBillDescription", model.Description ?? "");
                                command.Parameters.AddWithValue("@pBillDate", model.BillDate);
                                
                                // Output parameter for BillId
                                var billIdParam = new SqlParameter("@pBillId", SqlDbType.BigInt)
                                {
                                    Direction = ParameterDirection.Output
                                };
                                command.Parameters.Add(billIdParam);

                                await command.ExecuteNonQueryAsync();
                                billId = Convert.ToInt64(billIdParam.Value);
                            }

                            // Add bill items using AddBillDetails stored procedure
                            foreach (var item in model.BillItems)
                            {
                                using (var itemCommand = new SqlCommand("AddBillDetails", connection, transaction))
                                {
                                    itemCommand.CommandType = CommandType.StoredProcedure;
                                    itemCommand.Parameters.AddWithValue("@pBillId_FK", billId);
                                    itemCommand.Parameters.AddWithValue("@pPrductId_FK", item.ProductId);
                                    itemCommand.Parameters.AddWithValue("@pUnitPrice", item.UnitPrice);
                                    itemCommand.Parameters.AddWithValue("@pQuantity", (int)item.Quantity);
                                    itemCommand.Parameters.AddWithValue("@pPurchasePrice", item.BillPrice);
                                    itemCommand.Parameters.AddWithValue("@pLineDiscountAmount", item.DiscountAmount);
                                    itemCommand.Parameters.AddWithValue("@pPayableAmount", item.PayableAmount);
                                    itemCommand.Parameters.AddWithValue("@ProductRangeId_FK", item.ProductRangeId);
                                    
                                    // Output parameter for BillDetailsId
                                    var billDetailsIdParam = new SqlParameter("@pBillDetailsId", SqlDbType.BigInt)
                                    {
                                        Direction = ParameterDirection.Output
                                    };
                                    itemCommand.Parameters.Add(billDetailsIdParam);

                                    await itemCommand.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();
                            return billId;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vendor bill");
                throw;
            }
        }

        public async Task<decimal> GetAccountBalanceAsync(long accountId)
        {
            try
            {
                _logger.LogInformation("Getting account balance for account ID: {AccountId}", accountId);
                
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("SELECT (CreditAmount - DebitAmount) as Balance FROM PersonalPayments WHERE PersonalPaymentId = @AccountId", connection))
                    {
                        command.Parameters.AddWithValue("@AccountId", accountId);
                        
                        var result = await command.ExecuteScalarAsync();
                        var balance = result != null ? Convert.ToDecimal(result) : 0;
                        
                        _logger.LogInformation("Account balance retrieved: {Balance} for account ID: {AccountId}", balance, accountId);
                        return balance;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account balance for account ID: {AccountId}", accountId);
                throw;
            }
        }

        public async Task<bool> DeleteVendorBillAsync(long billId, long userId)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Step 1: Get bill items to reverse stock (using the same connection/transaction)
                            var billItems = new List<BillItemViewModel>();
                            using (var command = new SqlCommand("GetBillDetailsByPurchaseOrderIdFK", connection, transaction))
                            {
                                command.CommandType = CommandType.StoredProcedure;
                                command.Parameters.AddWithValue("@pBillId", billId);
                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        var productCode = reader.IsDBNull(reader.GetOrdinal("ProductCode")) ? "" : reader.GetString("ProductCode");
                                        
                                        
                                        
                                        billItems.Add(new BillItemViewModel
                                        {
                                            BillItemId = reader.GetInt64("PurchaseOrderId_FK"),
                                            ProductId = reader.GetInt64("PrductId_FK"),
                                            ProductRangeId = reader.GetInt64("ProductRangeId_FK"),
                                            UnitPrice = reader.GetDecimal("UnitPrice"),
                                            Quantity = reader.GetInt64("Quantity"),
                                            DiscountAmount = reader.GetDecimal("LineDiscountAmount"),
                                            PayableAmount = reader.GetDecimal("PayableAmount"),
                                            ProductName = reader.IsDBNull("ProductName") ? "" : reader.GetString("ProductName"),
                                            ProductCode = productCode
                                            
                                        });
                                    }
                                }
                            }
                            _logger.LogInformation("Found {Count} bill items to process for bill {BillId}", billItems.Count, billId);

                            // Step 2: Reverse stock for each product (decrease stock that was added)
                            foreach (var item in billItems)
                            {
                                var prodMaster = await _vendorService.GetStockByProductIdAsync(item.ProductId);
                                if (prodMaster != null)
                                {
                                    // Calculate new quantities (decrease stock)
                                    var newAvailableQuantity = prodMaster.AvailableQuantity - (decimal)item.Quantity;
                                    var newTotalQuantity = prodMaster.TotalQuantity - (decimal)item.Quantity;
                                    
                                    // Ensure quantities don't go negative
                                    if (newAvailableQuantity < 0) newAvailableQuantity = 0;
                                    if (newTotalQuantity < 0) newTotalQuantity = 0;

                                    // Update stock quantity (DECREASE available quantity to reverse the addition)
                                    var updateStockReturn = _vendorService.UpdateStock(
                                        prodMaster.StockMasterId,
                                        item.ProductId,
                                        newAvailableQuantity,
                                        newTotalQuantity,
                                        prodMaster.UsedQuantity, // Keep used quantity same
                                        userId,
                                        DateTime.Now
                                    );

                                    _logger.LogInformation("Stock reversed for product {ProductId}: Decreased by {Quantity}. New available: {NewAvailable}, New total: {NewTotal}",
                                        item.ProductId, item.Quantity, newAvailableQuantity, newTotalQuantity);
                                }
                                else
                                {
                                    _logger.LogWarning("Stock master not found for product {ProductId} when deleting bill {BillId}", item.ProductId, billId);
                                }
                            }

                            // Step 3: Mark PurchaseOrderItems (bill details) as deleted (soft delete) where PurchaseOrderIdFk = billId
                            try
                            {
                                using (var command = new SqlCommand(@"
                                    UPDATE PurchaseOrderItems 
                                    SET IsDeleted = 1, ModifiedDate = GETDATE() 
                                    WHERE PurchaseOrderId_FK = @BillId AND (IsDeleted = 0 OR IsDeleted IS NULL)", connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@BillId", billId);
                                    var itemsAffected = await command.ExecuteNonQueryAsync();
                                    _logger.LogInformation("Marked {Count} PurchaseOrderItems as deleted for bill {BillId}", itemsAffected, billId);
                                }
                            }
                            catch (Exception ex)
                            {
                                // If IsDeleted column doesn't exist, log warning but continue
                                _logger.LogWarning(ex, "Could not mark PurchaseOrderItems as deleted for bill {BillId}. Column may not exist.", billId);
                                // Continue with other deletion steps
                            }

                            // Step 4: Mark transactions as deleted (soft delete) where purchaseOrderId = billId
                            using (var command = new SqlCommand(@"
                                UPDATE StockTransactions 
                                SET IsDeleted = 1, ModifiedDate = GETDATE() 
                                WHERE PurchaseOrderId = @BillId AND (IsDeleted = 0 OR IsDeleted IS NULL)", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@BillId", billId);
                                var transactionsAffected = await command.ExecuteNonQueryAsync();
                                _logger.LogInformation("Marked {Count} transactions as deleted for bill {BillId}", transactionsAffected, billId);
                            }

                            // Step 4.5: Handle online payment records if payment method is Online
                            // Get bill information to check payment method
                            string? paymentMethod = null;
                            long? onlineAccountId = null;
                            using (var billCommand = new SqlCommand("GetBillByBillId", connection, transaction))
                            {
                                billCommand.CommandType = CommandType.StoredProcedure;
                                billCommand.Parameters.AddWithValue("@pBillId", billId);
                                using (var reader = await billCommand.ExecuteReaderAsync())
                                {
                                    if (await reader.ReadAsync())
                                    {
                                        try
                                        {
                                            if (!reader.IsDBNull(reader.GetOrdinal("PaymentMethod")))
                                            {
                                                paymentMethod = reader.GetString("PaymentMethod");
                                            }
                                        }
                                        catch
                                        {
                                            paymentMethod = null;
                                        }
                                        
                                        try
                                        {
                                            if (!reader.IsDBNull(reader.GetOrdinal("OnlineAccountId")))
                                            {
                                                onlineAccountId = reader.GetInt64("OnlineAccountId");
                                            }
                                        }
                                        catch
                                        {
                                            onlineAccountId = null;
                                        }
                                    }
                                }
                            }

                            if (paymentMethod != null && paymentMethod.Equals("Online", StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.LogInformation("Vendor bill {BillId} has Online payment method, updating PersonalPaymentPurchaseDetail and PersonalPayments", billId);

                                // Step 4.5.1: Get PersonalPaymentIds from PersonalPaymentPurchaseDetail for this bill
                                var personalPaymentIds = new List<long>();
                                using (var command = new SqlCommand(@"
                                    SELECT DISTINCT PersonalPaymentId 
                                    FROM PersonalPaymentPurchaseDetail 
                                    WHERE PurchaseId = @BillId AND IsActive = 1", connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@BillId", billId);
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

                                // Also add the OnlineAccountId from the bill if it exists
                                if (onlineAccountId.HasValue && onlineAccountId.Value > 0 && !personalPaymentIds.Contains(onlineAccountId.Value))
                                {
                                    personalPaymentIds.Add(onlineAccountId.Value);
                                }

                                // Step 4.5.2: Update PersonalPaymentPurchaseDetail - Set IsActive = 0 where PurchaseOrderId = billId
                                using (var command = new SqlCommand(@"
                                    UPDATE PersonalPaymentPurchaseDetail 
                                    SET IsActive = 0, ModifiedDate = GETDATE() 
                                    WHERE PurchaseId = @BillId AND IsActive = 1", connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@BillId", billId);
                                    var detailsAffected = await command.ExecuteNonQueryAsync();
                                    _logger.LogInformation("Updated {Count} PersonalPaymentPurchaseDetail records (IsActive = 0) for bill {BillId}", detailsAffected, billId);
                                }

                                // Step 4.5.3: Calculate total amount from PersonalPaymentPurchaseDetail and update CreditAmount in PersonalPayments
                                if (personalPaymentIds.Count > 0)
                                {
                                    // Calculate total amount per PersonalPaymentId from PersonalPaymentPurchaseDetail
                                    var paymentAmounts = new Dictionary<long, decimal>();
                                    using (var command = new SqlCommand(@"
                                        SELECT PersonalPaymentId, SUM(Amount) as TotalAmount
                                        FROM PersonalPaymentPurchaseDetail 
                                        WHERE PurchaseId = @BillId AND IsActive = 0
                                        GROUP BY PersonalPaymentId", connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@BillId", billId);
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

                                    // Update PersonalPayments - Decrease CreditAmount by the calculated total amount
                                    foreach (var ppId in personalPaymentIds)
                                    {
                                        // Get current CreditAmount for this PersonalPayment
                                        decimal currentCreditAmount = 0;
                                        using (var getCommand = new SqlCommand(@"
                                            SELECT CreditAmount 
                                            FROM PersonalPayments 
                                            WHERE PersonalPaymentId = @PersonalPaymentId", connection, transaction))
                                        {
                                            getCommand.Parameters.AddWithValue("@PersonalPaymentId", ppId);
                                            var result = await getCommand.ExecuteScalarAsync();
                                            if (result != null && result != DBNull.Value)
                                            {
                                                currentCreditAmount = Convert.ToDecimal(result);
                                            }
                                        }

                                        // Calculate new CreditAmount (subtract the total amount from PersonalPaymentPurchaseDetail)
                                        decimal amountToSubtract = paymentAmounts.ContainsKey(ppId) ? paymentAmounts[ppId] : 0;
                                        decimal newCreditAmount = currentCreditAmount + amountToSubtract;
                                        
                                        // Ensure CreditAmount doesn't go negative
                                        if (newCreditAmount < 0) newCreditAmount = 0;

                                        // Update PersonalPayments with new CreditAmount
                                        using (var updateCommand = new SqlCommand(@"
                                            UPDATE PersonalPayments 
                                            SET CreditAmount = @NewCreditAmount, ModifiedDate = GETDATE() 
                                            WHERE PersonalPaymentId = @PersonalPaymentId", connection, transaction))
                                        {
                                            updateCommand.Parameters.AddWithValue("@PersonalPaymentId", ppId);
                                            updateCommand.Parameters.AddWithValue("@NewCreditAmount", newCreditAmount);
                                            var paymentsAffected = await updateCommand.ExecuteNonQueryAsync();
                                            _logger.LogInformation("Updated PersonalPayment {PersonalPaymentId} for bill {BillId}: CreditAmount decreased by {AmountToSubtract} (from {OldCreditAmount} to {NewCreditAmount}), Rows affected: {RowsAffected}", 
                                                ppId, billId, amountToSubtract, currentCreditAmount, newCreditAmount, paymentsAffected);
                                        }
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("No PersonalPaymentIds found for bill {BillId} with Online payment method", billId);
                                }
                            }

                            // Step 5: Mark the bill as deleted (soft delete)
                            using (var command = new SqlCommand(@"
                                UPDATE PurchaseOrders 
                                SET IsDeleted = 1, ModifiedDate = GETDATE() 
                                WHERE PurchaseOrderId = @BillId", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@BillId", billId);
                                var rowsAffected = await command.ExecuteNonQueryAsync();
                                
                                if (rowsAffected > 0)
                                {
                                    transaction.Commit();
                                    _logger.LogInformation("Vendor bill soft deleted successfully: {BillId}, Rows affected: {RowsAffected}", billId, rowsAffected);
                                    return true;
                                }
                                else
                                {
                                    transaction.Rollback();
                                    _logger.LogWarning("No rows affected when deleting vendor bill {BillId}", billId);
                                    return false;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.LogError(ex, "Error deleting vendor bill {BillId} in transaction", billId);
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vendor bill with ID: {BillId}", billId);
                throw;
            }
        }

        public async Task<List<VendorBillViewModel>> GetAllBillNumbersForVendorAsync(long vendorId)
        {
            var bills = new List<VendorBillViewModel>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(@"
                        SELECT DISTINCT BillNumber, BillId, BillDate, TotalAmount, DueAmount
                        FROM PurchaseOrders 
                        WHERE SupplierId_FK = @VendorId 
                        AND IsDeleted = 0
                        ORDER BY BillNumber DESC", connection))
                    {
                        command.Parameters.AddWithValue("@VendorId", vendorId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                bills.Add(new VendorBillViewModel
                                {
                                    BillId = reader.GetInt64("BillId"),
                                    BillNumber = reader.GetInt64("BillNumber"),
                                    BillDate = reader.GetDateTime("BillDate"),
                                    TotalAmount = reader.GetDecimal("TotalAmount"),
                                    DueAmount = reader.GetDecimal("DueAmount")
                                });
                            }
                        }
                    }
                }
                
                _logger.LogInformation("Retrieved {Count} bills for vendor {VendorId}", bills.Count, vendorId);
                return bills;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bill numbers for vendor {VendorId}", vendorId);
                throw;
            }
        }

        public async Task<List<SupplierBillNumber>> GetActiveBillNumbersAsync(long supplierId)
        {
            var billNumbers = new List<SupplierBillNumber>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("GetAllVendorActiveBillNumbers", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pSupplierId", supplierId);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var billNumber = new SupplierBillNumber
                                {
                                    BillNumber = reader.GetInt64("BillNumber"),
                                    PurchaseOrderId = reader.GetInt64("PurchaseOrderId"),
                                    TotalDueAmount = reader.GetDecimal("TotalDueAmount")
                                };
                                billNumbers.Add(billNumber);
                            }
                        }
                    }
                }
                
                _logger.LogInformation("Retrieved {Count} active bill numbers for supplier {SupplierId}", billNumbers.Count, supplierId);
                return billNumbers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active bill numbers for supplier {SupplierId}", supplierId);
                throw;
            }
        }
    }
}
