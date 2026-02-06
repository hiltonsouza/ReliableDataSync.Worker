using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Worker.Application.UseCases;

namespace Worker.Host.Workers
{
    public sealed class SyncWorkerService : BackgroundService
    {
        readonly IServiceScopeFactory _serviceFactory;
        readonly ILogger<SyncWorkerService> _logger;
        readonly TimeSpan _pollInterval;

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
                    //shutdown normal
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Worker loop error");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
    }
}
