using IMS.DAL.PrimaryDBContext;

namespace IMS.Models
{
    public class StockTransactionHistoryViewModel
    {
        public long StockTransactionId { get; set; }
        public long StockMasterIdFk { get; set; }
        public decimal StockQuantity { get; set; }
        public DateTime TransactionDate { get; set; }
        public long CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public long TransactionStatusId { get; set; }
        public string? Comment { get; set; }
        public long? SaleId { get; set; }
        
        // Additional fields for display
        public string TransactionType { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string TransactionStatusName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class StockHistoryViewModel
    {
        public List<StockTransactionHistoryViewModel> TransactionList { get; set; } = new List<StockTransactionHistoryViewModel>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        
        // Filters
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? TransactionType { get; set; }
    }

    public class StockHistoryFilters
    {
        public long? StockMasterId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? TransactionType { get; set; }
        public long? TransactionTypeId { get; set; }
    }
}
