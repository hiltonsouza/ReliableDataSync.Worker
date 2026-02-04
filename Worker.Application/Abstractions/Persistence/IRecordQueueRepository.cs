using System;
using System.Collections.Generic;
using System.Text;
using Worker.Domain.Entities;
using Worker.Domain.Enums;

namespace Worker.Application.Abstractions.Persistence
{
    public interface IRecordQueueRepository
    {
        /// <summary>
        /// Claims the next eligible record for processing (Pending or RetryableError with backoff),
        /// automatically marking it as InProgress and incrementing attempts.
        /// Returns null if nothing is eligible.
        /// </summary>
        Task<SyncRecord?> ClaimNextEligibleAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default);

        Task SaveOutcomeAsync(Guid id, ProcessingStatus status, string message, DateTimeOffset nowUtc, CancellationToken cancellationToken = default);
    }
}
