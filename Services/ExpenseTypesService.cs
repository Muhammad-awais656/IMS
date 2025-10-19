using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMS.Services
{
    public class ExpenseTypesService: IExpenseType
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<ExpenseTypesService> _logger;
        public ExpenseTypesService(IDbContextFactory dbContextFactory,ILogger<ExpenseTypesService> logger)
        {
             _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<bool> CreateExpenseTypeAsync(AdminExpenseType ExpenseType)
        {
            bool response = false;
            try
            {
                
                
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("AddExpenseType", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pExpenseTypeName", ExpenseType.ExpenseTypeName);
                        command.Parameters.AddWithValue("@pExpenseTypeDescription", ExpenseType.ExpenseTypeDescription ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pIsEnabled", ExpenseType.IsEnabled);
                        command.Parameters.AddWithValue("@pCreatedDate", ExpenseType?.CreatedDate == default(DateTime) ? DBNull.Value : ExpenseType?.CreatedDate);
                        command.Parameters.AddWithValue("@pCreatedBy", ExpenseType?.CreatedBy!=null ? ExpenseType.CreatedBy : DBNull.Value);
                        command.Parameters.AddWithValue("@pModifiedDate",ExpenseType?.ModifiedDate == default(DateTime) ? DBNull.Value : ExpenseType?.ModifiedDate);
                        command.Parameters.AddWithValue("@pModifiedBy", ExpenseType?.ModifiedBy != null ? ExpenseType.ModifiedBy : DBNull.Value);

                        var userIdParam = new SqlParameter("@pExpenseTypeId", SqlDbType.BigInt)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(userIdParam);
                        await command.ExecuteNonQueryAsync();
                        long newUserId = (long)userIdParam.Value;
                        if (newUserId != 0)
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

        public async Task<int> DeleteExpenseTypeAsync(long id)
        {
            int response = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("DeleteExpenseTypeById", connection))
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

        public async Task<List<AdminExpenseType>> GetAllEnabledExpenseTypesAsync()
        {
            var expenseTypes = new List<AdminExpenseType>();
       
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    //Get total count
                    
                    using (var command = new SqlCommand("GetAllEnabledExpenseType", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                       

                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {


                                while (await reader.ReadAsync())
                                {
                                    expenseTypes.Add(new AdminExpenseType
                                    {
                                        ExpenseTypeId = reader.GetInt64(reader.GetOrdinal("ExpenseTypeId")),
                                        ExpenseTypeName = reader.GetString(reader.GetOrdinal("ExpenseTypeName")),
                                        
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
            return expenseTypes;

        }

        public async Task<ExpenseTypesViewModel> GetAllExpenseTypes(int pageNumber, int? pageSize,string? expenseTypeName)
        {
            var expenseTypes = new List<AdminExpenseType>();
            var totalCount = 0;


            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    //Get total count
                    using (var command = new SqlCommand("SELECT COUNT(*) FROM AdminExpenseTypes", connection))
                    {
                        try
                        {
                            totalCount = (int)await command.ExecuteScalarAsync();
                        }
                        catch 
                        {
                        }
                    }
                    using (var command = new SqlCommand("GetAllExpenseType", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@PageNumber", pageNumber);
                        command.Parameters.AddWithValue("@PageSize", pageSize);
                        command.Parameters.AddWithValue("@SearchName", expenseTypeName);
                        
                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                
                                    
                                    while (await reader.ReadAsync())
                                    {
                                    expenseTypes.Add(new AdminExpenseType
                                        {
                                            ExpenseTypeId = reader.GetInt64(reader.GetOrdinal("ExpenseTypeId")),
                                            ExpenseTypeName = reader.GetString(reader.GetOrdinal("ExpenseTypeName")),
                                            ExpenseTypeDescription = reader.IsDBNull(reader.GetOrdinal("ExpenseTypeDescription")) ? null : reader.GetString(reader.GetOrdinal("ExpenseTypeDescription")),
                                            CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                            CreatedBy = reader.GetInt64(reader.GetOrdinal("CreatedBy")),
                                            ModifiedDate = reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                                            ModifiedBy = reader.GetInt64(reader.GetOrdinal("ModifiedBy")),
                                            IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled"))
                                        });

                                    }
                                if (!string.IsNullOrEmpty(expenseTypeName))
                                {
                                    totalCount=expenseTypes.Count;

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
            return new ExpenseTypesViewModel
            {
                ExpenseTypes = expenseTypes,
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                PageSize = pageSize,
                TotalCount = totalCount
            };

        }

        public async Task<AdminExpenseType> GetExpenseTypeByIdAsync(long id)
        {
            AdminExpenseType user = new AdminExpenseType();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetExpenseTypeById", connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.CommandType = CommandType.StoredProcedure;
                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    return new AdminExpenseType
                                    {
                                        ExpenseTypeId = reader.GetInt64(reader.GetOrdinal("ExpenseTypeId")),
                                        ExpenseTypeName = reader.GetString(reader.GetOrdinal("ExpenseTypeName")),
                                        ExpenseTypeDescription = reader.IsDBNull(reader.GetOrdinal("ExpenseTypeDescription")) ? null : reader.GetString(reader.GetOrdinal("ExpenseTypeDescription")),
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

        public async Task<int> UpdateExpenseTypeAsync(AdminExpenseType ExpenseType)
        {
            var RowsAffectedResponse = 0;
            

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("UpdateExpenseType", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pExpenseTypeName", ExpenseType.ExpenseTypeName);
                        command.Parameters.AddWithValue("@pExpenseTypeDescription", ExpenseType.ExpenseTypeDescription ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pIsEnabled", ExpenseType.IsEnabled);
                        command.Parameters.AddWithValue("@pModifiedDate", ExpenseType?.ModifiedDate == default(DateTime) ? DBNull.Value : ExpenseType?.ModifiedDate);
                        command.Parameters.AddWithValue("@pModifiedBy", ExpenseType?.ModifiedBy != null ? ExpenseType.ModifiedBy : DBNull.Value);
                        command.Parameters.AddWithValue("@pExpenseTypeId", ExpenseType?.ExpenseTypeId);

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
