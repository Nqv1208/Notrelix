using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.ResendInvitation;

namespace Notrelix.API.Endpoints.Workspaces.Invitations.Commands;

public static class ResendInvitationEndpoint
{
    public static IEndpointRouteBuilder MapResendInvitation(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/{invitationId:guid}/resend", HandleAsync)
            .WithName("Workspaces.Invitations.ResendInvitation")
            .WithTags("Workspaces.Invitations")
            .WithSummary("Resend a workspace invitation");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid invitationId,
        ISender sender)
        => (await sender.Send(new ResendInvitationCommand(workspaceId, invitationId))).ToApiResult();
}
