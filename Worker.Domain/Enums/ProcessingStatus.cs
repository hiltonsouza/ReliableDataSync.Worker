using System;
using System.Collections.Generic;
using System.Text;

namespace Worker.Domain.Enums
{
    public enum ProcessingStatus
    {
        Pending = 0,
        InProgress = 1,
        Processed = 2,
        Skipped = 3,
        RetryableError = 4, 
        Failed = 5
    }

}
