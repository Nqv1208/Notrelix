using Microsoft.Extensions.Options;
using Notrelix.Infrastructure.Auth.Csrf;

namespace Notrelix.API.Middleware;

/// <summary>
/// Double Submit Cookie CSRF protection.
/// Sets CSRF cookie on GET requests, validates token on state-changing requests.
/// Feature-flagged via Security:Csrf:Enabled.
/// </summary>
public sealed class CsrfValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly CsrfProtector _protector;
    private readonly CsrfOptions _options;

    public CsrfValidationMiddleware(
        RequestDelegate next,
        CsrfProtector protector,
        IOptions<CsrfOptions> options)
    {
        _next = next;
        _protector = protector;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        // Set CSRF cookie on GET requests
        if (HttpMethods.IsGet(context.Request.Method))
        {
            var existingToken = context.Request.Cookies["csrf_token"];
            if (string.IsNullOrEmpty(existingToken))
            {
                var token = _protector.GenerateToken();
                _protector.SetCookie(context, token);
            }
        }

        // Validate on state-changing requests
        if (_protector.IsStateChangingMethod(context.Request.Method))
        {
            if (!_protector.Validate(context))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    type = "https://docs.notrelix.com/problems/csrf-validation-failed",
                    title = "CSRF validation failed",
                    status = 403,
                    detail = "Missing or invalid CSRF token. Include X-CSRF-Token header matching the csrf_token cookie.",
                });
                return;
            }
        }

        await _next(context);
    }
}

public sealed class CsrfOptions
{
    public bool Enabled { get; init; }
}
