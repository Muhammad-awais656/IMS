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
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("GetAllSupplier", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@PageNo", 1);
                        command.Parameters.AddWithValue("@PageSize", 1000); // Get all vendors for dropdown
                        command.Parameters.AddWithValue("@pSupplierName", DBNull.Value);
                        command.Parameters.AddWithValue("@pContactNumber", DBNull.Value);
                        command.Parameters.AddWithValue("@pNTN", DBNull.Value);
                        command.Parameters.AddWithValue("@pIsDeleted", false);
                        
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
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all vendors");
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
                        return result != null ? Convert.ToInt64(result) : 1;
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
                    
                    using (var command = new SqlCommand("GetPreviousDueAmountByBillId", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pBillId", billId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pVendorId", vendorId);

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
                    using (var command = new SqlCommand("SELECT ProductId, ProductCode, ProductName FROM Products WHERE IsDeleted = 0", connection))
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
                    using (var command = new SqlCommand("SELECT ProductRangeId, UnitPrice FROM ProductRange WHERE ProductId_FK = @ProductId AND IsDeleted = 0", connection))
                    {
                        command.Parameters.AddWithValue("@ProductId", productId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                productSizes.Add(new ProductRange
                                {
                                    ProductRangeId = reader.GetInt64("ProductRangeId"),
                                    UnitPrice = reader.GetDecimal("UnitPrice")
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
                    using (var command = new SqlCommand("GetPreviousDueAmountByVendorId", connection))
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
                            // Create the bill
                            using (var command = new SqlCommand("CreateVendorBill", connection, transaction))
                            {
                                command.CommandType = CommandType.StoredProcedure;
                                command.Parameters.AddWithValue("@pVendorId", model.VendorId);
                                command.Parameters.AddWithValue("@pBillNumber", model.BillNumber);
                                command.Parameters.AddWithValue("@pBillDate", model.BillDate);
                                command.Parameters.AddWithValue("@pTotalAmount", model.TotalAmount);
                                command.Parameters.AddWithValue("@pDiscountAmount", model.DiscountAmount);
                                command.Parameters.AddWithValue("@pPaidAmount", model.PaidAmount);
                                command.Parameters.AddWithValue("@pDueAmount", model.DueAmount);
                                command.Parameters.AddWithValue("@pDescription", model.Description ?? "");
                                command.Parameters.AddWithValue("@pCreatedBy", 1); // TODO: Get from session
                                command.Parameters.AddWithValue("@pCreatedDate", DateTime.Now);

                                var billId = await command.ExecuteScalarAsync();
                                var newBillId = Convert.ToInt64(billId);

                                // Add bill items
                                foreach (var item in model.BillItems)
                                {
                                    using (var itemCommand = new SqlCommand("CreateVendorBillItem", connection, transaction))
                                    {
                                        itemCommand.CommandType = CommandType.StoredProcedure;
                                        itemCommand.Parameters.AddWithValue("@pBillId", newBillId);
                                        itemCommand.Parameters.AddWithValue("@pProductId", item.ProductId);
                                        itemCommand.Parameters.AddWithValue("@pProductRangeId", item.ProductRangeId);
                                        itemCommand.Parameters.AddWithValue("@pUnitPrice", item.UnitPrice);
                                        itemCommand.Parameters.AddWithValue("@pUnitPurchasePrice", item.BillPrice);
                                        itemCommand.Parameters.AddWithValue("@pQuantity", (int)item.Quantity);
                                        itemCommand.Parameters.AddWithValue("@pDiscountAmount", item.DiscountAmount);
                                        itemCommand.Parameters.AddWithValue("@pPayableAmount", item.PayableAmount);
                                        itemCommand.Parameters.AddWithValue("@pCreatedBy", 1); // TODO: Get from session
                                        itemCommand.Parameters.AddWithValue("@pCreatedDate", DateTime.Now);

                                        await itemCommand.ExecuteNonQueryAsync();
                                    }
                                }

                                transaction.Commit();
                                return newBillId;
                            }
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
    }
}
