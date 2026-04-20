using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using StringEncrptandDecryptorApp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

public class UserService : IUserService
{
    private readonly IDbContextFactory _dbContextFactory;
    private readonly ILogger<UserService> _logger;

    public UserService(IDbContextFactory dbContextFactory, ILogger<UserService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<PagedUsersViewModel> GetPagedUsersAsync(int pageNumber, int pageSize,string? usernameSearch)
    {
        var users = new List<User>();
        var totalCount = 0;


        try
        {
            using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
            {
                await connection.OpenAsync();

                //Get total count
                using (var command = new SqlCommand("SELECT COUNT(*) FROM Users", connection))
                {
                    try
                    {
                        totalCount = (int)await command.ExecuteScalarAsync();
                    }
                    catch (SqlException ex)
                    {
                        Console.WriteLine($"SQL Error in GetPagedUsersAsync (count): {ex.Message}");
                        return new PagedUsersViewModel
                        {
                            Users = users,
                            CurrentPage = pageNumber,
                            TotalPages = 0,
                            PageSize = pageSize,
                            TotalCount = 0
                        };
                    }
                }
                EncryptionHelper encryptionHelper = new EncryptionHelper();
                // Get paged users
                using (var command = new SqlCommand("GetUsers", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@PageNumber", pageNumber);
                    command.Parameters.AddWithValue("@PageSize", pageSize);
                    command.Parameters.AddWithValue("@SearchName", usernameSearch);
                    //command.Parameters.AddWithValue("@SearchName", (object)usernameSearch ?? DBNull.Value);
                    var countRowParam = new SqlParameter("@RowCount", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(countRowParam);
                    try
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                  var user = new User
                                {
                                    UserId = reader.GetInt64(reader.GetOrdinal("UserId")),
                                    UserName = reader.GetString(reader.GetOrdinal("UserName")),
                                    //UserPassword = encryptionHelper.Decrypt(reader.GetString(reader.GetOrdinal("UserPassword"))),
                                    UserPassword = reader.GetString(reader.GetOrdinal("UserPassword")),
                                    IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled")),
                                    IsAdmin = reader.GetBoolean(reader.GetOrdinal("IsAdmin")),
                                };
                                if (HasColumn(reader, "BranchId"))
                                    user.BranchId = reader.GetInt32(reader.GetOrdinal("BranchId"));
                                users.Add(user);

                                
                       
                            }
                            if (!string.IsNullOrEmpty(usernameSearch))
                            {
                                totalCount = users.Count;
                            }
                        }
                    }
                    catch
                    {
                        

                    }
                }
            }
            return new PagedUsersViewModel
            {
                Users = users,
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, "Unexpected error in GetPagedUsersAsync");
            throw new Exception(ex.Message);
        }
    }

