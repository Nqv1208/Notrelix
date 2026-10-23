using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.ResendResourceInvitation;

namespace Notrelix.API.Endpoints.Workspaces.Invitations.Commands;

public static class ResendResourceInvitationEndpoint
{
    public static IEndpointRouteBuilder MapResendResourceInvitation(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/{invitationId:guid}/resend", HandleAsync)
            .WithName("Workspaces.Invitations.ResendResourceInvitation")
            .WithTags("Workspaces.Invitations")
            .WithSummary("Resend a resource invitation the caller issued")
            .Produces<Guid>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        string resourceType,
        Guid resourceId,
        Guid invitationId,
        ISender sender,
        CancellationToken cancellationToken)
        => (await sender.Send(
            new ResendResourceInvitationCommand(resourceType, resourceId, invitationId),
            cancellationToken)).ToApiResult();
}
