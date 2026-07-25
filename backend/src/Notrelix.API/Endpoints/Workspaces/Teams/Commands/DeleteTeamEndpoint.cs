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
            .WithSummary("Permanently delete a team");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid teamId,
        ISender sender)
    {
        var result = await sender.Send(new DeleteTeamCommand(workspaceId, teamId));
        return result.ToNoContentResult();
    }
}
