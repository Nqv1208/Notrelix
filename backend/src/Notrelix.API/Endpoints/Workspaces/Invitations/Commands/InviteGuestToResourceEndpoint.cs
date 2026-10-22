using Notrelix.API.Contracts.Workspaces.Invitations.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteGuestToResource;

namespace Notrelix.API.Endpoints.Workspaces.Invitations.Commands;

public static class InviteGuestToResourceEndpoint
{
    public static IEndpointRouteBuilder MapInviteGuestToResource(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/", HandleAsync)
            .WithName("Workspaces.Invitations.InviteGuestToResource")
            .WithTags("Workspaces.Invitations")
            .WithSummary("Invite an outsider by email to one Board or Page as a Guest")
            .Produces<Guid>(StatusCodes.Status201Created, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        string resourceType,
        Guid resourceId,
        InviteGuestToResourceRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new InviteGuestToResourceCommand(resourceType, resourceId, body.Email, body.Level),
            cancellationToken);
        return result.ToCreatedResult();
    }
}
