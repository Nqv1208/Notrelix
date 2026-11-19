using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Teams.Queries.GetDeletedTeams;

namespace Notrelix.API.Endpoints.Workspaces.Teams.Queries;

public static class GetDeletedTeamsEndpoint
{
    public static IEndpointRouteBuilder MapGetDeletedTeams(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/deleted", HandleAsync)
            .WithName("Workspaces.Teams.GetDeletedTeams")
            .WithTags("Workspaces.Teams")
            .WithSummary("Get the deleted teams of a workspace")
            .WithDescription(
                "For Owners and Admins only (ManageTeams, the right to restore): a Member or Guest is refused with 403 and a person outside the workspace gets 404. " +
                "Newest deletion first, at most 200. Restore a Team with POST .../teams/{teamId}/restore; its members return with it.")
            .Produces<IReadOnlyList<DeletedTeamDto>>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid workspaceId, ISender sender)
    {
        var result = await sender.Send(new GetDeletedTeamsQuery(workspaceId));
        return result.ToApiResult();
    }
}
