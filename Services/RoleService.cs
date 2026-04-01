using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMS.Services
{
    public class RoleService : IRoleService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<RoleService> _logger;

        public RoleService(IDbContextFactory dbContextFactory, ILogger<RoleService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<RoleViewModel> GetAllRolesAsync(int pageNumber, int? pageSize, string? search)
        {
            var viewModel = new RoleViewModel();
            var size = pageSize ?? 10;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    var countSql = "SELECT COUNT(*) FROM AdminRoles";
                    var listSql = @"SELECT RoleId, RoleName, RoleDescription, IsActive, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy 
FROM AdminRoles";
                    var orderBy = " ORDER BY RoleName OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                    SqlParameter? searchParam = null;
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        var term = search.Trim();
                        var where = " WHERE (RoleName LIKE @Search OR RoleDescription LIKE @Search)";
                        countSql += where;
                        listSql += where;
                        searchParam = new SqlParameter("@Search", SqlDbType.NVarChar, 250) { Value = "%" + term + "%" };
                    }

                    using (var countCmd = new SqlCommand(countSql, connection))
                    {
                        if (searchParam != null) countCmd.Parameters.Add(new SqlParameter("@Search", SqlDbType.NVarChar, 250) { Value = "%" + (search ?? "").Trim() + "%" });
                        viewModel.TotalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                    }

                    viewModel.TotalPages = (int)Math.Ceiling(viewModel.TotalCount / (double)size);
                    viewModel.CurrentPage = pageNumber;
                    viewModel.PageSize = size;

                    listSql += orderBy;
                    using (var listCmd = new SqlCommand(listSql, connection))
                    {
                        if (searchParam != null) listCmd.Parameters.Add(new SqlParameter("@Search", SqlDbType.NVarChar, 250) { Value = "%" + (search ?? "").Trim() + "%" });
                        listCmd.Parameters.AddWithValue("@Offset", (pageNumber - 1) * size);
                        listCmd.Parameters.AddWithValue("@PageSize", size);

                        using (var reader = await listCmd.ExecuteReaderAsync())
                        {
                            var list = new List<AdminRole>();
                            while (await reader.ReadAsync())
                                list.Add(ReadRoleFromReader(reader));
                            viewModel.Roles = list;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles");
            }

            return viewModel;
        }

        public async Task<AdminRole?> GetRoleByIdAsync(long id)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT RoleId, RoleName, RoleDescription, IsActive, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy FROM AdminRoles WHERE RoleId = @RoleId";
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@RoleId", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                                return ReadRoleFromReader(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role by id {RoleId}", id);
            }
            return null;
        }

        public async Task<bool> CreateRoleAsync(AdminRole role)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    var sql = @"INSERT INTO AdminRoles (RoleName, RoleDescription, IsActive, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy)
OUTPUT INSERTED.RoleId
VALUES (@RoleName, @RoleDescription, @IsActive, @CreatedDate, @CreatedBy, @ModifiedDate, @ModifiedBy)";
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        AddRoleParameters(cmd, role);
                        var newId = await cmd.ExecuteScalarAsync();
                        return newId != null && Convert.ToInt64(newId) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role");
                return false;
            }
        }

        public async Task<int> UpdateRoleAsync(AdminRole role)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    var sql = @"UPDATE AdminRoles SET RoleName = @RoleName, RoleDescription = @RoleDescription, IsActive = @IsActive, ModifiedDate = @ModifiedDate, ModifiedBy = @ModifiedBy WHERE RoleId = @RoleId";
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@RoleId", role.RoleId);
                        AddRoleParameters(cmd, role);
                        return await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role {RoleId}", role.RoleId);
                return 0;
            }
        }

        public async Task<int> DeleteRoleAsync(long id)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var cmd = new SqlCommand("DELETE FROM AdminRoles WHERE RoleId = @RoleId", connection))
                    {
                        cmd.Parameters.AddWithValue("@RoleId", id);
                        return await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role {RoleId}", id);
                return 0;
            }
        }

        public async Task<List<AdminRole>> GetActiveRolesForDropdownAsync()
        {
            var list = new List<AdminRole>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT RoleId, RoleName, RoleDescription, IsActive, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy FROM AdminRoles WHERE IsActive = 1 ORDER BY RoleName";
                    using (var cmd = new SqlCommand(sql, connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            list.Add(ReadRoleFromReader(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active roles for dropdown");
            }
            return list;
        }

        private static AdminRole ReadRoleFromReader(SqlDataReader reader)
        {
            return new AdminRole
            {
                RoleId = reader.GetInt64(reader.GetOrdinal("RoleId")),
                RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
                RoleDescription = reader.IsDBNull(reader.GetOrdinal("RoleDescription")) ? null : reader.GetString(reader.GetOrdinal("RoleDescription")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                CreatedBy = reader.GetInt64(reader.GetOrdinal("CreatedBy")),
                ModifiedDate = reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                ModifiedBy = reader.GetInt64(reader.GetOrdinal("ModifiedBy"))
            };
        }

        private static void AddRoleParameters(SqlCommand cmd, AdminRole role)
        {
            cmd.Parameters.AddWithValue("@RoleName", role.RoleName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@RoleDescription", (object?)role.RoleDescription ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", role.IsActive);
            cmd.Parameters.AddWithValue("@CreatedDate", (object?)role.CreatedDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedBy", (object?)role.CreatedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ModifiedDate", (object?)role.ModifiedDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ModifiedBy", (object?)role.ModifiedBy ?? DBNull.Value);
        }
    }
}
