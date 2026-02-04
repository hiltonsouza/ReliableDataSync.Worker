using System;
using System.Collections.Generic;
using System.Text;
using Worker.Domain.Entities;
using Worker.Domain.ValueObjects;

namespace Worker.Application.Abstractions.External
{
    public interface IExternalSystemClient
    {
        Task<ExecutionResult> UpsertAsync(SyncRecord record, CancellationToken cancellationToken = default);
    }
}
