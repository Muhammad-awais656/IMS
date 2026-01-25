using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMS.Services
{
    public class CustomerPaymentService : ICustomerPaymentService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<CustomerPaymentService> _logger;

        public CustomerPaymentService(IDbContextFactory dbContextFactory, ILogger<CustomerPaymentService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<CustomerPaymentViewModel> GetAllPaymentsAsync(int pageNumber, int? pageSize, CustomerPaymentFilters? filters)
        {
            var viewModel = new CustomerPaymentViewModel();
            var paymentsList = new List<PaymentWithCustomerViewModel>();
            int totalRecords = 0;
            
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("GetAllPayments", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                        // Set parameters for the stored procedure
                        command.Parameters.AddWithValue("@pSaleId", filters?.SaleId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pCustomerId", filters?.CustomerId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pPaymentDateFrom", filters?.PaymentDateFrom ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pPaymentDateTo", filters?.PaymentDateTo ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@PageNo", pageNumber);
                        command.Parameters.AddWithValue("@PageSize", pageSize);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            // Read the first result set (payment records)
                            while (await reader.ReadAsync())
                            {
                                var payment = new PaymentWithCustomerViewModel
                                {
                                    PaymentId = reader.GetInt64("PaymentId"),
                                    PaymentAmount = reader.GetDecimal("PaymentAmount"),
                                    SaleId = reader.IsDBNull("SaleId") ? 0 : reader.GetInt64("SaleId") ,
                                    BillNumber = reader.IsDBNull("BillNumber") ? 0 : reader.GetInt64("BillNumber"),
                                    CustomerId = reader.GetInt64("CustomerId"),
                                    CustomerName = reader.GetString("CustomerName"),
                                    PaymentDate = reader.GetDateTime("PaymentDate"),
                                    CreatedBy = reader.GetInt64("CreatedBy"),
                                    CreatedDate = reader.GetDateTime("CreatedDate"),
                                    Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                                    PaymentMethod = reader.IsDBNull("PaymentMethod") ? null : reader.GetString("PaymentMethod"),
                                    ModifiedBy = reader.IsDBNull("ModifiedBy") ? null : reader.GetInt64("ModifiedBy"),
                                    ModifiedDate = reader.IsDBNull("ModifiedDate") ? null : reader.GetDateTime("ModifiedDate")
                                };
                                paymentsList.Add(payment);
                            }

                            // Move to second result set for total count
                            await reader.NextResultAsync();

                            if (await reader.ReadAsync())
                            {
                                totalRecords = reader.GetInt32("TotalRecords");
                            }
                        }
                    }

                    // Set pagination properties using TotalRecords from stored procedure
                    viewModel.PaymentsList = paymentsList;
                    viewModel.CurrentPage = pageNumber;
                    viewModel.PageSize = pageSize ?? 10;
                    viewModel.TotalCount = totalRecords;
                    viewModel.TotalPages = pageSize.HasValue && pageSize.Value > 0
                        ? (int)Math.Ceiling(totalRecords / (double)pageSize.Value)
                        : 1;
                    
                    _logger.LogInformation($"Retrieved {paymentsList.Count} payment records out of {totalRecords} total records");
                }

                // Get customers and sales for dropdowns
                viewModel.CustomerList = await GetAllCustomersAsync();
                viewModel.SalesList = await GetAllSalesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all payments");
                throw;
            }
            return viewModel;
        }

        public async Task<Payment> GetPaymentByIdAsync(long id)
        {
            Payment payment = null;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"SELECT PaymentId, PaymentAmount, SaleId, CustomerId, PaymentDate, 
                                      CreatedBy, CreatedDate, Description, ModifiedBy, ModifiedDate,PaymentMethod,OnlineAccountId
                               FROM Payments WHERE PaymentId = @PaymentId";
                    
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@PaymentId", id);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                payment = new Payment
                                {
                                    PaymentId = reader.GetInt64("PaymentId"),
                                    PaymentAmount = reader.GetDecimal("PaymentAmount"),
                                    SaleId = reader.GetInt64("SaleId"),
                                    CustomerId = reader.GetInt64("CustomerId"),
                                    PaymentDate = reader.GetDateTime("PaymentDate"),
                                    CreatedBy = reader.GetInt64("CreatedBy"),
                                    CreatedDate = reader.GetDateTime("CreatedDate"),
                                    Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                                    ModifiedBy = reader.IsDBNull("ModifiedBy") ? null : reader.GetInt64("ModifiedBy"),
                                    ModifiedDate = reader.IsDBNull("ModifiedDate") ? null : reader.GetDateTime("ModifiedDate"),
                                    paymentMethod = reader.IsDBNull("PaymentMethod") ? null : reader.GetString("PaymentMethod"),
                                    onlineAccountId = reader.IsDBNull("OnlineAccountId") ? null : reader.GetInt64("OnlineAccountId"),
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment by ID");
                throw;
            }
            return payment;
        }

        public async Task<bool> CreatePaymentAsync(Payment payment)
        {
            bool response = false;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                   
                    
                    using (var command = new SqlCommand("AddPayment", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pPaymentAmount", payment.PaymentAmount);
                        command.Parameters.AddWithValue("@pSaleId", payment.SaleId);
                        command.Parameters.AddWithValue("@pCustomerId", payment.CustomerId);
                        command.Parameters.AddWithValue("@pPaymentDate", payment.PaymentDate);
                        command.Parameters.AddWithValue("@pCreatedBy", payment.CreatedBy);
                        command.Parameters.AddWithValue("@pCreatedDate", payment.CreatedDate == default(DateTime) ? DateTime.Now : payment.CreatedDate);
                        command.Parameters.AddWithValue("@pDescription", payment.Description ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@PaymentMethod", payment.paymentMethod ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@onlineAccountId", payment.onlineAccountId ?? (object)DBNull.Value);
                        var salesIdParam = new SqlParameter("@pPaymentId", SqlDbType.BigInt)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(salesIdParam);

                        await command.ExecuteNonQueryAsync();
                        var PaymentId = (long)salesIdParam.Value;
                        response = true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment");
                throw;
            }
            return response;
        }

        public async Task<int> UpdatePaymentAsync(Payment payment)
        {
            int response = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"UPDATE Payments SET 
                               PaymentAmount = @PaymentAmount,
                               SaleId = @SaleId,
                               CustomerId = @CustomerId,
                               PaymentDate = @PaymentDate,
                               Description = @Description,
                               PaymentMethod = @PaymentMethod,
                               OnlineAccountId = @OnlineAccountId,
                               ModifiedBy = @ModifiedBy,
                               ModifiedDate = @ModifiedDate
                               WHERE PaymentId = @PaymentId";
                    
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@PaymentId", payment.PaymentId);
                        command.Parameters.AddWithValue("@PaymentAmount", payment.PaymentAmount);
                        command.Parameters.AddWithValue("@SaleId", payment.SaleId);
                        command.Parameters.AddWithValue("@CustomerId", payment.CustomerId);
                        command.Parameters.AddWithValue("@PaymentDate", payment.PaymentDate);
                        command.Parameters.AddWithValue("@Description", payment.Description ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@PaymentMethod", payment.paymentMethod ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@OnlineAccountId", payment.onlineAccountId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@ModifiedBy", payment.ModifiedBy);
                        command.Parameters.AddWithValue("@ModifiedDate", payment.ModifiedDate ?? DateTime.Now);

                        response = await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment");
                throw;
            }
            return response;
        }

        public async Task<int> DeletePaymentAsync(long id, DateTime modifiedDate, long modifiedBy)
        {
            int response = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"DELETE FROM Payments WHERE PaymentId = @PaymentId";
                    
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@PaymentId", id);

                        response = await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting payment");
                throw;
            }
            return response;
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            var customers = new List<Customer>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("GetAllCustomers", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pIsEnabled", true);
                        command.Parameters.AddWithValue("@pCoustomerName", DBNull.Value);
                        command.Parameters.AddWithValue("@PhoneNumber", DBNull.Value);
                        command.Parameters.AddWithValue("@EmailAddress", DBNull.Value);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var customer = new Customer
                                {
                                    CustomerId = reader.GetInt64("CustomerId"),
                                    CustomerName = reader.GetString("CustomerName")
                                };
                                customers.Add(customer);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all customers");
                throw;
            }
            return customers;
        }

        public async Task<List<Sale>> GetAllSalesAsync()
        {
            var sales = new List<Sale>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"SELECT SaleId, BillNumber FROM Sales WHERE IsDeleted = 0";
                    
                    using (var command = new SqlCommand(sql, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var sale = new Sale
                                {
                                    SaleId = reader.GetInt64("SaleId"),
                                    BillNumber = reader.GetInt64("BillNumber")
                                };
                                sales.Add(sale);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all sales");
                throw;
            }
            return sales;
        }

        public async Task<List<Sale>> GetCustomerBillsAsync(long customerId)
        {
            var sales = new List<Sale>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("GetAllCustomerBillNumbers", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pCustomerId", customerId);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var sale = new Sale
                                {
                                    SaleId = reader.GetInt64("SaleId"),
                                    BillNumber = reader.GetInt64("BillNumber"),
                                    TotalDueAmount = reader.GetDecimal("TotalDueAmount")
                                };
                                sales.Add(sale);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer bills for customer {CustomerId}", customerId);
                throw;
            }
            return sales;
        }

        public async Task<bool> HasAnyPaymentsAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT COUNT(*) FROM Payments";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        var result = await command.ExecuteScalarAsync();
                        return Convert.ToInt32(result) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if payments exist");
                return false;
            }
        }
    }
}
