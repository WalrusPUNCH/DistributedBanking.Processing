using Serilog;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;

namespace DistributedBanking.Processing.Extensions;

public static class HostBuilderExtensions
{
    public static IHostBuilder UseSerilogAppLogging(this IHostBuilder builder)
    {
        builder.UseSerilog((context, configuration) => configuration.ConfigureSerilog(context.Configuration));

        return builder;
    }
    
    private static void ConfigureSerilog(this LoggerConfiguration loggerConfig, IConfiguration config)
    {
        loggerConfig
            .ReadFrom.Configuration(config)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .Enrich.WithExceptionDetails(new DestructuringOptionsBuilder()
                .WithDefaultDestructurers())
            .Destructure.ToMaximumDepth(6)
            .Destructure.ToMaximumStringLength(100)
            .Destructure.ToMaximumCollectionCount(10)
            .WriteTo.Console();
    }
}