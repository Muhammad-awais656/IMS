using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMS.Services
{
    public class BranchService : IBranchService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<BranchService> _logger;

        public BranchService(IDbContextFactory dbContextFactory, ILogger<BranchService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<BranchViewModel> GetAllBranchesAsync(int pageNumber, int? pageSize, string? search)
        {
            var viewModel = new BranchViewModel();
            var size = pageSize ?? 10;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();

                    var countSql = "SELECT COUNT(*) FROM Branches";
                    var listSql = @"SELECT BranchId, BranchCode, BranchName, Address, Phone, IsActive, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy 
FROM Branches";
                    var orderBy = " ORDER BY BranchName OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                    SqlParameter? searchParam = null;
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        var term = search.Trim();
                        var where = " WHERE (BranchCode LIKE @Search OR BranchName LIKE @Search OR Address LIKE @Search)";
                        countSql += where;
                        listSql += where;
                        searchParam = new SqlParameter("@Search", SqlDbType.NVarChar, 250) { Value = "%" + term + "%" };
                    }

                    using (var countCmd = new SqlCommand(countSql, connection))
                    {
                        if (searchParam != null) countCmd.Parameters.Add(searchParam);
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
                            var list = new List<Branch>();
                            while (await reader.ReadAsync())
                            {
                                list.Add(ReadBranchFromReader(reader));
                            }
                            viewModel.Branches = list;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting branches");
            }

            return viewModel;
        }

        public async Task<Branch?> GetBranchByIdAsync(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var cmd = new SqlCommand("SELECT BranchId, BranchCode, BranchName, Address, Phone, IsActive, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy FROM Branches WHERE BranchId = @BranchId", connection))
                    {
                        cmd.Parameters.AddWithValue("@BranchId", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                                return ReadBranchFromReader(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting branch by id {BranchId}", id);
            }
            return null;
        }

        public async Task<bool> CreateBranchAsync(Branch branch)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    var sql = @"INSERT INTO Branches (BranchCode, BranchName, Address, Phone, IsActive, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy)
OUTPUT INSERTED.BranchId
VALUES (@BranchCode, @BranchName, @Address, @Phone, @IsActive, @CreatedDate, @CreatedBy, @ModifiedDate, @ModifiedBy)";
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        AddBranchParameters(cmd, branch);
                        var newId = await cmd.ExecuteScalarAsync();
                        return newId != null && Convert.ToInt32(newId) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating branch");
                return false;
            }
        }

        public async Task<int> UpdateBranchAsync(Branch branch)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    var sql = @"UPDATE Branches SET BranchCode = @BranchCode, BranchName = @BranchName, Address = @Address, Phone = @Phone, IsActive = @IsActive, ModifiedDate = @ModifiedDate, ModifiedBy = @ModifiedBy WHERE BranchId = @BranchId";
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@BranchId", branch.BranchId);
                        AddBranchParameters(cmd, branch);
                        return await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating branch {BranchId}", branch.BranchId);
                return 0;
            }
        }

        public async Task<int> DeleteBranchAsync(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var cmd = new SqlCommand("DELETE FROM Branches WHERE BranchId = @BranchId", connection))
                    {
                        cmd.Parameters.AddWithValue("@BranchId", id);
                        return await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting branch {BranchId}", id);
                return 0;
            }
        }

        public async Task<List<Branch>> GetAllActiveBranchesAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var cmd = new SqlCommand("SELECT BranchId, BranchCode, BranchName, Address, Phone, IsActive, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy FROM Branches WHERE IsActive = 1 ORDER BY BranchName", connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var list = new List<Branch>();
                        while (await reader.ReadAsync())
                            list.Add(ReadBranchFromReader(reader));
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active branches");
                return new List<Branch>();
            }
        }

        private static Branch ReadBranchFromReader(SqlDataReader reader)
        {
            return new Branch
            {
                BranchId = reader.GetInt32(reader.GetOrdinal("BranchId")),
                BranchCode = reader.IsDBNull(reader.GetOrdinal("BranchCode")) ? null : reader.GetString(reader.GetOrdinal("BranchCode")),
                BranchName = reader.IsDBNull(reader.GetOrdinal("BranchName")) ? null : reader.GetString(reader.GetOrdinal("BranchName")),
                Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString(reader.GetOrdinal("Address")),
                Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                CreatedDate = reader.IsDBNull(reader.GetOrdinal("CreatedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? null : reader.GetInt64(reader.GetOrdinal("CreatedBy")),
                ModifiedDate = reader.IsDBNull(reader.GetOrdinal("ModifiedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                ModifiedBy = reader.IsDBNull(reader.GetOrdinal("ModifiedBy")) ? null : reader.GetInt64(reader.GetOrdinal("ModifiedBy"))
            };
        }

        private static void AddBranchParameters(SqlCommand cmd, Branch branch)
        {
            cmd.Parameters.AddWithValue("@BranchCode", (object?)branch.BranchCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BranchName", (object?)branch.BranchName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Address", (object?)branch.Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Phone", (object?)branch.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", branch.IsActive);
            cmd.Parameters.AddWithValue("@CreatedDate", (object?)branch.CreatedDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedBy", (object?)branch.CreatedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ModifiedDate", (object?)branch.ModifiedDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ModifiedBy", (object?)branch.ModifiedBy ?? DBNull.Value);
        }
    }
}
