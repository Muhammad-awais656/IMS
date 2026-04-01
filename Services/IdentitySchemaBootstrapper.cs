using IMS.DAL.PrimaryDBContext;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace IMS.Services
{
    /// <summary>
    /// Ensures AspNetUsers has custom columns required by <see cref="IMS.Authorization.ApplicationUser"/>
    /// and that AspNetUserBranches exists. Runs at startup so manual SQL is optional.
    /// </summary>
    public static class IdentitySchemaBootstrapper
    {
        public static async Task EnsureAsync(AppDbContext db, ILogger logger, CancellationToken cancellationToken = default)
        {
            try
            {
                await db.Database.OpenConnectionAsync(cancellationToken);
                var conn = db.Database.GetDbConnection();

                async Task ExecAsync(string sql)
                {
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }

                await using (var check = conn.CreateCommand())
                {
                    check.CommandText = "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'AspNetUsers'";
                    var exists = await check.ExecuteScalarAsync(cancellationToken);
                    if (exists == null || exists == DBNull.Value)
                    {
                        logger.LogWarning("Table dbo.AspNetUsers not found. Run: dotnet ef database update");
                        return;
                    }
                }

                await ExecAsync(@"
IF COL_LENGTH('dbo.AspNetUsers', 'IsActive') IS NULL
  ALTER TABLE dbo.AspNetUsers ADD IsActive bit NOT NULL CONSTRAINT DF_AspNetUsers_IsActive DEFAULT(1);");

                await ExecAsync(@"
IF COL_LENGTH('dbo.AspNetUsers', 'IsAdmin') IS NULL
  ALTER TABLE dbo.AspNetUsers ADD IsAdmin bit NOT NULL CONSTRAINT DF_AspNetUsers_IsAdmin DEFAULT(0);");

                await ExecAsync(@"
IF COL_LENGTH('dbo.AspNetUsers', 'LegacyUserId') IS NULL
  ALTER TABLE dbo.AspNetUsers ADD LegacyUserId bigint NULL;");

                await ExecAsync(@"
IF COL_LENGTH('dbo.AspNetUsers', 'CreatedDate') IS NULL
  ALTER TABLE dbo.AspNetUsers ADD CreatedDate datetime2 NOT NULL CONSTRAINT DF_AspNetUsers_CreatedDate DEFAULT(SYSUTCDATETIME());");

                await ExecAsync(@"
IF COL_LENGTH('dbo.AspNetUsers', 'ModifiedDate') IS NULL
  ALTER TABLE dbo.AspNetUsers ADD ModifiedDate datetime2 NULL;");

                await ExecAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AspNetUsers_LegacyUserId' AND object_id = OBJECT_ID('dbo.AspNetUsers'))
  CREATE INDEX IX_AspNetUsers_LegacyUserId ON dbo.AspNetUsers(LegacyUserId);");

                await ExecAsync(@"
IF OBJECT_ID('dbo.AspNetUserBranches', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.AspNetUserBranches (
    UserId nvarchar(450) NOT NULL,
    BranchId int NOT NULL,
    CONSTRAINT PK_AspNetUserBranches PRIMARY KEY (UserId, BranchId),
    CONSTRAINT FK_AspNetUserBranches_AspNetUsers FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AspNetUserBranches_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(BranchId) ON DELETE CASCADE
  );
END");

                logger.LogInformation("Identity schema bootstrap: AspNetUsers custom columns and AspNetUserBranches verified.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Identity schema bootstrap failed. Run Scripts/Patch_AspNetUsers_CustomColumns.sql manually.");
            }
            finally
            {
                await db.Database.CloseConnectionAsync();
            }
        }
    }
}
