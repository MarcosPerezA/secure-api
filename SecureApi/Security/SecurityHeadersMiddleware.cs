using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SecureApi.Security;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx)
    {
        var h = ctx.Response.Headers;

        // Evita sniffing de tipos
        h["X-Content-Type-Options"] = "nosniff";

        // Evita clickjacking
        h["X-Frame-Options"] = "DENY";

        // Política de referer más privada
        h["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Restringe permisos de APIs del navegador
        h["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

        // CSP para API rompería Swagger en dev; si quieres endurecer prod sin Swagger:
        // h["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none';";

        await _next(ctx);
    }
}

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}
