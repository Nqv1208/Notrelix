using Microsoft.Extensions.Options;
using Notrelix.API.Options;

namespace Notrelix.API.Middleware;

/// <summary>
/// Adds security headers to all HTTP responses.
/// Transport-level security only — no business authorization.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IOptions<SecurityHeaderOptions> options)
    {
        var securityHeaders = options.Value;

        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // Prevent MIME type sniffing
            headers["X-Content-Type-Options"] = "nosniff";

            // Prevent clickjacking
            headers["X-Frame-Options"] = "DENY";

            // Disable XSS filter (modern browsers use CSP instead)
            headers["X-XSS-Protection"] = "0";

            // Control referrer information
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Prevent DNS prefetching
            headers["X-DNS-Prefetch-Control"] = "off";

            // Remove server header
            headers["X-Powered-By"] = "";

            // Permissions Policy — restrict browser features
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=(), magnetometer=(), gyroscope=(), accelerometer=()";

            // Cross-Origin policies — isolate browsing context
            headers["Cross-Origin-Embedder-Policy"] = "require-corp";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";

            // Content Security Policy (if enabled)
            if (securityHeaders.EnableCsp && !string.IsNullOrWhiteSpace(securityHeaders.ContentSecurityPolicy))
            {
                headers["Content-Security-Policy"] = securityHeaders.ContentSecurityPolicy;
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
