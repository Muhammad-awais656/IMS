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
        public string SupplierName { get; set; }
        public string? CustomerUrduName { get; set; }

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
        public string? ProductUrduName { get; set; }
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
        public string? ProductUrduName { get; set; }
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

    public class DailyStockPositionReportViewModel
    {
        public List<DailyStockPositionReportItem> StockPositionList { get; set; } = new List<DailyStockPositionReportItem>();
        public DailyStockPositionReportFilters Filters { get; set; } = new DailyStockPositionReportFilters();
        public decimal TotalPurchase { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalClosing { get; set; }
        public decimal TotalBags { get; set; }
    }

    public class DailyStockPositionReportItem
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductUrduName { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public decimal PurchaseQuantity { get; set; }
        public decimal SalesQuantity { get; set; }
        public decimal ClosingStock { get; set; }
        public decimal Bags { get; set; }
    }

    public class DailyStockPositionReportFilters
    {
        public DateTime? ReportDate { get; set; }
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

    /// <summary>Customer-wise ledger: Debit = (TotalAmount - DiscountAmount) from Sales, Credit = PaymentAmount from Payments.</summary>
    public class CustomerLedgerReportViewModel
    {
        public List<CustomerLedgerReportItem> LedgerList { get; set; } = new List<CustomerLedgerReportItem>();
        public CustomerLedgerReportFilters Filters { get; set; } = new CustomerLedgerReportFilters();
        public string? CustomerName { get; set; }
        /// <summary>Urdu for selected customer (header) when filtering by one customer.</summary>
        public string? CustomerUrduName { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal ClosingBalance { get; set; }
    }

    public class CustomerLedgerReportItem
    {
        public DateTime Date { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerUrduName { get; set; }
        public string? GLAccount { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
    }

    public class CustomerLedgerReportFilters
    {
        public long? CustomerId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    /// <summary>Vendor-wise ledger: Debit = (TotalAmount - DiscountAmount) from PurchaseOrders, Credit = PaymentAmount from BillPayments.</summary>
    public class VendorLedgerReportViewModel
    {
        public List<VendorLedgerReportItem> LedgerList { get; set; } = new List<VendorLedgerReportItem>();
        public VendorLedgerReportFilters Filters { get; set; } = new VendorLedgerReportFilters();
        public string? VendorName { get; set; }
        /// <summary>Urdu for selected vendor (header) when filtering by one vendor.</summary>
        public string? VendorUrduName { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal ClosingBalance { get; set; }
    }

    public class VendorLedgerReportItem
    {
        public DateTime Date { get; set; }
        public string? VendorName { get; set; }
        public string? VendorUrduName { get; set; }
        public string? GLAccount { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
    }

    public class VendorLedgerReportFilters
    {
        public long? VendorId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    /// <summary>Customer balances as of a single date (sales minus payments); no debit/credit detail.</summary>
    public class CustomerBalanceReportViewModel
    {
        public List<CustomerBalanceReportItem> BalanceList { get; set; } = new List<CustomerBalanceReportItem>();
        public CustomerBalanceReportFilters Filters { get; set; } = new CustomerBalanceReportFilters();
        /// <summary>Display label e.g. "All Customers" or selected customer name.</summary>
        public string? ScopeLabel { get; set; }
        /// <summary>Sum of per-row balances (net receivable across listed customers).</summary>
        public decimal TotalBalance { get; set; }
    }

    public class CustomerBalanceReportItem
    {
        public long CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerUrduName { get; set; }
        public DateTime AsOfDate { get; set; }
        public decimal Balance { get; set; }
    }

    public class CustomerBalanceReportFilters
    {
        public long? CustomerId { get; set; }
        public DateTime? AsOfDate { get; set; }
    }

    /// <summary>Vendor balances as of a single date (purchases minus bill payments); no debit/credit detail.</summary>
    public class VendorBalanceReportViewModel
    {
        public List<VendorBalanceReportItem> BalanceList { get; set; } = new List<VendorBalanceReportItem>();
        public VendorBalanceReportFilters Filters { get; set; } = new VendorBalanceReportFilters();
        public string? ScopeLabel { get; set; }
        public decimal TotalBalance { get; set; }
    }

    public class VendorBalanceReportItem
    {
        public long VendorId { get; set; }
        public string? VendorName { get; set; }
        public string? VendorUrduName { get; set; }
        public DateTime AsOfDate { get; set; }
        public decimal Balance { get; set; }
    }

    public class VendorBalanceReportFilters
    {
        public long? VendorId { get; set; }
        public DateTime? AsOfDate { get; set; }
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
        public string? VendorUrduName { get; set; }
        public string CustomerName { get; set; }
        public string? CustomerUrduName { get; set; }
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

    public class ProductWiseSalesReportViewModel
    {
        public List<ProductWiseSalesReportItem> SalesList { get; set; } = new List<ProductWiseSalesReportItem>();
        public ProductWiseSalesReportFilters Filters { get; set; } = new ProductWiseSalesReportFilters();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public decimal TotalAmount { get; set; }
        public decimal TotalWeight { get; set; }
        public long TotalQty { get; set; }
    }

    public class ProductWiseSalesReportItem
    {
        public DateTime SaleDate { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductUrduName { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public long Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
        public bool IsTotalRow { get; set; } = false; // To identify total rows for each product
    }

    public class ProductWiseSalesReportFilters
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public long? ProductId { get; set; }
    }

    public class ProductWisePurchaseReportViewModel
    {
        public List<ProductWisePurchaseReportItem> PurchaseList { get; set; } = new List<ProductWisePurchaseReportItem>();
        public ProductWisePurchaseReportFilters Filters { get; set; } = new ProductWisePurchaseReportFilters();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public decimal TotalAmount { get; set; }
        public decimal TotalWeight { get; set; }
        public long TotalQty { get; set; }
    }

    public class ProductWisePurchaseReportItem
    {
        public DateTime PurchaseDate { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductUrduName { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public long Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
        public bool IsTotalRow { get; set; } = false; // To identify total rows for each product
    }

    public class ProductWisePurchaseReportFilters
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public long? ProductId { get; set; }
    }

    public class GeneralExpensesReportViewModel
    {
        public List<GeneralExpensesReportItem> ExpensesList { get; set; } = new List<GeneralExpensesReportItem>();
        public GeneralExpensesReportFilters Filters { get; set; } = new GeneralExpensesReportFilters();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public decimal TotalAmount { get; set; }
    }

    public class GeneralExpensesReportItem
    {
        public DateTime ExpenseDate { get; set; }
        public long ExpenseTypeId { get; set; }
        public string ExpenseTypeName { get; set; } = string.Empty;
        public string ExpenseDetail { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsTotalRow { get; set; } = false; // To identify total rows for each expense type
        public string ProductName { get; set; } = string.Empty;
        public string? ProductUrduName { get; set; }
    }

    public class GeneralExpensesReportFilters
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public long? ExpenseTypeId { get; set; }
        public long? ProductId { get; set; }
    }
    
}
