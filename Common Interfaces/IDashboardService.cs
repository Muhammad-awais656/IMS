using IMS.Models;

namespace IMS.Common_Interfaces
{
    public interface IDashboardService
    {
        /// <param name="branchId">When set (non-admin), scope metrics to legacy <c>Users.BranchId</c> via <c>CreatedBy</c>.</param>
        Task<int> GetTotalVendorsCount(int? branchId = null);
        Task<int> GetTotalProductCount(int? branchId = null);
        Task<int> GetTotalCategoryCount(int? branchId = null);
        Task<SalesChartViewModel> GetLast12MonthsSalesAsync(int? branchId = null);
        Task<decimal> GetCurrentMonthRevenueAsync(int? branchId = null);
        Task<List<StockStaus>> GetStockStatusAsync(long? productId, int? branchId = null);

        Task<AnalyticsSummaryDto> GetAnalyticsSummaryAsync(DateTime fromDate, DateTime toDate, int? branchId = null);
        Task<IReadOnlyList<SalesTrendPointDto>> GetSalesTrendAsync(DateTime fromDate, DateTime toDate, int? branchId = null);
    }
}
