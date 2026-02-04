using System;
using System.Collections.Generic;
using System.Text;

namespace Worker.Domain.Policies
{
    /// <summary>
    /// Defines when a retryable error can be retried again.
    /// </summary>
    public sealed class RetryPolicy
    {
        public TimeSpan MinimumRetryDelay { get; }

        public RetryPolicy(TimeSpan minimumRetryDelay)
        {
            if (minimumRetryDelay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(minimumRetryDelay), "Minimum retry delay must be non-negative.");

            MinimumRetryDelay = minimumRetryDelay;
        }

        /// <summary>
        /// True if the record may be retried now, given the last attempt time.
        /// </summary>
        public bool CanRetry(DateTimeOffset? lastAttemptAt, DateTimeOffset nowUtc)
        {
            if(MinimumRetryDelay == TimeSpan.Zero)
                return true;

            if(lastAttemptAt is null)
                return true;

            return (nowUtc - lastAttemptAt.Value) >= MinimumRetryDelay;
        }
    }
}
