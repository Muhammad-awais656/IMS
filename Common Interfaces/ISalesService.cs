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

        // Add Sale functionality methods
        Task<List<ProductSizeViewModel>> GetProductUnitPriceRangeByProductIdAsync(long productId);
        Task<long> CreateSaleAsync(decimal totalAmount, decimal totalReceivedAmount, decimal totalDueAmount, 
            long customerId, DateTime createdDate, long createdBy, DateTime modifiedDate, long modifiedBy, 
            decimal discountAmount, long billNumber, string saleDescription, DateTime saleDate, 
            string paymentMethod = null, long? onlineAccountId = null);
        Task<decimal> GetPreviousDueAmountByCustomerIdAsync(long customerId);
        long AddSaleDetails(long saleId, long productId, decimal unitPrice, decimal quantity, decimal salePrice,
            decimal lineDiscountAmount, decimal payableAmount, long productRangeId, out int returnValue);
      
        Task<StockMaster> GetStockByProductIdAsync(long productId);
        long UpdateStock(long stockMasterId, long productId, decimal availableQuantity,decimal totalQty, decimal usedQuantity,
            long modifiedBy, DateTime modifiedDate);
        long SaleTransactionCreate(long stockMasterId, decimal quantity, string comment, DateTime createdDate,
            long createdBy, long transactionStatusId, long saleId);
        
        // Edit Sale functionality methods
        Task<List<SaleDetailViewModel>> GetSaleDetailsBySaleIdAsync(long saleId);
        Task<int> DeleteSaleDetailsBySaleIdAsync(long saleId);
        Task<int> TransactionDeleteAndStockUpdate(long saleId);
        
        // Online Payment Transaction methods
        Task<long> ProcessOnlinePaymentTransactionAsync(long personalPaymentId, long saleId, decimal creditAmount, 
            string transactionDescription, long createdBy, DateTime? createdDate = null);
        
        // Print Receipt methods
        Task<SalePrintViewModel> GetSaleForPrintAsync(long saleId);
    }
}
