using DistributedBanking.Processing.Extensions;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services
    .AddServices(configuration)
    .AddBackgroundListeners()
    .ConfigureOptions(configuration);

builder.Host.UseSerilogAppLogging();

var application = builder.Build();

await application.RunAsync();
