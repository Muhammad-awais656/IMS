using IMS.DAL.PrimaryDBContext;
using IMS.Models;

namespace IMS.Common_Interfaces
{
    public interface IExpenseService
    {
        Task<ExpenseViewModel> GetAllExpenseAsync(int pageNumber, int? pageSize, ExpenseFilters expenseFilters);
        Task<Expense> GetExpenseByIdAsync(long id);
        Task<bool> CreateExpenseAsync(Expense expense);
        Task<int> UpdateExpenseAsync(Expense expense);
        Task<int> DeleteExpenseAsync(long id);
    }
}
