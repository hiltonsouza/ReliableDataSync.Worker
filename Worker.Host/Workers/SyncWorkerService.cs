using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Worker.Application.Configuration;
using Worker.Application.Services;

namespace Worker.Host.Workers;

public sealed class SyncWorkerService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceFactory;
    private readonly ILogger<SyncWorkerService> _logger;
    private readonly WorkerSettings _settings;

    public SyncWorkerService(IServiceScopeFactory serviceFactory, IOptions<WorkerSettings> settings, ILogger<SyncWorkerService> logger)
    {
        _serviceFactory = serviceFactory;
        _logger = logger;
        _settings = settings.Value; // Access the actual settings object
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Sync Worker started. Configuration: Interval={Interval}s, BatchSize={BatchSize}, MaxRetries={MaxRetries}",
            _settings.IntervalSeconds,
            _settings.BatchSize,
            _settings.MaxRetries);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceFactory.CreateScope();

                var processingService = scope.ServiceProvider.GetRequiredService<ISyncProcessingService>();

                _logger.LogDebug("Starting batch processing cycle...");

                // Pass configuration values to the service
                await processingService.ProcessPendingRecordsAsync(_settings.BatchSize, stoppingToken);

                // Wait for the configured interval before the next cycle
                await Task.Delay(TimeSpan.FromSeconds(_settings.IntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Shutdown signal received. Stopping Sync Worker...");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in the Worker Loop. Pausing for 5 seconds before retrying.");

                // Safety delay to prevent tight loop errors (CPU spikes)
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
        _logger.LogInformation("Sync Worker has stopped.");
    }
}
