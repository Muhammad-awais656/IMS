using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMS.Services
{
    public class AdminMeasuringUnitTypesService : IAdminMeasuringUnitTypesService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<AdminMeasuringUnitTypesService> _logger;
        public AdminMeasuringUnitTypesService(IDbContextFactory dbContextFactory, ILogger<AdminMeasuringUnitTypesService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }
        public async Task<bool> CreateAdminMeasuringUnitTypesAsync(AdminMeasuringUnitType adminMeasuringUnitType)
        {
            bool response = false;
            try
            {


                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("AddAdminMeasuringUnitType", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pMeasuringUnitTypeName", adminMeasuringUnitType.MeasuringUnitTypeName);
                        command.Parameters.AddWithValue("@pMeasuringUnitTypeDescription", adminMeasuringUnitType.MeasuringUnitTypeDescription);
                        command.Parameters.AddWithValue("@pIsEnabled", adminMeasuringUnitType.IsEnabled);
                        
                        command.Parameters.AddWithValue("@pCreatedDate", adminMeasuringUnitType?.CreatedDate == default(DateTime) ? DBNull.Value : adminMeasuringUnitType?.CreatedDate);
                        command.Parameters.AddWithValue("@pCreatedBy", adminMeasuringUnitType?.CreatedBy != null ? adminMeasuringUnitType.CreatedBy : DBNull.Value);


                        var unitTypeyIdParam = new SqlParameter("@pMeasuringUnitTypeId", SqlDbType.BigInt)
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

        public async Task<int> DeleteAdminMeasuringUnitTypesAsync(long id)
        {
            int response = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("DeleteAdminMeasuringUnitType", connection))
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
        
        public async Task<AdminMeasuringUnitType> GetAdminMeasuringUnitTypesByIdAsync(long id)
        {
            //AdminMeasuringUnitType adminMeasuringUnitType = new AdminMeasuringUnitType();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetAdminMeasuringUnitTypeDetails", connection))
                    {
                        command.Parameters.AddWithValue("@pMeasuringUnitTypeId", id);
                        command.CommandType = CommandType.StoredProcedure;
                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    return new AdminMeasuringUnitType
                                    {
                                        MeasuringUnitTypeId = reader.GetInt64(reader.GetOrdinal("MeasuringUnitTypeId")),
                                        MeasuringUnitTypeName = reader.GetString(reader.GetOrdinal("MeasuringUnitTypeName")),
                                        MeasuringUnitTypeDescription = reader.GetString(reader.GetOrdinal("MeasuringUnitTypeDescription")),
                                       
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

        public async Task<AdminMeasuringUnitTypesViewModel> GetAllAdminMeasuringUnitTypesAsync(int pageNumber, int? pageSize, string? name)
        {
            var adminMeasuringUnitTypes = new List<AdminMeasuringUnitType>();
            var totalCount = 0;


            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    //Get total count
                    using (var command = new SqlCommand("SELECT COUNT(*) FROM AdminMeasuringUnitTypes", connection))
                    {
                        try
                        {
                            totalCount = (int)await command.ExecuteScalarAsync();
                        }
                        catch
                        {
                        }
                    }
                    using (var command = new SqlCommand("GetAllAdminMeasuringUnitTypes", connection))
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
                                    adminMeasuringUnitTypes.Add(new AdminMeasuringUnitType
                                    {
                                        MeasuringUnitTypeId = reader.GetInt64(reader.GetOrdinal("MeasuringUnitTypeId")),
                                        MeasuringUnitTypeName = reader.GetString(reader.GetOrdinal("MeasuringUnitTypeName")),
                                        MeasuringUnitTypeDescription = reader.GetString(reader.GetOrdinal("MeasuringUnitTypeDescription")),
                                        
                                        CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                        CreatedBy = reader.GetInt64(reader.GetOrdinal("CreatedBy")),
                                        ModifiedDate = reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                                        ModifiedBy = reader.GetInt64(reader.GetOrdinal("ModifiedBy")),
                                        IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled"))
                                    });

                                }
                                if (!string.IsNullOrEmpty(name))
                                {
                                    totalCount = adminMeasuringUnitTypes.Count;

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
            return new AdminMeasuringUnitTypesViewModel
            {
                AdminMeasuringUnitTypes = adminMeasuringUnitTypes,
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<List<AdminMeasuringUnitType>> GetAllEnabledMeasuringUnitTypesAsync()
        {
            var adminMeasuringUnitTypesList = new List<AdminMeasuringUnitType>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("GetAllEnabledAdminMeasuringUnitTypes", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {


                                while (await reader.ReadAsync())
                                {
                                    adminMeasuringUnitTypesList.Add(new AdminMeasuringUnitType
                                    {
                                        MeasuringUnitTypeId = reader.GetInt64(reader.GetOrdinal("MeasuringUnitTypeId")),
                                        MeasuringUnitTypeName = reader.GetString(reader.GetOrdinal("MeasuringUnitTypeName")),
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

            return adminMeasuringUnitTypesList;
        }

        public async Task<int> UpdateAdminMeasuringUnitTypesAsync(AdminMeasuringUnitType adminMeasuringUnitType)
        {
            var RowsAffectedResponse = 0;


            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("UpdateAdminMeasuringUnitType", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pMeasuringUnitTypeName", adminMeasuringUnitType.MeasuringUnitTypeName);
                        command.Parameters.AddWithValue("@pMeasuringUnitTypeDescription", adminMeasuringUnitType.MeasuringUnitTypeDescription);
                        command.Parameters.AddWithValue("@pIsEnabled", adminMeasuringUnitType.IsEnabled);
                       
                        command.Parameters.AddWithValue("@pModifiedDate", adminMeasuringUnitType?.ModifiedDate == default(DateTime) ? DBNull.Value : adminMeasuringUnitType?.ModifiedDate);
                        command.Parameters.AddWithValue("@pModifiedBy", adminMeasuringUnitType?.ModifiedBy);
                        command.Parameters.AddWithValue("@pMeasuringUnitTypeId", adminMeasuringUnitType?.MeasuringUnitTypeId);

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
