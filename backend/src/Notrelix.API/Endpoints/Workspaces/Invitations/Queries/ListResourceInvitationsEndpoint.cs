using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Invitations.Queries.ListResourceInvitations;

namespace Notrelix.API.Endpoints.Workspaces.Invitations.Queries;

public static class ListResourceInvitationsEndpoint
{
    public static IEndpointRouteBuilder MapListResourceInvitations(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", HandleAsync)
            .WithName("Workspaces.Invitations.ListResourceInvitations")
            .WithTags("Workspaces.Invitations")
            .WithSummary("List the pending guest invitations of one Board or Page (UC-WM-A20)")
            .Produces<List<ResourceInvitationDto>>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        string resourceType,
        Guid resourceId,
        ISender sender,
        CancellationToken cancellationToken)
        => (await sender.Send(new ListResourceInvitationsQuery(resourceType, resourceId), cancellationToken)).ToApiResult();
}
