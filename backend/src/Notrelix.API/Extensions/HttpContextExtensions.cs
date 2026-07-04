namespace Notrelix.API.Extensions;

public static class HttpContextExtensions
{
    public static Guid GetWorkspaceIdHint(this HttpContext context)
    {
        if (context.Request.RouteValues.TryGetValue("workspaceId", out var routeValue)
            && Guid.TryParse(routeValue?.ToString(), out var routeGuid))
        {
            return routeGuid;
        }

        if (context.Request.Headers.TryGetValue("X-Workspace-Id", out var headerValue)
            && Guid.TryParse(headerValue.FirstOrDefault(), out var headerGuid))
        {
            return headerGuid;
        }

        return Guid.Empty;
    }
}
