using Microsoft.Extensions.Options;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Operators;

namespace Notrelix.API.Middleware;

/// <summary>
/// Puts every request under <c>/operator/</c> on the operator database login before anything resolves the
/// database, and answers 404 when the operator console is not configured (no operator login or no signing key).
/// A tenant request never runs on the operator login and an operator request never runs on a tenant login: the
/// principal of a scope cannot change once its connection is chosen.
/// </summary>
public sealed class OperatorPrincipalMiddleware
{
    private const string Prefix = "/operator/";
    private readonly RequestDelegate _next;

    public OperatorPrincipalMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/operator", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var principal = context.RequestServices.GetRequiredService<DatabasePrincipal>();
        var options = context.RequestServices.GetRequiredService<IOptions<OperatorOptions>>().Value;
        if (!options.IsConfigured || !principal.OperatorConfigured)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        principal.UseOperator();
        await _next(context);
    }
}
