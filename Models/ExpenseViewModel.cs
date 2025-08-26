using IMS.DAL.PrimaryDBContext;

namespace IMS.Models
{
    public class ExpenseViewModel
    {
        public List<Expense> ExpenseList { get; set; }
        
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        
    }   
    public class ExpenseFilters
    {
        public string? ExpenseType { get; set; }
        public long? ExpenseTypeId { get; set; }
        public string? Details { get; set; }
        public decimal? AmountFrom { get; set; }
        public decimal? AmountTo { get; set; }
        
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
    }
}
