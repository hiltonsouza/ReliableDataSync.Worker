using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Worker.Infrastructure.Persistence;

public class DbSession : IDisposable
{
    public IDbConnection Connection { get; }
    public IDbTransaction? Transaction { get; set; }

    public DbSession(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ReliableDataSync")
        ?? throw new ArgumentNullException("ConnectionString 'ReliableDataSync' not found");

        Connection = new SqlConnection(connectionString);
        Connection.Open();
    }

    public void Dispose()
    {
        Transaction?.Dispose();
        Connection.Dispose();
    }
}