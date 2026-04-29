using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMS.Services
{
    
    public class ProductService : IProductService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<ProductService> _logger;
        public ProductService(IDbContextFactory dbContextFactory, ILogger<ProductService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }
        public async Task<bool> CreateProductAsync(Product product)
        {
            bool response = false;
            try
            {


                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("ProductCreate", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pProductName", product.ProductName);
                        command.Parameters.AddWithValue("@pProductDescription", product.ProductDescription ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@UrduName", product.UrduName ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pSizeId_FK", product.SizeIdFk);
                        command.Parameters.AddWithValue("@pLabelId_FK", product.LabelIdFk);
                        command.Parameters.AddWithValue("@pUnitPrice", product.UnitPrice);
                        command.Parameters.AddWithValue("@pProductCode", product.ProductCode ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pIsEnabled", product.IsEnabled);
                        command.Parameters.AddWithValue("@pCategoryId_FK", product.CategoryIdFk);
                        command.Parameters.AddWithValue("@pMeasuringUnitTypeId", product.MeasuringUnitTypeIdFk ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@SupplierId_FK", product.SupplierIdFk ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pLocation", product.Location ?? (object)DBNull.Value);

                        

                        command.Parameters.AddWithValue("@pModifiedDate", product.ModifiedDate == default(DateTime) ? DBNull.Value : product.ModifiedDate);
                        command.Parameters.AddWithValue("@pModifiedBy", product.ModifiedBy);
                        command.Parameters.AddWithValue("@pCreatedDate", product.CreatedDate == default(DateTime) ? DBNull.Value : product.CreatedDate);
                        command.Parameters.AddWithValue("@pCreatedBy", product.CreatedBy);


                        var unitTypeyIdParam = new SqlParameter("@pProductId", SqlDbType.BigInt)
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
            catch
            {

            }
            return response;
        }

        public async Task<int> DeleteProductAsync(long id)
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
                            // First SP
                            using (var command = new SqlCommand("DeleteProductById", connection, transaction))
                            {
                                command.CommandType = CommandType.StoredProcedure;
                                command.Parameters.AddWithValue("@pProductId", id);

                                var rowAffected = new SqlParameter("@RowsAffected", SqlDbType.Int)
                                {
                                    Direction = ParameterDirection.Output
                                };
                                command.Parameters.Add(rowAffected);

                                await command.ExecuteNonQueryAsync();
                                int affected = command.Parameters["@RowsAffected"].Value != DBNull.Value
                                    ? (int)command.Parameters["@RowsAffected"].Value
                                      : 0;
                                if (affected > 0)
                                {
                                    response = affected;
                                }


                            }

                            // Second SP
                            using (var command2 = new SqlCommand("DeleteProductRangeByProductId", connection, transaction))
                            {
                                command2.CommandType = CommandType.StoredProcedure;
                                command2.Parameters.AddWithValue("@pProductId", id);
        
                                await command2.ExecuteNonQueryAsync();
                            }

                            // Commit both
                            transaction.Commit();
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
                // log ex
            }
            return response;
        }



        public async Task<ProductViewModel> GetAllProductAsync(int pageNumber, int? pageSize, ProductViewModel.ProductFilters productFilters)
        {
            var products = new List<ProductViewModel>();
            var totalRecords = 0;

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                   
                    using (var command = new SqlCommand("GetAllProducts", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                      
                        // Add parameters
                        command.Parameters.AddWithValue("@pIsEnabled", DBNull.Value);
                        command.Parameters.AddWithValue("@pProductName", productFilters.ProductName ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pProductCode", productFilters.ProductCode ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pPriceFrom", productFilters.PriceFrom ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pPriceTo", productFilters.PriceTo ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pSizeId", productFilters.SizeId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@plabelId", productFilters.LabelId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pCategoryId", productFilters.CategoryId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pMeasuringUnitTypeId", productFilters.MeasuringUnitTypeId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@PageNumber", pageNumber);
                        command.Parameters.AddWithValue("@PageSize", pageSize);
                        command.Parameters.AddWithValue("@productId", productFilters.ProductId ?? (object)DBNull.Value);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                products.Add(new ProductViewModel
                                {
                                    ProductId = reader.GetInt64(reader.GetOrdinal("ProductId")),
                                    ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                                    ProductCode = reader.IsDBNull(reader.GetOrdinal("ProductCode")) ? null : reader.GetString(reader.GetOrdinal("ProductCode")),
                                    ProductDescription = reader.IsDBNull(reader.GetOrdinal("ProductDescription")) ? null : reader.GetString(reader.GetOrdinal("ProductDescription")),
                                    UrduName = reader.IsDBNull(reader.GetOrdinal("UrduName")) ? null : reader.GetString(reader.GetOrdinal("UrduName")),
                                    Location = reader.IsDBNull(reader.GetOrdinal("Location")) ? null : reader.GetString(reader.GetOrdinal("Location")),
                                    Price = reader.IsDBNull(reader.GetOrdinal("UnitPrice")) ? 0 : reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                                    LabelName = reader.GetString(reader.GetOrdinal("LabelName")),
                                    IsEnabledProduct = reader.GetByte(reader.GetOrdinal("IsEnabled")),
                                    MeasuringUnitTypeName = reader.IsDBNull(reader.GetOrdinal("MeasuringUnitTypeName")) ? null : reader.GetString(reader.GetOrdinal("MeasuringUnitTypeName"))
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
                throw new Exception($"Error fetching products: {ex.Message}");
            }

            return new ProductViewModel
            {
                Items = products,
                CurrentPage = pageNumber,
                TotalPages = pageSize.HasValue && pageSize.Value > 0
                    ? (int)Math.Ceiling(totalRecords / (double)pageSize.Value)
                    : 1,
                PageSize = pageSize,
                TotalCount = totalRecords

            };
        }

        public async Task<List<Product>> GetAllEnabledProductsAsync()
        {
            var productList = new List<Product>();
            

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetAllEnabledProducts", connection))
                    {
                        
                        command.CommandType = CommandType.StoredProcedure;
                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    productList.Add(new Product
                                    {
                                        ProductId = reader.GetInt64(reader.GetOrdinal("ProductId")),
                                        ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                                        
                                    });
                                    
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
            return productList;
        }

        public async Task<ProductViewModel> GetProductByIdAsync(long id)
        {
            var productList = new Product();
          
           
            var productRanges = new List<ProductRange>();
            var updatedRoductRange = new ProductRange();

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetProductDetail", connection))
                    {
                        command.Parameters.AddWithValue("@pProductId", id);
                        command.CommandType = CommandType.StoredProcedure;
                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    productList= new Product
                                    {
                                        ProductId = reader.GetInt64(reader.GetOrdinal("ProductId")),
                                        ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                                        ProductDescription = reader.IsDBNull(reader.GetOrdinal("ProductDescription")) ? null : reader.GetString(reader.GetOrdinal("ProductDescription")),
                                        Location = reader.IsDBNull(reader.GetOrdinal("Location")) ? null : reader.GetString(reader.GetOrdinal("Location")),
                                        SizeIdFk = reader.IsDBNull(reader.GetOrdinal("SizeId_FK")) ? 0 : reader.GetInt64(reader.GetOrdinal("SizeId_FK")),
                                        LabelIdFk = reader.IsDBNull(reader.GetOrdinal("LabelId_FK")) ? 0 : reader.GetInt64(reader.GetOrdinal("LabelId_FK")),
                                        UnitPrice = reader.IsDBNull(reader.GetOrdinal("UnitPrice")) ? 0 : reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                                        ProductCode = reader.IsDBNull(reader.GetOrdinal("ProductCode")) ? null : reader.GetString(reader.GetOrdinal("ProductCode")),
                                        IsEnabled = reader.GetByte(reader.GetOrdinal("IsEnabled")),
                                        CategoryIdFk = reader.IsDBNull(reader.GetOrdinal("CategoryId_FK")) ? 0 : reader.GetInt64(reader.GetOrdinal("CategoryId_FK")),
                                        MeasuringUnitTypeIdFk = reader.IsDBNull(reader.GetOrdinal("MeasuringUnitTypeId_FK")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("MeasuringUnitTypeId_FK")),
                                        SupplierIdFk = reader.IsDBNull(reader.GetOrdinal("SupplierId_FK")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("SupplierId_FK")),
                                        UrduName = null
                                    };
                                    try { var uIdx = reader.GetOrdinal("UrduName"); if (!reader.IsDBNull(uIdx)) productList.UrduName = reader.GetString(uIdx); } catch { }
                                    if (!reader.IsDBNull(reader.GetOrdinal("ProductId_FK")))
                                    {
                                        // Safely get ProductRangeId if column exists
                                        long productRangeId = 0;
                                        try
                                        {
                                            int productRangeIdOrdinal = reader.GetOrdinal("ProductRangeId");
                                            productRangeId = reader.IsDBNull(productRangeIdOrdinal) ? 0 : reader.GetInt64(productRangeIdOrdinal);
                                        }
                                        catch
                                        {
                                            // Column doesn't exist in result set, default to 0
                                            productRangeId = 0;
                                        }

                                        var pr = new ProductRange
                                        {
                                            ProductRangeId = productRangeId,
                                            ProductIdFk = reader.GetInt64(reader.GetOrdinal("ProductId_FK")),
                                            MeasuringUnitIdFk = reader.IsDBNull(reader.GetOrdinal("MeasuringUnitId_Fk")) ? 0 : reader.GetInt64(reader.GetOrdinal("MeasuringUnitId_Fk")),
                                            RangeFrom = reader.IsDBNull(reader.GetOrdinal("RangeFrom")) ? 0 : reader.GetDecimal(reader.GetOrdinal("RangeFrom")),
                                            RangeTo = reader.IsDBNull(reader.GetOrdinal("RangeTo")) ? 0 : reader.GetDecimal(reader.GetOrdinal("RangeTo")),
                                            UnitPrice = reader.IsDBNull(reader.GetOrdinal("RangeUnitPrice")) ? 0 : reader.GetDecimal(reader.GetOrdinal("RangeUnitPrice"))
                                        };
                                        try
                                        {
                                            int ord = reader.GetOrdinal("ProductRangeName");
                                            if (!reader.IsDBNull(ord)) pr.ProductRangeName = reader.GetString(ord);
                                        }
                                        catch { }
                                        try
                                        {
                                            int ord = reader.GetOrdinal("ProductRangeUrduName");
                                            if (!reader.IsDBNull(ord)) pr.UrduName = reader.GetString(ord);
                                        }
                                        catch { }
                                        if (pr.UrduName == null)
                                        {
                                            try
                                            {
                                                int ord = reader.GetOrdinal("RangeUrduName");
                                                if (!reader.IsDBNull(ord)) pr.UrduName = reader.GetString(ord);
                                            }
                                            catch { }
                                        }
                                        productRanges.Add(pr);

                                    }
                                    


                                }
                            }
                        }
                        catch
                        {

                        }

                    }
                    // Populate MeasuringUnitName and MeasuringUnitAbbreviation for each product range (for MU column display on Edit)
                    var muIds = productRanges.Where(pr => pr.MeasuringUnitIdFk > 0).Select(pr => pr.MeasuringUnitIdFk).Distinct().ToList();
                    if (muIds.Any())
                    {
                        var idList = string.Join(",", muIds);
                        using (var muCmd = new SqlCommand($"SELECT MeasuringUnitId, MeasuringUnitName, MeasuringUnitAbbreviation FROM AdminMeasuringUnits WHERE MeasuringUnitId IN ({idList})", connection))
                        {
                            using (var muReader = await muCmd.ExecuteReaderAsync())
                            {
                                var muMap = new Dictionary<long, (string Name, string Abbrev)>();
                                while (await muReader.ReadAsync())
                                {
                                    var muId = muReader.GetInt64(0);
                                    var name = muReader.IsDBNull(1) ? null : muReader.GetString(1);
                                    var abbrev = muReader.IsDBNull(2) ? null : muReader.GetString(2);
                                    muMap[muId] = (name ?? "", abbrev ?? "");
                                }
                                foreach (var pr in productRanges)
                                {
                                    if (muMap.TryGetValue(pr.MeasuringUnitIdFk, out var mu))
                                    {
                                        pr.MeasuringUnitName = mu.Name;
                                        pr.MeasuringUnitAbbreviation = mu.Abbrev;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {

            }
            return new ProductViewModel {
                ProductList = productList,
                productRanges = productRanges,
                //SizeNameList = sizeNameList

            };
        }

        public async Task<int> UpdateProductAsync(Product product)
        {
            long RowsAffectedResponse = 0;

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("ProductModify", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pProductName", product.ProductName);
                        command.Parameters.AddWithValue("@pProductDescription", product.ProductDescription ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pSizeId_FK", product.SizeIdFk);
                        command.Parameters.AddWithValue("@pLabelId_FK", product.LabelIdFk);
                        command.Parameters.AddWithValue("@pModifiedDate", product.ModifiedDate == default(DateTime) ? DBNull.Value : product.ModifiedDate);
                        
                        command.Parameters.AddWithValue("@pModifiedBy", product.ModifiedBy);
                        
                        command.Parameters.AddWithValue("@pProductId", product.ProductId);
                        command.Parameters.AddWithValue("@pUnitPrice", product.UnitPrice);
                        command.Parameters.AddWithValue("@pProductCode", product.ProductCode ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pIsEnabled", product.IsEnabled);
                        command.Parameters.AddWithValue("@pCategoryId_FK", product.CategoryIdFk);
                        command.Parameters.AddWithValue("@MeasuringUnitTypeId_FK", product.MeasuringUnitTypeIdFk ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@SupplierId_FK", product.SupplierIdFk ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pLocation", product.Location ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@UrduName", product.UrduName ?? (object)DBNull.Value);

                        var expenseTypeidParam = new SqlParameter("@RowsAffected", SqlDbType.BigInt)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(expenseTypeidParam);

                        await command.ExecuteNonQueryAsync();
                        long affected = command.Parameters["@RowsAffected"].Value != DBNull.Value
                                        ? (long)command.Parameters["@RowsAffected"].Value
                                          : 0;
                        if (affected > 0)
                        {
                            RowsAffectedResponse = affected;
                        }
                    }
                }
            }
            catch
            {
            }
            return (int)RowsAffectedResponse;
        }

        public async Task<bool> CreateProductRange(ProductRange productRange)
        {
            bool response = false;
            try
            {


                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("AddProductRange", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pProductId_FK", productRange.ProductIdFk);
                        command.Parameters.AddWithValue("@pMeasuringUnitId_FK", productRange.MeasuringUnitIdFk);
                        command.Parameters.AddWithValue("@pRangeFrom", productRange.RangeFrom==null ? 0: productRange.RangeFrom);
                        command.Parameters.AddWithValue("@pRangeTo", productRange.RangeTo==null ? 0 : productRange.RangeTo);
                        command.Parameters.AddWithValue("@pUnitPrice", productRange.UnitPrice);
                        command.Parameters.AddWithValue("@pProductRangeName", string.IsNullOrEmpty(productRange.ProductRangeName) ? (object)DBNull.Value : productRange.ProductRangeName);
                        command.Parameters.AddWithValue("@pUrduName", string.IsNullOrEmpty(productRange.UrduName) ? (object)DBNull.Value : productRange.UrduName);
                  
                        var unitTypeyIdParam = new SqlParameter("@pProductRangeId", SqlDbType.BigInt)
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
            catch
            {

            }
            return response;
        }

        public async Task<Product?> GetProductByCodeAsync(string productCode)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("SELECT TOP 1 * FROM Products WHERE ProductCode = @ProductCode ORDER BY ProductId DESC", connection))
                    {
                        command.Parameters.AddWithValue("@ProductCode", productCode);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new Product
                                {
                                    ProductId = reader.GetInt64(reader.GetOrdinal("ProductId")),
                                    ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                                    ProductCode = reader.IsDBNull(reader.GetOrdinal("ProductCode")) ? null : reader.GetString(reader.GetOrdinal("ProductCode")),
                                    ProductDescription = reader.IsDBNull(reader.GetOrdinal("ProductDescription")) ? null : reader.GetString(reader.GetOrdinal("ProductDescription")),
                                    CategoryIdFk = reader.IsDBNull(reader.GetOrdinal("CategoryId_FK")) ? 0 : reader.GetInt64(reader.GetOrdinal("CategoryId_FK")),
                                    LabelIdFk = reader.IsDBNull(reader.GetOrdinal("LabelId_FK")) ? 0 : reader.GetInt64(reader.GetOrdinal("LabelId_FK")),
                                    MeasuringUnitTypeIdFk = reader.IsDBNull(reader.GetOrdinal("MeasuringUnitTypeId_FK")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("MeasuringUnitTypeId_FK")),
                                    SupplierIdFk = reader.IsDBNull(reader.GetOrdinal("SupplierId_FK")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("SupplierId_FK")),
                                    UnitPrice = reader.IsDBNull(reader.GetOrdinal("UnitPrice")) ? 0 : reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                                    IsEnabled = reader.GetByte(reader.GetOrdinal("IsEnabled")),
                                    SizeIdFk = reader.IsDBNull(reader.GetOrdinal("SizeId_FK")) ? 0 : reader.GetInt64(reader.GetOrdinal("SizeId_FK")),
                                    CreatedBy = reader.GetInt64(reader.GetOrdinal("CreatedBy")),
                                    CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                    ModifiedBy = reader.GetInt64(reader.GetOrdinal("ModifiedBy")),
                                    ModifiedDate = reader.GetDateTime(reader.GetOrdinal("ModifiedDate"))
                                };
                            }
                        }
                    }
                }
            }
            catch
            {
                // Log error if needed
            }
            return null;
        }

        public async Task<bool> ProductCodeExistsAsync(string productCode, long? excludeProductId = null)
        {
            bool response = false;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("ProductCodeAlreadyExists", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pProductCode", productCode);
                        //command.Parameters.AddWithValue("@pProductId", excludeProductId ?? 0);

                        var returnValue = new SqlParameter("@isAlreadyExists", SqlDbType.Bit)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(returnValue);

                        await command.ExecuteNonQueryAsync();
                        response = (bool)returnValue.Value;
                    }
                }
            }
            catch
            {
                // Log error if needed
                return false;
            }
            return response;
        }

        public async Task<bool> DeleteProductRangesByProductIdAsync(long productId)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("DELETE FROM ProductRange WHERE ProductId_FK = @ProductId", connection))
                    {
                        command.Parameters.AddWithValue("@ProductId", productId);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected >= 0; // Return true even if no rows were affected
                    }
                }
            }
            catch
            {
                // Log error if needed
                return false;
            }
        }
    }
}
