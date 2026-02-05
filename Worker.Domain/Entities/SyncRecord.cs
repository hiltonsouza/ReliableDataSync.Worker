using Worker.Domain.Enums;
using Worker.Domain.Exceptions;
using Worker.Domain.Policies;
using Worker.Domain.ValueObjects;

namespace Worker.Domain.Entities
{
    public sealed class SyncRecord
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        /// <summary>
        /// Logical identifier used for idempotency against the external system.
        /// </summary>
        public string ExternalKey { get; private set; } = string.Empty;

        /// <summary>
        /// Optional: useful to detect if input changed across runs.
        /// Can be a hash of important payload fields.
        /// </summary>
        public string PayloadHash { get; private set; } = string.Empty;     // opcional: ajuda a detectar mudança

        public ProcessingStatus Status { get; private set; } = ProcessingStatus.Pending;

        /// <summary>
        /// Human-readable message that explains what happened (auditability).
        /// </summary>
        public string Message { get; private set; } = string.Empty;

        public int Attempts { get; private set; } = 0;
        public DateTimeOffset? LastAttemptAt { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

        private SyncRecord() { } // For ORM

        public SyncRecord(string externalKey, string payloadHash)
        {
            ExternalKey = externalKey;
            PayloadHash = payloadHash;
            Touch();
        }

        public static SyncRecord Rehydrate(
            Guid id,
            string externalKey,
            string payloadHash,
            ProcessingStatus status,
            string message,
            int attempts,
            DateTimeOffset? lastAttemptAt,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt)
        {
            if (id == Guid.Empty)
                throw new DomainValidationException("Id is required.");
            if(string.IsNullOrWhiteSpace(externalKey))
                throw new DomainValidationException("ExternalKey is required.");

            var record = new SyncRecord(externalKey, payloadHash);

            // set internal state (rehydration)
            record.Id = id;
            record.Status = status;
            record.Message = (message ?? string.Empty).Trim();
            record.Attempts = attempts < 0 ? 0 : attempts;
            record.LastAttemptAt = lastAttemptAt;
            record.CreatedAt = createdAt;
            record.UpdatedAt = updatedAt;

            return record;
        }
        public void SetExternalKey(string externalKey)
        {
            if (string.IsNullOrWhiteSpace(externalKey))
                throw new DomainValidationException("ExternalKey is required.");

            ExternalKey = externalKey.Trim();
            Touch();
        }

        public void SetPayloadHash(string payloadHash)
        {
            PayloadHash = (payloadHash ?? string.Empty).Trim();
            Touch();
        }

        public void MarkPending(string message = "")
        {
            Status = ProcessingStatus.Pending;
            SetMessage(message);
            Touch();
        }

        public void MarkInProgress(DateTimeOffset nowUtc, string message = "")
        {
            EnsureNotTerminal();
            Status = ProcessingStatus.InProgress;
            SetMessage(message);
            LastAttemptAt = nowUtc;
            Attempts = Attempts + 1;
            Touch();
        }

        public void MarkProcessed(string message = "Processed successfully.")
        {
            Status = ProcessingStatus.Processed;
            SetMessage(message);
            Touch();
        }

        public void MarkSkipped(string message = "Skipped: already applied.")
        {
            Status = ProcessingStatus.Skipped;
            SetMessage(message);
            Touch();
        }

        public void MarkRetryableError(string message)
        {
            EnsureNotTerminal();
            Status = ProcessingStatus.RetryableError;
            SetMessage(message);
            Touch();
        }

        public void MarkFailed(string message)
        {
            Status = ProcessingStatus.Failed;
            SetMessage(message);
            Touch();
        }

        public bool IsTerminal()
        => Status is ProcessingStatus.Processed or ProcessingStatus.Skipped or ProcessingStatus.Failed;

        public bool CanBeClaimed(RetryPolicy retryPolicy, AttemptLimitPolicy attemptLimitPolicy, DateTimeOffset nowUtc)
        {
            var attempts = attemptLimitPolicy.Clamp(Attempts);
            if (attemptLimitPolicy.HasExceeded(attempts))
                return false;

            return Status switch
            {
                ProcessingStatus.Pending => true,
                ProcessingStatus.RetryableError => retryPolicy.CanRetry(LastAttemptAt, nowUtc),
                _ => false
            };
        }

        public void EnforceAttemptLimit(AttemptLimitPolicy attemptLimitPolicy, string messageIfExceeded = "Attempt limit exceeded.")
        {
            var attempts = attemptLimitPolicy.Clamp(Attempts);
            if (attemptLimitPolicy.HasExceeded(attempts))
                MarkFailed(messageIfExceeded);
        }

        private void EnsureNotTerminal()
        {
            if (IsTerminal())
                throw new DomainValidationException($"Cannot modify a terminal record. Current status: {Status}.");
        }

        private void SetMessage(string message)
        {
            Message = (message ?? string.Empty).Trim();
        }

        private void Touch()
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

}
