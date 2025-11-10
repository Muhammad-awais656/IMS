using IMS.DAL.PrimaryDBContext;

namespace IMS.Models
{
    public class StockWithProductViewModel
    {
        public long StockMasterId { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal UsedQuantity { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal AvailableQuantityAmt { get; set; }
        public DateTime? CreatedDate { get; set; }
        public long? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public long? ModifiedBy { get; set; }
        
        // Product information from JOIN
        public string ProductName { get; set; } = string.Empty;
        public string? ProductCode { get; set; }
        public string? StockLocaion { get; set; }
        public decimal UnitPrice { get; set; }
    }
    public class StockViewModel
    {
        public List<StockWithProductViewModel> StockList { get; set; } = new List<StockWithProductViewModel>();
        public List<Product> ProductList { get; set; } = new List<Product>();
        public List<TransactionStatus> TransactionStatusList { get; set; } = new List<TransactionStatus>();
        public List<StockTransaction> TransactionList { get; set; } = new List<StockTransaction>();

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class StockFilters
    {
        public string? ProductName { get; set; }
    }
}


