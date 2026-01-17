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
        public decimal TotalAmount { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public decimal TotalReceivedAmount { get; set; }
        public decimal TotalDueAmount { get; set; }
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

    public class DailyStockReportViewModel
    {
        public List<DailyStockReportItem> StockList { get; set; }
        public DailyStockReportFilters Filters { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public decimal TotalStockValue { get; set; }
        public decimal TotalAvailableQuantity { get; set; }
        public decimal TotalUsedQuantity { get; set; }
        public decimal TotalQuantity { get; set; }
    }

    public class DailyStockReportItem
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal UsedQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal StockValue { get; set; }
        public string? StockLocation { get; set; }
    }

    public class DailyStockReportFilters
    {
        public DateTime? ReportDate { get; set; }
        public long? ProductId { get; set; }
    }

    public class BankCreditDebitReportViewModel
    {
        public List<BankCreditDebitReportItem> TransactionList { get; set; }
        public BankCreditDebitReportFilters Filters { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public decimal TotalCreditAmount { get; set; }
        public decimal TotalDebitAmount { get; set; }
        public decimal NetBalance { get; set; }
    }

    public class BankCreditDebitReportItem
    {
        public long TransactionId { get; set; }
        public long PersonalPaymentId { get; set; }
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
        public string AccountHolderName { get; set; }
        public string? BankBranch { get; set; }
        public string TransactionType { get; set; }
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
        public string? TransactionDescription { get; set; }
        public DateTime TransactionDate { get; set; }
        public long? SaleId { get; set; }
        public long? BillNumber { get; set; }
        public string? ReferenceDescription { get; set; }
    }

    public class BankCreditDebitReportFilters
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public long? PersonalPaymentId { get; set; }
        public string? TransactionType { get; set; }
    }

    public class PurchaseReportViewModel
    {
        public List<PurchaseReportItem> PurchaseList { get; set; }
        public PurchaseReportFilters Filters { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public decimal TotalAmount { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalDueAmount { get; set; }
    }

    public class PurchaseReportItem
    {
        public long PurchaseOrderId { get; set; }
        public string VendorName { get; set; }
        public long BillNumber { get; set; }
        public long VendorIdFk { get; set; }
        public DateTime PurchaseDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public string? PurchaseDescription { get; set; }
    }

    public class PurchaseReportFilters
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public long? VendorId { get; set; }
    }
    
}
