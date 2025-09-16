using IMS.Models;
using IMS.DAL.PrimaryDBContext;

namespace IMS.Common_Interfaces
{
    public interface ISalesService
    {
        Task<SalesViewModel> GetAllSalesAsync(int pageNumber, int? pageSize, SalesFilters? filters);
        Task<Sale> GetSaleByIdAsync(long id);
        Task<bool> CreateSaleAsync(Sale sale);
        Task<int> UpdateSaleAsync(Sale sale);
        Task<int> DeleteSaleAsync(long id, DateTime modifiedDate, long modifiedBy);
        Task<List<Customer>> GetAllCustomersAsync();
        Task<long> GetNextBillNumberAsync();
        Task<bool> HasAnySalesAsync();
    }
}
