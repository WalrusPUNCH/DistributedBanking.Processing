using System.Net;
using System.Text;

namespace DistributedBanking.Processing.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next, 
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError;

        if (exception is ArgumentException)
        {
            code = HttpStatusCode.UnprocessableEntity;
        }
        
        var fullErrorMessage = GetErrorMessage(exception);
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        _logger.LogError(exception, fullErrorMessage);

        return context.Response.WriteAsync(fullErrorMessage);
    }

    private static string GetErrorMessage(Exception ex)
    {
        var messageBuilder = new StringBuilder(ex.Message);

        if (ex.InnerException != null)
        {
            messageBuilder.AppendLine(GetErrorMessage(ex.InnerException));
        }

        return messageBuilder.ToString();
    }
}