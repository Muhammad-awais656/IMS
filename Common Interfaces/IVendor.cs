using IMS.DAL.PrimaryDBContext;
using IMS.Models;

namespace IMS.Common_Interfaces
{
    public interface IVendor
    {
        Task<VendorViewModel> GetAllVendors(int pageNumber, int? pageSize, string? SupplierName, string? contactNo, string? NTN);
        Task<AdminSupplier> GetVendorByIdAsync(long id);
        Task<bool> CreateVendorAsync(AdminSupplier adminSupplier);
        Task<int> UpdateVendorAsync(AdminSupplier adminSupplier);
        Task<int> DeleteVendorAsync(long id);

        Task<List<AdminSupplier>> GetAllEnabledVendors();

        // Bill Generation Methods
        Task<long> GetNextBillNumberAsync();
        Task<List<Product>> GetAllEnabledProductsAsync();
        Task<List<ProductSizeViewModel>> GetProductUnitPriceRangeByProductIdAsync(long productId);
        Task<StockMaster?> GetStockByProductIdAsync(long productId);
        Task<decimal> GetPreviousDueAmountByVendorIdAsync(long vendorId);
        Task<long> CreateVendorBillAsync(decimal totalAmount, decimal paidAmount, decimal dueAmount, long vendorId, DateTime createdDate, long createdBy, DateTime modifiedDate, long modifiedBy, decimal discountAmount, long billNumber, string description, DateTime billDate, string? paymentMethod, long? onlineAccountId);
        Task<long> AddVendorBillDetails(long billId, long productId, decimal unitPrice, decimal purchasePrice, long quantity, decimal salePrice, decimal lineDiscountAmount, decimal payableAmount, long productRangeId);
        long UpdateStock(long stockMasterId, long productId, decimal availableQuantity, decimal totalQuantity, decimal usedQuantity, long modifiedBy, DateTime modifiedDate);
        long VendorBillTransactionCreate(long stockMasterId, decimal quantity, string description, DateTime transactionDate, long createdBy, int transactionType, long billId);
        Task<long> ProcessOnlinePaymentTransactionAsync(long personalPaymentId, long billId, decimal amount, string description, long createdBy, DateTime createdDate);
        Task<VendorBillPrintViewModel?> GetVendorBillForPrintAsync(long billId);
        Task<PersonalPaymentViewModel> GetAllPersonalPaymentsAsync(int pageNumber, int pageSize, PersonalPaymentFilters filters);
    }
}
