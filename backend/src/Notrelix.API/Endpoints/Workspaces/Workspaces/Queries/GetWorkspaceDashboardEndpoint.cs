using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.WorkspaceHome.Queries.GetWorkspaceDashboard;

namespace Notrelix.API.Endpoints.Workspaces.Workspaces.Queries;

public static class GetWorkspaceDashboardEndpoint
{
    public static IEndpointRouteBuilder MapGetWorkspaceDashboard(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/dashboard", HandleAsync)
            .WithName("Workspaces.Workspaces.GetWorkspaceDashboard")
            .WithTags("Workspaces.Workspaces")
            .WithSummary("Get the workspace figures")
            .WithDescription(
                "Owner or Admin. Active members only (suspended and removed people are not counted), the Spaces the caller can see, Teams and pending invitations, and whether the workspace is archived. " +
                "A Member or Guest is forbidden; a person outside the workspace is told it does not exist. Read-only, so an archived workspace still answers.")
            .Produces<WorkspaceDashboardDto>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceDashboardQuery(workspaceId));
        return result.ToApiResult();
    }
}
