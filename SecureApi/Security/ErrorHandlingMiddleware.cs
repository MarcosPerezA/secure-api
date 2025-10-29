using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SecureApi.Security;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private static readonly JsonSerializerOptions jsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next; _logger = logger;
    }

    public async Task Invoke(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error");
            ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            ctx.Response.ContentType = "application/json";

            // Mensaje neutro al cliente
            var payload = new { error = "Se produjo un error inesperado." };
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(payload, jsonOpts));
        }
    }
}

public static class ErrorHandlingExtensions
{
    public static IApplicationBuilder UseGlobalErrorHandler(this IApplicationBuilder app)
        => app.UseMiddleware<ErrorHandlingMiddleware>();
}
