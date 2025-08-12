using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMS.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<CategoryService> _logger;
        public CategoryService(IDbContextFactory dbContextFactory, ILogger<CategoryService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }
        public async Task<bool> CreateAdminCategoryAsync(AdminCategory adminCategory)
        {
            bool response = false;
            try
            {


                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("AddAdminCategory", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pCategoryName", adminCategory.CategoryName);
                        command.Parameters.AddWithValue("@pCategoryDescription", adminCategory.CategoryDescription);
                        command.Parameters.AddWithValue("@pIsEnabled", adminCategory.IsEnabled);
                        command.Parameters.AddWithValue("@pOtherAdjustments", adminCategory.OtherAdjustments);
                        command.Parameters.AddWithValue("@pCreatedDate", adminCategory?.CreatedDate != null ? adminCategory.CreatedDate : DBNull.Value);
                        command.Parameters.AddWithValue("@pCreatedBy", adminCategory?.CreatedBy != null ? adminCategory.CreatedBy : DBNull.Value);


                        var AdmincategoryIdParam = new SqlParameter("@pCategoryId", SqlDbType.BigInt)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(AdmincategoryIdParam);
                        await command.ExecuteNonQueryAsync();
                        long newAdmincategoryIdParam = (long)AdmincategoryIdParam.Value;
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

        public async Task<int> DeleteAdminCategoryAsync(long id)
        {
            int response = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("DeleteCategory", connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
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

        public async Task<AdminCategory> GetAdminCategoryByIdAsync(long id)
        {
            AdminCategory adminLabel = new AdminCategory();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetCategoryDetail", connection))
                    {
                        command.Parameters.AddWithValue("@pCategoryId", id);
                        command.CommandType = CommandType.StoredProcedure;
                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    return new AdminCategory
                                    {
                                        CategoryId = reader.GetInt64(reader.GetOrdinal("CategoryId")),
                                        CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                                        CategoryDescription = reader.GetString(reader.GetOrdinal("CategoryDescription")),
                                        OtherAdjustments = reader.GetByte(reader.GetOrdinal("OtherAdjustments")),
                                        CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                        CreatedBy = reader.GetInt64(reader.GetOrdinal("CreatedBy")),
                                        ModifiedDate = reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                                        ModifiedBy = reader.GetInt64(reader.GetOrdinal("ModifiedBy")),
                                        IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled"))
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
            return null;
        }

        public async Task<CategoriesViewModel> GetAllAdminCategoryAsync(int pageNumber, int? pageSize, string? name)
        {
            var adminCategory = new List<AdminCategory>();
            var totalCount = 0;


            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    //Get total count
                    using (var command = new SqlCommand("SELECT COUNT(*) FROM AdminCategory", connection))
                    {
                        try
                        {
                            totalCount = (int)await command.ExecuteScalarAsync();
                        }
                        catch
                        {
                        }
                    }
                    using (var command = new SqlCommand("GetAllCategory", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@PageNumber", pageNumber);
                        command.Parameters.AddWithValue("@PageSize", pageSize);
                        command.Parameters.AddWithValue("@SearchName", name);

                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {


                                while (await reader.ReadAsync())
                                {
                                    adminCategory.Add(new AdminCategory
                                    {
                                        CategoryId = reader.GetInt64(reader.GetOrdinal("CategoryId")),
                                        CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                                        CategoryDescription = reader.GetString(reader.GetOrdinal("CategoryDescription")),
                                        OtherAdjustments = reader.GetByte(reader.GetOrdinal("OtherAdjustments")),
                                        CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                        CreatedBy = reader.GetInt64(reader.GetOrdinal("CreatedBy")),
                                        ModifiedDate = reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                                        ModifiedBy = reader.GetInt64(reader.GetOrdinal("ModifiedBy")),
                                        IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled"))
                                    });

                                }
                                if (!string.IsNullOrEmpty(name))
                                {
                                    totalCount = adminCategory.Count;

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
            return new CategoriesViewModel
            {
                AdminCategories = adminCategory,
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<int> UpdateAdminCategoryAsync(AdminCategory adminCategory)
        {
            var RowsAffectedResponse = 0;


            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("UpdateAdminCategory", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pCategoryName", adminCategory.CategoryName);
                        command.Parameters.AddWithValue("@pCategoryDescription", adminCategory.CategoryDescription);
                        command.Parameters.AddWithValue("@pIsEnabled", adminCategory.IsEnabled);
                        command.Parameters.AddWithValue("@pOtherAdjustments", adminCategory.OtherAdjustments);
                        command.Parameters.AddWithValue("@ModifiedDate", adminCategory?.ModifiedDate == default(DateTime) ? DBNull.Value : adminCategory?.ModifiedDate);
                        command.Parameters.AddWithValue("@ModifiedBy", adminCategory?.ModifiedBy);
                        command.Parameters.AddWithValue("@pCategoryId", adminCategory?.CategoryId);

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
