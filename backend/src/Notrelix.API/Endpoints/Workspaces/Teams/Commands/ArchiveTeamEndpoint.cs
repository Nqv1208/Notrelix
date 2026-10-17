using Notrelix.API.Contracts.Workspaces.Teams.Requests;
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
            .WithSummary("Archive a team")
            .Produces(StatusCodes.Status204NoContent);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid teamId,
        ArchiveTeamRequest request,
        ISender sender)
    {
        var result = await sender.Send(new ArchiveTeamCommand(workspaceId, teamId, request.ExpectedVersion));
        return result.ToNoContentResult();
    }
}
