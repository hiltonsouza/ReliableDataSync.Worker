using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Worker.Application.Abstractions.Persistence;
using Worker.Application.Abstractions.Time;
using Worker.Domain.Policies;
using Worker.Domain.ValueObjects;
using Worker.Infrastructure.Persistence.Sql;
using Worker.Infrastructure.Persistence.Sql.Repositories;
using Worker.Infrastructure.Time;

namespace Worker.Infrastructure
{
    public static class InfrastructureModule
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddCommon(configuration)
                .AddSqlPersistence(configuration);

            return services;
        }

        private static IServiceCollection AddCommon(this IServiceCollection services, IConfiguration configuration)
        {
            var maxAttempts = configuration.GetValue<int?>("Worker:MaxAttempts") ?? 5;
            var minRetryDelaySeconds = configuration.GetValue<int?>("Worker:MinimumRetryDelaySeconds") ?? 30;

            services.AddSingleton(new AttemptLimitPolicy(maxAttempts));
            services.AddSingleton(new RetryPolicy(TimeSpan.FromSeconds(minRetryDelaySeconds)));

            services.AddSingleton<IClock, SystemClock>();

            return services;
        }

        private static IServiceCollection AddSqlPersistence(IServiceCollection services, IConfiguration configuration)
        {
            services
            .AddSqlSession(configuration)
            .AddSqlRepositories();

            return services;
        }
        private static IServiceCollection AddSqlSession(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(new ReliableDataSyncDbSession(configuration));
            return services;
        }
        private static IServiceCollection AddSqlRepositories(this IServiceCollection services)
        {
            services.AddScoped<IRecordQueueRepository, SqlRecordQueueRepository>();
            return services;
        }

    }
}
