using Microsoft.Extensions.DependencyInjection;
using Worker.Application.Abstractions.Metrics;
using Worker.Application.Metrics;
using Worker.Application.Services;
using Worker.Application.UseCases;

namespace Worker.Application
{
    public static class ApplicationModule
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddMetrics();
            services.AddSingleton<IWorkerMetrics, WorkerMetrics>();
            services.AddSingleton<ExecutionClassifier>();
            services.AddScoped<ProcessNextRecordUseCase>();
            return services;
        }
    }
}
