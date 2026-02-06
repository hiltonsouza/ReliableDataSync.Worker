
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Worker.Application;
using Worker.Host.Workers;
using Worker.Infrastructure;
using Worker.Infrastructure.Persistence.Sql;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddHostedService<SyncWorkerService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var init = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await init.InitializerAsync(CancellationToken.None);
}

app.Run();

