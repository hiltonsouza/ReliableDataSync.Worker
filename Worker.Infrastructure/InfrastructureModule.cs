using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Worker.Application.Abstractions.Persistence;
using Worker.Infrastructure.Persistence;
using Worker.Infrastructure.Persistence.Repositories;

namespace Worker.Infrastructure
{
    public static class InfrastructureModule
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddRepositories()
                .AddDbSession(configuration)
                .AddUnitOfWork();

            return services;
        }
        private static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services
                .AddScoped<ISyncRecordRepository, SyncRecordRepository>();

            return services;
        }

        private static IServiceCollection AddUnitOfWork(this IServiceCollection services)
        {
            services
                .AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
        private static IServiceCollection AddDbSession(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddScoped<DbSession>();

            return services;
        }

    }
}
