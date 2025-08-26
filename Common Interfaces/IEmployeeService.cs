using IMS.DAL.PrimaryDBContext;
using IMS.Models;

namespace IMS.Common_Interfaces
{
    public interface IEmployeeService
    {
        Task<EmployeeViewModel> GetAllEmployeesAsync(int pageNumber, int? pageSize, EmployeesFilters employeesFilters);
        Task<Employee> GetEmployeeByIdAsync(long id);
        Task<bool> CreateEmployeeAsync(Employee employee);
        Task<int> UpdateEmployeeAsync(Employee employee);
        Task<int> DeleteEmployeeAsync(long id, DateTime modifiedDate, long modifiedUserId);
    }
}
