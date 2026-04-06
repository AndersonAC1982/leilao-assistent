using LeilaoAuto.Application;
using LeilaoAuto.Infrastructure;
using LeilaoAuto.Workers;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Services.AddSerilog();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<LotSyncWorker>();

var host = builder.Build();
await host.Services.InitializeDatabaseAsync();
await host.RunAsync();
