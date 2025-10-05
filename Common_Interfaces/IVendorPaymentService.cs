using IMS.DAL.PrimaryDBContext;
using IMS.Models;

namespace IMS.Common_Interfaces
{
    public interface IVendorPaymentService
    {
        Task<VendorPaymentViewModel> GetAllBillPaymentsAsync(int pageNumber, int? pageSize, VendorPaymentFilters? filters);
        Task<List<AdminSupplier>> GetAllVendorsAsync();
        Task<List<SupplierBillNumber>> GetSupplierBillNumbersAsync(long supplierId);
    }
}
