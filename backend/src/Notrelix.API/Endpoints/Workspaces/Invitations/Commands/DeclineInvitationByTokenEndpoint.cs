using Notrelix.API.Contracts.Identity;
using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.DeclineInvitationByToken;

namespace Notrelix.API.Endpoints.Workspaces.Invitations.Commands;

public static class DeclineInvitationByTokenEndpoint
{
    public static IEndpointRouteBuilder MapDeclineInvitationByToken(this IEndpointRouteBuilder group)
    {
        group.MapPublicPost("/decline", HandleAsync)
            .WithName("Workspaces.Invitations.DeclineInvitationByToken")
            .WithTags("Workspaces.Invitations")
            .WithSummary("Decline a workspace invitation with its token, without signing in")
            .WithDescription("Declining is permanent. A repeat press on an already declined invitation answers the same 204.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status429TooManyRequests)
            .WithMetadata(new RateLimitPolicyAttribute("AuthCredentialByIp"));
        return group;
    }

    private static async Task<IResult> HandleAsync(
        OneTimeTokenRequest request,
        ISender sender)
    {
        var result = await sender.Send(new DeclineInvitationByTokenCommand(request.Token));
        return result.ToNoContentResult();
    }
}
