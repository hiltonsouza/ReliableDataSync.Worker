using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Worker.Application;
using Worker.Host.Workers;
using Worker.Infrastructure;
using Worker.Infrastructure.Persistence.Sql;

var builder = Host.CreateApplicationBuilder(args);

// Logging console (bonito e consistente)
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(o =>
{
    o.SingleLine = true;
    o.TimestampFormat = "HH:mm:ss ";
});

// DI
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddHostedService<SyncWorkerService>();

var app = builder.Build();

// Bootstrap + init do banco
var bootstrapLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Bootstrap");

bootstrapLogger.LogInformation("Starting ReliableDataSync Worker...");
bootstrapLogger.LogInformation("Initializing dependencies...");
using (var scope = app.Services.CreateScope())
{
    bootstrapLogger.LogInformation("Initializing SQL persistence...");

    var init = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await init.InitializerAsync(CancellationToken.None);

    bootstrapLogger.LogInformation("SQL persistence ready.");
}

bootstrapLogger.LogInformation("Initialization completed. Worker is running.");

app.Run();
