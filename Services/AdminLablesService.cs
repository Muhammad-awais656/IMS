using IMS.Common_Interfaces;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;

namespace IMS.Services
{
    public class AdminLablesService : IAdminLablesService
    {
        public Task<bool> CreateAdminLablesAsync(AdminLabel expenseType)
        {
            throw new NotImplementedException();
        }

        public Task<int> DeleteAdminLablesAsync(long id)
        {
            throw new NotImplementedException();
        }

        public Task<AdminLabel> GetAdminLablesByIdAsync(long id)
        {
            throw new NotImplementedException();
        }

        public Task<AdminLablesViewModel> GetAllAdminLablesAsync(int pageNumber, int? pageSize, string? name)
        {
            throw new NotImplementedException();
        }

        public Task<int> UpdateAdminLablesAsync(AdminLabel settings)
        {
            throw new NotImplementedException();
        }
    }
}
