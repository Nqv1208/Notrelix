using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation;

namespace Notrelix.API.Endpoints.Workspaces.Invitations.Commands;

public static class AcceptInvitationEndpoint
{
    public static IEndpointRouteBuilder MapAcceptInvitation(this IEndpointRouteBuilder group)
    {
        group.MapPublicPost("/accept/{token}", HandleAsync)
            .WithName("Workspaces.Invitations.AcceptInvitation")
            .WithTags("Workspaces.Invitations")
            .WithSummary("Accept a workspace invitation by token");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        string token,
        ISender sender)
    {
        var result = await sender.Send(new AcceptInvitationCommand(token));
        return result.ToApiResult();
    }
}
