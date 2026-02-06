using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Worker.Infrastructure.Persistence.Sql
{
    public sealed class DbInitializer
    {
        readonly IConfiguration _configuration;
        readonly ILogger<DbInitializer> _logger;

        public DbInitializer(IConfiguration configuration, ILogger<DbInitializer> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InitializerAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Initializing SQL persistence...");

            var connectionString = _configuration.GetConnectionString("ReliableDataSync")
                ?? throw new InvalidOperationException("Connection string 'ReliableDataSync' not found.");

            var builder = new SqlConnectionStringBuilder(connectionString);
            _logger.LogInformation("Target database from connection string: {Db}", builder.InitialCatalog);

            await TryEnsureDatabaseAsync(connectionString, cancellationToken);
            await EnsureSchemaAsync(connectionString, cancellationToken);

            var seedEnabled = _configuration.GetValue<bool?>("Seed:Enabled") ?? true;
            if (seedEnabled)
                await SeedIfEmptyAsync(connectionString, cancellationToken);

            _logger.LogInformation("SQL persistence ready.");
        }

        private async Task TryEnsureDatabaseAsync(string connectionString, CancellationToken cancellationToken)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);

                var targetDb = builder.InitialCatalog;
                if (string.IsNullOrWhiteSpace(targetDb))
                    throw new InvalidOperationException("Connection string must specify an Initial Catalog (database name).");

                // conecta em master pra poder criar DB
                builder.InitialCatalog = "master";

                await using var connection = new SqlConnection(builder.ConnectionString);
                await connection.OpenAsync(cancellationToken);

                const string sql = @"
IF DB_ID(@dbName) IS NULL
BEGIN
    DECLARE @stmt NVARCHAR(MAX) = N'CREATE DATABASE [' + @dbName + N']';
    EXEC(@stmt);
END;
";

                await connection.ExecuteAsync(new CommandDefinition(
                    sql,
                    new { dbName = targetDb },
                    cancellationToken: cancellationToken));

                _logger.LogInformation("Database ensured: {Database}", targetDb);
            }
            catch (Exception ex)
            {
                // best-effort
                _logger.LogWarning(ex, "Could not ensure database exists (best-effort). Continuing...");
            }
        }

        private async Task EnsureSchemaAsync(string connectionString, CancellationToken cancellationToken)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
IF OBJECT_ID('dbo.SyncRecords', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SyncRecords
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ExternalKey NVARCHAR(200) NOT NULL,
        PayloadHash NVARCHAR(100) NOT NULL CONSTRAINT DF_SyncRecords_PayloadHash DEFAULT '',
        Status INT NOT NULL,
        Message NVARCHAR(2000) NOT NULL CONSTRAINT DF_SyncRecords_Message DEFAULT '',
        Attempts INT NOT NULL CONSTRAINT DF_SyncRecords_Attempts DEFAULT 0,
        LastAttemptAt DATETIMEOFFSET NULL,
        CreatedAt DATETIMEOFFSET NOT NULL,
        UpdatedAt DATETIMEOFFSET NOT NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SyncRecords_Status_CreatedAt' AND object_id = OBJECT_ID('dbo.SyncRecords'))
BEGIN
    CREATE INDEX IX_SyncRecords_Status_CreatedAt ON dbo.SyncRecords(Status, CreatedAt);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_SyncRecords_ExternalKey' AND object_id = OBJECT_ID('dbo.SyncRecords'))
BEGIN
    CREATE UNIQUE INDEX UX_SyncRecords_ExternalKey ON dbo.SyncRecords(ExternalKey);
END;
";

            await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
            _logger.LogInformation("Schema ensured: dbo.SyncRecords");
        }

        private async Task SeedIfEmptyAsync(string connectionString, CancellationToken cancellationToken)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT COUNT(1) FROM dbo.SyncRecords",
                cancellationToken: cancellationToken));

            if (count > 0)
            {
                _logger.LogInformation("Seed skipped: SyncRecords already has {Count} rows.", count);
                return;
            }

            var now = DateTimeOffset.UtcNow;

            const string insert = @"
INSERT INTO dbo.SyncRecords (Id, ExternalKey, PayloadHash, Status, Message, Attempts, LastAttemptAt, CreatedAt, UpdatedAt)
VALUES (@Id, @ExternalKey, @PayloadHash, @Status, @Message, @Attempts, @LastAttemptAt, @CreatedAt, @UpdatedAt);
";

            var seed = new[]
            {
                new { Id = Guid.NewGuid(), ExternalKey = "KEY-001", PayloadHash = "h1", Status = 0, Message = "", Attempts = 0, LastAttemptAt = (DateTimeOffset?)null, CreatedAt = now, UpdatedAt = now },
                new { Id = Guid.NewGuid(), ExternalKey = "KEY-002", PayloadHash = "h1", Status = 0, Message = "", Attempts = 0, LastAttemptAt = (DateTimeOffset?)null, CreatedAt = now, UpdatedAt = now },
                new { Id = Guid.NewGuid(), ExternalKey = "KEY-003", PayloadHash = "h1", Status = 0, Message = "", Attempts = 0, LastAttemptAt = (DateTimeOffset?)null, CreatedAt = now, UpdatedAt = now },
            };

            await connection.ExecuteAsync(new CommandDefinition(insert, seed, cancellationToken: cancellationToken));
            _logger.LogInformation("Seed inserted: {Count} SyncRecords.", seed.Length);
        }
    }
}
