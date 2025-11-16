using IMS.DAL.PrimaryDBContext;


namespace IMS.Models
{
    public class ReportsViewModel
    {
        public List<salesReportItems> SalesList { get; set; }
        public SalesReportsFilters SalesReportsFilters { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
 public class salesReportItems
    {
        public long SaleId { get; set; }
        public string CustomerName { get; set; }

        public long BillNumber { get; set; }
        public long CustomerIdFk { get; set; }

        public DateTime SaleDate { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TotalReceivedAmount { get; set; }

        public decimal TotalDueAmount { get; set; }

      

        public string? SaleDescription { get; set; }

    }
    public class SalesReportsFilters
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public long? CustomerId { get; set; }
       
        
    }
    
    public class ProfitLossReportViewModel
    {
        public List<ProfitLossReportItem> ProfitLossList { get; set; }
        public ProfitLossReportFilters Filters { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public decimal TotalSalesAmount { get; set; }
        public decimal TotalPurchaseCost { get; set; }
        public decimal TotalProfitLoss { get; set; }
    }

    public class ProfitLossReportItem
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public long TotalQuantitySold { get; set; }
        public decimal TotalSalesAmount { get; set; }
        public decimal TotalPurchaseCost { get; set; }
        public decimal ProfitLoss { get; set; }
        public decimal ProfitLossPercentage { get; set; }
    }

    public class ProfitLossReportFilters
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public long? ProductId { get; set; }
    }
    
}
