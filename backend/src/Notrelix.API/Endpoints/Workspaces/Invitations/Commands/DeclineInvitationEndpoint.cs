using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.DeclineInvitation;

namespace Notrelix.API.Endpoints.Workspaces.Invitations.Commands;

public static class DeclineInvitationEndpoint
{
    public static IEndpointRouteBuilder MapDeclineInvitation(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedPost("/{invitationId:guid}/decline", HandleAsync)
            .WithName("Workspaces.Invitations.DeclineInvitation")
            .WithTags("Workspaces.Invitations")
            .WithSummary("Decline a workspace invitation");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid invitationId,
        ISender sender)
    {
        var result = await sender.Send(new DeclineInvitationCommand(invitationId));
        return result.ToNoContentResult();
    }
}
