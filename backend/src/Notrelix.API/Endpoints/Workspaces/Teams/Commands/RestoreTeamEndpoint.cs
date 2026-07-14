using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Teams.Commands.RestoreTeam;

namespace Notrelix.API.Endpoints.Workspaces.Teams.Commands;

public static class RestoreTeamEndpoint
{
    public static IEndpointRouteBuilder MapRestoreTeam(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/restore", HandleAsync)
            .WithName("Workspaces.Teams.RestoreTeam")
            .WithTags("Workspaces.Teams")
            .WithSummary("Restore a soft-deleted team");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid teamId,
        ISender sender)
    {
        var result = await sender.Send(new RestoreTeamCommand(workspaceId, teamId));
        return result.ToNoContentResult();
    }
}
