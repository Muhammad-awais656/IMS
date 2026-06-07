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
            long? customerId, long? vendorId, DateTime createdDate, long createdBy, DateTime modifiedDate, long modifiedBy, 
            decimal discountAmount, long billNumber, string saleDescription, DateTime saleDate, 
            string paymentMethod = null, long? onlineAccountId = null, decimal salesFreight = 0);
        Task<decimal> GetPreviousDueAmountByCustomerIdAsync(long customerId);
        /// <summary>Inserts an opening balance sale row (BillNumber=0, SaleDescription='opening Balance', PaymentMethod='Pay Later').</summary>
        Task<long> AddOpeningBalanceSaleAsync(long customerId, string typePayableOrReceivable, decimal openingBalance, long createdBy, DateTime? balanceDate = null);
        long AddSaleDetails(long saleId, long productId, decimal unitPrice, decimal quantity, decimal salePrice,
            decimal lineDiscountAmount, decimal payableAmount, long productRangeId, DateTime currentdate, long userId, string paymentMethod, long? accountId, out int returnValue);
      
        Task<StockMaster> GetStockByProductIdAsync(long productId);
        long UpdateStock(long stockMasterId, long productId, decimal availableQuantity,decimal totalQty, decimal usedQuantity,
            long modifiedBy, DateTime modifiedDate);
        long SaleTransactionCreate(long stockMasterId, decimal quantity, string comment, DateTime createdDate,
            long createdBy, long transactionStatusId, long saleId);
        
        // Edit Sale functionality methods
        Task<List<SaleDetailViewModel>> GetSaleDetailsBySaleIdAsync(long saleId);
        Task<int> DeleteSaleDetailsBySaleIdAsync(long saleId);
        Task<int> DeletePaymentBySaleIdAsync(long saleId);
        Task<int> DeleteStockTransactionBySaleIdAsync(long saleId);
        Task<int> ReverseOnlinePaymentTransactionBySaleIdAsync(long saleId, long modifiedBy);
        Task<int> UpdatePaymentsBySaleIdAsync(long saleId);

        Task<int> TransactionDeleteAndStockUpdate(long saleId);
        
        // Online Payment Transaction methods
        Task<long> ProcessOnlinePaymentTransactionAsync(long personalPaymentId, long saleId, decimal creditAmount, 
            string transactionDescription, long createdBy, DateTime? createdDate = null);
        
        // Print Receipt methods
        Task<SalePrintViewModel> GetSaleForPrintAsync(long saleId);

        /// <summary>Gets sale detail report data for Excel export (SaleDetails + ProductName + Code). Respects SalesFilters when provided.</summary>
        Task<List<SaleDetailReportItem>> GetSaleDetailReportForExportAsync(SalesFilters? filters);
    }
}
