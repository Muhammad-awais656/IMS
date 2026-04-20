using IMS.DAL.PrimaryDBContext;
using IMS.Models;

namespace IMS.Common_Interfaces
{
    public interface IUserService
    {
        Task<PagedUsersViewModel> GetPagedUsersAsync(int pageNumber, int pageSize,string? UsernameSearch);
        Task<User?> GetUserByIdAsync(long id);
        Task<User?> GetUserByUserNameAsync(string userName);
        /// <summary>Returns branch IDs assigned to the user (from UserBranches). Empty if none.</summary>
        Task<List<int>> GetUserBranchIdsAsync(long userId);
        Task<bool> CreateUserAsync(User user, List<int> branchIds);
        Task<int> UpdateUserAsync(User user, List<int> branchIds);
        Task<int> DeleteUserAsync(long id);

        Task<User?> GetUserByCredentialsAsync(string username, string password, int branchId);
    }
}
