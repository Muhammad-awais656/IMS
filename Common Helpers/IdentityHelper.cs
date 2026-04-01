
using IMS.Authorization;
using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMS.Common_Helpers
{
    public  class IdentityHelper
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILoginBranchesService _loginBranchesService;
        private readonly IDbContextFactory _dbContextFactory;

        public IdentityHelper(
            UserManager<ApplicationUser> userManager,
            ILoginBranchesService loginBranchesService,
            IDbContextFactory dbContextFactory)
        {
            _userManager = userManager;
            _loginBranchesService = loginBranchesService;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<List<Branch>> GetAllActiveBranches()
        {
            return await _loginBranchesService.GetBranchesForLoginAsync();
        }

        public async Task<string> GetUserId(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            return user?.Id;
        }

      

public async Task<bool> IsAuthenticatedUser(string userName, string password, int branchId)
    {
        try
        {
            await using var connection = new SqlConnection(_dbContextFactory.DBConnectionString());
            await connection.OpenAsync();

            await using var command = new SqlCommand("sp_AuthenticateUser", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };

            //Strongly typed parameters (BEST PRACTICE)
            command.Parameters.Add("@UserName", SqlDbType.NVarChar, 100).Value = userName;
            command.Parameters.Add("@Password", SqlDbType.NVarChar, 100).Value = password;
            command.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId;

            //Output parameter
            var isValidParam = new SqlParameter("@IsValid", SqlDbType.Bit)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(isValidParam);

            await command.ExecuteNonQueryAsync();

            return isValidParam.Value != DBNull.Value && (bool)isValidParam.Value;
        }
        catch (Exception ex)
        {
            // Log error properly (important)
            // _logger.LogError(ex, "Error in IsAuthenticatedUser");

            throw; // or return false depending on your design
        }
    }


}
}
