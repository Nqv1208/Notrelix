using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitationById;

namespace Notrelix.API.Endpoints.Workspaces.Invitations.Commands;

public static class AcceptInvitationByIdEndpoint
{
    public static IEndpointRouteBuilder MapAcceptInvitationById(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedPost("/{invitationId:guid}/accept", HandleAsync)
            .WithName("Workspaces.Invitations.AcceptInvitationById")
            .WithTags("Workspaces.Invitations")
            .WithSummary("Accept a workspace invitation by its invitation id");
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