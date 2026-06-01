using IMS.DAL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace IMS.Controllers
{
    public class DatabaseBackupController : Controller
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly IConfiguration _configuration;

        public DatabaseBackupController(IDbContextFactory dbContextFactory, IConfiguration configuration)
        {
            _dbContextFactory = dbContextFactory;
            _configuration = configuration;
        }

        private bool UserIsAdmin()
        {
            var v = User.FindFirst("IsAdmin")?.Value;
            return bool.TryParse(v, out var b) && b;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (!UserIsAdmin())
            {
                return Forbid();
            }

            return View();
        }

        /// <summary>
        /// Creates a SQL Server .bak for the current session domain database and returns it as a download.
        /// The backup file is written to a path on the SQL Server machine (default backup folder from registry, or configured override).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Download(CancellationToken cancellationToken)
        {
            if (!UserIsAdmin())
            {
                return Forbid();
            }

            string connectionString;
            try
            {
                connectionString = _dbContextFactory.DBConnectionString();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            var csb = new SqlConnectionStringBuilder(connectionString);
            var databaseName = csb.InitialCatalog;
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                return BadRequest("Database name is missing from the connection string.");
            }

            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(cancellationToken);

            var backupDir = await ResolveBackupDirectoryAsync(conn, cancellationToken);

            var safeFileName = $"{SanitizeFileNameFragment(databaseName)}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_UTC.bak";
            var physicalPath = Path.Combine(backupDir, safeFileName);

            try
            {
                await RunBackupAsync(conn, databaseName, physicalPath, cancellationToken);
            }
            catch (SqlException ex)
            {
                return BadRequest(
                    $"SQL Server could not create the backup. Ensure the SQL Server service account can write to \"{backupDir}\". Details: {ex.Message}");
            }

            if (!System.IO.File.Exists(physicalPath))
            {
                return BadRequest(
                    "The backup ran on SQL Server, but this application cannot see the .bak file. " +
                    "That usually means SQL wrote under its default folder (for example Program Files\\...\\Backup) where your Windows user is not allowed to read, " +
                    "or SQL is on another machine. " +
                    "Fix: set appsettings \"DatabaseBackup:SqlServerBackupDirectory\" to a folder both can use (for example C:\\ProgramData\\IMS\\SqlBackups) and grant the SQL Server service account Modify on that folder, or use a UNC share.");
            }

            FileStream fileStream;
            try
            {
                fileStream = new FileStream(
                    physicalPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 64 * 1024,
                    options: FileOptions.Asynchronous | FileOptions.SequentialScan);
            }
            catch (Exception ex)
            {
                TryDelete(physicalPath);
                return BadRequest($"Could not read the backup file from \"{physicalPath}\". {ex.Message}");
            }

            var pathToDelete = physicalPath;
            Response.OnCompleted(
                _ =>
                {
                    TryDelete(pathToDelete);
                    return Task.CompletedTask;
                },
                null);

            return File(fileStream, "application/octet-stream", safeFileName);
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
            catch
            {
                // ignore
            }
        }

        private async Task<string> ResolveBackupDirectoryAsync(SqlConnection conn, CancellationToken cancellationToken)
        {
            var configured = _configuration["DatabaseBackup:SqlServerBackupDirectory"]?.Trim();
            if (!string.IsNullOrEmpty(configured))
            {
                var full = Path.GetFullPath(configured);
                Directory.CreateDirectory(full);
                return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            // Default: folder under ProgramData that the web app creates and can read. SQL Server's own
            // default backup directory is often under Program Files and backups succeed there while
            // File.Exists fails for the app user (looks like "file not visible").
            var shared = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "IMS",
                "SqlBackups");
            Directory.CreateDirectory(shared);
            var normalized = Path.GetFullPath(shared).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (_configuration.GetValue("DatabaseBackup:UseSqlServerRegistryBackupPath", defaultValue: false))
            {
                await using (var cmd = new SqlCommand(
                                 "EXEC master.dbo.xp_instance_regread N'HKEY_LOCAL_MACHINE', N'Software\\Microsoft\\MSSQLServer\\MSSQLServer', N'BackupDirectory'",
                                 conn))
                await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            if (reader.IsDBNull(i))
                            {
                                continue;
                            }

                            if (reader.GetValue(i) is string s && s.Length > 2 && s.Contains(':', StringComparison.Ordinal))
                            {
                                return s.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                            }
                        }
                    }
                }
            }

            return normalized;
        }

        private static async Task RunBackupAsync(
            SqlConnection conn,
            string databaseName,
            string physicalPath,
            CancellationToken cancellationToken)
        {
            var quotedDb = "[" + databaseName.Replace("]", "]]", StringComparison.Ordinal) + "]";
            await using var cmd = new SqlCommand(
                $"BACKUP DATABASE {quotedDb} TO DISK = @disk WITH COPY_ONLY, FORMAT, INIT, STATS = 10",
                conn);
            cmd.CommandTimeout = 0;
            cmd.Parameters.AddWithValue("@disk", physicalPath);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private static string SanitizeFileNameFragment(string name)
        {
            var chars = name.Where(ch => !Path.GetInvalidFileNameChars().Contains(ch)).ToArray();
            var s = new string(chars);
            return string.IsNullOrWhiteSpace(s) ? "database" : s;
        }
    }
}
