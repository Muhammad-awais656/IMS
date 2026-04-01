using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMS.Services
{
    public class UserPermissionsService : IUserPermissionsService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<UserPermissionsService> _logger;

        public UserPermissionsService(IDbContextFactory dbContextFactory, ILogger<UserPermissionsService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<List<UserForPermissionsDto>> GetUsersForPermissionsAsync()
        {
            var list = new List<UserForPermissionsDto>();
            try
            {
                using var connection = new SqlConnection(_dbContextFactory.DBConnectionString());
                await connection.OpenAsync();
                const string sql = "SELECT UserId, UserName FROM Users WHERE IsEnabled = 1 ORDER BY UserName";
                using var cmd = new SqlCommand(sql, connection);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    list.Add(new UserForPermissionsDto
                    {
                        UserId = reader.GetInt64(reader.GetOrdinal("UserId")),
                        UserName = reader.GetString(reader.GetOrdinal("UserName"))
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users for permissions");
            }
            return list;
        }

        public async Task<List<FeatureNodeDto>> GetFeatureHierarchyAsync()
        {
            var flat = new List<FeatureNodeDto>();
            try
            {
                using var connection = new SqlConnection(_dbContextFactory.DBConnectionString());
                await connection.OpenAsync();
                const string sql = @"SELECT FeatureId, FeatureLabel, FeatureName, ParentFeatureId_FK, FeatureOrder 
FROM AdminFeatures WHERE IsDeleted = 0 ORDER BY ParentFeatureId_FK, FeatureOrder, FeatureId";
                using var cmd = new SqlCommand(sql, connection);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    flat.Add(new FeatureNodeDto
                    {
                        FeatureId = reader.GetInt64(reader.GetOrdinal("FeatureId")),
                        FeatureLabel = reader.GetString(reader.GetOrdinal("FeatureLabel")),
                        FeatureName = reader.GetString(reader.GetOrdinal("FeatureName")),
                        ParentFeatureIdFk = reader.GetInt64(reader.GetOrdinal("ParentFeatureId_FK")),
                        FeatureOrder = reader.GetInt64(reader.GetOrdinal("FeatureOrder"))
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feature hierarchy");
                return new List<FeatureNodeDto>();
            }

            return BuildTree(flat, 0);
        }

        private static List<FeatureNodeDto> BuildTree(List<FeatureNodeDto> flat, long parentId, int level = 0)
        {
            var nodes = flat.Where(f => f.ParentFeatureIdFk == parentId).OrderBy(f => f.FeatureOrder).ThenBy(f => f.FeatureId).ToList();
            var result = new List<FeatureNodeDto>();
            foreach (var node in nodes)
            {
                node.Level = level;
                node.Children = BuildTree(flat, node.FeatureId, level + 1);
                result.Add(node);
            }
            return result;
        }

        public async Task<List<UserPermissionItemDto>> GetUserPermissionsAsync(long userId)
        {
            var list = new List<UserPermissionItemDto>();
            try
            {
                using var connection = new SqlConnection(_dbContextFactory.DBConnectionString());
                await connection.OpenAsync();
                const string sql = "SELECT FeatureId_FK, CanView, CanModify FROM RoleFeatureAccessRights WHERE UserId_FK = @UserId";
                using var cmd = new SqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    list.Add(new UserPermissionItemDto
                    {
                        FeatureId = reader.GetInt64(reader.GetOrdinal("FeatureId_FK")),
                        CanView = reader.GetBoolean(reader.GetOrdinal("CanView")),
                        CanModify = reader.GetBoolean(reader.GetOrdinal("CanModify"))
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for user {UserId}", userId);
            }
            return list;
        }

        public async Task ReplicatePermissionsAsync(long sourceUserId, long targetUserId)
        {
            if (sourceUserId == targetUserId) return;
            var sourcePerms = await GetUserPermissionsAsync(sourceUserId);
            await SaveUserPermissionsAsync(targetUserId, sourcePerms);
        }

        public async Task SaveUserPermissionsAsync(long userId, List<UserPermissionItemDto> permissions)
        {
            try
            {
                using var connection = new SqlConnection(_dbContextFactory.DBConnectionString());
                await connection.OpenAsync();
                using (var del = new SqlCommand("DELETE FROM RoleFeatureAccessRights WHERE UserId_FK = @UserId", connection))
                {
                    del.Parameters.AddWithValue("@UserId", userId);
                    await del.ExecuteNonQueryAsync();
                }
                if (permissions == null) return;
                foreach (var p in permissions.Where(x => x.CanView || x.CanModify))
                {
                    using var ins = new SqlCommand(
                        "INSERT INTO RoleFeatureAccessRights (UserId_FK, FeatureId_FK, CanView, CanModify) VALUES (@UserId, @FeatureId, @CanView, @CanModify)",
                        connection);
                    ins.Parameters.AddWithValue("@UserId", userId);
                    ins.Parameters.AddWithValue("@FeatureId", p.FeatureId);
                    ins.Parameters.AddWithValue("@CanView", p.CanView);
                    ins.Parameters.AddWithValue("@CanModify", p.CanModify);
                    await ins.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving permissions for user {UserId}", userId);
                throw;
            }
        }
    }
}
