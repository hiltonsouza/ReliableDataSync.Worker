using System;
using System.Collections.Generic;
using System.Text;

namespace Worker.Application.Abstractions.Time
{
    public interface IClock
    {
        DateTimeOffset UtcNow { get; }
    }
}
