using DocumentFormat.OpenXml.Wordprocessing;
using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMS.Services
{
    public class AdminMeasuringUnitService : IAdminMeasuringUnitService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<AdminMeasuringUnitService> _logger;
        public AdminMeasuringUnitService(IDbContextFactory dbContextFactory, ILogger<AdminMeasuringUnitService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<List<AdminMeasuringUnitType>> AdminMeasuringUnitTypeCacheAsync()
        {
            var adminMeasuringUnitTypes = new List<AdminMeasuringUnitType>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetAllMeasuringUnitTypesCache", connection))
                    {
                        
                        command.CommandType = CommandType.StoredProcedure;
                        
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
            return
            adminMeasuringUnitTypes;
                

           
        }

        public async Task<bool> CreateAdminMeasuringUnitAsync(AdminMeasuringUnit adminMeasuringUnit)
        {
            bool response = false;
            try
            {


                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("AddMeasuringUnit", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pMeasuringUnitName", adminMeasuringUnit.MeasuringUnitName);
                        command.Parameters.AddWithValue("@pMeasuringUnitDescription", adminMeasuringUnit.MeasuringUnitDescription);
                        command.Parameters.AddWithValue("@pMeasuringUnitTypeId_FK", adminMeasuringUnit.MeasuringUnitTypeIdFk);
                        command.Parameters.AddWithValue("@pIsSmallestUnit", adminMeasuringUnit?.IsSmallestUnit==null ? 0:adminMeasuringUnit.IsSmallestUnit);
                        command.Parameters.AddWithValue("@pMeasuringUnitAbbreviation", adminMeasuringUnit.MeasuringUnitAbbreviation);
                        command.Parameters.AddWithValue("@pIsEnabled", adminMeasuringUnit.IsEnabled);

                        command.Parameters.AddWithValue("@pCreatedDate", adminMeasuringUnit?.CreatedDate == default(DateTime) ? DBNull.Value : adminMeasuringUnit?.CreatedDate);
                        command.Parameters.AddWithValue("@pCreatedBy", adminMeasuringUnit?.CreatedBy != null ? adminMeasuringUnit.CreatedBy : DBNull.Value);


                        var unitTypeyIdParam = new SqlParameter("@pMeasuringUnitId", SqlDbType.BigInt)
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

        public async Task<int> DeleteAdminMeasuringUnitAsync(long id)
        {
            int response = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("DeleteAdminMeasuringUnit", connection))
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
        
        public async Task<AdminMeasuringUnit> GetAdminMeasuringUnitByIdAsync(long id)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetAllMeasuringUnitsByMeasuringTypeId", connection))
                    {
                        command.Parameters.AddWithValue("@pMeasuringTypeId", id);
                        command.CommandType = CommandType.StoredProcedure;
                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    return new AdminMeasuringUnit
                                    {
                                        MeasuringUnitId = reader.GetInt64(reader.GetOrdinal("MeasuringUnitId")),
                                        MeasuringUnitName = reader.GetString(reader.GetOrdinal("MeasuringUnitName")),
                                        MeasuringUnitDescription = reader.GetString(reader.GetOrdinal("MeasuringUnitDescription")),
                                        MeasuringUnitAbbreviation = reader.GetString(reader.GetOrdinal("MeasuringUnitAbbreviation")),
                                        MeasuringUnitTypeIdFk = reader.GetInt64(reader.GetOrdinal("MeasuringUnitTypeId_FK")),
                                        
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

        public async Task<MeasuringUnitViewModel> GetAllAdminMeasuringUnitAsync(int pageNumber, int? pageSize, string? name)
        {
            var adminMeasuringUnits = new List<AdminMeasuringUnit>();
            var adminMeasuringUnitTypes = new List<AdminMeasuringUnitType>();
            int totalCount = 0;

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    // Total count
                    using (var countCmd = new SqlCommand("SELECT COUNT(*) FROM AdminMeasuringUnits", connection))
                    {
                        totalCount = (int)await countCmd.ExecuteScalarAsync();
                    }

                    using (var cmd = new SqlCommand("GetAllMeasuringUnits", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SearchName", name ?? (object)DBNull.Value);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                // Main Measuring Unit
                                adminMeasuringUnits.Add(new AdminMeasuringUnit
                                {
                                    MeasuringUnitId = reader.GetInt64(reader.GetOrdinal("MeasuringUnitId")),
                                    MeasuringUnitName = reader.GetString(reader.GetOrdinal("MeasuringUnitName")),
                                    MeasuringUnitDescription = reader.GetString(reader.GetOrdinal("MeasuringUnitDescription")),
                                    CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                    CreatedBy = reader.GetInt64(reader.GetOrdinal("CreatedBy")),
                                    ModifiedDate = reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                                    ModifiedBy = reader.GetInt64(reader.GetOrdinal("ModifiedBy")),
                                    IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled")),
                                    MeasuringUnitTypeIdFk = reader.GetInt64(reader.GetOrdinal("MeasuringUnitTypeId_FK"))
                                });

                                // Measuring Unit Type Name from another entity
                                adminMeasuringUnitTypes.Add(new AdminMeasuringUnitType
                                {
                                    MeasuringUnitTypeName = reader.GetString(reader.GetOrdinal("MeasuringUnitTypeName")),
                                    MeasuringUnitTypeId = reader.GetInt64(reader.GetOrdinal("MeasuringUnitTypeId")),
                                });
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(name))
                    {
                        totalCount = adminMeasuringUnits.Count;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

            return new MeasuringUnitViewModel
            {
                AdminMeasuringUnits = adminMeasuringUnits,
                AdminMeasuringUnitTypes = adminMeasuringUnitTypes,
                CurrentPage = pageNumber,
                TotalPages = pageSize.HasValue && pageSize.Value > 0
                    ? (int)Math.Ceiling(totalCount / (double)pageSize.Value)
                    : 1,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<List<AdminMeasuringUnit>> GetAllMeasuringUnitsByMUTIdAsync(long? id)
        {
            var adminMeasuringUnits = new List<AdminMeasuringUnit>();
   
           

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                   

                    using (var cmd = new SqlCommand("GetAllMeasuringUnitsByMeasuringTypeId", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                 
                        cmd.Parameters.AddWithValue("@pMeasuringTypeId", id ?? (object)DBNull.Value);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                // Main Measuring Unit
                                adminMeasuringUnits.Add(new AdminMeasuringUnit
                                {
                                    MeasuringUnitId = reader.GetInt64(reader.GetOrdinal("MeasuringUnitId")),
                                    MeasuringUnitName = reader.GetString(reader.GetOrdinal("MeasuringUnitName")),
                                    
                                });

                               
                            }
                        }
                    }

                   
                }
            }
            catch 
            {
                
            }

            return adminMeasuringUnits;
        }

        public async Task<int> UpdateAdminMeasuringUnitAsync(AdminMeasuringUnit adminMeasuringUnit)
        {
            var RowsAffectedResponse = 0;


            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("UpdateMeasuringUnit", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pMeasuringUnitName", adminMeasuringUnit.MeasuringUnitName);
                        command.Parameters.AddWithValue("@pMeasuringUnitDescription", adminMeasuringUnit.MeasuringUnitDescription);
                        command.Parameters.AddWithValue("@pMeasuringUnitTypeId_FK", adminMeasuringUnit.MeasuringUnitTypeIdFk);
                        command.Parameters.AddWithValue("@pIsSmallestUnit", adminMeasuringUnit?.IsSmallestUnit==null ? 0: adminMeasuringUnit.IsSmallestUnit);
                        command.Parameters.AddWithValue("@pMeasuringUnitAbbreviation", adminMeasuringUnit?.MeasuringUnitAbbreviation);
                        command.Parameters.AddWithValue("@pIsEnabled", adminMeasuringUnit.IsEnabled);

                        command.Parameters.AddWithValue("@pModifiedDate", adminMeasuringUnit?.ModifiedDate == default(DateTime) ? DBNull.Value : adminMeasuringUnit?.ModifiedDate);
                        command.Parameters.AddWithValue("@pModifiedBy", adminMeasuringUnit?.ModifiedBy);
                        command.Parameters.AddWithValue("@pMeasuringUnitId", adminMeasuringUnit?.MeasuringUnitId);

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
