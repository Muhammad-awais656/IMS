using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMS.Services
{
    public class UnitConversionService : IUnitConversionService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<UnitConversionService> _logger;

        public UnitConversionService(IDbContextFactory dbContextFactory, ILogger<UnitConversionService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<List<UnitConversion>> GetAllUnitConversionsAsync()
        {
            var conversions = new List<UnitConversion>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(@"
                        SELECT 
                            UnitConversionId, FromUnitId, ToUnitId, ConversionFactor, 
                            Description, IsEnabled, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy
                        FROM UnitConversions
                        ORDER BY FromUnitId, ToUnitId", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                conversions.Add(new UnitConversion
                                {
                                    UnitConversionId = reader.GetInt64(reader.GetOrdinal("UnitConversionId")),
                                    FromUnitId = reader.GetInt64(reader.GetOrdinal("FromUnitId")),
                                    ToUnitId = reader.GetInt64(reader.GetOrdinal("ToUnitId")),
                                    ConversionFactor = reader.GetDecimal(reader.GetOrdinal("ConversionFactor")),
                                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                                    IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled")),
                                    CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                    CreatedBy = reader.GetInt64(reader.GetOrdinal("CreatedBy")),
                                    ModifiedDate = reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                                    ModifiedBy = reader.GetInt64(reader.GetOrdinal("ModifiedBy"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all unit conversions");
                throw;
            }
            return conversions;
        }

        public async Task<UnitConversion> GetUnitConversionByIdAsync(long id)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(@"
                        SELECT 
                            UnitConversionId, FromUnitId, ToUnitId, ConversionFactor, 
                            Description, IsEnabled, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy
                        FROM UnitConversions
                        WHERE UnitConversionId = @Id", connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new UnitConversion
                                {
                                    UnitConversionId = reader.GetInt64(reader.GetOrdinal("UnitConversionId")),
                                    FromUnitId = reader.GetInt64(reader.GetOrdinal("FromUnitId")),
                                    ToUnitId = reader.GetInt64(reader.GetOrdinal("ToUnitId")),
                                    ConversionFactor = reader.GetDecimal(reader.GetOrdinal("ConversionFactor")),
                                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                                    IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled")),
                                    CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                    CreatedBy = reader.GetInt64(reader.GetOrdinal("CreatedBy")),
                                    ModifiedDate = reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                                    ModifiedBy = reader.GetInt64(reader.GetOrdinal("ModifiedBy"))
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unit conversion by ID: {Id}", id);
                throw;
            }
            return null;
        }

        public async Task<bool> CreateUnitConversionAsync(UnitConversion unitConversion)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(@"
                        INSERT INTO UnitConversions 
                            (FromUnitId, ToUnitId, ConversionFactor, Description, IsEnabled, 
                             CreatedDate, CreatedBy, ModifiedDate, ModifiedBy)
                        VALUES 
                            (@FromUnitId, @ToUnitId, @ConversionFactor, @Description, @IsEnabled, 
                             @CreatedDate, @CreatedBy, @ModifiedDate, @ModifiedBy);
                        SELECT CAST(SCOPE_IDENTITY() AS BIGINT);", connection))
                    {
                        command.Parameters.AddWithValue("@FromUnitId", unitConversion.FromUnitId);
                        command.Parameters.AddWithValue("@ToUnitId", unitConversion.ToUnitId);
                        command.Parameters.AddWithValue("@ConversionFactor", unitConversion.ConversionFactor);
                        command.Parameters.AddWithValue("@Description", (object)unitConversion.Description ?? DBNull.Value);
                        command.Parameters.AddWithValue("@IsEnabled", unitConversion.IsEnabled);
                        command.Parameters.AddWithValue("@CreatedDate", unitConversion.CreatedDate);
                        command.Parameters.AddWithValue("@CreatedBy", unitConversion.CreatedBy);
                        command.Parameters.AddWithValue("@ModifiedDate", unitConversion.ModifiedDate);
                        command.Parameters.AddWithValue("@ModifiedBy", unitConversion.ModifiedBy);

                        var result = await command.ExecuteScalarAsync();
                        return result != null && Convert.ToInt64(result) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating unit conversion");
                throw;
            }
        }

        public async Task<int> UpdateUnitConversionAsync(UnitConversion unitConversion)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(@"
                        UPDATE UnitConversions 
                        SET FromUnitId = @FromUnitId, 
                            ToUnitId = @ToUnitId, 
                            ConversionFactor = @ConversionFactor, 
                            Description = @Description, 
                            IsEnabled = @IsEnabled, 
                            ModifiedDate = @ModifiedDate, 
                            ModifiedBy = @ModifiedBy
                        WHERE UnitConversionId = @UnitConversionId", connection))
                    {
                        command.Parameters.AddWithValue("@UnitConversionId", unitConversion.UnitConversionId);
                        command.Parameters.AddWithValue("@FromUnitId", unitConversion.FromUnitId);
                        command.Parameters.AddWithValue("@ToUnitId", unitConversion.ToUnitId);
                        command.Parameters.AddWithValue("@ConversionFactor", unitConversion.ConversionFactor);
                        command.Parameters.AddWithValue("@Description", (object)unitConversion.Description ?? DBNull.Value);
                        command.Parameters.AddWithValue("@IsEnabled", unitConversion.IsEnabled);
                        command.Parameters.AddWithValue("@ModifiedDate", unitConversion.ModifiedDate);
                        command.Parameters.AddWithValue("@ModifiedBy", unitConversion.ModifiedBy);

                        return await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating unit conversion: {Id}", unitConversion.UnitConversionId);
                throw;
            }
        }

        public async Task<int> DeleteUnitConversionAsync(long id)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("DELETE FROM UnitConversions WHERE UnitConversionId = @Id", connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        return await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting unit conversion: {Id}", id);
                throw;
            }
        }

        public async Task<decimal?> ConvertUnitAsync(long fromUnitId, long toUnitId, decimal quantity)
        {
            // If same unit, return as is
            if (fromUnitId == toUnitId)
            {
                return quantity;
            }

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    // Try direct conversion
                    using (var command = new SqlCommand(@"
                        SELECT ConversionFactor 
                        FROM UnitConversions 
                        WHERE FromUnitId = @FromUnitId 
                          AND ToUnitId = @ToUnitId 
                          AND IsEnabled = 1", connection))
                    {
                        command.Parameters.AddWithValue("@FromUnitId", fromUnitId);
                        command.Parameters.AddWithValue("@ToUnitId", toUnitId);
                        
                        var factor = await command.ExecuteScalarAsync();
                        if (factor != null)
                        {
                            return quantity * Convert.ToDecimal(factor);
                        }
                    }

                    // Try reverse conversion
                    using (var command = new SqlCommand(@"
                        SELECT ConversionFactor 
                        FROM UnitConversions 
                        WHERE FromUnitId = @ToUnitId 
                          AND ToUnitId = @FromUnitId 
                          AND IsEnabled = 1", connection))
                    {
                        command.Parameters.AddWithValue("@FromUnitId", fromUnitId);
                        command.Parameters.AddWithValue("@ToUnitId", toUnitId);
                        
                        var factor = await command.ExecuteScalarAsync();
                        if (factor != null)
                        {
                            return quantity / Convert.ToDecimal(factor);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting unit from {FromUnitId} to {ToUnitId}", fromUnitId, toUnitId);
            }

            return null; // Conversion not found
        }

        public async Task<List<UnitConversion>> GetConversionsByFromUnitAsync(long fromUnitId)
        {
            var conversions = new List<UnitConversion>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(@"
                        SELECT 
                            UnitConversionId, FromUnitId, ToUnitId, ConversionFactor, 
                            Description, IsEnabled, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy
                        FROM UnitConversions
                        WHERE FromUnitId = @FromUnitId AND IsEnabled = 1
                        ORDER BY ToUnitId", connection))
                    {
                        command.Parameters.AddWithValue("@FromUnitId", fromUnitId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                conversions.Add(new UnitConversion
                                {
                                    UnitConversionId = reader.GetInt64(reader.GetOrdinal("UnitConversionId")),
                                    FromUnitId = reader.GetInt64(reader.GetOrdinal("FromUnitId")),
                                    ToUnitId = reader.GetInt64(reader.GetOrdinal("ToUnitId")),
                                    ConversionFactor = reader.GetDecimal(reader.GetOrdinal("ConversionFactor")),
                                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                                    IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled")),
                                    CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                    CreatedBy = reader.GetInt64(reader.GetOrdinal("CreatedBy")),
                                    ModifiedDate = reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                                    ModifiedBy = reader.GetInt64(reader.GetOrdinal("ModifiedBy"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversions by from unit: {FromUnitId}", fromUnitId);
                throw;
            }
            return conversions;
        }

        public async Task<List<UnitConversion>> GetEnabledConversionsAsync()
        {
            var conversions = new List<UnitConversion>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(@"
                        SELECT 
                            UnitConversionId, FromUnitId, ToUnitId, ConversionFactor, 
                            Description, IsEnabled, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy
                        FROM UnitConversions
                        WHERE IsEnabled = 1
                        ORDER BY FromUnitId, ToUnitId", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                conversions.Add(new UnitConversion
                                {
                                    UnitConversionId = reader.GetInt64(reader.GetOrdinal("UnitConversionId")),
                                    FromUnitId = reader.GetInt64(reader.GetOrdinal("FromUnitId")),
                                    ToUnitId = reader.GetInt64(reader.GetOrdinal("ToUnitId")),
                                    ConversionFactor = reader.GetDecimal(reader.GetOrdinal("ConversionFactor")),
                                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                                    IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled")),
                                    CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                    CreatedBy = reader.GetInt64(reader.GetOrdinal("CreatedBy")),
                                    ModifiedDate = reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                                    ModifiedBy = reader.GetInt64(reader.GetOrdinal("ModifiedBy"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enabled unit conversions");
                throw;
            }
            return conversions;
        }
    }
}





