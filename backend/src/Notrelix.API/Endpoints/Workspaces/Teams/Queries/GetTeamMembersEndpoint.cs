using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Teams.Queries.GetTeamMembers;

namespace Notrelix.API.Endpoints.Workspaces.Teams.Queries;

public static class GetTeamMembersEndpoint
{
    public static IEndpointRouteBuilder MapGetTeamMembers(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("Workspaces.Teams.GetTeamMembers")
            .WithTags("Workspaces.Teams.Members")
            .WithSummary("List the active members of a team")
            .Produces<IReadOnlyList<TeamMemberDto>>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid teamId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var members = await sender.Send(new GetTeamMembersQuery(workspaceId, teamId), cancellationToken);
        return Results.Ok(members);
    }
}
