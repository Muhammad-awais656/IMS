using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMS.Services
{
    public class PersonalPaymentService : IPersonalPaymentService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<PersonalPaymentService> _logger;

        public PersonalPaymentService(IDbContextFactory dbContextFactory, ILogger<PersonalPaymentService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<PersonalPaymentViewModel> GetAllPersonalPaymentsAsync(int pageNumber, int pageSize, PersonalPaymentFilters filters)
        {
            var personalPayments = new List<PersonalPaymentModel>();
            var totalCount = 0;

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    // Get total count
                    using (var command = new SqlCommand("SELECT COUNT(*) FROM PersonalPayments", connection))
                    {
                        try
                        {
                            totalCount = (int)await command.ExecuteScalarAsync();
                        }
                        catch
                        {
                        }
                    }

                    using (var command = new SqlCommand("GetAllPersonalPayments", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@PageNumber", pageNumber);
                        command.Parameters.AddWithValue("@PageSize", pageSize);
                        command.Parameters.AddWithValue("@SearchBankName", string.IsNullOrEmpty(filters.BankName) ? DBNull.Value : filters.BankName);
                        command.Parameters.AddWithValue("@SearchAccountNumber", string.IsNullOrEmpty(filters.AccountNumber) ? DBNull.Value : filters.AccountNumber);
                        command.Parameters.AddWithValue("@SearchPaymentDescription", string.IsNullOrEmpty(filters.PaymentDescription) ? DBNull.Value : filters.PaymentDescription);
                        command.Parameters.AddWithValue("@CreditAmountFrom", filters.CreditAmountFrom ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@CreditAmountTo", filters.CreditAmountTo ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@DebitAmountFrom", filters.DebitAmountFrom ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@DebitAmountTo", filters.DebitAmountTo ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@DateFrom", filters.DateFrom == default(DateTime) ? DBNull.Value : filters.DateFrom);
                        command.Parameters.AddWithValue("@DateTo", filters.DateTo == default(DateTime) ? DBNull.Value : filters.DateTo);
                        command.Parameters.AddWithValue("@TransactionType", string.IsNullOrEmpty(filters.TransactionType) ? DBNull.Value : filters.TransactionType);
                        command.Parameters.AddWithValue("@IsActive", filters.IsActive ?? (object)DBNull.Value);

                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    personalPayments.Add(new PersonalPaymentModel
                                    {
                                        PersonalPaymentId = reader.GetInt64(reader.GetOrdinal("PersonalPaymentId")),
                                        BankName = reader.GetString(reader.GetOrdinal("BankName")),
                                        AccountNumber = reader.GetString(reader.GetOrdinal("AccountNumber")),
                                        AccountHolderName = reader.GetString(reader.GetOrdinal("AccountHolderName")),
                                        BankBranch = reader.IsDBNull(reader.GetOrdinal("BankBranch")) ? null : reader.GetString(reader.GetOrdinal("BankBranch")),
                                        CreditAmount = reader.GetDecimal(reader.GetOrdinal("CreditAmount")),
                                        DebitAmount = reader.GetDecimal(reader.GetOrdinal("DebitAmount")),
                                        PaymentDescription = reader.GetString(reader.GetOrdinal("PaymentDescription")),
                                        PaymentDate = reader.GetDateTime(reader.GetOrdinal("PaymentDate")),
                                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                        CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                        CreatedBy = "System", // You might want to join with user table
                                        ModifiedDate = reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                                        ModifiedBy = "System" // You might want to join with user table
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

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return new PersonalPaymentViewModel
            {
                PersonalPaymentList = personalPayments,
                PaymentFilters = filters,
                CurrentPage = pageNumber,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PersonalPayment?> GetPersonalPaymentByIdAsync(long id)
        {
            PersonalPayment personalPayment = new PersonalPayment();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetPersonalPaymentById", connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.CommandType = CommandType.StoredProcedure;
                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    return new PersonalPayment
                                    {
                                        PersonalPaymentId = reader.GetInt64(reader.GetOrdinal("PersonalPaymentId")),
                                        BankName = reader.GetString(reader.GetOrdinal("BankName")),
                                        AccountNumber = reader.GetString(reader.GetOrdinal("AccountNumber")),
                                        AccountHolderName = reader.GetString(reader.GetOrdinal("AccountHolderName")),
                                        BankBranch = reader.IsDBNull(reader.GetOrdinal("BankBranch")) ? null : reader.GetString(reader.GetOrdinal("BankBranch")),
                                        CreditAmount = reader.GetDecimal(reader.GetOrdinal("CreditAmount")),
                                        DebitAmount = reader.GetDecimal(reader.GetOrdinal("DebitAmount")),
                                        PaymentDescription = reader.GetString(reader.GetOrdinal("PaymentDescription")),
                                        PaymentDate = reader.GetDateTime(reader.GetOrdinal("PaymentDate")),
                                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                        CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                        CreatedBy = reader.GetInt64(reader.GetOrdinal("CreatedBy")),
                                        ModifiedDate = reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                                        ModifiedBy = reader.GetInt64(reader.GetOrdinal("ModifiedBy"))
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

        public async Task<bool> CreatePersonalPaymentAsync(PersonalPayment personalPayment)
        {
            bool response = false;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("AddPersonalPayment", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pBankName", personalPayment.BankName);
                        command.Parameters.AddWithValue("@pAccountNumber", personalPayment.AccountNumber);
                        command.Parameters.AddWithValue("@pAccountHolderName", personalPayment.AccountHolderName);
                        command.Parameters.AddWithValue("@pBankBranch", personalPayment.BankBranch ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pCreditAmount", personalPayment.CreditAmount);
                        command.Parameters.AddWithValue("@pDebitAmount", personalPayment.DebitAmount);
                        command.Parameters.AddWithValue("@pPaymentDescription", personalPayment.PaymentDescription);
                        command.Parameters.AddWithValue("@pPaymentDate", personalPayment.PaymentDate);
                        command.Parameters.AddWithValue("@pIsActive", personalPayment.IsActive);
                        command.Parameters.AddWithValue("@pCreatedDate", personalPayment?.CreatedDate == default(DateTime) ? DBNull.Value : personalPayment?.CreatedDate);
                        command.Parameters.AddWithValue("@pCreatedBy", personalPayment?.CreatedBy != null ? personalPayment.CreatedBy : DBNull.Value);

                        var PersonalPaymentIdParam = new SqlParameter("@pPersonalPaymentId", SqlDbType.BigInt)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(PersonalPaymentIdParam);
                        await command.ExecuteNonQueryAsync();
                        long newPersonalPaymentId = (long)PersonalPaymentIdParam.Value;
                        if (newPersonalPaymentId != 0)
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

        public async Task<long> UpdatePersonalPaymentAsync(PersonalPayment personalPayment)
        {
            var RowsAffectedResponse = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("UpdatePersonalPayment", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pPersonalPaymentId", personalPayment.PersonalPaymentId);
                        command.Parameters.AddWithValue("@pBankName", personalPayment.BankName);
                        command.Parameters.AddWithValue("@pAccountNumber", personalPayment.AccountNumber);
                        command.Parameters.AddWithValue("@pAccountHolderName", personalPayment.AccountHolderName);
                        command.Parameters.AddWithValue("@pBankBranch", personalPayment.BankBranch ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pCreditAmount", personalPayment.CreditAmount);
                        command.Parameters.AddWithValue("@pDebitAmount", personalPayment.DebitAmount);
                        command.Parameters.AddWithValue("@pPaymentDescription", personalPayment.PaymentDescription);
                        command.Parameters.AddWithValue("@pPaymentDate", personalPayment.PaymentDate);
                        command.Parameters.AddWithValue("@pIsActive", personalPayment.IsActive);
                        command.Parameters.AddWithValue("@pModifiedDate", personalPayment?.ModifiedDate == default(DateTime) ? DBNull.Value : personalPayment?.ModifiedDate);
                        command.Parameters.AddWithValue("@pModifiedBy", personalPayment?.ModifiedBy != null ? personalPayment.ModifiedBy : DBNull.Value);

                        var RowsAffectedParam = new SqlParameter("@RowsAffected", SqlDbType.BigInt)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(RowsAffectedParam);

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

        public async Task<long> DeletePersonalPaymentAsync(long id)
        {
            int response = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("DeletePersonalPaymentById", connection))
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

        public async Task<List<string>> GetBankNamesAsync()
        {
            var bankNamesList = new List<string>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetAllEnabledBankNames", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    bankNamesList.Add(reader.GetString(reader.GetOrdinal("BankName")));
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
            return bankNamesList;
        }

        public async Task<decimal> GetTotalCreditAmountAsync()
        {
            decimal totalCredit = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetTotalCreditAmount", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    totalCredit = reader.GetDecimal(reader.GetOrdinal("TotalCreditAmount"));
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
            return totalCredit;
        }

        public async Task<decimal> GetTotalDebitAmountAsync()
        {
            decimal totalDebit = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetTotalDebitAmount", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    totalDebit = reader.GetDecimal(reader.GetOrdinal("TotalDebitAmount"));
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
            return totalDebit;
        }

        public async Task<decimal> GetNetAmountAsync()
        {
            decimal netAmount = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetNetAmount", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    netAmount = reader.GetDecimal(reader.GetOrdinal("NetAmount"));
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
            return netAmount;
        }

        public async Task<object> GetTransactionHistoryAsync(long personalPaymentId, int pageNumber, int pageSize, 
            DateTime? fromDate, DateTime? toDate, string? transactionType)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("GetPersonalPaymentTransactionHistory", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pPersonalPaymentId", personalPaymentId);
                        command.Parameters.AddWithValue("@pPageNumber", pageNumber);
                        command.Parameters.AddWithValue("@pPageSize", pageSize);
                        command.Parameters.AddWithValue("@pFromDate", fromDate ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pToDate", toDate ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pTransactionType", string.IsNullOrEmpty(transactionType) ? (object)DBNull.Value : transactionType);

                        var transactions = new List<PersonalPaymentTransactionViewModel>();
                        var accountSummary = new PersonalPaymentAccountSummary();
                        var totalCount = 0;
                        var currentPage = pageNumber;
                        var totalPages = 0;

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            // First result set: transactions
                            while (await reader.ReadAsync())
                            {
                                transactions.Add(new PersonalPaymentTransactionViewModel
                                {
                                    PersonalPaymentSaleDetailId = reader.GetInt64("PersonalPaymentSaleDetailId"),
                                    PersonalPaymentId = reader.GetInt64("PersonalPaymentId"),
                                    SaleId = reader.GetInt64("SaleId"),
                                    TransactionType = reader.GetString("TransactionType"),
                                    Amount = reader.GetDecimal("Amount"),
                                    Balance = reader.GetDecimal("Balance"),
                                    TransactionDescription = reader.IsDBNull("TransactionDescription") ? null : reader.GetString("TransactionDescription"),
                                    TransactionDate = reader.GetDateTime("TransactionDate"),
                                    IsActive = reader.GetBoolean("IsActive"),
                                    CreatedDate = reader.GetDateTime("CreatedDate"),
                                    CreatedBy = reader.GetInt64("CreatedBy"),
                                    ModifiedDate = reader.GetDateTime("ModifiedDate"),
                                    ModifiedBy = reader.GetInt64("ModifiedBy"),
                                    BankName = reader.GetString("BankName"),
                                    AccountNumber = reader.GetString("AccountNumber"),
                                    AccountHolderName = reader.GetString("AccountHolderName"),
                                    SaleDescription = reader.IsDBNull("SaleDescription") ? null : reader.GetString("SaleDescription"),
                                    BillNumber = reader.IsDBNull("BillNumber") ? 0 : reader.GetInt64("BillNumber")
                                });
                            }

                            // Second result set: total count
                            if (await reader.NextResultAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    totalCount = reader.GetInt32("TotalCount");
                                }
                            }

                            // Third result set: account summary
                            if (await reader.NextResultAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    accountSummary = new PersonalPaymentAccountSummary
                                    {
                                        PersonalPaymentId = reader.GetInt64("PersonalPaymentId"),
                                        BankName = reader.GetString("BankName"),
                                        AccountNumber = reader.GetString("AccountNumber"),
                                        AccountHolderName = reader.GetString("AccountHolderName"),
                                        CurrentBalance = reader.GetDecimal("CurrentBalance"),
                                        TotalCredit = reader.GetDecimal("TotalCredit"),
                                        TotalDebit = reader.GetDecimal("TotalDebit"),
                                        TransactionCount = reader.GetInt32("TransactionCount"),
                                        LastTransactionDate = reader.GetDateTime("LastTransactionDate")
                                    };
                                }
                            }
                        }

                        // Calculate pagination
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                        return new
                        {
                            success = true,
                            transactions = transactions,
                            accountSummary = accountSummary,
                            currentPage = currentPage,
                            totalPages = totalPages,
                            totalCount = totalCount,
                            pageSize = pageSize
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction history for PersonalPaymentId: {PersonalPaymentId}", personalPaymentId);
                return new
                {
                    success = false,
                    message = "Error loading transaction history: " + ex.Message,
                    transactions = new List<object>(),
                    accountSummary = new object()
                };
            }
        }
    }
}
