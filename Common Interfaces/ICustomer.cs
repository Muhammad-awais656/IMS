using IMS.DAL.PrimaryDBContext;
using IMS.Models;

namespace IMS.Common_Interfaces
{
    public interface ICustomer
    {
        Task<CustomerViewModel> GetCustomers(int pageNumber, int? pageSize, string? Customername,string? contactNo,string? email );
        Task<Customer> GetCustomerIdAsync(long id);
        Task<bool> CreateCustomerAsync(Customer customer);
        Task<int> UpdateCustomerAsync(Customer customer);
        Task<int> DeleteCustomerAsync(long id);

        Task<List<Customer>> GetAllEnabledCustomers();
    }
}
