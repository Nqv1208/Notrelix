using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Teams.Commands.DeleteTeam;

namespace Notrelix.API.Endpoints.Workspaces.Teams.Commands;

public static class DeleteTeamEndpoint
{
    public static IEndpointRouteBuilder MapDeleteTeam(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceDelete("/", HandleAsync)
            .WithName("Workspaces.Teams.DeleteTeam")
            .WithTags("Workspaces.Teams")
            .WithSummary("Delete a team (restorable)")
            .WithDescription("A Team that Spaces still list is refused with 409 and the stable code workspaces.team.still-referenced; the problem carries references (only the Spaces the caller can see) and moreReferencesHidden (true when some referencing Space is hidden from the caller, no count). Remove the entries first, then delete. A Team with no references is deleted and can be restored.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblems(StatusCodes.Status409Conflict);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid teamId,
        ISender sender,
        [FromQuery] long expectedVersion)
    {
        var result = await sender.Send(new DeleteTeamCommand(workspaceId, teamId, expectedVersion));
        return result.ToNoContentResult();
    }
}
