using IMS.DAL;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMS.Services
{
    /// <summary>
    /// Checks whether a user has a given privilege (feature) for custom authorization.
    /// Uses AdminFeatures.FeatureName and RoleFeatureAccessRights (UserId_FK, FeatureId_FK, CanView, CanModify).
    /// Admin users (IsAdmin) are treated as having all privileges.
    /// </summary>
    public interface IPrivilegeAuthorizationService
    {
        /// <summary>
        /// Returns true if the user has the privilege (by name). Admin users always have access.
        /// </summary>
        Task<bool> UserHasPrivilegeAsync(string userName, string privilegeName);

        /// <summary>
        /// Returns true if the user has the privilege (by id). Admin users always have access.
        /// </summary>
        Task<bool> UserHasPrivilegeAsync(long userId, string privilegeName);

        /// <summary>
        /// Returns (IsAdmin, set of FeatureNames the user has). Use for menu visibility: if IsAdmin, show all; else show only items whose privilege is in the set.
        /// </summary>
        Task<(bool IsAdmin, HashSet<string> PrivilegeNames)> GetUserPrivilegeNamesAsync(string? userName);
    }

    public class PrivilegeAuthorizationService : IPrivilegeAuthorizationService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<PrivilegeAuthorizationService> _logger;

        public PrivilegeAuthorizationService(IDbContextFactory dbContextFactory, ILogger<PrivilegeAuthorizationService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<bool> UserHasPrivilegeAsync(string userName, string privilegeName)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(privilegeName))
                return false;
            try
            {
                using var connection = new SqlConnection(_dbContextFactory.DBConnectionString());
                await connection.OpenAsync();
                var (userId, isAdmin) = await GetUserInfoByUserNameAsync(connection, userName);
                if (userId != null)
                {
                    if (isAdmin) return true;
                    return await UserHasPrivilegeForUserAsync(connection, userId.Value, privilegeName);
                }

                var identityRow = await GetIdentityUserByUserNameAsync(connection, userName);
                if (identityRow == null) return false;
                var (legacyId, isAdminIdentity) = identityRow.Value;
                if (isAdminIdentity) return true;
                if (legacyId == null) return false;
                return await UserHasPrivilegeForUserAsync(connection, legacyId.Value, privilegeName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Privilege check failed for user {UserName}, privilege {Privilege}", userName, privilegeName);
                return false;
            }
        }

        public async Task<bool> UserHasPrivilegeAsync(long userId, string privilegeName)
        {
            if (string.IsNullOrWhiteSpace(privilegeName)) return false;
            try
            {
                using var connection = new SqlConnection(_dbContextFactory.DBConnectionString());
                await connection.OpenAsync();
                const string adminSql = "SELECT IsAdmin FROM Users WHERE UserId = @UserId";
                using (var adminCmd = new SqlCommand(adminSql, connection))
                {
                    adminCmd.Parameters.AddWithValue("@UserId", userId);
                    var isAdmin = await adminCmd.ExecuteScalarAsync();
                    if (isAdmin != null && isAdmin != DBNull.Value && Convert.ToBoolean(isAdmin))
                        return true;
                }
                return await UserHasPrivilegeForUserAsync(connection, userId, privilegeName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Privilege check failed for userId {UserId}, privilege {Privilege}", userId, privilegeName);
                return false;
            }
        }

        private static async Task<(long? UserId, bool IsAdmin)> GetUserInfoByUserNameAsync(SqlConnection connection, string userName)
        {
            const string sql = "SELECT UserId, IsAdmin FROM Users WHERE UserName = @UserName AND IsEnabled = 1";
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@UserName", userName);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return (reader.GetInt64(reader.GetOrdinal("UserId")), reader.GetBoolean(reader.GetOrdinal("IsAdmin")));
            return (null, false);
        }

        /// <summary>
        /// Identity-only users (e.g. seeded in AspNetUsers) are not in legacy <c>Users</c>; resolve admin/privileges from AspNetUsers + LegacyUserId.
        /// </summary>
        private static async Task<(long? LegacyUserId, bool IsAdmin)?> GetIdentityUserByUserNameAsync(SqlConnection connection, string userName)
        {
            const string sql = "SELECT LegacyUserId, IsAdmin FROM AspNetUsers WHERE NormalizedUserName = @Norm";
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Norm", userName.ToUpperInvariant());
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;
            var legacyOrdinal = reader.GetOrdinal("LegacyUserId");
            var legacyId = reader.IsDBNull(legacyOrdinal) ? (long?)null : reader.GetInt64(legacyOrdinal);
            var isAdmin = reader.GetBoolean(reader.GetOrdinal("IsAdmin"));
            return (legacyId, isAdmin);
        }

        private static async Task AppendPrivilegesForUserAsync(SqlConnection connection, long userId, HashSet<string> privileges)
        {
            const string sql = @"
SELECT f.FeatureName FROM RoleFeatureAccessRights r
INNER JOIN AdminFeatures f ON f.FeatureId = r.FeatureId_FK
WHERE r.UserId_FK = @UserId AND (r.CanView = 1 OR r.CanModify = 1)";
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@UserId", userId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(reader.GetOrdinal("FeatureName"));
                if (!string.IsNullOrEmpty(name)) privileges.Add(name);
            }
        }

        public async Task<(bool IsAdmin, HashSet<string> PrivilegeNames)> GetUserPrivilegeNamesAsync(string? userName)
        {
            var privileges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(userName))
                return (false, privileges);
            try
            {
                using var connection = new SqlConnection(_dbContextFactory.DBConnectionString());
                await connection.OpenAsync();
                var (userId, isAdmin) = await GetUserInfoByUserNameAsync(connection, userName);
                if (userId != null)
                {
                    if (isAdmin) return (true, privileges);
                    await AppendPrivilegesForUserAsync(connection, userId.Value, privileges);
                    return (false, privileges);
                }

                var identityRow = await GetIdentityUserByUserNameAsync(connection, userName);
                if (identityRow == null)
                    return (false, privileges);

                var (legacyId, isAdminIdentity) = identityRow.Value;
                if (isAdminIdentity)
                    return (true, privileges);
                if (legacyId != null)
                    await AppendPrivilegesForUserAsync(connection, legacyId.Value, privileges);
                return (false, privileges);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetUserPrivilegeNames failed for user {UserName}", userName);
            }
            return (false, privileges);
        }

        private static async Task<bool> UserHasPrivilegeForUserAsync(SqlConnection connection, long userId, string privilegeName)
        {
            const string sql = @"
SELECT 1 FROM RoleFeatureAccessRights r
INNER JOIN AdminFeatures f ON f.FeatureId = r.FeatureId_FK
WHERE r.UserId_FK = @UserId AND f.FeatureName = @FeatureName AND (r.CanView = 1 OR r.CanModify = 1)";
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@FeatureName", privilegeName);
            var result = await cmd.ExecuteScalarAsync();
            return result != null && result != DBNull.Value;
        }
    }
}
