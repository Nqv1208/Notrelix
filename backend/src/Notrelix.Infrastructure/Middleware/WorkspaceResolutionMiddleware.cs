using System.Security.Claims;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Security;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Infrastructure.Middleware;

public sealed class WorkspaceResolutionMiddleware
{
    private const string ItemsKey = "CurrentWorkspaceId";
    private readonly RequestDelegate _next;

    public WorkspaceResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentWorkspace currentWorkspace, IPermissionEvaluator permissionEvaluator)
    {
        var workspaceId = ResolveWorkspaceId(context);

        if (workspaceId is null)
        {
            await _next(context);
            return;
        }

        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var decision = await permissionEvaluator.EvaluateAsync(
            new PermissionContext(
                userId,
                workspaceId.Value,
                ResourceType.Workspace,
                null,
                PermissionAction.ViewWorkspace),
            context.RequestAborted);

        if (!decision.IsAllowed)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "forbidden",
                message = "You are not a member of this workspace."
            }, context.RequestAborted);
            return;
        }

        context.Items[ItemsKey] = workspaceId.Value;
        currentWorkspace.SetWorkspace(workspaceId.Value);

        await _next(context);
    }

    private static Guid? ResolveWorkspaceId(HttpContext context)
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

        return null;
    }
}

public static class WorkspaceResolutionMiddlewareExtensions
{
    public static IApplicationBuilder UseWorkspaceResolution(this IApplicationBuilder app)
    {
        return app.UseMiddleware<WorkspaceResolutionMiddleware>();
    }
}
