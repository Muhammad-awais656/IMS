using IMS.Models;
using IMS.DAL.PrimaryDBContext;

namespace IMS.Common_Interfaces
{
    public interface ICustomerPaymentService
    {
        Task<CustomerPaymentViewModel> GetAllPaymentsAsync(int pageNumber, int? pageSize, CustomerPaymentFilters? filters);
        Task<Payment> GetPaymentByIdAsync(long id);
        Task<bool> CreatePaymentAsync(Payment payment);
        Task<int> UpdatePaymentAsync(Payment payment);
        Task<int> DeletePaymentAsync(long id, DateTime modifiedDate, long modifiedBy);
        Task<List<Customer>> GetAllCustomersAsync();
        Task<List<Sale>> GetAllSalesAsync();
        Task<List<Sale>> GetCustomerBillsAsync(long customerId);
        Task<bool> HasAnyPaymentsAsync();
    }
}
