using DocumentFormat.OpenXml.Office2010.Excel;
using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using NuGet.Protocol;
using System.Data;
using static IMS.Models.ProductViewModel;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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
                        command.Parameters.AddWithValue("@pProductDescription", product.ProductDescription);
                        command.Parameters.AddWithValue("@pSizeId_FK", product.SizeIdFk);
                        command.Parameters.AddWithValue("@pLabelId_FK", product.LabelIdFk);
                        command.Parameters.AddWithValue("@pUnitPrice", product.UnitPrice);
                        command.Parameters.AddWithValue("@pProductCode", product.ProductCode);
                        command.Parameters.AddWithValue("@pIsEnabled", product.IsEnabled);
                        command.Parameters.AddWithValue("@pCategoryId_FK", product.CategoryIdFk);
                        command.Parameters.AddWithValue("@pMeasuringUnitTypeId", product.MeasuringUnitTypeIdFk);
                        command.Parameters.AddWithValue("@SupplierId_FK", product.SupplierIdFk);

                        

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
                    using (var command = new SqlCommand("DeleteProductById", connection))
                    {
                        command.Parameters.AddWithValue("@pProductId", id);
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


        public async Task<ProductViewModel> GetAllProductAsync(int pageNumber, int? pageSize, ProductFilters productFilters)
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
                                    Price = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                                    IsEnabled = Convert.ToBoolean(reader.GetByte(reader.GetOrdinal("IsEnabled"))), // Use GetBoolean for BIT
                                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                                    LabelName = reader.GetString(reader.GetOrdinal("LabelName")),
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
            var productList = new List<Product>();
            var categoryNameList = new List<AdminCategory>();
            var labelNameList = new List<AdminLabel>();
            var sizeNameList = new List<AdminSize>();

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetProductDetail", connection))
                    {
                        command.Parameters.AddWithValue("'@pProductId", id);
                        command.CommandType = CommandType.StoredProcedure;
                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    productList.Add(new Product
                                    {
                                        ProductId = reader.GetInt64(reader.GetOrdinal("ProductId")),
                                        ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                                        ProductDescription = reader.IsDBNull(reader.GetOrdinal("ProductDescription")) ? null : reader.GetString(reader.GetOrdinal("ProductDescription")),
                                        SizeIdFk = reader.GetInt64(reader.GetOrdinal("SizeId_FK")),
                                        LabelIdFk = reader.GetInt64(reader.GetOrdinal("LabelId_FK")),
                                        UnitPrice = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                                        ProductCode = reader.IsDBNull(reader.GetOrdinal("ProductCode")) ? null : reader.GetString(reader.GetOrdinal("ProductCode")),
                                        IsEnabled = reader.GetByte(reader.GetOrdinal("IsEnabled")),
                                        CategoryIdFk = reader.GetInt64(reader.GetOrdinal("CategoryId_FK")),
                                        MeasuringUnitTypeIdFk = reader.IsDBNull(reader.GetOrdinal("MeasuringUnitTypeId_FK")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("MeasuringUnitTypeId_FK")),
                                        SupplierIdFk = reader.IsDBNull(reader.GetOrdinal("SupplierId_FK")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("SupplierId_FK"))
                                    });
                                    categoryNameList.Add(new AdminCategory
                                    {
                                        CategoryId = reader.GetInt64(reader.GetOrdinal("CategoryId")),
                                        CategoryName = reader.GetString(reader.GetOrdinal("CategoryName"))
                                    });
                                    labelNameList.Add(new AdminLabel
                                    {
                                        LabelId = reader.GetInt64(reader.GetOrdinal("LabelId")),
                                        LabelName = reader.GetString(reader.GetOrdinal("LabelName"))
                                    });
                                    sizeNameList.Add(new AdminSize
                                    {
                                        SizeId = reader.GetInt64(reader.GetOrdinal("SizeId")),
                                        SizeName = reader.GetString(reader.GetOrdinal("SizeName"))
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
            return new ProductViewModel{
                //ProductList = productList,
                //CategoryNameList = categoryNameList,
                //LabelNameList = labelNameList,
                //SizeNameList = sizeNameList
               
            };
        }

        public async Task<int> UpdateProductAsync(Product product)
        {
            var RowsAffectedResponse = 0;

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("ProductModify", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pProductName", product.ProductName);
                        command.Parameters.AddWithValue("@pProductDescription", product.ProductDescription);
                        command.Parameters.AddWithValue("@pSizeId_FK", product.SizeIdFk);
                        command.Parameters.AddWithValue("@pLabelId_FK", product.LabelIdFk);
                        command.Parameters.AddWithValue("@pModifiedDate", product?.ModifiedDate == default(DateTime) ? DBNull.Value : product?.ModifiedDate);
                        command.Parameters.AddWithValue("@pCreatedDate", product?.CreatedDate == default(DateTime) ? DBNull.Value : product?.CreatedDate);
                        command.Parameters.AddWithValue("@pModifiedBy", product?.ModifiedBy);
                        command.Parameters.AddWithValue("@pCreatedBy", product?.CreatedBy);
                        command.Parameters.AddWithValue("@pProductId", product?.ProductId);
                        command.Parameters.AddWithValue("@pUnitPrice", product?.UnitPrice);
                        command.Parameters.AddWithValue("@pProductCode", product?.ProductCode);
                        command.Parameters.AddWithValue("@pIsEnabled", product?.IsEnabled);
                        command.Parameters.AddWithValue("@pCategoryId_FK", product?.CategoryIdFk);
                        command.Parameters.AddWithValue("@MeasuringUnitTypeId_FK", product?.MeasuringUnitTypeIdFk);
                        command.Parameters.AddWithValue("@SupplierId_FK", product?.SupplierIdFk);

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
    }
}
