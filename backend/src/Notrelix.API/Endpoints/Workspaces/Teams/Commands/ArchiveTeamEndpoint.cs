using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Teams.Commands.ArchiveTeam;

namespace Notrelix.API.Endpoints.Workspaces.Teams.Commands;

public static class ArchiveTeamEndpoint
{
    public static IEndpointRouteBuilder MapArchiveTeam(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/archive", HandleAsync)
            .WithName("Workspaces.Teams.ArchiveTeam")
            .WithTags("Workspaces.Teams")
            .WithSummary("Archive a team");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid teamId,
        ISender sender)
    {
        var result = await sender.Send(new ArchiveTeamCommand(workspaceId, teamId));
        return result.ToNoContentResult();
    }
}
