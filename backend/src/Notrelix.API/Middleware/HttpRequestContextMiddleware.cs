using System.Security.Claims;

namespace Notrelix.API.Middleware;

/// <summary>
/// Populates IExecutionContext from the HTTP request context.
/// Must run AFTER UseAuthentication() and BEFORE UseAuthorization().
/// Extracts user identity, workspace hints, and request metadata.
/// Does NOT perform business authorization — that is Application layer responsibility.
/// </summary>
public sealed class HttpRequestContextMiddleware
{
    private const string WorkspaceHeaderName = "X-Workspace-Id";
    private readonly RequestDelegate _next;
    private readonly ILogger<HttpRequestContextMiddleware> _logger;

    public HttpRequestContextMiddleware(RequestDelegate next, ILogger<HttpRequestContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var executionContext = context.RequestServices.GetRequiredService<IExecutionContext>();

        // Extract user identity from JWT claims (after UseAuthentication)
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = GetUserId(context.User);
            var email = GetEmail(context.User);
            var name = GetName(context.User);

            if (userId != Guid.Empty)
            {
                executionContext.SetUser(userId, email, name);
            }
        }

        // Extract workspace hint from header (just a hint, not validated here)
        if (context.Request.Headers.TryGetValue(WorkspaceHeaderName, out var workspaceHeaderValue)
            && Guid.TryParse(workspaceHeaderValue.ToString(), out var headerWorkspaceId)
            && headerWorkspaceId != Guid.Empty)
        {
            // Store workspace hint in HttpContext.Items for downstream use
            // TenantBootstrapBehavior will validate and set proper tenant context
            context.Items["WorkspaceHint"] = headerWorkspaceId;
        }

        // Extract workspace from route parameter if present
        if (context.Request.RouteValues.TryGetValue("workspaceId", out var routeValue)
            && routeValue is string routeValueStr
            && Guid.TryParse(routeValueStr, out var parsedWorkspaceId)
            && parsedWorkspaceId != Guid.Empty)
        {
            context.Items["WorkspaceHint"] = parsedWorkspaceId;
        }

        await _next(context);
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out var userId) ? userId : Guid.Empty;
    }

    private static string GetEmail(ClaimsPrincipal user)
    {
        return user.FindFirstValue("email") ?? user.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    }

    private static string GetName(ClaimsPrincipal user)
    {
        return user.FindFirstValue("name") ?? user.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
    }
}
