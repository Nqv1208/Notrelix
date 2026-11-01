using Notrelix.Application.Features.Workspaces.Teams.Queries.GetTeam;
using Notrelix.API.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Teams.Queries.GetWorkspaceTeams;

namespace Notrelix.API.Endpoints.Workspaces.Teams.Queries;

public static class GetWorkspaceTeamsEndpoint
{
    public static IEndpointRouteBuilder MapGetWorkspaceTeams(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("Workspaces.Teams.GetWorkspaceTeams")
            .WithTags("Workspaces.Teams")
            .WithSummary("Get the teams of a workspace")
            .WithDescription(
                "Cursor-paginated (default 50, max 200), ordered by name; archived teams are included. " +
                "Pass nextCursor back as cursor for the next page.")
            .Produces<CursorPage<TeamDto>>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        string? cursor,
        int? limit,
        ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceTeamsQuery(workspaceId, cursor, limit));
        return result.ToApiResult();
    }
}
