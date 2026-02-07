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
    private Task? _currentLoopTask;

    public SyncWorkerService(
        IServiceScopeFactory serviceFactory,
        IConfiguration configuration,
        ILogger<SyncWorkerService> logger)
    {
        _serviceFactory = serviceFactory;
        _logger = logger;

        var seconds = configuration.GetValue<int?>("Worker:PollIntervalSeconds") ?? 2;
        _pollInterval = TimeSpan.FromSeconds(Math.Max(1, seconds));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Sync worker started. PollIntervalSeconds={Seconds}", _pollInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _currentLoopTask = ProcessSingleCycleAsync(stoppingToken);
                var didWork = await _currentLoopTask;

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

        _logger.LogInformation("Sync worker stopped.");
    }

    private async Task<bool> ProcessSingleCycleAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceFactory.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ProcessNextRecordUseCase>();
        return await useCase.ExecuteAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stop requested. Waiting in-flight processing to complete...");

        if (_currentLoopTask is { IsCompleted: false })
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);

            try
            {
                await _currentLoopTask.WaitAsync(linked.Token);
                _logger.LogInformation("In-flight processing completed.");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Stop timeout reached while waiting in-flight processing.");
            }
        }

        await base.StopAsync(cancellationToken);
    }
}
