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
        public List<ProfitLossReportItem> ProfitLossList { get; set; } = new List<ProfitLossReportItem>();
        /// <summary>Spreadsheet-style blocks: Previous, Purchase, Purchase Exp, Total Stock, Sale, Balance, Bags, Profit.</summary>
        public List<ProductWiseProfitLossDetailSection> ProductDetailSections { get; set; } = new List<ProductWiseProfitLossDetailSection>();
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

    /// <summary>One product block matching the Product Wise P&amp;L spreadsheet (weight = qty in stock base units).</summary>
    public class ProductWiseProfitLossDetailSection
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductUrduName { get; set; }
        public string ProductCode { get; set; } = string.Empty;

        public decimal PreviousWeight { get; set; }
        public decimal PreviousAmount { get; set; }

        public decimal PurchaseWeight { get; set; }
        public decimal PurchaseAmount { get; set; }

        public decimal PurchaseExpenseAmount { get; set; }

        public decimal TotalStockWeight { get; set; }
        public decimal TotalStockAmount { get; set; }

        public decimal SaleWeight { get; set; }
        public decimal SaleAmount { get; set; }

        public decimal BalanceStockWeight { get; set; }
        public decimal BalanceStockAmount { get; set; }

        /// <summary>Packaging units (optional). Amount column in UI = BagsWeight × BagsRate when both set.</summary>
        public decimal BagsWeight { get; set; }
        public decimal BagsRate { get; set; }

        public decimal Profit { get; set; }

        public decimal PreviousRate => PreviousWeight > 0 ? PreviousAmount / PreviousWeight : 0;
        public decimal PurchaseRate => PurchaseWeight > 0 ? PurchaseAmount / PurchaseWeight : 0;
        public decimal TotalStockRate => TotalStockWeight > 0 ? TotalStockAmount / TotalStockWeight : 0;
        public decimal SaleRate => SaleWeight > 0 ? SaleAmount / SaleWeight : 0;
        public decimal BalanceRate => TotalStockRate;
        public decimal BagsAmount => BagsWeight * BagsRate;
    }

    /// <summary>Product wise = paged spreadsheet blocks per product. Overall = all products, summary totals + compact table (no pagination).</summary>
    public enum ProfitLossReportViewMode
    {
        ProductWise = 0,
        Overall = 1
    }

    public class ProfitLossReportFilters
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public long? ProductId { get; set; }
        public ProfitLossReportViewMode ViewMode { get; set; } = ProfitLossReportViewMode.ProductWise;
    }

    /// <summary>
    /// Bank account balances from ledger transactions (credits minus debits) up to end of the selected date.
    /// </summary>
    public class BankBalancesReportViewModel
    {
        public BankBalancesReportFilters Filters { get; set; } = new BankBalancesReportFilters();
        public List<BankBalancesReportRow> Rows { get; set; } = new List<BankBalancesReportRow>();
        /// <summary>Sum of balances when showing all accounts; same as the one account balance when filtered.</summary>
        public decimal TotalBalance { get; set; }
    }

    public class BankBalancesReportFilters
    {
        /// <summary>Balance as of end of this calendar day (all transactions on or before this time).</summary>
        public DateTime? ReportDate { get; set; }
        /// <summary>When set, only this bank account; when null, all active accounts.</summary>
        public long? PersonalPaymentId { get; set; }
    }

    public class BankBalancesReportRow
    {
        public long PersonalPaymentId { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolderName { get; set; } = string.Empty;
        public string? BankBranch { get; set; }
        public decimal BalanceAsOf { get; set; }
    }

    public class BankLedgerReportViewModel
    {
        public BankLedgerReportFilters Filters { get; set; } = new BankLedgerReportFilters();
        public List<PersonalPaymentTransactionViewModel> Transactions { get; set; } = new List<PersonalPaymentTransactionViewModel>();
        public PersonalPaymentAccountSummary? AccountSummary { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public int? PageSize { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class BankLedgerReportFilters
    {
        public long? PersonalPaymentId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? TransactionType { get; set; }
    }

    /// <summary>Stock ledger for one product (same data as Stock Transaction History modal).</summary>
    public class StockTransactionsReportViewModel
    {
        public StockHistoryFilters Filters { get; set; } = new StockHistoryFilters();
        public List<StockTransactionHistoryViewModel> TransactionList { get; set; } = new List<StockTransactionHistoryViewModel>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public decimal? AvailableQuantity { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }
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
        /// <summary>When quantities are converted for display, the selected unit abbreviation (e.g. kg, Bori).</summary>
        public string? DisplayMeasuringUnitAbbreviation { get; set; }
        /// <summary>Full measuring unit name when a display unit is selected (for labels and exports).</summary>
        public string? DisplayMeasuringUnitName { get; set; }
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
        /// <summary>Optional display unit (measuring unit id). When set, total/used/available quantities are converted from base (smallest) unit like the Stock screen.</summary>
        public long? DisplayMeasuringUnitId { get; set; }
        /// <summary>When false (Stock Available Balance report), unit price and stock value columns are omitted and exports stay pricing-free.</summary>
        public bool IncludeUnitPriceAndValue { get; set; } = true;
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

    /// <summary>Customer-wise ledger: Debit = TotalAmount from Sales (net invoice total; matches Add Sale due logic), Credit = PaymentAmount from Payments.</summary>
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

    /// <summary>Vendor-wise ledger: Debit = TotalAmount from PurchaseOrders (net), Credit = PaymentAmount from BillPayments.</summary>
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

    /// <summary>
    /// As-of snapshot: customer balance (sales − payments) appears as receivable when ≥ 0, otherwise as payable (positive) when the customer is in credit.
    /// Vendor balance (purchases − bill payments) appears as payable when ≥ 0, otherwise as receivable (positive) when the vendor balance is a credit in our favor.
    /// </summary>
    public class PayableReceivableReportViewModel
    {
        public List<PayableReceivableReportItem> Rows { get; set; } = new List<PayableReceivableReportItem>();
        public PayableReceivableReportFilters Filters { get; set; } = new PayableReceivableReportFilters();
        /// <summary>Sum of amounts shown in the Receivable column.</summary>
        public decimal TotalReceivable { get; set; }
        /// <summary>Sum of amounts shown in the Payable column.</summary>
        public decimal TotalPayable { get; set; }
    }

    public class PayableReceivableReportItem
    {
        public DateTime AsOfDate { get; set; }
        public string? CustomerName { get; set; }
        public string? VendorName { get; set; }
        /// <summary>Amount in the Payable column (non-negative); may reflect a customer credit balance moved from receivable.</summary>
        public decimal Payable { get; set; }
        /// <summary>Amount in the Receivable column (non-negative); may reflect a vendor credit balance moved from payable.</summary>
        public decimal Receivable { get; set; }
    }

    public class PayableReceivableReportFilters
    {
        public DateTime? AsOfDate { get; set; }
        public long? CustomerId { get; set; }
        public long? VendorId { get; set; }
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
        public decimal TotalQty { get; set; }
    }

    public class ProductWiseSalesReportItem
    {
        public DateTime SaleDate { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductUrduName { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public decimal Qty { get; set; }
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
        public decimal TotalQty { get; set; }
    }

    public class ProductWisePurchaseReportItem
    {
        public DateTime PurchaseDate { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductUrduName { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public decimal Qty { get; set; }
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

    public class CashInHandReportViewModel
    {
        public List<CashInHandReportItem> Items { get; set; } = new List<CashInHandReportItem>();
        public CashInHandReportFilters Filters { get; set; } = new CashInHandReportFilters();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        /// <summary>Total cash inflows in the period (sales + customer cash payments).</summary>
        public decimal TotalCashIn { get; set; }
        /// <summary>Total cash outflows (expenses, purchase payments, salaries).</summary>
        public decimal TotalCashOut { get; set; }
        /// <summary>Net cash movement (inflows − outflows).</summary>
        public decimal NetCash { get; set; }
    }

    public class CashInHandReportItem
    {
        public DateTime TransactionDate { get; set; }
        public long BillNumber { get; set; }
        public string PartyName { get; set; } = string.Empty;
        /// <summary>Cash sale, cash payment, expense, purchase payment (cash), salary, etc.</summary>
        public string SourceKind { get; set; } = string.Empty;
        /// <summary>Positive = cash in; negative = cash out.</summary>
        public decimal CashAmount { get; set; }
        public long? SaleId { get; set; }
        public long? PaymentId { get; set; }
    }

    public class CashInHandReportFilters
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public long? CustomerId { get; set; }
        /// <summary>Limits purchase (cash) payment rows to this supplier when set.</summary>
        public long? VendorId { get; set; }
        /// <summary>Exact <see cref="CashInHandReportItem.SourceKind"/> value, or null/empty for all sources.</summary>
        public string? SourceKind { get; set; }
    }

}
