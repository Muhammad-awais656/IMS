using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using Microsoft.EntityFrameworkCore;

namespace IMS.Services
{
    /// <summary>
    /// Provides active branches for the login page using the default (Shop) connection, so no session is required.
    /// </summary>
    public class LoginBranchesService : ILoginBranchesService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<LoginBranchesService> _logger;

        public LoginBranchesService(IDbContextFactory dbContextFactory, ILogger<LoginBranchesService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<List<Branch>> GetBranchesForLoginAsync()
        {
            try
            {
                var connectionString = _dbContextFactory.GetDefaultConnectionString();
                using (var context = new AppDbContext(connectionString))
                {
                    return await context.Branches
                        .AsNoTracking()
                        .Where(b => b.IsActive)
                        .OrderBy(b => b.BranchName)
                        .ToListAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading branches for login");
                return new List<Branch>();
            }
        }
    }
}
