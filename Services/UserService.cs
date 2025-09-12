using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.Data.SqlClient;
using StringEncrptandDecryptorApp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Security.Policy;
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
                                  users.Add(new User
                                    {
                                        UserId = reader.GetInt64(reader.GetOrdinal("UserId")),
                                        UserName = reader.GetString(reader.GetOrdinal("UserName")),
                                        UserPassword = encryptionHelper.Decrypt(reader.GetString(reader.GetOrdinal("UserPassword"))),
                                        IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled")),
                                        IsAdmin = reader.GetBoolean(reader.GetOrdinal("IsAdmin")),

                                    });

                                
                       
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
                                return new User
                                {
                                    UserId = reader.GetInt64(reader.GetOrdinal("UserId")),
                                    UserName = reader.GetString(reader.GetOrdinal("UserName")),
                                    UserPassword = encryptionHelper.Decrypt(reader.GetString(reader.GetOrdinal("UserPassword"))),
                                    IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled")),
                                    IsAdmin = reader.GetBoolean(reader.GetOrdinal("IsAdmin")),

                                };
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

    public async Task<bool> CreateUserAsync(User user)
    {
        bool response = false;
        try
        {
            EncryptionHelper encryptionHelper = new EncryptionHelper();
            var encrytedPassword = encryptionHelper.Encrypt(user.UserPassword);
            using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("AddUser", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@pName", user.UserName);
                    command.Parameters.AddWithValue("@pPassword", encrytedPassword);
                    command.Parameters.AddWithValue("@pIsEnabled", user.IsEnabled);
                    command.Parameters.AddWithValue("@pIsAdmin", user.IsAdmin);
                    var userIdParam = new SqlParameter("@pUserId", SqlDbType.BigInt)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(userIdParam);
                    await command.ExecuteNonQueryAsync();
                    long newUserId = (long)userIdParam.Value;
                    if (newUserId != 0)
                    {
                        response = true;
                    }

                }
            }
        }
        catch
        {
            
        }
        return response;
    }

    public async Task<int> UpdateUserAsync(User user)
    {
        var RowsAffectedResponse = 0;
        EncryptionHelper encryptionHelper = new EncryptionHelper();
        var encyptPassword = encryptionHelper.Encrypt(user.UserPassword);

        try
        {
            using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("UpdateUser", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@pId", user.UserId);
                    command.Parameters.AddWithValue("@pUserName", user.UserName);
                    command.Parameters.AddWithValue("@pPassword", encyptPassword);
                    command.Parameters.AddWithValue("@pIsEnabled", user.IsEnabled);
                    command.Parameters.AddWithValue("@IsAdmin", user.IsAdmin);
                    var AfftectedRowCount = new SqlParameter("@RowsAffected", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(AfftectedRowCount);
                    
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected != 0)
                        {
                            RowsAffectedResponse=rowsAffected;
                        }
                }
            }
        }
        catch
        {  
        }
        return RowsAffectedResponse;
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

    public async Task<User> GetUserByCredentialsAsync(string username, string password)
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

                    try
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new User
                                {
                                    UserId = reader.GetInt64(reader.GetOrdinal("UserId")),
                                    UserName = reader.GetString(reader.GetOrdinal("UserName")),
                                    UserPassword = reader.GetString(reader.GetOrdinal("UserPassword")),
                                    IsEnabled = reader.GetBoolean(reader.GetOrdinal("IsEnabled")),
                                    IsAdmin = reader.GetBoolean(reader.GetOrdinal("IsAdmin"))
                                };
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
