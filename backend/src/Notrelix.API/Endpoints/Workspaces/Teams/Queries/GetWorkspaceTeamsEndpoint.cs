using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Teams.Queries.GetWorkspaceTeams;

namespace Notrelix.API.Endpoints.Workspaces.Teams.Queries;

public static class GetWorkspaceTeamsEndpoint
{
    public static IEndpointRouteBuilder MapGetWorkspaceTeams(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("Workspaces.Teams.GetWorkspaceTeams")
            .WithTags("Workspaces.Teams")
            .WithSummary("Get all teams in a workspace");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceTeamsQuery(workspaceId));
        return result.ToApiResult();
    }
}
