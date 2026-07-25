using Notrelix.API.Contracts.Workspaces.Invitations.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.ChangeInvitationRole;

namespace Notrelix.API.Endpoints.Workspaces.Invitations.Commands;

public static class ChangeInvitationRoleEndpoint
{
    public static IEndpointRouteBuilder MapChangeInvitationRole(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePatch("/{invitationId:guid}/role", HandleAsync)
            .WithName("Workspaces.Invitations.ChangeInvitationRole")
            .WithTags("Workspaces.Invitations")
            .WithSummary("Change the role of a pending invitation");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid invitationId,
        ChangeInvitationRoleRequest request,
        ISender sender)
    {
        var result = await sender.Send(new ChangeInvitationRoleCommand(workspaceId, invitationId, request.NewRole));
        return result.ToNoContentResult();
    }
}
