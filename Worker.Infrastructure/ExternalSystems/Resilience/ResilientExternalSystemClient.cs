using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;
using Worker.Application.Abstractions.External;
using Worker.Domain.Entities;
using Worker.Domain.ValueObjects;

namespace Worker.Infrastructure.ExternalSystems.Resilience;

public sealed class ResilientExternalSystemClient : IExternalSystemClient
{
    private readonly IExternalSystemClient _inner;
    private readonly ILogger<ResilientExternalSystemClient> _logger;
    private readonly ExternalSystemResilienceOptions _options;

    public ResilientExternalSystemClient(
        FakeExternalSystemClient inner,
        IOptions<ExternalSystemResilienceOptions> options,
        ILogger<ResilientExternalSystemClient> logger)
    {
        _inner = inner;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<ExecutionResult> UpsertAsync(SyncRecord record, CancellationToken cancellationToken = default)
    {
        var timeoutPolicy = Policy.TimeoutAsync<ExecutionResult>(TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds)));

        var retryPolicy = Policy<ExecutionResult>
            .Handle<Exception>()
            .WaitAndRetryAsync(
                Math.Max(0, _options.RetryCount),
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                (ex, delay, attempt, _) =>
                {
                    _logger.LogWarning(ex, "Retry {Attempt} for {ExternalKey} after {Delay}s", attempt, record.ExternalKey, delay.TotalSeconds);
                });

        var circuitPolicy = Policy<ExecutionResult>
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: Math.Max(2, _options.CircuitBreakerFailures),
                durationOfBreak: TimeSpan.FromSeconds(Math.Max(5, _options.CircuitBreakerBreakSeconds)),
                onBreak: (ex, ts) => _logger.LogError(ex, "Circuit opened for {Seconds}s", ts.TotalSeconds),
                onReset: () => _logger.LogInformation("Circuit closed"),
                onHalfOpen: () => _logger.LogInformation("Circuit half-open, probing external system"));

        var wrapped = Policy.WrapAsync(timeoutPolicy, retryPolicy, circuitPolicy);

        try
        {
            return await wrapped.ExecuteAsync(ct => _inner.UpsertAsync(record, ct), cancellationToken);
        }
        catch (BrokenCircuitException)
        {
            return ExecutionResult.RetryableError("External system unavailable (circuit open).");
        }
        catch (TimeoutRejectedException)
        {
            return ExecutionResult.RetryableError("External system timeout.");
        }
    }
}
