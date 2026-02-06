using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Worker.Application.UseCases;

namespace Worker.Host.Workers;

public sealed class SyncWorkerService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceFactory;
    private readonly ILogger<SyncWorkerService> _logger;
    private readonly TimeSpan _pollInterval;

    public SyncWorkerService(
        IServiceScopeFactory serviceFactory,
        IConfiguration configuration,
        ILogger<SyncWorkerService> logger)
    {
        _serviceFactory = serviceFactory;
        _logger = logger;

        var seconds = configuration.GetValue<int?>("Worker:PollIntervalSeconds") ?? 2;
        _pollInterval = TimeSpan.FromSeconds(Math.Max(1, seconds));
        _logger.LogInformation("Sync worker started. PollIntervalSeconds={PollIntervalSeconds}", (int)_pollInterval.TotalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Sync worker started. PollIntervalSeconds={Seconds}", _pollInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceFactory.CreateScope();
                var useCase = scope.ServiceProvider.GetRequiredService<ProcessNextRecordUseCase>();

                var didWork = await useCase.ExecuteAsync(stoppingToken);

                if (!didWork)
                    await Task.Delay(_pollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Sync worker stopping...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker loop error");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
