using Notrelix.API.Contracts.Workspaces.Teams.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Teams.Commands.RenameTeam;

namespace Notrelix.API.Endpoints.Workspaces.Teams.Commands;

public static class RenameTeamEndpoint
{
    public static IEndpointRouteBuilder MapRenameTeam(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePut("/", HandleAsync)
            .WithName("Workspaces.Teams.RenameTeam")
            .WithTags("Workspaces.Teams")
            .WithSummary("Rename a team");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid teamId,
        RenameTeamRequest request,
        ISender sender)
    {
        var result = await sender.Send(new RenameTeamCommand(workspaceId, teamId, request.Name));
        return result.ToNoContentResult();
    }
}
