using IMS.DAL.PrimaryDBContext;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.VisualStudio.Services.WebApi;

namespace IMS.Models
{
    public class ExpenseViewModel
    {
        public List<ExpenseModel> ExpenseList { get; set; } 
        public ExpenseFilters expenseFilters { get; set; }
        public ExpenseFilters ExpenseFilters { get; set; } = new ExpenseFilters();

        public IPagedList<Expense> Expenses { get; set; }

        public IEnumerable<SelectListItem> EnabledExpenses { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        
    }
    public class ExpenseModel
    {
        public long ExpenseId { get; set; }
        public long? ProductId_FK { get; set; }
        public string ExpenseDetail { get; set; } = null!;
        public string? ProductName { get; set; }
        public string ExpenseType { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        
    }
    public class ExpenseFilters
    {
        public string? ExpenseType { get; set; }
        public long? ExpenseTypeId { get; set; }
        public long? ProductId { get; set; }
        public string? Details { get; set; }
        public decimal? AmountFrom { get; set; }
        public decimal? AmountTo { get; set; }
        
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}
