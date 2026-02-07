namespace Worker.Infrastructure.ExternalSystems.Resilience;

public sealed class ExternalSystemResilienceOptions
{
    public const string SectionName = "Worker:Resilience";

    public int TimeoutSeconds { get; init; } = 15;
    public int RetryCount { get; init; } = 3;
    public int CircuitBreakerFailures { get; init; } = 5;
    public int CircuitBreakerBreakSeconds { get; init; } = 30;
}
