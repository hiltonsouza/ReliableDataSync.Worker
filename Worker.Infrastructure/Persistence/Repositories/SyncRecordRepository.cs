using Dapper;
using System;
using System.Data;
using Worker.Application.Abstractions.Persistence;
using Worker.Domain.Entities;
using Worker.Domain.Enums;

namespace Worker.Infrastructure.Persistence.Repositories;

public class SyncRecordRepository : BaseRepository<SyncRecord>, ISyncRecordRepository
{
    public SyncRecordRepository(DbSession session) : base(session) { }

    public async Task<SyncRecord?> GetNextPendingAsync()
    {
        var sql = $@"
                UPDATE TOP (1) {_tableName}
                SET Status = @Processing,
                    UpdatedAt = GETUTCDATE()
                OUTPUT INSERTED.*
                WHERE Status IN (@Pending, @Retry
                AND (NextAttempetAt IS NULL OR NextAttemptAt <= GETUTCDATE())";

        return await _session.Connection.QuerySingleOrDefaultAsync<SyncRecord>(
            sql,
            new { Processing = ProcessingStatus.Processing, Pending = ProcessingStatus.Pending, Retry = ProcessingStatus.Retry },
            _session.Transaction);
    }
    public async Task UpdateStatusAsync(Guid id, ProcessingStatus status, string message, DateTime? nextAttempt = null)
    {
        var sql = $@"
                UPDATE {_tableName}
                SET Status = @Status,
                    ErrorMessage = @Message,
                    NextAttemptAt = @NextAttempt,
                    UpdatedAt = GETUTCDATE(),
                    ProcessedAt = CASE WHEN @Status = 3 THEN GETUTCDATE() ELSE ProcessedAt END
                WHERE Id = @Id";

        await _session.Connection.ExecuteAsync(sql,
            new
            {
                Id = id,
                Status = status,
                Message = message,
                NextAttempt = nextAttempt
            }, _session.Transaction);
    }
}
