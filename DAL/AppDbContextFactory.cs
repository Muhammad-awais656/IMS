using IMS.DAL.PrimaryDBContext;
using Microsoft.Data.SqlClient;
using StringEncrptandDecryptorApp;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc;

namespace IMS.DAL
{
    public interface IDbContextFactory
    {
        // Uncomment the following line if you want to create a DbContext instance means Code First approach
        //AppDbContext CreateDbContext();
        public  string DBConnectionString();
    }

    public class AppDbContextFactory : IDbContextFactory
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public AppDbContextFactory(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public  string DBConnectionString()
        {
            var DecrptedConnectionString = string.Empty;
            var selectedDb = _httpContextAccessor?.HttpContext?.Session.GetString("Domain");
            try
            {
                if (string.IsNullOrEmpty(selectedDb))
                {
                    throw new InvalidOperationException("Please select a Domain by logging in.");
                }
                var connectionString = selectedDb switch
                {
                    "Shop" => _configuration.GetConnectionString("ShopConnectionString"),
                    "Factory" => _configuration.GetConnectionString("FactoryConnectionString"),
                    _ => throw new InvalidOperationException("No Domain selected or invalid domain selection.")
                };
                if (!string.IsNullOrEmpty(connectionString))
                {
                    var encryptionHelper = new EncryptionHelper();
                    var userId = string.Empty;
                    var password = string.Empty;
                    try
                    {
                        var builder = new SqlConnectionStringBuilder(connectionString);
                        userId = builder.UserID;
                        password = builder.Password;
                        var DecryptedUserId = encryptionHelper.Decrypt(userId);
                        var DecryptedPassword = encryptionHelper.Decrypt(password);
                        var decryptedBuilder = new SqlConnectionStringBuilder(connectionString)
                        {
                            UserID = DecryptedUserId,
                            Password = DecryptedPassword
                        };
                        DecrptedConnectionString = decryptedBuilder.ConnectionString;
                    }
                    catch (SqlException ex)
                    {

                        throw new Exception($"{ex.Message}");
                    }
                }
                }
            catch (Exception ex)
            {
               
                throw new Exception($"{ex.Message}");
            }
            return DecrptedConnectionString;
        }

    }
}
