using IMS.DAL.PrimaryDBContext;

namespace IMS.Common_Interfaces
{
    public interface IIdentityUserSyncService
    {
        Task SyncFromLegacyUserAsync(User legacyUser, List<int> branchIds);
        Task DeleteByLegacyUserIdAsync(long legacyUserId);
    }
}
