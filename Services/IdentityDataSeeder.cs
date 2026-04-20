using IMS.Authorization;
using IMS.DAL.PrimaryDBContext;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IMS.Services
{
    /// <summary>
    /// Creates the first Identity user when AspNetUsers is empty (first deploy / empty DB).
    /// Configure in appsettings: IdentitySeed section.
    /// </summary>
    public static class IdentityDataSeeder
    {
        public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
        {
            var enabled = configuration.GetValue("IdentitySeed:Enabled", false);
            if (!enabled)
                return;

            using var scope = services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (await userManager.Users.AnyAsync())
                return;

            var userName = configuration["IdentitySeed:AdminUserName"]?.Trim();
            var password = configuration["IdentitySeed:AdminPassword"];
            var branchId = configuration.GetValue("IdentitySeed:BranchId", 0);

            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("IdentitySeed is enabled but AdminUserName or AdminPassword is missing. Skipping seed.");
                return;
            }

            if (branchId <= 0)
            {
                logger.LogWarning("IdentitySeed:BranchId must be a valid branch (e.g. 1). Skipping seed.");
                return;
            }

            var branchExists = await db.Branches.AnyAsync(b => b.BranchId == branchId);
            if (!branchExists)
            {
                logger.LogWarning("IdentitySeed: BranchId {BranchId} not found in Branches. Skipping seed.", branchId);
                return;
            }

            var email = configuration["IdentitySeed:AdminEmail"];
            if (string.IsNullOrWhiteSpace(email))
                email = $"{userName}@local.ims";

            var user = new ApplicationUser
            {
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                EmailConfirmed = true,
                IsActive = true,
                IsAdmin = true,
                CreatedDate = DateTime.UtcNow,
                LockoutEnabled = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                logger.LogError("Identity seed failed: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }

            db.IdentityUserBranches.Add(new IdentityUserBranch { UserId = user.Id, BranchId = branchId });
            await db.SaveChangesAsync();
            logger.LogInformation("Identity seed: first admin user '{UserName}' created and assigned to branch {BranchId}. Disable IdentitySeed in appsettings after first login.", userName, branchId);
        }
    }
}
