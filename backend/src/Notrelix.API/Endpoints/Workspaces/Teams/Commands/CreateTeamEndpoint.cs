using Notrelix.API.Contracts.Workspaces.Teams.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Teams.Commands.CreateTeam;

namespace Notrelix.API.Endpoints.Workspaces.Teams.Commands;

public static class CreateTeamEndpoint
{
    public static IEndpointRouteBuilder MapCreateTeam(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/", HandleAsync)
            .WithName("Workspaces.Teams.CreateTeam")
            .WithTags("Workspaces.Teams")
            .WithSummary("Create a new team in the workspace");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        CreateTeamRequest request,
        ISender sender)
    {
        var result = await sender.Send(new CreateTeamCommand(workspaceId, request.Name, request.Description));
        return result.ToCreatedResult();
    }
}
