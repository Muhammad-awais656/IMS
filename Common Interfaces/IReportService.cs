using IMS.Models;

namespace IMS.Common_Interfaces
{
    public interface IReportService
    {
        Task<ReportsViewModel> GetAllSales(int pageNumber, int? pageSize, SalesReportsFilters? salesReportsFilters);
        Task<ReportsViewModel> GetAllSalesReport(int pageNumber, int? pageSize, SalesReportsFilters? salesReportsFilters);
        Task<ProfitLossReportViewModel> GetProductWiseProfitLoss(int pageNumber, int? pageSize, ProfitLossReportFilters? filters);
        Task<ProfitLossReportViewModel> GetProductWiseProfitLossReport(int pageNumber, int? pageSize, ProfitLossReportFilters? filters);
        Task<DailyStockReportViewModel> GetDailyStockReport(int pageNumber, int? pageSize, DailyStockReportFilters? filters);
        Task<DailyStockReportViewModel> GetDailyStockReportForExport(int pageNumber, int? pageSize, DailyStockReportFilters? filters);
        Task<BankCreditDebitReportViewModel> GetBankCreditDebitReport(int pageNumber, int? pageSize, BankCreditDebitReportFilters? filters);
        Task<BankCreditDebitReportViewModel> GetBankCreditDebitReportForExport(int pageNumber, int? pageSize, BankCreditDebitReportFilters? filters);
        Task<PurchaseReportViewModel> GetPurchaseReport(int pageNumber, int? pageSize, PurchaseReportFilters? filters);
        Task<PurchaseReportViewModel> GetPurchaseReportForExport(int pageNumber, int? pageSize, PurchaseReportFilters? filters);
        Task<ProductWiseSalesReportViewModel> GetProductWiseSalesReport(int pageNumber, int? pageSize, ProductWiseSalesReportFilters? filters);
        Task<ProductWisePurchaseReportViewModel> GetProductWisePurchaseReport(int pageNumber, int? pageSize, ProductWisePurchaseReportFilters? filters);
        Task<GeneralExpensesReportViewModel> GetGeneralExpensesReport(int pageNumber, int? pageSize, GeneralExpensesReportFilters? filters);
        Task<DailyStockPositionReportViewModel> GetDailyStockPositionReport(DailyStockPositionReportFilters? filters);
    }
}
