using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitationById;

namespace Notrelix.API.Endpoints.Workspaces.Invitations.Commands;

public static class AcceptInvitationByIdEndpoint
{
    public static IEndpointRouteBuilder MapAcceptInvitationById(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedPost("/{invitationId:guid}/accept", HandleAsync)
            .WithName("Workspaces.Invitations.AcceptInvitationById")
            .WithTags("Workspaces.Invitations")
            .WithSummary("Accept a pending workspace invitation from the signed-in person's own list")
            .WithDescription("Same rules as accepting by link: the invitation must be pending and addressed to the person's confirmed email, a plan seat must be free for a new membership, a suspended member stays suspended. An invitation that is unknown, not pending or addressed to another email answers NotFound.")
            .Produces<AcceptInvitationResultDto>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid invitationId,
        ISender sender)
    {
        var result = await sender.Send(new AcceptInvitationByIdCommand(invitationId));
        return result.ToApiResult();
    }
}
