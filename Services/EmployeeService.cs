using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using System.Collections;
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
                                    Salary = reader.IsDBNull(reader.GetOrdinal("Salary")) ? 0 : reader.GetDecimal(reader.GetOrdinal("Salary")),
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
                                        Salary = reader.IsDBNull(reader.GetOrdinal("Salary")) ? 0 : reader.GetDecimal(reader.GetOrdinal("Salary")),
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

        public async Task<EmployeeVoucherType> GetVoucherTypeByIdAsync(int voucherTypeId)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(
                        @"SELECT VoucherTypeId, VoucherTypeName, Nature 
                  FROM EmployeeVoucherTypes 
                  WHERE VoucherTypeId = @VoucherTypeId", connection))
                    {
                        command.CommandType = CommandType.Text;
                        command.Parameters.AddWithValue("@VoucherTypeId", voucherTypeId);

                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    return new EmployeeVoucherType
                                    {
                                        VoucherTypeId = reader.GetInt32(reader.GetOrdinal("VoucherTypeId")),
                                        VoucherTypeName = reader.GetString(reader.GetOrdinal("VoucherTypeName")),
                                        Nature = reader.GetString(reader.GetOrdinal("Nature"))
                                    };
                                }
                            }
                        }
                        catch
                        {
                            // optional: log reader exception
                        }
                    }
                }
            }
            catch
            {
                // optional: log connection exception
            }

            return null;
        }

        public async Task<bool> AddEmployeeLedgerAsync(EmployeeLedger ledger)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(@"
                INSERT INTO EmployeeLedger
                (
                    EmployeeId_FK,
                    VoucherTypeId,
                    VoucherDate,
                    ReferenceNo,
                    DebitAmount,
                    CreditAmount,
                    Remarks,
                    CreatedBy,
                    CreatedOn
                )
                VALUES
                (
                    @EmployeeId,
                    @VoucherTypeId,
                    @VoucherDate,
                    @ReferenceNo,
                    @DebitAmount,
                    @CreditAmount,
                    @Remarks,
                    @CreatedBy,
                    GETDATE()
                )", connection))
                    {
                        command.CommandType = CommandType.Text;

                        command.Parameters.AddWithValue("@EmployeeId", ledger.EmployeeId);
                        command.Parameters.AddWithValue("@VoucherTypeId", ledger.VoucherTypeId);
                        command.Parameters.AddWithValue("@VoucherDate", ledger.VoucherDate);
                        command.Parameters.AddWithValue("@ReferenceNo", (object?)ledger.ReferenceNo ?? DBNull.Value);
                        command.Parameters.AddWithValue("@DebitAmount", ledger.DebitAmount);
                        command.Parameters.AddWithValue("@CreditAmount", ledger.CreditAmount);
                        command.Parameters.AddWithValue("@Remarks", (object?)ledger.Remarks ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CreatedBy", ledger.CreatedBy);

                        var rows = await command.ExecuteNonQueryAsync();
                        return rows > 0;
                    }
                }
            }
            catch
            {
                // optional logging
            }

            return false;
        }

        public async Task<bool> IsOpeningBalanceExistsAsync(long employeeId, int voucherTypeId)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(@"
                SELECT COUNT(1)
                FROM EmployeeLedger
                WHERE EmployeeId_FK = @EmployeeId
                  AND VoucherTypeId = @VoucherTypeId", connection))
                    {
                        command.CommandType = CommandType.Text;
                        command.Parameters.AddWithValue("@EmployeeId", employeeId);
                        command.Parameters.AddWithValue("@VoucherTypeId", voucherTypeId);

                        var count = (int)await command.ExecuteScalarAsync();
                        return count > 0;
                    }
                }
            }
            catch
            {
            }

            return false;
        }


        public async Task<List<EmployeeLedgerReportVM>> GetEmployeeLedgerReportAsync(long employeeId)
        {
            var result = new List<EmployeeLedgerReportVM>();

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(@"
                SELECT 
                    L.VoucherDate,
                    VT.VoucherTypeName,
                    L.ReferenceNo,
                    L.DebitAmount,
                    L.CreditAmount,
                    SUM(L.DebitAmount - L.CreditAmount)
                        OVER (ORDER BY L.VoucherDate, L.LedgerId) AS RunningBalance,
                    L.Remarks
                FROM EmployeeLedger L
                INNER JOIN EmployeeVoucherTypes VT 
                    ON VT.VoucherTypeId = L.VoucherTypeId
                WHERE 
                L.EmployeeId_FK = @EmployeeId
                ORDER BY L.VoucherDate, L.LedgerId", connection))
                    {
                        command.CommandType = CommandType.Text;
                        command.Parameters.AddWithValue("@EmployeeId", employeeId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Add(new EmployeeLedgerReportVM
                                {
                                    VoucherDate = reader.GetDateTime(0),
                                    VoucherTypeName = reader.GetString(1),
                                    ReferenceNo = reader.IsDBNull(2) ? null : reader.GetString(2),
                                    DebitAmount = reader.GetDecimal(3),
                                    CreditAmount = reader.GetDecimal(4),
                                    RunningBalance = reader.GetDecimal(5),
                                    Remarks = reader.IsDBNull(6) ? null : reader.GetString(6)
                                });
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return result;
        }
        public async Task<List<EmployeeLedgerReportVM>> GetAllEmployeeLedgerReportAsync()
        {
            var result = new List<EmployeeLedgerReportVM>();

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(@"
                SELECT 

                    L.VoucherDate,
                    VT.VoucherTypeName,
                    L.ReferenceNo,
                    L.DebitAmount,
                    L.CreditAmount,
                    SUM(L.DebitAmount - L.CreditAmount)
                        OVER (ORDER BY L.VoucherDate, L.LedgerId) AS RunningBalance,
                    L.Remarks,
                     LTRIM(RTRIM(
        ISNULL(emp.FirstName, '') + ' ' + ISNULL(emp.LastName, '')
    )) AS EmployeeName,
L.EmployeeId_FK
                FROM EmployeeLedger L
                INNER JOIN EmployeeVoucherTypes VT
                    ON VT.VoucherTypeId = L.VoucherTypeId
                Left JOIN Employees emp on emp.EmployeeId= L.EmployeeId_FK
                ORDER BY L.VoucherDate, L.LedgerId", connection))
                    {
                        command.CommandType = CommandType.Text;
                        //command.Parameters.AddWithValue("@EmployeeId", employeeId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Add(new EmployeeLedgerReportVM
                                {
                                    VoucherDate = reader.GetDateTime(0),
                                    VoucherTypeName = reader.GetString(1),
                                    ReferenceNo = reader.IsDBNull(2) ? null : reader.GetString(2),
                                    DebitAmount = reader.GetDecimal(3),
                                    CreditAmount = reader.GetDecimal(4),
                                    RunningBalance = reader.GetDecimal(5),
                                    Remarks = reader.IsDBNull(6) ? null : reader.GetString(6),
                                    EmployeeName = reader.GetString(7),
                                    EmployeeId_FK = reader.GetInt64(8)
                                });
                            }
                        }
                    }
                }
            }
            catch(Exception)
            {
                throw;
            }

            return result;
        }
        public async Task<decimal> GetEmployeeBalanceAsync(long employeeId)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(@"
                SELECT ISNULL(SUM(DebitAmount - CreditAmount), 0)
                FROM EmployeeLedger
                WHERE EmployeeId = @EmployeeId", connection))
                    {
                        command.CommandType = CommandType.Text;
                        command.Parameters.AddWithValue("@EmployeeId", employeeId);

                        return Convert.ToDecimal(await command.ExecuteScalarAsync());
                    }
                }
            }
            catch
            {
            }

            return 0;
        }

        public async Task<List<EmployeeVoucherType>> GetAllVoucherTypesAsync()
        {
            var list = new List<EmployeeVoucherType>();

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(
                        "SELECT VoucherTypeId, VoucherTypeName FROM EmployeeVoucherTypes", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                list.Add(new EmployeeVoucherType
                                {
                                    VoucherTypeId = reader.GetInt32(0),
                                    VoucherTypeName = reader.GetString(1)
                                });
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return list;
        }

        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
        {
            var employees = new List<Employee>();

            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(@"
               
             
             SELECT EmployeeId, FirstName, LastName,LTRIM(RTRIM(
        ISNULL(FirstName, '') + ' ' + ISNULL(LastName, '')
    )) AS EmployeeName, PhoneNumber, CNIC, Address, Gender, Age, EmailAddress, 
                       MaritalStatus, HusbandFatherName, Salary, JoiningDate, CreatedDate, CreatedByUserId_FK,
                       ModifiedDate, ModifiedByUserId_FK, IsDeleted
                FROM Employees
                WHERE ISNULL(IsDeleted,0) = 0
                ORDER BY FirstName, LastName", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                employees.Add(new Employee
                                {
                                    EmployeeId = reader.GetInt64(reader.GetOrdinal("EmployeeId")),
                                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                    EmployeeName = reader.GetString(reader.GetOrdinal("EmployeeName")),
                                    LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? null : reader.GetString(reader.GetOrdinal("LastName")),
                                    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                                    Cnic = reader.IsDBNull(reader.GetOrdinal("CNIC")) ? null : reader.GetString(reader.GetOrdinal("CNIC")),
                                    Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString(reader.GetOrdinal("Address")),
                                    Gender = reader.IsDBNull(reader.GetOrdinal("Gender")) ? null : reader.GetString(reader.GetOrdinal("Gender")),
                                    Age = reader.IsDBNull(reader.GetOrdinal("Age")) ? 0 : reader.GetInt32(reader.GetOrdinal("Age")),
                                    EmailAddress = reader.IsDBNull(reader.GetOrdinal("EmailAddress")) ? null : reader.GetString(reader.GetOrdinal("EmailAddress")),
                                    MaritalStatus = reader.IsDBNull(reader.GetOrdinal("MaritalStatus")) ? null : reader.GetString(reader.GetOrdinal("MaritalStatus")),
                                    HusbandFatherName = reader.IsDBNull(reader.GetOrdinal("HusbandFatherName")) ? null : reader.GetString(reader.GetOrdinal("HusbandFatherName")),
                                    Salary = reader.IsDBNull(reader.GetOrdinal("Salary")) ? 0 : reader.GetDecimal(reader.GetOrdinal("Salary")),
                                    JoiningDate = reader.IsDBNull(reader.GetOrdinal("JoiningDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("JoiningDate")),
                                    CreatedDate = reader.IsDBNull(reader.GetOrdinal("CreatedDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                    CreatedByUserIdFk = reader.IsDBNull(reader.GetOrdinal("CreatedByUserId_FK")) ? 0 : reader.GetInt64(reader.GetOrdinal("CreatedByUserId_FK")),
                                    ModifiedDate = reader.IsDBNull(reader.GetOrdinal("ModifiedDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                                    ModifiedByUserIdFk = reader.IsDBNull(reader.GetOrdinal("ModifiedByUserId_FK")) ? 0 : reader.GetInt64(reader.GetOrdinal("ModifiedByUserId_FK")),
                                    IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // optional: log exception
                Console.WriteLine($"Error fetching employees: {ex.Message}");
            }

            return employees;
        }

    }
}
