using IMS.Authorization;
using IMS.Common_Interfaces;
using IMS.DAL.PrimaryDBContext;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IMS.Services
{
    /// <summary>
    /// Keeps Identity users synchronized with the legacy Users table.
    /// </summary>
    public class IdentityUserSyncService : IIdentityUserSyncService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<IdentityUserSyncService> _logger;

        public IdentityUserSyncService(
            UserManager<ApplicationUser> userManager,
            AppDbContext dbContext,
            ILogger<IdentityUserSyncService> logger)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task SyncFromLegacyUserAsync(User legacyUser, List<int> branchIds)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.LegacyUserId == legacyUser.UserId)
                       ?? await _userManager.FindByNameAsync(legacyUser.UserName);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = legacyUser.UserName,
                    Email = $"{legacyUser.UserName}@local.ims",
                    EmailConfirmed = true,
                    IsActive = legacyUser.IsEnabled,
                    IsAdmin = legacyUser.IsAdmin,
                    LegacyUserId = legacyUser.UserId,
                    CreatedDate = DateTime.UtcNow,
                };
                var create = await _userManager.CreateAsync(user, legacyUser.UserPassword);
                if (!create.Succeeded)
                {
                    var err = string.Join("; ", create.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Identity user create failed: {err}");
                }
            }
            else
            {
                user.IsActive = legacyUser.IsEnabled;
                user.IsAdmin = legacyUser.IsAdmin;
                user.LegacyUserId = legacyUser.UserId;
                user.ModifiedDate = DateTime.UtcNow;
                user.UserName = legacyUser.UserName;
                user.NormalizedUserName = _userManager.NormalizeName(legacyUser.UserName);
                user.Email ??= $"{legacyUser.UserName}@local.ims";
                user.NormalizedEmail = _userManager.NormalizeEmail(user.Email);
                var update = await _userManager.UpdateAsync(user);
                if (!update.Succeeded)
                {
                    var err = string.Join("; ", update.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Identity user update failed: {err}");
                }

                if (!string.IsNullOrWhiteSpace(legacyUser.UserPassword))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var reset = await _userManager.ResetPasswordAsync(user, token, legacyUser.UserPassword);
                    if (!reset.Succeeded)
                    {
                        _logger.LogWarning("Password sync failed for user {UserName}", legacyUser.UserName);
                    }
                }
            }

            user.LockoutEnabled = true;
            user.LockoutEnd = legacyUser.IsEnabled ? null : DateTimeOffset.UtcNow.AddYears(100);
            await _userManager.UpdateAsync(user);

            var existing = _dbContext.IdentityUserBranches.Where(x => x.UserId == user.Id);
            _dbContext.IdentityUserBranches.RemoveRange(existing);
            foreach (var branchId in (branchIds ?? new List<int>()).Distinct())
            {
                _dbContext.IdentityUserBranches.Add(new IdentityUserBranch
                {
                    UserId = user.Id,
                    BranchId = branchId
                });
            }
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteByLegacyUserIdAsync(long legacyUserId)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.LegacyUserId == legacyUserId);
            if (user == null) return;
            var existing = _dbContext.IdentityUserBranches.Where(x => x.UserId == user.Id);
            _dbContext.IdentityUserBranches.RemoveRange(existing);
            await _dbContext.SaveChangesAsync();
            await _userManager.DeleteAsync(user);
        }
    }
}
