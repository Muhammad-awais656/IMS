using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Org.BouncyCastle.Asn1.Cms;
using System;
using System.Data;
using System.Net.Mail;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IMS.Services
{
    public class CustomerService : ICustomer
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(IDbContextFactory dbContextFactory, ILogger<CustomerService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<bool> CreateCustomerAsync(Customer customer)
        {
            bool response = false;
            try
            {


                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("AddCustomer", connection))
                    {
                         
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pCustomerName", customer.CustomerName);
                        command.Parameters.AddWithValue("@pCustomerContactNumber", customer.CustomerContactNumber);
                        command.Parameters.AddWithValue("@pCustomerEmail", DBNull.Value);
                        command.Parameters.AddWithValue("@pCustomerEmailCC", DBNull.Value);
                        command.Parameters.AddWithValue("@pCustomerAddress", customer?.CustomerAddress != null ? customer?.CustomerAddress : DBNull.Value);
                        command.Parameters.AddWithValue("@pCreatedDate", customer?.CreatedDate != null ? customer?.CreatedDate : DBNull.Value);
                        command.Parameters.AddWithValue("@pIsEnabled", customer?.IsEnabled != null ? customer?.IsEnabled: DBNull.Value);
                        command.Parameters.AddWithValue("@pCreatedBy", customer?.CreatedBy != null ? customer.CreatedBy : DBNull.Value);
                        command.Parameters.AddWithValue("@pInvoiceCreditPeriod", DBNull.Value);
                        command.Parameters.AddWithValue("@pStartWorkingTime", DBNull.Value);
                        command.Parameters.AddWithValue("@pEndWorkingTime", DBNull.Value);
                        
                        var CustomerIdParam = new SqlParameter("@pCustomerId", SqlDbType.BigInt)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(CustomerIdParam);
                        await command.ExecuteNonQueryAsync();
                        long newAdmincategoryIdParam = (long)CustomerIdParam.Value;
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

        public async Task<int> DeleteCustomerAsync(long id)
        {
            int response = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("DeleteCustomerById", connection))
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

        public async Task<List<Customer>> GetAllEnabledCustomers()
        {
            var customers = new List<Customer>();
          
            try
            {

                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {

                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetAllEnabledCustomers", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                       
                      
                        using (var reader = await command.ExecuteReaderAsync())
                        {

                            while (await reader.ReadAsync())
                            {
                                customers.Add(new Customer
                                {
                                    CustomerId = reader.GetInt64(reader.GetOrdinal("CustomerId")),
                                    CustomerName = reader.GetString(reader.GetOrdinal("CustomerName")),
                                    
                                });
                            }
                            
                        }
                    }



                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customers");

            }



            return customers;
        }

        public async Task<Customer> GetCustomerIdAsync(long id)
        {
            Customer customer = new Customer();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetCustomerDetail", connection))
                    {
                        command.Parameters.AddWithValue("@pCustomerId", id);
                        command.CommandType = CommandType.StoredProcedure;
                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    return new Customer
                                    {
                                        CustomerId = reader.GetInt64(reader.GetOrdinal("CustomerId")),
                                        //InvoiceCreditPeriod = reader.GetInt64(reader.GetOrdinal("InvoiceCreditPeriod")),
                                        CustomerName = reader.GetString(reader.GetOrdinal("CustomerName")),
                                        CustomerContactNumber = reader.IsDBNull(reader.GetOrdinal("CustomerContactNumber")) ? null : reader.GetString(reader.GetOrdinal("CustomerContactNumber")),
                                        //CustomerEmail = reader.IsDBNull(reader.GetOrdinal("CustomerEmail")) ? null : reader.GetString(reader.GetOrdinal("CustomerEmail")),
                                        //CustomerEmailCc = reader.IsDBNull(reader.GetOrdinal("CustomerEmailCC")) ? null : reader.GetString(reader.GetOrdinal("CustomerEmailCC")),
                                        CustomerAddress = reader.IsDBNull(reader.GetOrdinal("CustomerAddress")) ? null : reader.GetString(reader.GetOrdinal("CustomerAddress")),
                                        IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled"))
                                        //StartWorkingTime  = TimeOnly.FromTimeSpan(reader.GetTimeSpan(reader.GetOrdinal("StartWorkingTime"))),
                                        //EndWorkingTime = TimeOnly.FromTimeSpan(reader.GetTimeSpan(reader.GetOrdinal("EndWorkingTime")))
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
            return customer;
        }

        public async Task<CustomerViewModel> GetCustomers(int pageNumber, int? pageSize, string? Customername, string? contactNo, string? email)
        {
            var customers = new List<Customer>();
            int totalRecords = 0;
            try
            {
              
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetAllCustomersPaged", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@PageNo", pageNumber);
                        command.Parameters.AddWithValue("@PageSize", pageSize);
                        command.Parameters.AddWithValue("@pCoustomerName", (object)Customername ?? DBNull.Value);
                        command.Parameters.AddWithValue("@PhoneNumber", (object)contactNo ?? DBNull.Value);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {

                            while (await reader.ReadAsync())
                            {
                                customers.Add(new Customer
                                {
                                    CustomerId = reader.GetInt64(reader.GetOrdinal("CustomerId")),
                                    CustomerName = reader.GetString(reader.GetOrdinal("CustomerName")),
                                    CustomerContactNumber = reader.IsDBNull(reader.GetOrdinal("CustomerContactNumber")) ? null : reader.GetString(reader.GetOrdinal("CustomerContactNumber")),
                                    CustomerEmail = reader.IsDBNull(reader.GetOrdinal("CustomerEmail")) ? null : reader.GetString(reader.GetOrdinal("CustomerEmail")),
                                    CustomerAddress = reader.IsDBNull(reader.GetOrdinal("CustomerAddress")) ? null : reader.GetString(reader.GetOrdinal("CustomerAddress")),
                                    IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled"))
                                });
                            }
                            // Move to second result set for total count
                            await reader.NextResultAsync();

                            if (await reader.ReadAsync())
                            {
                                totalRecords = reader.GetInt32(reader.GetOrdinal("TotalRecords"));
                            }

                        }
                    }



                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customers");

            }



            return new CustomerViewModel
            {
                Customers = customers,
                CurrentPage = pageNumber,
                TotalPages = pageSize.HasValue && pageSize.Value > 0
                   ? (int)Math.Ceiling(totalRecords / (double)pageSize.Value)
                   : 1,
                PageSize = pageSize,
                TotalCount = totalRecords
            };
        }

        public async Task<int> UpdateCustomerAsync(Customer customer)
        {
            var RowsAffectedResponse = 0;


            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    

                    await connection.OpenAsync();
                    using (var command = new SqlCommand("UpdateCustomer", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pCustomerId", customer.CustomerId);
                        command.Parameters.AddWithValue("@pCustomerName", customer.CustomerName);
                        command.Parameters.AddWithValue("@pCustomerContactNumber", customer.CustomerContactNumber);
                        command.Parameters.AddWithValue("@pCustomerEmail", DBNull.Value);
                        command.Parameters.AddWithValue("@pCustomerEmailCC", DBNull.Value);
                        command.Parameters.AddWithValue("@pCustomerAddress", customer.CustomerAddress);
                        command.Parameters.AddWithValue("@pIsEnabled", customer.IsEnabled);
                        command.Parameters.AddWithValue("@pStartWorkingTime", DBNull.Value);
                        command.Parameters.AddWithValue("@pEndWorkingTime", DBNull.Value);
                        command.Parameters.AddWithValue("@ModifiedDate", customer?.ModifiedDate == default(DateTime) ? DBNull.Value : customer?.ModifiedDate);
                        command.Parameters.AddWithValue("@ModifiedBy", customer?.ModifiedBy);
                        command.Parameters.AddWithValue("@pInvoiceCreditPeriod", DBNull.Value);

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
