using System.Net;
using System.Text.Json;

namespace Helpdesk.Api.Middleware;

public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            var body = JsonSerializer.Serialize(new { error = "Внутренняя ошибка сервера" });
            await context.Response.WriteAsync(body);
        }
    }
}
