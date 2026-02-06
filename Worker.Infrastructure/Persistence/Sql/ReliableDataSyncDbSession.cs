using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;


namespace Worker.Infrastructure.Persistence.Sql
{
    /// <summary>
    /// Dapper-style DB session
    /// </summary>
    public sealed class ReliableDataSyncDbSession
    {
        readonly string _connectionString;

        public ReliableDataSyncDbSession(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("ReliableDataSync")
           ?? throw new InvalidOperationException("ConnectionStrings:ReliableDataSync is required.");
        }

        public IDbConnection CreateConnection() => new SqlConnection(_connectionString);

        public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            return connection;
        }
    }
}
