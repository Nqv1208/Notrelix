using Notrelix.API.Extensions;
using Notrelix.API.Contracts.Identity;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation;

namespace Notrelix.API.Endpoints.Workspaces.Invitations.Commands;

public static class AcceptInvitationEndpoint
{
    public static IEndpointRouteBuilder MapAcceptInvitation(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedPost("/accept", HandleAsync)
            .WithName("Workspaces.Invitations.AcceptInvitation")
            .WithTags("Workspaces.Invitations")
            .WithSummary("Accept a workspace invitation by token");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        OneTimeTokenRequest request,
        ISender sender)
    {
        var result = await sender.Send(new AcceptInvitationCommand(request.Token));
        return result.ToApiResult();
    }
}
