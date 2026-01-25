using IMS.DAL.PrimaryDBContext;
using IMS.Models;

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
        
        // New methods for Generate Bill functionality
        Task<List<Product>> GetAllProductsAsync();
        Task<List<ProductRange>> GetProductSizesAsync(long productId);
        Task<decimal> GetPreviousDueAmountAsync(long vendorId);
        Task<long> CreateBillAsync(GenerateBillViewModel model);
        
        // Edit functionality methods
        Task<VendorBillViewModel?> GetVendorBillByIdAsync(long billId);
        Task<BillPayment> GetVendorBillByPaymentIdAsync(long paymentId);
        Task<List<BillItemViewModel>> GetVendorBillItemsAsync(long billId);
        Task<bool> UpdateVendorBillAsync(long billId, VendorBillGenerationViewModel model);
        
        // Account balance method
        Task<decimal> GetAccountBalanceAsync(long accountId);
        
        // Delete method
        Task<bool> DeleteVendorBillAsync(long billId, long userId);
        
        // Get all bill numbers for vendor
        Task<List<VendorBillViewModel>> GetAllBillNumbersForVendorAsync(long vendorId);
        
        // Get active bill numbers for vendor using GetAllVendorActiveBillNumbers stored procedure
        Task<List<SupplierBillNumber>> GetActiveBillNumbersAsync(long supplierId);
    }
}
