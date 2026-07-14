using Notrelix.API.Contracts.Workspaces.Teams.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Teams.Commands.ChangeTeamMemberRole;

namespace Notrelix.API.Endpoints.Workspaces.Teams.Commands;

public static class ChangeTeamMemberRoleEndpoint
{
    public static IEndpointRouteBuilder MapChangeTeamMemberRole(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePut("/{userId:guid}/role", HandleAsync)
            .WithName("Workspaces.Teams.ChangeTeamMemberRole")
            .WithTags("Workspaces.Teams")
            .WithSummary("Change a team member's role");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid teamId,
        Guid userId,
        ChangeTeamMemberRoleRequest request,
        ISender sender)
    {
        var result = await sender.Send(new ChangeTeamMemberRoleCommand(workspaceId, teamId, userId, request.NewRole));
        return result.ToNoContentResult();
    }
}
