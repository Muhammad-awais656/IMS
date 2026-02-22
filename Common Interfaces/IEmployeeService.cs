using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using System.Collections;

namespace IMS.Common_Interfaces
{
    public interface IEmployeeService
    {
        Task<EmployeeViewModel> GetAllEmployeesAsync(int pageNumber, int? pageSize, EmployeesFilters employeesFilters);
        Task<Employee> GetEmployeeByIdAsync(long id);
        Task<bool> CreateEmployeeAsync(Employee employee);
        Task<int> UpdateEmployeeAsync(Employee employee);
        Task<int> DeleteEmployeeAsync(long id, DateTime modifiedDate, long modifiedUserId);
        Task<EmployeeVoucherType> GetVoucherTypeByIdAsync(int voucherTypeId);
        Task<bool> AddEmployeeLedgerAsync(EmployeeLedger ledger);

        Task<bool> IsOpeningBalanceExistsAsync(long employeeId, int voucherTypeId);
        Task<List<EmployeeLedgerReportVM>> GetEmployeeLedgerReportAsync(long employeeId);

        Task<List<EmployeeLedgerReportVM>> GetAllEmployeeLedgerReportAsync();
        Task<decimal> GetEmployeeBalanceAsync(long employeeId);

        Task<List<EmployeeVoucherType>> GetAllVoucherTypesAsync();
        Task<IEnumerable<Employee>> GetAllEmployeesAsync();
    }
}
