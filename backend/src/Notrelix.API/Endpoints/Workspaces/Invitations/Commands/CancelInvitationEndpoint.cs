using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.CancelInvitation;

namespace Notrelix.API.Endpoints.Workspaces.Invitations.Commands;

public static class CancelInvitationEndpoint
{
    public static IEndpointRouteBuilder MapCancelInvitation(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceDelete("/{invitationId:guid}", HandleAsync)
            .WithName("Workspaces.Invitations.CancelInvitation")
            .WithTags("Workspaces.Invitations")
            .WithSummary("Cancel a workspace invitation");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid invitationId,
        ISender sender)
    {
        var result = await sender.Send(new CancelInvitationCommand(workspaceId, invitationId));
        return result.ToNoContentResult();
    }
}
