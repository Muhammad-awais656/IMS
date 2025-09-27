using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using IMS.DAL.PrimaryDBContext;

namespace IMS.Common_Interfaces
{
    public interface IVendorBillsService
    {
        Task<VendorBillsViewModel> GetAllBillsAsync(int pageNumber, int? pageSize, VendorBillsFilters? filters);
        Task<List<AdminSupplier>> GetAllVendorsAsync();
        Task<List<SupplierBillNumber>> GetSupplierBillNumbersAsync(long supplierId);
        
        // Bill Generation Methods
        Task<BillGenerationViewModel> GetBillGenerationDataAsync(long? vendorId = null);
        Task<long> GetNextBillNumberAsync(long supplierId);
        Task<List<Product>> GetVendorProductsAsync(long supplierId);
        Task<List<ProductRange>> GetProductUnitPriceRangesAsync(long productId);
        Task<decimal> GetPreviousDueAmountAsync(long? billId, long vendorId);
    }
}
