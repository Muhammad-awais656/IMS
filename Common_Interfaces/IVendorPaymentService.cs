using IMS.DAL.PrimaryDBContext;
using IMS.Models;

namespace IMS.Common_Interfaces
{
    public interface IVendorPaymentService
    {
        Task<VendorPaymentViewModel> GetAllBillPaymentsAsync(int pageNumber, int? pageSize, VendorPaymentFilters? filters);
        Task<List<AdminSupplier>> GetAllVendorsAsync();
        Task<List<SupplierBillNumber>> GetSupplierBillNumbersAsync(long supplierId);
        Task<bool> CreateVendorPaymentAsync(decimal paymentAmount, long billId, long supplierId, DateTime paymentDate, long createdBy, DateTime createdDate, string? description, string? paymentMethod = null, long? onlineAccountId = null);
        Task<bool> DeleteBillPaymentAsync(long paymentId);
        Task<bool> UpdateBillPaymentAsync(long paymentId, decimal paymentAmount, long supplierId, DateTime paymentDate, string? description, string? paymentMethod = null, long? onlineAccountId = null);
    }
}
