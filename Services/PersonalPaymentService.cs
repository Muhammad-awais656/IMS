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
                                // Read first result set - paginated payment data
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
                                
                                // Move to second result set (total count)
                                if (await reader.NextResultAsync())
                                {
                                    if (await reader.ReadAsync())
                                    {
                                        try
                                        {
                                            totalCount = reader.GetInt32(reader.GetOrdinal("TotalRecords"));
                                        }
                                        catch
                                        {
                                            // Try alternative column names
                                            try
                                            {
                                                totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
                                            }
                                            catch
                                            {
                                                _logger.LogWarning("TotalRecords or TotalCount column not found in second result set");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error reading personal payments data");
                            throw;
                        }
                    }

                    // If totalCount is still 0, we need to get it separately
                    // This happens if the stored procedure doesn't return a second result set
                    if (totalCount == 0 && personalPayments.Count > 0 && connection.State == System.Data.ConnectionState.Open)
                    {
                        _logger.LogWarning("Total count not returned from stored procedure, querying separately");
                        try
                        {
                            // Query total count with same filters
                            using (var countCommand = new SqlCommand("GetAllPersonalPayments", connection))
                            {
                                countCommand.CommandType = CommandType.StoredProcedure;
                                countCommand.Parameters.AddWithValue("@PageNumber", 1);
                                countCommand.Parameters.AddWithValue("@PageSize", int.MaxValue); // Get all to count
                                countCommand.Parameters.AddWithValue("@SearchBankName", string.IsNullOrEmpty(filters.BankName) ? DBNull.Value : filters.BankName);
                                countCommand.Parameters.AddWithValue("@SearchAccountNumber", string.IsNullOrEmpty(filters.AccountNumber) ? DBNull.Value : filters.AccountNumber);
                                countCommand.Parameters.AddWithValue("@SearchPaymentDescription", string.IsNullOrEmpty(filters.PaymentDescription) ? DBNull.Value : filters.PaymentDescription);
                                countCommand.Parameters.AddWithValue("@CreditAmountFrom", filters.CreditAmountFrom ?? (object)DBNull.Value);
                                countCommand.Parameters.AddWithValue("@CreditAmountTo", filters.CreditAmountTo ?? (object)DBNull.Value);
                                countCommand.Parameters.AddWithValue("@DebitAmountFrom", filters.DebitAmountFrom ?? (object)DBNull.Value);
                                countCommand.Parameters.AddWithValue("@DebitAmountTo", filters.DebitAmountTo ?? (object)DBNull.Value);
                                countCommand.Parameters.AddWithValue("@DateFrom", filters.DateFrom == default(DateTime) ? DBNull.Value : filters.DateFrom);
                                countCommand.Parameters.AddWithValue("@DateTo", filters.DateTo == default(DateTime) ? DBNull.Value : filters.DateTo);
                                countCommand.Parameters.AddWithValue("@TransactionType", string.IsNullOrEmpty(filters.TransactionType) ? DBNull.Value : filters.TransactionType);
                                countCommand.Parameters.AddWithValue("@IsActive", filters.IsActive ?? (object)DBNull.Value);

                                using (var countReader = await countCommand.ExecuteReaderAsync())
                                {
                                    var tempCount = 0;
                                    while (await countReader.ReadAsync())
                                    {
                                        tempCount++;
                                    }
                                    totalCount = tempCount;
                                    
                                    // Try to get total from second result set
                                    if (await countReader.NextResultAsync())
                                    {
                                        if (await countReader.ReadAsync())
                                        {
                                            try
                                            {
                                                totalCount = countReader.GetInt32(countReader.GetOrdinal("TotalRecords"));
                                            }
                                            catch
                                            {
                                                try
                                                {
                                                    totalCount = countReader.GetInt32(countReader.GetOrdinal("TotalCount"));
                                                }
                                                catch
                                                {
                                                    // Use tempCount we already calculated
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error getting total count separately");
                            // Fall back to using record count
                            if (totalCount == 0)
                            {
                                totalCount = personalPayments.Count;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated personal payments");
                throw;
            }

            // Ensure totalCount is at least the number of records we have
            // This handles cases where stored procedure doesn't return total count
            if (totalCount == 0 && personalPayments.Count > 0)
            {
                // If we have records but no total count, and we're on page 1 with full page,
                // estimate that there might be more pages
                if (pageNumber == 1 && personalPayments.Count == pageSize)
                {
                    // Likely more records exist, set a minimum to show pagination
                    totalCount = pageSize + 1; // At least one more record
                    _logger.LogWarning("Total count not available, estimating based on returned records");
                }
                else
                {
                    // Use actual count as fallback
                    totalCount = personalPayments.Count;
                }
            }

            var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 1;
            
            // Ensure at least 1 page
            if (totalPages < 1 && personalPayments.Count > 0)
            {
                totalPages = 1;
            }

            _logger.LogInformation("Personal Payments pagination: Page {PageNumber}/{TotalPages}, PageSize: {PageSize}, TotalCount: {TotalCount}, RecordsReturned: {RecordsCount}",
                pageNumber, totalPages, pageSize, totalCount, personalPayments.Count);

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
                                    var bankName = reader.GetString(reader.GetOrdinal("BankName"));
                                    // Only add unique bank names
                                    if (!string.IsNullOrEmpty(bankName) && !bankNamesList.Contains(bankName))
                                    {
                                        bankNamesList.Add(bankName);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error reading bank names from stored procedure");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bank names");
            }
            return bankNamesList;
        }

        public async Task<List<(long PersonalPaymentId, string BankName)>> GetBankAccountsAsync()
        {
            var bankAccountsList = new List<(long PersonalPaymentId, string BankName)>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(@"
                        SELECT DISTINCT PersonalPaymentId, BankName 
                        FROM PersonalPayments 
                        WHERE IsActive = 1
                        ORDER BY BankName", connection))
                    {
                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var personalPaymentId = reader.GetInt64(reader.GetOrdinal("PersonalPaymentId"));
                                    var bankName = reader.GetString(reader.GetOrdinal("BankName"));
                                    if (!string.IsNullOrEmpty(bankName))
                                    {
                                        bankAccountsList.Add((personalPaymentId, bankName));
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error reading bank accounts from database");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bank accounts");
            }
            return bankAccountsList;
        }

        public async Task<string?> GetBankNameByIdAsync(long personalPaymentId)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(@"
                        SELECT BankName 
                        FROM PersonalPayments 
                        WHERE PersonalPaymentId = @PersonalPaymentId", connection))
                    {
                        command.Parameters.AddWithValue("@PersonalPaymentId", personalPaymentId);
                        var result = await command.ExecuteScalarAsync();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bank name by ID: {PersonalPaymentId}", personalPaymentId);
                return null;
            }
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

        public async Task<decimal> GetAccountBalanceAsync(long personalPaymentId)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(@"
                        SELECT ISNULL(CreditAmount, 0) - ISNULL(DebitAmount, 0) AS Balance
                        FROM PersonalPayments
                        WHERE PersonalPaymentId = @PersonalPaymentId AND IsActive = 1", connection))
                    {
                        command.Parameters.AddWithValue("@PersonalPaymentId", personalPaymentId);
                        var result = await command.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            return Convert.ToDecimal(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account balance for PersonalPaymentId: {PersonalPaymentId}", personalPaymentId);
            }
            return 0;
        }

        public async Task<bool> ProcessBankDepositAsync(long personalPaymentId, decimal amount, string description, long createdBy)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Get current balance
                            decimal currentCreditAmount = 0;
                            using (var getCommand = new SqlCommand(@"
                                SELECT ISNULL(CreditAmount, 0) AS CreditAmount
                                FROM PersonalPayments
                                WHERE PersonalPaymentId = @PersonalPaymentId", connection, transaction))
                            {
                                getCommand.Parameters.AddWithValue("@PersonalPaymentId", personalPaymentId);
                                var result = await getCommand.ExecuteScalarAsync();
                                if (result != null && result != DBNull.Value)
                                {
                                    currentCreditAmount = Convert.ToDecimal(result);
                                }
                            }

                            // Update CreditAmount in PersonalPayments
                            using (var updateCommand = new SqlCommand(@"
                                UPDATE PersonalPayments
                                SET CreditAmount = ISNULL(CreditAmount, 0) + @Amount,
                                    ModifiedDate = GETDATE(),
                                    ModifiedBy = @ModifiedBy
                                WHERE PersonalPaymentId = @PersonalPaymentId", connection, transaction))
                            {
                                updateCommand.Parameters.AddWithValue("@PersonalPaymentId", personalPaymentId);
                                updateCommand.Parameters.AddWithValue("@Amount", amount);
                                updateCommand.Parameters.AddWithValue("@ModifiedBy", createdBy);
                                await updateCommand.ExecuteNonQueryAsync();
                            }

                            // Calculate new balance
                            decimal newBalance = currentCreditAmount + amount;

                            // Create transaction record in PersonalPaymentSaleDetail
                            // Using SaleId = 0 to indicate manual deposit transaction
                            using (var insertCommand = new SqlCommand(@"
                                INSERT INTO PersonalPaymentSaleDetail 
                                (PersonalPaymentId, SaleId, TransactionType, Amount, Balance, TransactionDescription, 
                                 TransactionDate, IsActive, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy)
                                VALUES 
                                (@PersonalPaymentId, 0, 'Credit', @Amount, @Balance, @Description, 
                                 GETDATE(), 1, GETDATE(), @CreatedBy, GETDATE(), @CreatedBy)", connection, transaction))
                            {
                                insertCommand.Parameters.AddWithValue("@PersonalPaymentId", personalPaymentId);
                                insertCommand.Parameters.AddWithValue("@Amount", amount);
                                insertCommand.Parameters.AddWithValue("@Balance", newBalance);
                                insertCommand.Parameters.AddWithValue("@Description", description ?? "Bank Deposit");
                                insertCommand.Parameters.AddWithValue("@CreatedBy", createdBy);
                                await insertCommand.ExecuteNonQueryAsync();
                            }

                            transaction.Commit();
                            return true;
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
                _logger.LogError(ex, "Error processing bank deposit for PersonalPaymentId: {PersonalPaymentId}", personalPaymentId);
                return false;
            }
        }

        public async Task<bool> ProcessBankWithdrawAsync(long personalPaymentId, decimal amount, string description, long createdBy)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Get current balance
                            decimal currentCreditAmount = 0;
                            decimal currentDebitAmount = 0;
                            using (var getCommand = new SqlCommand(@"
                                SELECT ISNULL(CreditAmount, 0) AS CreditAmount, ISNULL(DebitAmount, 0) AS DebitAmount
                                FROM PersonalPayments
                                WHERE PersonalPaymentId = @PersonalPaymentId", connection, transaction))
                            {
                                getCommand.Parameters.AddWithValue("@PersonalPaymentId", personalPaymentId);
                                using (var reader = await getCommand.ExecuteReaderAsync())
                                {
                                    if (await reader.ReadAsync())
                                    {
                                        currentCreditAmount = reader.GetDecimal(reader.GetOrdinal("CreditAmount"));
                                        currentDebitAmount = reader.GetDecimal(reader.GetOrdinal("DebitAmount"));
                                    }
                                }
                            }

                            // Check if sufficient balance
                            decimal currentBalance = currentCreditAmount - currentDebitAmount;
                            if (amount > currentBalance)
                            {
                                throw new Exception("Insufficient balance");
                            }

                            // Update DebitAmount in PersonalPayments
                            using (var updateCommand = new SqlCommand(@"
                                UPDATE PersonalPayments
                                SET DebitAmount = ISNULL(DebitAmount, 0) + @Amount,
                                    ModifiedDate = GETDATE(),
                                    ModifiedBy = @ModifiedBy
                                WHERE PersonalPaymentId = @PersonalPaymentId", connection, transaction))
                            {
                                updateCommand.Parameters.AddWithValue("@PersonalPaymentId", personalPaymentId);
                                updateCommand.Parameters.AddWithValue("@Amount", amount);
                                updateCommand.Parameters.AddWithValue("@ModifiedBy", createdBy);
                                await updateCommand.ExecuteNonQueryAsync();
                            }

                            // Calculate new balance
                            decimal newBalance = currentBalance - amount;

                            // Create transaction record in PersonalPaymentSaleDetail
                            // Using SaleId = 0 to indicate manual withdraw transaction
                            using (var insertCommand = new SqlCommand(@"
                                INSERT INTO PersonalPaymentSaleDetail 
                                (PersonalPaymentId, SaleId, TransactionType, Amount, Balance, TransactionDescription, 
                                 TransactionDate, IsActive, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy)
                                VALUES 
                                (@PersonalPaymentId, 0, 'Debit', @Amount, @Balance, @Description, 
                                 GETDATE(), 1, GETDATE(), @CreatedBy, GETDATE(), @CreatedBy)", connection, transaction))
                            {
                                insertCommand.Parameters.AddWithValue("@PersonalPaymentId", personalPaymentId);
                                insertCommand.Parameters.AddWithValue("@Amount", amount);
                                insertCommand.Parameters.AddWithValue("@Balance", newBalance);
                                insertCommand.Parameters.AddWithValue("@Description", description ?? "Bank Withdraw");
                                insertCommand.Parameters.AddWithValue("@CreatedBy", createdBy);
                                await insertCommand.ExecuteNonQueryAsync();
                            }

                            transaction.Commit();
                            return true;
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
                _logger.LogError(ex, "Error processing bank withdraw for PersonalPaymentId: {PersonalPaymentId}", personalPaymentId);
                return false;
            }
        }
    }
}
