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
        Task<List<AdminExpenseType>> GetAllEnabledExpenseTypesAsync();

    }
    public interface IAdminLablesService
    {
        Task<AdminLablesViewModel> GetAllAdminLablesAsync(int pageNumber, int? pageSize, string? name);
        Task<AdminLabel> GetAdminLablesByIdAsync(long id);
        Task<bool> CreateAdminLablesAsync(AdminLabel expenseType);
        Task<int> UpdateAdminLablesAsync(AdminLabel settings);
        Task<int> DeleteAdminLablesAsync(long id);

        Task<List<AdminLabel>> GetAllEnabledAdminLablesAsync();



    }
    public interface ICategoryService
    {
        Task<CategoriesViewModel> GetAllAdminCategoryAsync(int pageNumber, int? pageSize, string? name);
        Task<AdminCategory> GetAdminCategoryByIdAsync(long id);
        Task<bool> CreateAdminCategoryAsync(AdminCategory expenseType);
        Task<int> UpdateAdminCategoryAsync(AdminCategory settings);
        Task<int> DeleteAdminCategoryAsync(long id);
        Task<List<AdminCategory>> GetAllEnabledCategoriesAsync();
    }

    public interface IAdminMeasuringUnitTypesService
    {
        Task<AdminMeasuringUnitTypesViewModel> GetAllAdminMeasuringUnitTypesAsync(int pageNumber, int? pageSize, string? name);
        Task<AdminMeasuringUnitType> GetAdminMeasuringUnitTypesByIdAsync(long id);
        Task<bool> CreateAdminMeasuringUnitTypesAsync(AdminMeasuringUnitType expenseType);
        Task<int> UpdateAdminMeasuringUnitTypesAsync(AdminMeasuringUnitType settings);
        Task<int> DeleteAdminMeasuringUnitTypesAsync(long id);
        Task<List<AdminMeasuringUnitType>> GetAllEnabledMeasuringUnitTypesAsync();

    }

    public interface IAdminMeasuringUnitService
    {
        Task<MeasuringUnitViewModel> GetAllAdminMeasuringUnitAsync(int pageNumber, int? pageSize, string? name);
        Task<AdminMeasuringUnit> GetAdminMeasuringUnitByIdAsync(long id);
        Task<bool> CreateAdminMeasuringUnitAsync(AdminMeasuringUnit adminMeasuringUnit);
        Task<int> UpdateAdminMeasuringUnitAsync(AdminMeasuringUnit adminMeasuringUnit);
        Task<int> DeleteAdminMeasuringUnitAsync(long id);

        Task<List<AdminMeasuringUnitType>> AdminMeasuringUnitTypeCacheAsync();
        Task<List<AdminMeasuringUnit>> GetAllMeasuringUnitsByMUTIdAsync(long? id);
        Task<List<AdminMeasuringUnit>> GetAllEnabledMeasuringUnitsByMUTIdAsync(long? id);



    }

    public interface IUnitConversionService
    {
        Task<List<UnitConversion>> GetAllUnitConversionsAsync();
        Task<UnitConversionViewModel> GetAllUnitConversionsPagedAsync(int pageNumber, int pageSize, UnitConversionFilters filters);
        Task<UnitConversion> GetUnitConversionByIdAsync(long id);
        Task<bool> CreateUnitConversionAsync(UnitConversion unitConversion);
        Task<int> UpdateUnitConversionAsync(UnitConversion unitConversion);
        Task<int> DeleteUnitConversionAsync(long id);
        Task<decimal?> ConvertUnitAsync(long fromUnitId, long toUnitId, decimal quantity);
        Task<decimal?> ConvertUnitToSmallestAsync(long fromUnitId, long toUnitId, decimal quantity);
        Task<List<UnitConversion>> GetConversionsByFromUnitAsync(long fromUnitId);
        Task<List<UnitConversion>> GetEnabledConversionsAsync();
        Task<AdminMeasuringUnit> GetSmallestMeasuringUnitAsync();
    }

    public interface IBranchService
    {
        Task<BranchViewModel> GetAllBranchesAsync(int pageNumber, int? pageSize, string? search);
        Task<Branch?> GetBranchByIdAsync(int id);
        Task<bool> CreateBranchAsync(Branch branch);
        Task<int> UpdateBranchAsync(Branch branch);
        Task<int> DeleteBranchAsync(int id);
        Task<List<Branch>> GetAllActiveBranchesAsync();
    }

    /// <summary>
    /// Loads branches for the login page without requiring an existing session (uses default Shop connection).
    /// </summary>
    public interface ILoginBranchesService
    {
        Task<List<Branch>> GetBranchesForLoginAsync();
    }

    public interface IRoleService
    {
        Task<RoleViewModel> GetAllRolesAsync(int pageNumber, int? pageSize, string? search);
        Task<AdminRole?> GetRoleByIdAsync(long id);
        Task<bool> CreateRoleAsync(AdminRole role);
        Task<int> UpdateRoleAsync(AdminRole role);
        Task<int> DeleteRoleAsync(long id);
        /// <summary>Active roles for dropdowns (e.g. Create/Edit User).</summary>
        Task<List<AdminRole>> GetActiveRolesForDropdownAsync();
    }

    public interface IUserPermissionsService
    {
        Task<List<UserForPermissionsDto>> GetUsersForPermissionsAsync();
        Task<List<FeatureNodeDto>> GetFeatureHierarchyAsync();
        Task<List<UserPermissionItemDto>> GetUserPermissionsAsync(long userId);
        Task ReplicatePermissionsAsync(long sourceUserId, long targetUserId);
        Task SaveUserPermissionsAsync(long userId, List<UserPermissionItemDto> permissions);
    }
}
