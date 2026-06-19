using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Invitations.Queries.GetUserPendingInvitations;

namespace Notrelix.API.Endpoints.Workspaces.Invitations.Queries;

public static class GetUserPendingInvitationsEndpoint
{
    public static IEndpointRouteBuilder MapGetUserPendingInvitations(this IEndpointRouteBuilder group)
    {
        group.MapGet("/pending", HandleAsync)
            .WithName("Workspaces.Invitations.GetUserPendingInvitations")
            .WithTags("Workspaces.Invitations")
            .WithSummary("Get pending invitations for the current logged-in user");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        ISender sender)
    {
        var result = await sender.Send(new GetUserPendingInvitationsQuery());
        return result.ToApiResult();
    }
}
