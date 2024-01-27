using AutoWrapper;
using DistributedBanking.Processing.Middleware;
using Serilog;

namespace DistributedBanking.Processing.Extensions;

public static class ApplicationBuilderExtensions
{
    internal static IApplicationBuilder UseAppCore(this IApplicationBuilder applicationBuilder)
    {
        applicationBuilder
            .UseAuthentication()
            .UseAuthorization();

        return applicationBuilder;
    }
    
    internal static IApplicationBuilder UseAppSerilog(this IApplicationBuilder applicationBuilder)
    {
        applicationBuilder.UseSerilogRequestLogging();

        return applicationBuilder;
    }
    
    internal static IApplicationBuilder UseMiddleware(this IApplicationBuilder applicationBuilder)
    {
        applicationBuilder.UseMiddleware<ExceptionHandlingMiddleware>();
        
        return applicationBuilder;
    }
    
    internal static IApplicationBuilder UseAutoWrapper(this IApplicationBuilder applicationBuilder)
    {
        applicationBuilder.UseApiResponseAndExceptionWrapper(
            new AutoWrapperOptions 
            { 
                IgnoreWrapForOkRequests = true
            });
        
        return applicationBuilder;
    }
}