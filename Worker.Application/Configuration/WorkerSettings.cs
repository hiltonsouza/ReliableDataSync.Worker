using System;
using System.Collections.Generic;
using System.Text;

namespace Worker.Application.Configuration;

public class WorkerSettings
{
    public int BatchSize { get; set; } = 100;
    public int IntervalSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
}
