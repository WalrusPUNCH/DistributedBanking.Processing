using DistributedBanking.Processing.Extensions;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services
    .AddApi(configuration)
    .AddServices(configuration)
    .ConfigureOptions(configuration);

builder.Host.UseSerilogAppLogging();

var application = builder.Build();
application
    .UseAppSerilog()
    .UseMiddleware()
    .UseAutoWrapper()
    .UseAppCore();

await application.RunAsync();
