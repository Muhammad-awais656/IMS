using IMS.Models;

namespace IMS.Common_Interfaces
{
    public interface IDashboardService
    {
        public Task<int> GetTotalVendorsCount();
        public Task<int> GetTotalProductCount();
        public Task<int> GetTotalCategoryCount();
        public Task<SalesChartViewModel> GetLast12MonthsSalesAsync();
        public Task<List<StockStaus>> GetStockStatusAsync(long? productId);


    }
}
