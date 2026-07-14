using Notrelix.API.Contracts.Workspaces.Teams.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Teams.Commands.UpdateTeamDescription;

namespace Notrelix.API.Endpoints.Workspaces.Teams.Commands;

public static class UpdateTeamDescriptionEndpoint
{
    public static IEndpointRouteBuilder MapUpdateTeamDescription(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePut("/description", HandleAsync)
            .WithName("Workspaces.Teams.UpdateTeamDescription")
            .WithTags("Workspaces.Teams")
            .WithSummary("Update a team's description");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid teamId,
        UpdateTeamDescriptionRequest request,
        ISender sender)
    {
        var result = await sender.Send(new UpdateTeamDescriptionCommand(workspaceId, teamId, request.Description));
        return result.ToNoContentResult();
    }
}
