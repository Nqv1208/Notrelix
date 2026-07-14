using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Teams.Commands.UnarchiveTeam;

namespace Notrelix.API.Endpoints.Workspaces.Teams.Commands;

public static class UnarchiveTeamEndpoint
{
    public static IEndpointRouteBuilder MapUnarchiveTeam(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/unarchive", HandleAsync)
            .WithName("Workspaces.Teams.UnarchiveTeam")
            .WithTags("Workspaces.Teams")
            .WithSummary("Unarchive a previously archived team");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid teamId,
        ISender sender)
    {
        var result = await sender.Send(new UnarchiveTeamCommand(workspaceId, teamId));
        return result.ToNoContentResult();
    }
}
