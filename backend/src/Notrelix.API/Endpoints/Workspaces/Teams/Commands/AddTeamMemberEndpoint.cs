using Notrelix.API.Contracts.Workspaces.Teams.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Teams.Commands.AddTeamMember;

namespace Notrelix.API.Endpoints.Workspaces.Teams.Commands;

public static class AddTeamMemberEndpoint
{
    public static IEndpointRouteBuilder MapAddTeamMember(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/", HandleAsync)
            .WithName("Workspaces.Teams.AddTeamMember")
            .WithTags("Workspaces.Teams")
            .WithSummary("Add a member to a team");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid teamId,
        AddTeamMemberRequest request,
        ISender sender)
    {
        var result = await sender.Send(new AddTeamMemberCommand(workspaceId, teamId, request.UserId, request.Role));
        return result.ToNoContentResult();
    }
}
