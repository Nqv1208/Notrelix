using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.RevokeResourceInvitation;

namespace Notrelix.API.Endpoints.Workspaces.Invitations.Commands;

public static class RevokeResourceInvitationEndpoint
{
    public static IEndpointRouteBuilder MapRevokeResourceInvitation(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/{invitationId:guid}", HandleAsync)
            .WithName("Workspaces.Invitations.RevokeResourceInvitation")
            .WithTags("Workspaces.Invitations")
            .WithSummary("Revoke a resource invitation the caller issued");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        string resourceType,
        Guid resourceId,
        Guid invitationId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RevokeResourceInvitationCommand(resourceType, resourceId, invitationId),
            cancellationToken);
        return result.ToNoContentResult();
    }
}
