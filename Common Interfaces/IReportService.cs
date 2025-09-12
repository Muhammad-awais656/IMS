using IMS.Models;

namespace IMS.Common_Interfaces
{
    public interface IReportService
    {
        Task<ReportsViewModel> GetAllSales(int pageNumber, int? pageSize, SalesReportsFilters? salesReportsFilters);
    }
}
