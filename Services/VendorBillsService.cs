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

        public VendorBillsService(IDbContextFactory dbContextFactory, ILogger<VendorBillsService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
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
                                    Description = reader.IsDBNull(reader.GetOrdinal("PurchaseOrderDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("PurchaseOrderDescription"))
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
                    using (var command = new SqlCommand("SELECT ProductId, ProductCode, ProductName FROM Products WHERE Isenabled = 1", connection))
                    {
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
                    using (var command = new SqlCommand(@"
                        SELECT pr.ProductRangeId, pr.UnitPrice, pr.RangeFrom, pr.RangeTo, pr.MeasuringUnitId_FK,
       mu.MeasuringUnitName, mu.MeasuringUnitAbbreviation
FROM ProductRange pr
LEFT JOIN AdminMeasuringUnits mu ON pr.MeasuringUnitId_FK = mu.MeasuringUnitId
                        WHERE pr.ProductId_FK = @ProductId", connection))
                    {
                        command.Parameters.AddWithValue("@ProductId", productId);
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
                    
                    using (var command = new SqlCommand("SELECT SUM(ISNULL(p.TotalDueAmount,0)) PreviousDueAmount FROM PurchaseOrders p WHERE p.SupplierId_FK = @pVendorId", connection))
                    {
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
                    using (var command = new SqlCommand(@"
                       SELECT PurchaseOrderId, BillNumber, SupplierId_FK, BillNumber, PurchaseOrderDate, TotalAmount, DiscountAmount, 
        TotalReceivedAmount,TotalAmount, TotalDueAmount, PurchaseOrderDescription, po.CreatedDate,asp.SupplierName as VendorName
 FROM PurchaseOrders po
 left join AdminSuppliers asp on asp.SupplierId = po.SupplierId_FK
 WHERE PurchaseOrderId = @BillId AND po.IsDeleted = 0
", connection))
                    {
                        command.Parameters.AddWithValue("@BillId", billId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
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
                                    Description = reader.IsDBNull("PurchaseOrderDescription") ? "" : reader.GetString("PurchaseOrderDescription")
                                  
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

        public async Task<List<BillItemViewModel>> GetVendorBillItemsAsync(long billId)
        {
            var billItems = new List<BillItemViewModel>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(@"
                        SELECT bi.PurchaseOrderId_FK, bi.PrductId_FK, bi.ProductRangeId_FK, bi.UnitPrice, 
                               bi.Quantity, bi.LineDiscountAmount, bi.PayableAmount, p.ProductName
                        FROM PurchaseOrderItems bi
                        LEFT JOIN Products p ON bi.PrductId_FK = p.ProductId
                        WHERE bi.PurchaseOrderId_FK = @BillId ", connection))
                    {
                        command.Parameters.AddWithValue("@BillId", billId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                billItems.Add(new BillItemViewModel
                                {
                                    BillItemId = reader.GetInt64("PurchaseOrderId_FK"),
                                    ProductId = reader.GetInt64("PrductId_FK"),
                                    ProductRangeId = reader.GetInt64("ProductRangeId_FK"),
                                    UnitPrice = reader.GetDecimal("UnitPrice"),
                                    Quantity = reader.GetInt64("Quantity"),
                                    DiscountAmount = reader.GetDecimal("LineDiscountAmount"),
                                    PayableAmount = reader.GetDecimal("PayableAmount"),
                                    ProductName = reader.IsDBNull("ProductName") ? "" : reader.GetString("ProductName")
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
                            // Update the main bill
                            using (var command = new SqlCommand(@"
                                UPDATE PurchaseOrders 
                                SET VendorId = @VendorId, BillNumber = @BillNumber, BillDate = @BillDate,
                                    TotalAmount = @TotalAmount, DiscountAmount = @DiscountAmount,
                                    PaidAmount = @PaidAmount, DueAmount = @DueAmount, Description = @Description
                                WHERE BillId = @BillId", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@BillId", billId);
                                command.Parameters.AddWithValue("@VendorId", model.VendorId);
                                command.Parameters.AddWithValue("@BillNumber", model.BillNumber);
                                command.Parameters.AddWithValue("@BillDate", model.BillDate);
                                command.Parameters.AddWithValue("@TotalAmount", model.TotalAmount);
                                command.Parameters.AddWithValue("@DiscountAmount", model.DiscountAmount);
                                command.Parameters.AddWithValue("@PaidAmount", model.PaidAmount);
                                command.Parameters.AddWithValue("@DueAmount", model.DueAmount);
                                command.Parameters.AddWithValue("@Description", model.Description ?? "");

                                await command.ExecuteNonQueryAsync();
                            }

                            // Delete existing bill items
                            using (var command = new SqlCommand("UPDATE PurchaseOrderItems SET IsDeleted = 1 WHERE BillId = @BillId", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@BillId", billId);
                                await command.ExecuteNonQueryAsync();
                            }

                            // Insert new bill items
                            foreach (var item in model.BillDetails)
                            {
                                using (var command = new SqlCommand(@"
                                    INSERT INTO PurchaseOrderItems (BillId, ProductId, ProductRangeId, UnitPrice, Quantity, DiscountAmount, PayableAmount, CreatedDate)
                                    VALUES (@BillId, @ProductId, @ProductRangeId, @UnitPrice, @Quantity, @DiscountAmount, @PayableAmount, @CreatedDate)", connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@BillId", billId);
                                    command.Parameters.AddWithValue("@ProductId", item.ProductId);
                                    command.Parameters.AddWithValue("@ProductRangeId", item.ProductRangeId);
                                    command.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                                    command.Parameters.AddWithValue("@Quantity", item.Quantity);
                                    command.Parameters.AddWithValue("@DiscountAmount", item.LineDiscountAmount);
                                    command.Parameters.AddWithValue("@PayableAmount", item.PayableAmount);
                                    command.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                                    await command.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();
                            return true;
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

        public async Task<bool> DeleteVendorBillAsync(long billId)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    // Mark the bill as deleted (soft delete)
                    using (var command = new SqlCommand(@"
                        UPDATE PurchaseOrders 
                        SET IsDeleted = 1, ModifiedDate = GETDATE() 
                        WHERE PurchaseOrderId = @BillId", connection))
                    {
                        command.Parameters.AddWithValue("@BillId", billId);
                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        
                        _logger.LogInformation("Vendor bill soft deleted: {BillId}, Rows affected: {RowsAffected}", billId, rowsAffected);
                        return rowsAffected > 0;
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
    }
}
