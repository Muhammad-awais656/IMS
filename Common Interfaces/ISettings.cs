using IMS.DAL.PrimaryDBContext;
using IMS.Models;

namespace IMS.Common_Interfaces
{
    public interface ISettings
    {
       

    }
    public interface IExpenseType
    {
        Task<ExpenseTypesViewModel> GetAllExpenseTypes(int pageNumber, int? pageSize,string? name);
        Task<AdminExpenseType> GetExpenseTypeByIdAsync(long id);
        Task<bool> CreateExpenseTypeAsync(AdminExpenseType ExpenseType);
        Task<int> UpdateExpenseTypeAsync(AdminExpenseType ExpenseType);
        Task<int> DeleteExpenseTypeAsync(long id);

    }
    public interface IAdminLablesService
    {
        Task<AdminLablesViewModel> GetAllAdminLablesAsync(int pageNumber, int? pageSize, string? name);
        Task<AdminLabel> GetAdminLablesByIdAsync(long id);
        Task<bool> CreateAdminLablesAsync(AdminLabel expenseType);
        Task<int> UpdateAdminLablesAsync(AdminLabel settings);
        Task<int> DeleteAdminLablesAsync(long id);
       
        
    }
}
