using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMS.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(IDbContextFactory dbContextFactory,ILogger<EmployeeService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }
        public async Task<bool> CreateEmployeeAsync(Employee employee)
        {
            bool response = false;
            try
            {


                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("AddEmployee", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@FirstName", employee.FirstName);
                        command.Parameters.AddWithValue("@LastName", employee.LastName);
                        command.Parameters.AddWithValue("@PhoneNumber", employee.PhoneNumber);
                        command.Parameters.AddWithValue("@CNIC", employee.Cnic);
                        command.Parameters.AddWithValue("@Address", employee.Address);
                        command.Parameters.AddWithValue("@Gender", employee.Gender);
                        command.Parameters.AddWithValue("@Age", employee.Age);
                        command.Parameters.AddWithValue("@Salary", employee.Salary ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@HusbandFatherName", employee.HusbandFatherName ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@EmailAddress", employee.EmailAddress ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@MaritalStatus", employee.MaritalStatus ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@IsDeleted", employee.IsDeleted);

                        command.Parameters.AddWithValue("@JoiningDate", employee.JoiningDate == default(DateTime) ? DBNull.Value : employee.JoiningDate);
                        command.Parameters.AddWithValue("@CreatedDate", employee.CreatedDate == default(DateTime) ? DBNull.Value : employee.CreatedDate);
                        command.Parameters.AddWithValue("@CreatedByUserId_FK", employee.CreatedByUserIdFk);


                        var unitTypeyIdParam = new SqlParameter("@pEmployeeId", SqlDbType.BigInt)
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
        
        public async Task<int> DeleteEmployeeAsync(long id,DateTime modifiedDate, long modifiedUserId)
        {
            int response = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("DeleteEmployee", connection))
                    {
                        command.Parameters.AddWithValue("@pEmployeeId", id);
                        command.Parameters.AddWithValue("@ModifiedDate", modifiedDate);
                        command.Parameters.AddWithValue("@ModifiedByUserId_FK",modifiedUserId);
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

        public async Task<EmployeeViewModel> GetAllEmployeesAsync(int pageNumber, int? pageSize, EmployeesFilters employeesFilters)
        {
            var employees = new List<Employee>();
            
            int totalCount = 0;

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    using (var cmd = new SqlCommand("GetAllEmployees", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@pIsDeleted", employeesFilters.IsDeleted ? employeesFilters.IsDeleted : 0);
                        cmd.Parameters.AddWithValue("@FirstName", employeesFilters.FirstName ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@LastName", employeesFilters.LastName ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PhoneNumber", employeesFilters.PhoneNo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@CNIC", employeesFilters.CNIC ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Gender", employeesFilters.gender ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@MaritalStatus", employeesFilters.maritalStatus ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@JoiningDateTo", employeesFilters.EndDateTime == default(DateTime) ? DBNull.Value : employeesFilters.EndDateTime);
                        cmd.Parameters.AddWithValue("@JoiningDateFrom", employeesFilters.startDate == default(DateTime) ? DBNull.Value : employeesFilters.startDate);
                        cmd.Parameters.AddWithValue("@HusbandFatherName", employeesFilters.HusbandFatherName ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EmailAddress", employeesFilters.email ?? (object)DBNull.Value);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            // Read first result set - paginated employee data
                            while (await reader.ReadAsync())
                            {
                                // Main Employee
                                employees.Add(new Employee
                                {
                                    EmployeeId = reader.GetInt64(reader.GetOrdinal("EmployeeId")),
                                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                    PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")),
                                    Cnic = reader.GetString(reader.GetOrdinal("CNIC")),
                                    Address = reader.GetString(reader.GetOrdinal("Address")),
                                    Gender = reader.GetString(reader.GetOrdinal("Gender")),
                                    Age = reader.GetInt32(reader.GetOrdinal("Age")),
                                    EmailAddress = reader.IsDBNull(reader.GetOrdinal("EmailAddress")) ? null : reader.GetString(reader.GetOrdinal("EmailAddress")),
                                    MaritalStatus = reader.IsDBNull(reader.GetOrdinal("MaritalStatus")) ? null : reader.GetString(reader.GetOrdinal("MaritalStatus")),
                                    
                                    
                                    CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                    JoiningDate = reader.GetDateTime(reader.GetOrdinal("JoiningDate")),
                                    Salary = reader.IsDBNull(reader.GetOrdinal("Salary")) ? 0 : reader.GetInt64(reader.GetOrdinal("Salary")),
                                    ModifiedDate = reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                                    HusbandFatherName = reader.IsDBNull(reader.GetOrdinal("HusbandFatherName")) ? null : reader.GetString(reader.GetOrdinal("HusbandFatherName")),
                                    IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                                    CreatedByUserIdFk = reader.GetInt64(reader.GetOrdinal("CreatedByUserId_FK")),
                                    ModifiedByUserIdFk = reader.GetInt64(reader.GetOrdinal("ModifiedByUserId_FK"))
                                });
                            }
                            
                            // Move to second result set (total count)
                            await reader.NextResultAsync();
                            
                            if (await reader.ReadAsync())
                            {
                                totalCount = reader.GetInt32(reader.GetOrdinal("TotalRecords"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

            return new EmployeeViewModel
            {
                EmployeesList = employees,
                CurrentPage = pageNumber,
                TotalPages = pageSize.HasValue && pageSize.Value > 0
                    ? (int)Math.Ceiling(totalCount / (double)pageSize.Value)
                    : 1,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<Employee> GetEmployeeByIdAsync(long id)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetEmployeeDetail", connection))
                    {
                        command.Parameters.AddWithValue("@pEmployeeId", id);
                        command.CommandType = CommandType.StoredProcedure;
                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    return new Employee
                                    {
                                        EmployeeId = reader.GetInt64(reader.GetOrdinal("EmployeeId")),
                                        FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                        LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                        PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")),
                                        Cnic = reader.GetString(reader.GetOrdinal("CNIC")),
                                        Address = reader.GetString(reader.GetOrdinal("Address")),
                                        Gender = reader.GetString(reader.GetOrdinal("Gender")),
                                        Age = reader.GetInt32(reader.GetOrdinal("Age")),
                                        EmailAddress = reader.IsDBNull(reader.GetOrdinal("EmailAddress")) ? null : reader.GetString(reader.GetOrdinal("EmailAddress")),
                                        MaritalStatus = reader.IsDBNull(reader.GetOrdinal("MaritalStatus")) ? null : reader.GetString(reader.GetOrdinal("MaritalStatus")),


                                        CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                        JoiningDate = reader.GetDateTime(reader.GetOrdinal("JoiningDate")),
                                        Salary = reader.IsDBNull(reader.GetOrdinal("Salary")) ? 0 : reader.GetInt64(reader.GetOrdinal("Salary")),
                                        ModifiedDate = reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                                        HusbandFatherName = reader.IsDBNull(reader.GetOrdinal("HusbandFatherName")) ? null : reader.GetString(reader.GetOrdinal("HusbandFatherName")),
                                        IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                                        CreatedByUserIdFk = reader.GetInt64(reader.GetOrdinal("CreatedByUserId_FK")),
                                        ModifiedByUserIdFk = reader.GetInt64(reader.GetOrdinal("ModifiedByUserId_FK"))
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

        public async Task<int> UpdateEmployeeAsync(Employee employee)
        {
            var RowsAffectedResponse = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("UpdateEmployee", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@FirstName", employee.FirstName);
                        command.Parameters.AddWithValue("@LastName", employee.LastName);
                        command.Parameters.AddWithValue("@PhoneNumber", employee.PhoneNumber);
                        command.Parameters.AddWithValue("@CNIC", employee.Cnic);
                        command.Parameters.AddWithValue("@Address", employee.Address);
                        command.Parameters.AddWithValue("@Gender", employee.Gender);
                        command.Parameters.AddWithValue("@Age", employee.Age);
                        command.Parameters.AddWithValue("@JoiningDate", employee.JoiningDate == default(DateTime) ? DBNull.Value: employee.JoiningDate );
                        command.Parameters.AddWithValue("@Salary", employee.Salary ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@HusbandFatherName", employee.HusbandFatherName ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@EmailAddress", employee.EmailAddress ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@MaritalStatus", employee.MaritalStatus ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@IsDeleted", employee.IsDeleted);

                        command.Parameters.AddWithValue("@ModifiedDate", employee?.ModifiedDate == default(DateTime) ? DBNull.Value : employee?.ModifiedDate);
                        command.Parameters.AddWithValue("@ModifiedByUserId_FK", employee?.ModifiedByUserIdFk);
                        command.Parameters.AddWithValue("@pEmployeeId", employee?.EmployeeId);

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
