using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Worker.Application.Services;
using Worker.Application.UseCases;

namespace Worker.Application
{
    public static class ApplicationModule
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddSingleton<ExecutionClassifier>();
            services.AddScoped<ProcessNextRecordUseCase>();
            return services;
        }
    }
}