    public async Task<User> GetUserByIdAsync(long id)
    {
        EncryptionHelper encryptionHelper = new EncryptionHelper();
        User user = new User();
        try
        {
            using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("GetUserById", connection))
                {
                    command.Parameters.AddWithValue("@pUserId", id);
                    command.CommandType = CommandType.StoredProcedure;
                    try
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var u = new User
                                {
                                    UserId = reader.GetInt64(reader.GetOrdinal("UserId")),
                                    UserName = reader.GetString(reader.GetOrdinal("UserName")),
                                    UserPassword = encryptionHelper.Decrypt(reader.GetString(reader.GetOrdinal("UserPassword"))),
                                    IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled")),
                                    IsAdmin = reader.GetBoolean(reader.GetOrdinal("IsAdmin")),
                                };
                                if (HasColumn(reader, "BranchId"))
                                    u.BranchId = reader.GetInt32(reader.GetOrdinal("BranchId"));
                                u.RoleId = await GetUserRoleIdAsync(connection, u.UserId);
                                return u;
                            }
                        }
                    }
                    catch 
                    {
                       
                    }
                    
                }
            }
        }
        catch 
        {
            
        }
        return null;
    }

    public async Task<User?> GetUserByUserNameAsync(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName)) return null;
        try
        {
            using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
            {
                await connection.OpenAsync();
                using (var cmd = new SqlCommand("SELECT TOP 1 UserId FROM Users WHERE UserName = @UserName", connection))
                {
                    cmd.Parameters.AddWithValue("@UserName", userName);
                    var idObj = await cmd.ExecuteScalarAsync();
                    if (idObj != null && idObj != DBNull.Value)
                        return await GetUserByIdAsync(Convert.ToInt64(idObj));
                }
            }
        }
        catch
        {
            // ignored
        }
        return null;
    }

    private static async Task<long?> GetUserRoleIdAsync(SqlConnection connection, long userId)
    {
        try
        {
            using (var cmd = new SqlCommand("SELECT RoleId FROM UserRoles WHERE UserId = @UserId", connection))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                var obj = await cmd.ExecuteScalarAsync();
                if (obj != null && obj != DBNull.Value)
                    return Convert.ToInt64(obj);
            }
        }
        catch
        {
            // UserRoles table may not exist yet
        }
        return null;
    }

    public async Task<bool> CreateUserAsync(User user, List<int> branchIds)
    {
        bool response = false;
        try
        {
            var ids = branchIds ?? new List<int>();
            user.BranchId = ids.Count > 0 ? ids[0] : 0; // first branch for SP/backward compat
            //EncryptionHelper encryptionHelper = new EncryptionHelper();
            //var encrytedPassword = encryptionHelper.Encrypt(user.UserPassword);
            using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
            {
                await connection.OpenAsync();
                // AddUser stored procedure must accept: @pName, @pPassword, @pIsEnabled, @pIsAdmin, @pBranchId (INT), output @pUserId
                using (var command = new SqlCommand("AddUser", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@pName", user.UserName);
                    command.Parameters.AddWithValue("@pPassword", user.UserPassword);
                    command.Parameters.AddWithValue("@pIsEnabled", user.IsEnabled);
                    command.Parameters.AddWithValue("@pIsAdmin", user.IsAdmin);
                    command.Parameters.AddWithValue("@pBranchId", user.BranchId);
                    var userIdParam = new SqlParameter("@pUserId", SqlDbType.BigInt)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(userIdParam);
                    await command.ExecuteNonQueryAsync();
                    long newUserId = (long)userIdParam.Value;
                    if (newUserId != 0)
                    {
                        user.UserId = newUserId;
                        response = true;
                        await SaveUserBranchesAsync(connection, newUserId, ids);
                        if (user.RoleId.HasValue && user.RoleId.Value > 0)
                            await SaveUserRoleAsync(connection, newUserId, user.RoleId.Value);
                    }
                }
            }
        }
        catch
        {
            // ignored
        }
        return response;
    }

    public async Task<int> UpdateUserAsync(User user, List<int> branchIds)
    {
        var RowsAffectedResponse = 0;
        var ids = branchIds ?? new List<int>();
        user.BranchId = ids.Count > 0 ? ids[0] : user.BranchId;
        //EncryptionHelper encryptionHelper = new EncryptionHelper();
        //var encyptPassword = encryptionHelper.Encrypt(user.UserPassword);

        try
        {
            using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
            {
                await connection.OpenAsync();
                // UpdateUser stored procedure must accept: @pId, @pUserName, @pPassword, @pIsEnabled, @IsAdmin, @pBranchId (INT), output @RowsAffected
                using (var command = new SqlCommand("UpdateUser", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@pId", user.UserId);
                    command.Parameters.AddWithValue("@pUserName", user.UserName);
                    command.Parameters.AddWithValue("@pPassword", user.UserPassword);
                    command.Parameters.AddWithValue("@pIsEnabled", user.IsEnabled);
                    command.Parameters.AddWithValue("@IsAdmin", user.IsAdmin);
                    command.Parameters.AddWithValue("@pBranchId", user.BranchId);
                    var AfftectedRowCount = new SqlParameter("@RowsAffected", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(AfftectedRowCount);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected != 0)
                    {
                        RowsAffectedResponse = rowsAffected;
                        await SaveUserBranchesAsync(connection, user.UserId, ids);
                        await SaveUserRoleAsync(connection, user.UserId, user.RoleId);
                    }
                }
            }
        }
        catch
        {
            // ignored
        }
        return RowsAffectedResponse;
    }

    public async Task<List<int>> GetUserBranchIdsAsync(long userId)
    {
        var list = new List<int>();
        try
        {
            using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
            {
                await connection.OpenAsync();
                using (var cmd = new SqlCommand("SELECT BranchId FROM UserBranches WHERE UserId = @UserId ORDER BY BranchId", connection))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            list.Add(reader.GetInt32(0));
                    }
                }
            }
        }
        catch
        {
            // UserBranches table may not exist yet; return empty
        }
        return list;
    }

    private static async Task SaveUserBranchesAsync(SqlConnection connection, long userId, List<int> branchIds)
    {
        using (var del = new SqlCommand("DELETE FROM UserBranches WHERE UserId = @UserId", connection))
        {
            del.Parameters.AddWithValue("@UserId", userId);
            await del.ExecuteNonQueryAsync();
        }
        if (branchIds == null || branchIds.Count == 0) return;
        foreach (var branchId in branchIds.Distinct())
        {
            using (var ins = new SqlCommand("INSERT INTO UserBranches (UserId, BranchId) VALUES (@UserId, @BranchId)", connection))
            {
                ins.Parameters.AddWithValue("@UserId", userId);
                ins.Parameters.AddWithValue("@BranchId", branchId);
                await ins.ExecuteNonQueryAsync();
            }
        }
    }

    private static async Task SaveUserRoleAsync(SqlConnection connection, long userId, long? roleId)
    {
        using (var del = new SqlCommand("DELETE FROM UserRoles WHERE UserId = @UserId", connection))
        {
            del.Parameters.AddWithValue("@UserId", userId);
            await del.ExecuteNonQueryAsync();
        }
        if (roleId.HasValue && roleId.Value > 0)
        {
            using (var ins = new SqlCommand("INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)", connection))
            {
                ins.Parameters.AddWithValue("@UserId", userId);
                ins.Parameters.AddWithValue("@RoleId", roleId.Value);
                await ins.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task<int> DeleteUserAsync(long id)
    {
        int response = 0;
        try
        {
            using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("DeleteUserById", connection))
                {
                    command.Parameters.AddWithValue("@pUserId", id);
                    command.CommandType = CommandType.StoredProcedure;
                    var RowAffected = new SqlParameter("@RowsAffected", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(RowAffected);
                    
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected != 0)
                        {
                          response = rowsAffected;
                        }
                    
                }
            }
        }
        catch 
        {
            
        }
        return response;
    }

    public async Task<User?> GetUserByCredentialsAsync(string username, string password, int branchId)
    {
        try
        {
            EncryptionHelper encryptionHelper = new EncryptionHelper();
            var encryptedPassword = encryptionHelper.Encrypt(password);
            using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("GetUser", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@pUserName", username);
                    command.Parameters.AddWithValue("@pUserPassword", encryptedPassword);

                    User? user = null;
                    try
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                user = new User
                                {
                                    UserId = reader.GetInt64(reader.GetOrdinal("UserId")),
                                    UserName = reader.GetString(reader.GetOrdinal("UserName")),
                                    UserPassword = reader.GetString(reader.GetOrdinal("UserPassword")),
                                    IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled")),
                                    IsAdmin = reader.GetBoolean(reader.GetOrdinal("IsAdmin"))
                                };
                                if (HasColumn(reader, "BranchId"))
                                    user.BranchId = reader.GetInt32(reader.GetOrdinal("BranchId"));
                            }
                        }
                        if (user != null && user.BranchId == 0)
                            user.BranchId = await GetUserBranchIdAsync(connection, user.UserId);
                        if (user == null) return null;
                        var allowedBranchIds = await GetUserBranchIdsFromConnectionAsync(connection, user.UserId);
                        if (allowedBranchIds.Count == 0)
                            allowedBranchIds.Add(user.BranchId);
                        if (!allowedBranchIds.Contains(branchId))
                            return null;
                        user.BranchId = branchId;
                        return user;
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }
        catch
        {
            // ignored
        }
        return null;
    }

    private static bool HasColumn(SqlDataReader reader, string columnName)
    {
        for (int i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static async Task<int> GetUserBranchIdAsync(SqlConnection connection, long userId)
    {
        using (var cmd = new SqlCommand("SELECT BranchId FROM Users WHERE UserId = @UserId", connection))
        {
            cmd.Parameters.AddWithValue("@UserId", userId);
            var obj = await cmd.ExecuteScalarAsync();
            if (obj != null && obj != DBNull.Value)
                return Convert.ToInt32(obj);
        }
        return 0;
    }

    private static async Task<List<int>> GetUserBranchIdsFromConnectionAsync(SqlConnection connection, long userId)
    {
        var list = new List<int>();
        try
        {
            using (var cmd = new SqlCommand("SELECT BranchId FROM UserBranches WHERE UserId = @UserId ORDER BY BranchId", connection))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        list.Add(reader.GetInt32(0));
                }
            }
        }
        catch
        {
            // UserBranches table may not exist
        }
        return list;
    }
    //public async Task<List<User>> GetUserByNameAsync(string username)
    //{
    //    var RowsAffectedResponse = 0;
        
    //    try
    //    {
    //        using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
    //        {
    //            await connection.OpenAsync();
    //            using (var command = new SqlCommand("UpdateUser", connection))
    //            {
    //                command.CommandType = CommandType.StoredProcedure;
                    
    //                command.Parameters.AddWithValue("@UserName", username);
    //                var AfftectedRowCount = new SqlParameter("@RowsAffected", SqlDbType.Int)
    //                {
    //                    Direction = ParameterDirection.Output
    //                };
    //                command.Parameters.Add(AfftectedRowCount);

    //                int rowsAffected = await command.ExecuteNonQueryAsync();
    //                if (rowsAffected != 0)
    //                {
    //                    RowsAffectedResponse = rowsAffected;
    //                }
    //            }
    //        }
    //    }
    //    catch
    //    {
    //    }
    //    return RowsAffectedResponse;
    //}
}
