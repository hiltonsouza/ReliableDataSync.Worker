using Worker.Domain.ValueObjects;
using Worker.Application.Abstractions.External;
using Worker.Domain.Entities;

namespace Worker.Infrastructure.ExternalSystems;

    public sealed class FakeExternalSystemClient :IExternalSystemClient
    {
        public Task<ExecutionResult> UpsertAsync(SyncRecord record, CancellationToken cancellationToken)
        {
            // regra simples só pra demo:
            // - ExternalKey terminando com "3" => AlreadyExists
            // - ExternalKey terminando com "2" => RetryableError
            // - resto => Ok
            if (record.ExternalKey.EndsWith("3"))
                return Task.FromResult(ExecutionResult.Exists("Already present in external system."));

            if (record.ExternalKey.EndsWith("2"))
                return Task.FromResult(ExecutionResult.RetryableError("Transient external failure."));

            return Task.FromResult(ExecutionResult.Ok("Applied successfully."));
        }
    }
