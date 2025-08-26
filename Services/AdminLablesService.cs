using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMS.Services
{
    public class AdminLablesService : IAdminLablesService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<AdminLablesService> _logger;
        public AdminLablesService(IDbContextFactory dbContextFactory, ILogger<AdminLablesService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }
        public async Task<bool> CreateAdminLablesAsync(AdminLabel adminLabel)
        {
            bool response = false;
            try
            {


                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("AddAdminLabels", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pLabelName", adminLabel.LabelName);
                        command.Parameters.AddWithValue("@pLabelDescription", adminLabel.LabelDescription);
                        command.Parameters.AddWithValue("@pIsEnabled", adminLabel.IsEnabled);
                        command.Parameters.AddWithValue("@pCreatedDate", adminLabel?.CreatedDate == default(DateTime) ? DBNull.Value : adminLabel?.CreatedDate);
                        command.Parameters.AddWithValue("@pCreatedBy", adminLabel?.CreatedBy != null ? adminLabel.CreatedBy : DBNull.Value);
                        

                        var AdminLableIdParam = new SqlParameter("@pLabelId", SqlDbType.BigInt)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(AdminLableIdParam);
                        await command.ExecuteNonQueryAsync();
                        long newAdminLableIdParamId = (long)AdminLableIdParam.Value;
                        if (newAdminLableIdParamId != 0)
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

        public async Task<int> DeleteAdminLablesAsync(long id)
        {
            int response = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("DeleteAdminLabelsById", connection))
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

        public async Task<AdminLabel> GetAdminLablesByIdAsync(long id)
        {
            AdminLabel adminLabel = new AdminLabel();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetAdminLabelsById", connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.CommandType = CommandType.StoredProcedure;
                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    return new AdminLabel
                                    {
                                        LabelId = reader.GetInt64(reader.GetOrdinal("LabelId")),
                                        LabelName = reader.GetString(reader.GetOrdinal("LabelName")),
                                        LabelDescription = reader.GetString(reader.GetOrdinal("LabelDescription")),
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
        
        public async Task<AdminLablesViewModel> GetAllAdminLablesAsync(int pageNumber, int? pageSize, string? name)
        {
            var adminlabels = new List<AdminLabel>();
            var totalCount = 0;


            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    //Get total count
                    using (var command = new SqlCommand("SELECT COUNT(*) FROM AdminLabels", connection))
                    {
                        try
                        {
                            totalCount = (int)await command.ExecuteScalarAsync();
                        }
                        catch
                        {
                        }
                    }
                    using (var command = new SqlCommand("GetAllLabels", connection))
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
                                    adminlabels.Add(new AdminLabel
                                    {
                                        LabelId = reader.GetInt64(reader.GetOrdinal("LabelId")),
                                        LabelName = reader.GetString(reader.GetOrdinal("LabelName")),
                                        LabelDescription = reader.GetString(reader.GetOrdinal("LabelDescription")),
                                        CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                        CreatedBy = reader.GetInt64(reader.GetOrdinal("CreatedBy")),
                                        ModifiedDate = reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                                        ModifiedBy = reader.GetInt64(reader.GetOrdinal("ModifiedBy")),
                                        IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled"))
                                    });

                                }
                                if (!string.IsNullOrEmpty(name))
                                {
                                    totalCount = adminlabels.Count;

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
            return new AdminLablesViewModel
            {
                AdminLabels = adminlabels,
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<List<AdminLabel>> GetAllEnabledAdminLablesAsync()
        {
            var adminlabelsList = new List<AdminLabel>();
           


            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    
                    using (var command = new SqlCommand("GetAllEnabledLabels", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                     
                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {


                                while (await reader.ReadAsync())
                                {
                                    adminlabelsList.Add(new AdminLabel
                                    {
                                        LabelId = reader.GetInt64(reader.GetOrdinal("LabelId")),
                                        LabelName = reader.GetString(reader.GetOrdinal("LabelName")),
                                        
                                        IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled"))
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
            return adminlabelsList;
        }

        public async Task<int> UpdateAdminLablesAsync(AdminLabel adminLabel)
        {
            var RowsAffectedResponse = 0;


            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("UpdateAdminLabels", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pLabelName", adminLabel.LabelName);
                        command.Parameters.AddWithValue("@pLabelDescription", adminLabel.LabelDescription);
                        command.Parameters.AddWithValue("@pIsEnabled", adminLabel.IsEnabled);
                        command.Parameters.AddWithValue("@ModifiedDate", adminLabel?.ModifiedDate == default(DateTime) ? DBNull.Value : adminLabel?.ModifiedDate);
                        command.Parameters.AddWithValue("@ModifiedBy", adminLabel?.ModifiedBy != null ? adminLabel.ModifiedBy : DBNull.Value);
                        command.Parameters.AddWithValue("@pLabelId", adminLabel?.LabelId);

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
