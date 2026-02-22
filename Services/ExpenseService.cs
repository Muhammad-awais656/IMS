using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMS.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<ExpenseService> _logger;
        public ExpenseService(IDbContextFactory dbContextFactory, ILogger<ExpenseService> logger)
        {
                _dbContextFactory = dbContextFactory;
            _logger = logger;
        }
        public async Task<bool> CreateExpenseAsync(Expense expense)
        {
            bool response = false;
            try
            {


                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("AddExpense", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pExpenseDetail", expense.ExpenseDetail);
                        command.Parameters.AddWithValue("@pExpenseTypeId_FK", expense.ExpenseTypeIdFk);
                        command.Parameters.AddWithValue("@pAmount", expense.Amount);
                        command.Parameters.AddWithValue("@pCreatedDate", expense?.CreatedDate == default(DateTime) ? DBNull.Value : expense?.CreatedDate);
                        command.Parameters.AddWithValue("@pModifiedDate", expense?.ModifiedDate == default(DateTime) ? DBNull.Value : expense?.ModifiedDate);
                        command.Parameters.AddWithValue("@pExpenseDate", expense?.ExpenseDate == default(DateTime) ? DBNull.Value : expense?.ExpenseDate);
                        
                        command.Parameters.AddWithValue("@pCreatedBy", expense?.CreatedBy != null ? expense.CreatedBy : DBNull.Value);
                        command.Parameters.AddWithValue("@pModifiedBy", expense?.ModifiedBy != null ? expense.ModifiedBy : DBNull.Value);
                        command.Parameters.AddWithValue("@pProductId", expense?.ProductId_FK == 0 || expense?.ProductId_FK==null ? DBNull.Value : expense?.ProductId_FK);


                        var ExpenseIdParam = new SqlParameter("@pExpenseId", SqlDbType.BigInt)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(ExpenseIdParam);
                        await command.ExecuteNonQueryAsync();
                        long ExpenseIdParamId = (long)ExpenseIdParam.Value;
                        if (ExpenseIdParamId != 0)
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

        public async Task<int> DeleteExpenseAsync(long id)
        {
            int response = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("DeleteExpenseById", connection))
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

        public async Task<ExpenseViewModel> GetAllExpenseAsync(int pageNumber, int? pageSize, ExpenseFilters expenseFilters)
        {
            var expenses = new List<ExpenseModel>();
            var totalCount = 0;


            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    //Get total count
                    using (var command = new SqlCommand("GetAllExpenseCount", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                        command.Parameters.AddWithValue("@ExpenseDetail", expenseFilters.Details ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pExpenseTypeId", expenseFilters.ExpenseTypeId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pProductId", expenseFilters.ProductId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@AmountFrom", expenseFilters.AmountFrom ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@AmountTo", expenseFilters.AmountTo ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@ExpenseDateFrom", expenseFilters.DateFrom == default(DateTime) ? DBNull.Value : expenseFilters.DateFrom);
                        command.Parameters.AddWithValue("@ExpenseDateTo", expenseFilters.DateTo == default(DateTime) ? DBNull.Value : expenseFilters.DateTo);

                        try
                        {
                            totalCount = (int)await command.ExecuteScalarAsync();
                        }
                        catch
                        {
                        }
                    }
                    using (var command = new SqlCommand("GetAllExpense", connection))
                    {
                         


                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@PageNumber", pageNumber);
                        command.Parameters.AddWithValue("@PageSize", pageSize);
                        command.Parameters.AddWithValue("@ExpenseDetail", expenseFilters.Details ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pExpenseTypeId", expenseFilters.ExpenseTypeId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pProductId", expenseFilters.ProductId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@AmountFrom", expenseFilters.AmountFrom ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@AmountTo", expenseFilters.AmountTo ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@ExpenseDateFrom", expenseFilters.DateFrom == default(DateTime) ? DBNull.Value : expenseFilters.DateFrom);
                        command.Parameters.AddWithValue("@ExpenseDateTo", expenseFilters.DateTo == default(DateTime) ? DBNull.Value : expenseFilters.DateTo);

                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {


                                while (await reader.ReadAsync())
                                {
                                    expenses.Add(new ExpenseModel
                                    {
                                        ExpenseId = reader.GetInt64(reader.GetOrdinal("ExpenseId")),
                                        ProductId_FK = reader.IsDBNull(reader.GetOrdinal("ProductId_FK"))? null: reader.GetInt64(reader.GetOrdinal("ProductId_FK")),
                                        ExpenseType = reader.GetString(reader.GetOrdinal("ExpenseTypeName")),
                                        ExpenseDetail = reader.GetString(reader.GetOrdinal("ExpenseDetail")),
                                        ProductName = reader.IsDBNull(reader.GetOrdinal("ProductName")) ? null : reader.GetString(reader.GetOrdinal("ProductName")),
                                        Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                                        
                                        ExpenseDate = reader.GetDateTime(reader.GetOrdinal("ExpenseDate"))
                                        
                                        

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
            return new ExpenseViewModel
            {
                ExpenseList = expenses,
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<Expense> GetExpenseByIdAsync(long id)
        {
            Expense expense = new Expense();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetExpensesById", connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.CommandType = CommandType.StoredProcedure;
                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    var expenseTypeIdOrdinal = reader.GetOrdinal("ExpenseTypeId_FK");
                                    var productIdOrdinal = reader.GetOrdinal("ProductId_FK");
                                    
                                    return new Expense
                                    {
                                        ExpenseId = reader.GetInt64(reader.GetOrdinal("ExpenseId")),
                                        ExpenseTypeIdFk = reader.IsDBNull(expenseTypeIdOrdinal) ? null : reader.GetInt64(expenseTypeIdOrdinal),
                                        ExpenseDetail = reader.IsDBNull(reader.GetOrdinal("ExpenseDetail")) ? string.Empty : reader.GetString(reader.GetOrdinal("ExpenseDetail")),
                                        Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                                        CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                        CreatedBy = reader.GetInt64(reader.GetOrdinal("CreatedBy")),
                                        ModifiedDate = reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                                        ExpenseDate = reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
                                        ModifiedBy = reader.GetInt64(reader.GetOrdinal("ModifiedBy")),
                                        ProductId_FK = reader.IsDBNull(productIdOrdinal) ? null : reader.GetInt64(productIdOrdinal),

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
            return expense;
        }

        public async Task<int> UpdateExpenseAsync(Expense expense)
        {
            var RowsAffectedResponse = 0;


            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                   
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("UpdateExpense", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pExpenseDetail", expense.ExpenseDetail);
                        command.Parameters.AddWithValue("@pExpenseTypeId_FK", expense?.ExpenseTypeIdFk != null ? expense.ExpenseTypeIdFk : DBNull.Value);
                        command.Parameters.AddWithValue("@pAmount", expense.Amount );
                        
                        command.Parameters.AddWithValue("@pModifiedDate", expense?.ModifiedDate == default(DateTime) ? DBNull.Value : expense?.ModifiedDate);
                        command.Parameters.AddWithValue("@pExpenseDate", expense?.ExpenseDate == default(DateTime) ? DBNull.Value : expense?.ExpenseDate);
                        command.Parameters.AddWithValue("@pModifiedBy", expense?.ModifiedBy != null ? expense.ModifiedBy : DBNull.Value);
                        
                        command.Parameters.AddWithValue("@pExpenseId", expense?.ExpenseId);

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
