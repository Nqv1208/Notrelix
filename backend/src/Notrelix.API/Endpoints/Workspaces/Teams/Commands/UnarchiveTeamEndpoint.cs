using Notrelix.API.Contracts.Workspaces.Teams.Requests;
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
            .WithSummary("Unarchive a previously archived team")
            .Produces(StatusCodes.Status204NoContent);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid teamId,
        ArchiveTeamRequest request,
        ISender sender)
    {
        var result = await sender.Send(new UnarchiveTeamCommand(workspaceId, teamId, request.ExpectedVersion));
        return result.ToNoContentResult();
    }
}
