using System;
using System.Collections.Generic;
using System.Text;

namespace Worker.Domain.Enums
{
    public enum ProcessingStatus
    {
        Pending = 1,
        Processing = 2,
        Completed = 3,
        Failed = 4,
        Retry = 5, 
        RetryableError = 6 // optional: used to distinguish between temporary issues (like network errors) and permanent failures (like validation errors)
    }

}
