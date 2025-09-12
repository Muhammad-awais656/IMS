using Microsoft.AspNetCore.Mvc.Rendering;

namespace IMS.Models
{
    public class DashboardViewModel
    {
        // Summary cards
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalVendors { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public List<StockStaus> StockStaus { get; set; } = new();
        // Chart series
        public List<decimal> MonthlySales { get; set; } = new();
        public List<string> MonthlyLabels { get; set; } = new();

        // Stock status for donut
        public decimal? InStockCount { get; set; }
        public decimal? LowStockCount { get; set; }
        public decimal? OutOfStockCount { get; set; }
        public List<SelectListItem> ProductList { get; set; } = new();

        // Top products
        public List<TopProductDto> TopProducts { get; set; } = new();

        // Recent transactions (could be purchases or sales)
        public List<RecentTransactionDto> RecentTransactions { get; set; } = new();
    }

    public class TopProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public int Sold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class RecentTransactionDto
    {
        public long TransactionId { get; set; }
        public string Type { get; set; } // Sale/Purchase
        public string Party { get; set; } // Customer/Vendor
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
    }
    public class SalesChartViewModel
{
    public List<string> Months { get; set; } = new List<string>();
    public List<decimal> Sales { get; set; } = new List<decimal>();
}
    public class StockStaus
    {

        // Stock status for donut
        public long? ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal? InStockCount { get; set; }
        public decimal? AvailableStockCount { get; set; }
        public decimal? OutOfStockCount { get; set; }
    }

}
