using IMS.DAL.PrimaryDBContext;
using IMS.Models;

namespace IMS.Common_Interfaces
{
    public interface IStockService
    {
        Task<bool> CreateStockAsync(StockMaster stock);
        Task<int> UpdateStockAsync(StockMaster stock);
        Task<int> DeleteStockAsync(long id, DateTime modifiedDate, long modifiedBy);
        Task<StockMaster> GetStockByIdAsync(long id);
        Task<StockViewModel> GetAllStocksAsync(int pageNumber, int? pageSize, StockFilters? filters);
        Task<List<Product>> GetAllProductsAsync();
        Task<List<TransactionStatus>> GetAllTransactionStatusesAsync();
        Task<List<AdminCategory>> GetAllEnabledCategoriesAsync();
        Task<List<Product>> GetProductsByCategoryIdAsync(long categoryId);
        Task<StockMaster> GetStockByProductIdAsync(long productId);
        Task<StockHistoryViewModel> GetStockHistoryAsync(int pageNumber, int? pageSize, StockHistoryFilters? filters);
    }
}


