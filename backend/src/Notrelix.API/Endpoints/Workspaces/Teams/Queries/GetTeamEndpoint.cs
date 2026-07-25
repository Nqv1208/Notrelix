using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Teams.Queries.GetTeam;

namespace Notrelix.API.Endpoints.Workspaces.Teams.Queries;

public static class GetTeamEndpoint
{
    public static IEndpointRouteBuilder MapGetTeam(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("Workspaces.Teams.GetTeam")
            .WithTags("Workspaces.Teams")
            .WithSummary("Get a team by ID");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid teamId,
        ISender sender)
    {
        var result = await sender.Send(new GetTeamQuery(workspaceId, teamId));
        return result.ToApiResult();
    }
}
