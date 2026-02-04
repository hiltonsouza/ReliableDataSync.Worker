using System;
using System.Collections.Generic;
using System.Text;
using Worker.Domain.Enums;
using Worker.Domain.ValueObjects;

namespace Worker.Application.Services
{
    public sealed class ExecutionClassifier
    {
        public ProcessingStatus Classify(ExecutionResult result)
        {
            result = result.EnsureMessage();

            if(result.Success) return ProcessingStatus.Processed;
            if(result.AlreadyExists) return ProcessingStatus.Skipped;
            if(result.Retryable) return ProcessingStatus.RetryableError;

            return ProcessingStatus.Failed;
        }
    }
}
