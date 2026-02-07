using Microsoft.Extensions.DependencyInjection;
using Worker.Application.Services;

namespace Worker.Application
{
    public static class ApplicationModule
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Serviços de Aplicação (Regra de Negócio)
            services
                .AddScoped<ISyncProcessingService, SyncProcessingService>();

            // Se usar MediatR ou AutoMapper, registre aqui também:
            // services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationModule).Assembly));
            return services;
        }
    }
}
