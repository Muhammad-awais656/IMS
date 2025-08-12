using IMS.DAL.PrimaryDBContext;
using IMS.Models;

namespace IMS.Common_Interfaces
{
    public interface IUserService
    {
        Task<PagedUsersViewModel> GetPagedUsersAsync(int pageNumber, int pageSize,string? UsernameSearch);
        Task<User> GetUserByIdAsync(long id);
        Task<bool> CreateUserAsync(User user);
        Task<int> UpdateUserAsync(User user);
        Task<int> DeleteUserAsync(long id);

        Task<User> GetUserByCredentialsAsync(string username, string password);
    }
}
