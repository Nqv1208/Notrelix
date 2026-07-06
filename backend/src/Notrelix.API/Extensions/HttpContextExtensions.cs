namespace Notrelix.API.Extensions;

public static class HttpContextExtensions
{
    public static bool TryGetWorkspaceIdHint(this HttpContext context, out Guid workspaceId)
    {
        if (context.Request.RouteValues.TryGetValue("workspaceId", out var routeValue)
            && Guid.TryParse(routeValue?.ToString(), out var routeGuid)
            && routeGuid != Guid.Empty)
        {
            workspaceId = routeGuid;
            return true;
        }

        if (context.Request.Headers.TryGetValue("X-Workspace-Id", out var headerValue)
            && Guid.TryParse(headerValue.FirstOrDefault(), out var headerGuid)
            && headerGuid != Guid.Empty)
        {
            workspaceId = headerGuid;
            return true;
        }

        workspaceId = Guid.Empty;
        return false;
    }
}
