using Microsoft.Extensions.Options;
using Notrelix.API.Options;

namespace Notrelix.API.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeaderOptions _options;

    public SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecurityHeaderOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["Permissions-Policy"] =
            "camera=(), microphone=(), geolocation=()";

        if (_options.EnableCsp)
        {
            context.Response.Headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;
        }

        await _next(context);
    }
}
