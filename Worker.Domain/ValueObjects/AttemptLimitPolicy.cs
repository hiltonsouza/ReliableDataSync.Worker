using System;
using System.Collections.Generic;
using System.Text;

namespace Worker.Domain.ValueObjects
{
    /// <summary>
    /// Defines how many attempts a record may have before being considered terminally failed.
    /// </summary>
    public sealed class AttemptLimitPolicy
    {
        public int MaxAttempts { get; }
        public AttemptLimitPolicy(int maxAttempts)
        {
            if (maxAttempts <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max attempts must be at least 1.");
            }

            MaxAttempts = maxAttempts;
        }
        public bool HasExceeded(int attempts) => attempts >= MaxAttempts;
        public int Clamp(int attempts) => attempts < 0 ? 0 : attempts;
    }
}
