namespace Notrelix.API.Extensions;

public static class HttpContextExtensions
{
    private const string AccountHeaderName = "X-Account-Id";

    public static bool TryGetAccountIdHint(this HttpContext context, out Guid accountId)
    {
        if (context.Request.RouteValues.TryGetValue("accountId", out var routeValue)
            && Guid.TryParse(routeValue?.ToString(), out var routeGuid)
            && routeGuid != Guid.Empty)
        {
            accountId = routeGuid;
            return true;
        }

        if (context.Request.Headers.TryGetValue(AccountHeaderName, out var headerValue)
            && Guid.TryParse(headerValue.FirstOrDefault(), out var headerGuid)
            && headerGuid != Guid.Empty)
        {
            accountId = headerGuid;
            return true;
        }

        accountId = Guid.Empty;
        return false;
    }

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
