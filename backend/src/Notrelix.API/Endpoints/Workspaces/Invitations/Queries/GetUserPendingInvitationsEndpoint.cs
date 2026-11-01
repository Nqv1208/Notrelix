using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Invitations.Queries.GetUserPendingInvitations;

namespace Notrelix.API.Endpoints.Workspaces.Invitations.Queries;

public static class GetUserPendingInvitationsEndpoint
{
    public static IEndpointRouteBuilder MapGetUserPendingInvitations(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedGet("/pending", HandleAsync)
            .WithName("Workspaces.Invitations.GetUserPendingInvitations")
            .WithTags("Workspaces.Invitations")
            .WithSummary("Get pending invitations for the current logged-in user")
            .WithDescription("The pending, unexpired invitations addressed to the person's own confirmed email, newest first. At most 50 are returned (the newest). A person whose email is not confirmed gets an empty list, not an error.")
            .Produces<List<UserPendingInvitationDto>>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        ISender sender)
    {
        var result = await sender.Send(new GetUserPendingInvitationsQuery());
        return result.ToApiResult();
    }
}
