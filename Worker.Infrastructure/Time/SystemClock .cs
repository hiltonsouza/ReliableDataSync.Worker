using System;
using System.Collections.Generic;
using System.Text;
using Worker.Application.Abstractions.Time;

namespace Worker.Infrastructure.Time
{
    public sealed class SystemClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
