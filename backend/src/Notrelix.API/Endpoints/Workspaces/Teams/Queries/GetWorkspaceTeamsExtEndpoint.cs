using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Teams.Queries.GetWorkspaceTeamsExt;

namespace Notrelix.API.Endpoints.Workspaces.Teams.Queries;

public static class GetWorkspaceTeamsExtEndpoint
{
    public static IEndpointRouteBuilder MapGetWorkspaceTeamsExt(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("Workspaces.Teams.GetWorkspaceTeamsExt")
            .WithTags("Workspaces.Teams")
            .WithSummary("Get what the Teams pages add to the team list")
            .WithDescription("Per Team: people, who created and archived it and the Spaces where it holds a role; plus the workspace admins and the people a Team may take. Needs the right to see the member list, so a Guest is refused. Email is shown only to a workspace Owner or Admin and only for non-Guest people.")
            .Produces<WorkspaceTeamsExtDto>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceTeamsExtQuery(workspaceId));
        return result.ToApiResult();
    }
}
