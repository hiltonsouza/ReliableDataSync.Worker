using Dapper;
using System.Data;
using Worker.Application.Abstractions.Persistence;
using Worker.Domain.Entities;
using Worker.Domain.Enums;
using Worker.Domain.Policies;
using Worker.Domain.ValueObjects;

namespace Worker.Infrastructure.Persistence.Sql.Repositories
{
    public sealed class SqlRecordQueueRepository : IRecordQueueRepository
    {
        readonly ReliableDataSyncDbSession _db;
        readonly RetryPolicy _retryPolicy;
        readonly AttemptLimitPolicy _attemptLimitPolicy;

        public SqlRecordQueueRepository(
            ReliableDataSyncDbSession db,
            RetryPolicy retryPolicy,
            AttemptLimitPolicy attemptLimitPolicy)
        {
            _db = db;
            _retryPolicy = retryPolicy;
            _attemptLimitPolicy = attemptLimitPolicy;
        }

        public async Task<SyncRecord?> ClaimNextEligibleAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken)
        {
            const string sql = @"
;WITH Candidates AS
(
    SELECT TOP(1) *
    FROM dbo.SyncRecords WITH (READPAST, UPDLOCK, ROWLOCK)
    WHERE
        (
            Status = @Pending
            OR
            (
                Status = @RetryableError
                AND (LastAttemptAt IS NULL OR DATEADD(SECOND, @MinRetryDelaySeconds, LastAttemptAt) <= @NowUtc)
            )
        )
        AND Attempts < @MaxAttempts
    ORDER BY CreatedAt ASC
)
UPDATE Candidates
SET
    Status = @InProgress,
    Attempts = Attempts + 1,
    LastAttemptAt = @NowUtc,
    UpdatedAt = @NowUtc,
    Message = ''
OUTPUT
    inserted.Id,
    inserted.ExternalKey,
    inserted.PayloadHash,
    inserted.Status,
    inserted.Message,
    inserted.Attempts,
    inserted.LastAttemptAt,
    inserted.CreatedAt,
    inserted.UpdatedAt;
";

            using var conn = await _db.CreateOpenConnectionAsync(cancellationToken);
            using var tx = conn.BeginTransaction(IsolationLevel.ReadCommitted);

            var row = await conn.QueryFirstOrDefaultAsync<SyncRecordRow>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        NowUtc = nowUtc,
                        Pending = (int)ProcessingStatus.Pending,
                        RetryableError = (int)ProcessingStatus.RetryableError,
                        InProgress = (int)ProcessingStatus.InProgress,
                        MaxAttempts = _attemptLimitPolicy.MaxAttempts,
                        MinRetryDelaySeconds = (int)_retryPolicy.MinimumRetryDelay.TotalSeconds,
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            tx.Commit();

            return row is null ? null : MapToDomain(row);
        }

        public async Task SaveOutcomeAsync(
            Guid id,
            ProcessingStatus status,
            string message,
            DateTimeOffset nowUtc,
            CancellationToken cancellationToken)
        {
            const string sql = @"
UPDATE dbo.SyncRecords
SET
    Status =
        CASE
            WHEN @Status = @RetryableError AND Attempts >= @MaxAttempts THEN @Failed
            ELSE @Status
        END,
    Message = @Message,
    UpdatedAt = @NowUtc,
    LastAttemptAt = @NowUtc
WHERE Id = @Id;
";

            using var conn = await _db.CreateOpenConnectionAsync(cancellationToken);

            await conn.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    Id = id,
                    Status = (int)status,
                    Message = message ?? "",
                    NowUtc = nowUtc,
                    RetryableError = (int)ProcessingStatus.RetryableError,
                    Failed = (int)ProcessingStatus.Failed,
                    MaxAttempts = _attemptLimitPolicy.MaxAttempts
                },
                cancellationToken: cancellationToken));
        }

        private static SyncRecord MapToDomain(SyncRecordRow row)
        {
            return SyncRecord.Rehydrate(
                row.Id,
                row.ExternalKey,
                row.PayloadHash,
                (ProcessingStatus)row.Status,
                row.Message,
                row.Attempts,
                row.LastAttemptAt,
                row.CreatedAt,
                row.UpdatedAt);
        }

        private sealed class SyncRecordRow
        {
            public Guid Id { get; init; }
            public string ExternalKey { get; init; } = string.Empty;
            public string PayloadHash { get; init; } = string.Empty;
            public int Status { get; init; }
            public string Message { get; init; } = string.Empty;
            public int Attempts { get; init; }
            public DateTimeOffset? LastAttemptAt { get; init; }
            public DateTimeOffset CreatedAt { get; init; }
            public DateTimeOffset UpdatedAt { get; init; }
        }
    }
}
