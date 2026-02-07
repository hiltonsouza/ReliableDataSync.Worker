using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Worker.Application;
using Worker.Application.Configuration;
using Worker.Domain.Repositories;
using Worker.Host.Workers;
using Worker.Infrastructure;
using Worker.Infrastructure.Decorators;

// Setup Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices((context, services) =>
        {
            // 1. Configurações (Bind do appsettings)
            services.Configure<WorkerSettings>(context.Configuration.GetSection("Worker"));

            // Modules (Clean Architecture root)
            services.AddApplication();
            services.AddInfrastructure(context.Configuration);

            // 3. Register Decorator (Requires 'Scrutor' NuGet package)
            // This wraps the standard SyncRecordRepository with the Logged version
            services.Decorate<ISyncRecordRepository, LoggedSyncRecordRepository>();

            // 4. Hosted Service
            services.AddHostedService<SyncWorkerService>();
        })
        .Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Fatal error trying start");
}
finally
{
    Log.CloseAndFlush();
}