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
            .WithSummary("Permanently delete a team")
            .Produces(StatusCodes.Status204NoContent);
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
